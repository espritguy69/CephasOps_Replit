using CephasOps.Application.Companies.DTOs;
using CephasOps.Application.Companies.Services;
using CephasOps.Application.Departments.Services;
using CephasOps.Application.Buildings.Services;
using CephasOps.Application.ServiceInstallers.Services;
using CephasOps.Application.Inventory.Services;
using CephasOps.Application.Billing.Services;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Departments.Entities;
using CephasOps.Domain.Buildings.Entities;
using CephasOps.Domain.ServiceInstallers.Entities;
using CephasOps.Domain.Inventory.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Syncfusion.XlsIO;
using System.Text;

namespace CephasOps.Application.Companies.Services;

/// <summary>
/// Service for company deployment via Excel import/export
/// </summary>
public class CompanyDeploymentService : ICompanyDeploymentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CompanyDeploymentService> _logger;
    private readonly ICompanyService _companyService;
    private readonly IDepartmentService _departmentService;
    private readonly IPartnerService _partnerService;
    private readonly IPartnerGroupService _partnerGroupService;
    private readonly IBuildingService _buildingService;
    private readonly IServiceInstallerService _serviceInstallerService;
    private readonly IMaterialCategoryService _materialCategoryService;

    public CompanyDeploymentService(
        ApplicationDbContext context,
        ILogger<CompanyDeploymentService> logger,
        ICompanyService companyService,
        IDepartmentService departmentService,
        IPartnerService partnerService,
        IPartnerGroupService partnerGroupService,
        IBuildingService buildingService,
        IServiceInstallerService serviceInstallerService,
        IMaterialCategoryService materialCategoryService)
    {
        _context = context;
        _logger = logger;
        _companyService = companyService;
        _departmentService = departmentService;
        _partnerService = partnerService;
        _partnerGroupService = partnerGroupService;
        _buildingService = buildingService;
        _serviceInstallerService = serviceInstallerService;
        _materialCategoryService = materialCategoryService;
    }

    public async Task<byte[]> GenerateTemplateAsync(string format, CancellationToken cancellationToken = default)
    {
        using var excelEngine = new ExcelEngine();
        var application = excelEngine.Excel;
        application.DefaultVersion = ExcelVersion.Excel2016;

        IWorkbook workbook = application.Workbooks.Create(1);

        if (format == "single")
        {
            // Single file with multiple sheets
            CreateCompanyInfoSheet(workbook);
            CreateDepartmentsSheet(workbook);
            CreatePartnerGroupsSheet(workbook);
            CreatePartnersSheet(workbook);
            CreateMaterialsSheet(workbook);
            CreateBuildingsSheet(workbook);
            CreateSplittersSheet(workbook);
            CreateServiceInstallersSheet(workbook);
            CreateUsersSheet(workbook);
            CreateRateCardsSheet(workbook);
        }
        else
        {
            // Separate files - for now, create a master file with instructions
            var sheet = workbook.Worksheets[0];
            sheet.Name = "Instructions";
            sheet.Range["A1"].Text = "Company Deployment - Separate Files Format";
            sheet.Range["A3"].Text = "Upload separate Excel files with these names:";
            sheet.Range["A5"].Text = "01-company-info.xlsx";
            sheet.Range["A6"].Text = "02-departments.xlsx";
            sheet.Range["A7"].Text = "03-partner-groups.xlsx";
            sheet.Range["A8"].Text = "04-partners.xlsx";
            sheet.Range["A9"].Text = "05-materials.xlsx";
            sheet.Range["A10"].Text = "06-buildings.xlsx";
            sheet.Range["A11"].Text = "07-splitters.xlsx (GPON only)";
            sheet.Range["A12"].Text = "08-service-installers.xlsx";
            sheet.Range["A13"].Text = "09-users.xlsx";
            sheet.Range["A14"].Text = "10-rate-cards.xlsx";
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        workbook.Close();

        return stream.ToArray();
    }

    public async Task<DeploymentValidationResult> ValidateDeploymentAsync(
        IFormFileCollection files,
        CancellationToken cancellationToken = default)
    {
        var result = new DeploymentValidationResult();
        var format = DetectFormat(files);

        try
        {
            if (format == "single")
            {
                await ValidateSingleFileAsync(files[0], result, cancellationToken);
            }
            else
            {
                await ValidateSeparateFilesAsync(files, result, cancellationToken);
            }

            result.IsValid = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating deployment");
            result.Errors.Add(new DeploymentError
            {
                DataType = "System",
                RowNumber = 0,
                Message = $"Validation failed: {ex.Message}"
            });
            result.IsValid = false;
        }

        return result;
    }

    public async Task<DeploymentImportResult> ImportDeploymentAsync(
        IFormFileCollection files,
        DeploymentImportOptions options,
        CancellationToken cancellationToken = default)
    {
        var result = new DeploymentImportResult();
        var format = DetectFormat(files);

        try
        {
            // Validate first
            var validation = await ValidateDeploymentAsync(files, cancellationToken);
            if (!validation.IsValid && !options.CreateMissingDependencies)
            {
                result.Errors.AddRange(validation.Errors);
                result.Success = false;
                return result;
            }

            // Import in correct order
            if (format == "single")
            {
                await ImportSingleFileAsync(files[0], options, result, cancellationToken);
            }
            else
            {
                await ImportSeparateFilesAsync(files, options, result, cancellationToken);
            }

            result.Success = result.ErrorCount == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing deployment");
            result.Errors.Add(new DeploymentError
            {
                DataType = "System",
                RowNumber = 0,
                Message = $"Import failed: {ex.Message}"
            });
            result.Success = false;
        }

        return result;
    }

    public async Task<byte[]> ExportCompanyAsync(
        Guid? companyId,
        string format,
        CancellationToken cancellationToken = default)
    {
        using var excelEngine = new ExcelEngine();
        var application = excelEngine.Excel;
        application.DefaultVersion = ExcelVersion.Excel2016;

        IWorkbook workbook = application.Workbooks.Create(1);

        // Export company data to Excel
        // Implementation would populate sheets with actual data from database

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        workbook.Close();

        return stream.ToArray();
    }

    // Private helper methods

    private string DetectFormat(IFormFileCollection files)
    {
        if (files.Count == 1 && files[0].FileName.Contains("deployment", StringComparison.OrdinalIgnoreCase))
        {
            return "single";
        }
        return "separate";
    }

    private void CreateCompanyInfoSheet(IWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Create();
        sheet.Name = "Company Info";
        sheet.Range["A1"].Text = "LegalName";
        sheet.Range["B1"].Text = "ShortName";
        sheet.Range["C1"].Text = "RegistrationNo";
        sheet.Range["D1"].Text = "TaxId";
        sheet.Range["E1"].Text = "Vertical";
        sheet.Range["F1"].Text = "Address";
        sheet.Range["G1"].Text = "Phone";
        sheet.Range["H1"].Text = "Email";
        sheet.Range["I1"].Text = "IsActive";
        sheet.Range["J1"].Text = "DefaultTimezone";
        sheet.Range["K1"].Text = "DefaultCurrency";
        sheet.Range["L1"].Text = "DefaultLanguage";
        sheet.Range["M1"].Text = "InvoicePrefix";
        sheet.Range["N1"].Text = "WorkOrderPrefix";
        sheet.Range["O1"].Text = "BillingDayOfMonth";

        // Example row
        sheet.Range["A2"].Text = "Example Company Sdn Bhd";
        sheet.Range["B2"].Text = "Example";
        sheet.Range["E2"].Text = "ISP";
        sheet.Range["I2"].Text = "TRUE";
        sheet.Range["J2"].Text = "Asia/Kuala_Lumpur";
        sheet.Range["K2"].Text = "MYR";
    }

    private void CreateDepartmentsSheet(IWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Create();
        sheet.Name = "Departments";
        sheet.Range["A1"].Text = "Name";
        sheet.Range["B1"].Text = "Code";
        sheet.Range["C1"].Text = "Description";
        sheet.Range["D1"].Text = "CostCentreCode";
        sheet.Range["E1"].Text = "IsActive";

        sheet.Range["A2"].Text = "GPON";
        sheet.Range["B2"].Text = "GPON";
        sheet.Range["E2"].Text = "TRUE";
    }

    private void CreatePartnerGroupsSheet(IWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Create();
        sheet.Name = "Partner Groups";
        sheet.Range["A1"].Text = "Name";

        sheet.Range["A2"].Text = "TIME Group";
    }

    private void CreatePartnersSheet(IWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Create();
        sheet.Name = "Partners";
        sheet.Range["A1"].Text = "Name";
        sheet.Range["B1"].Text = "Code";
        sheet.Range["C1"].Text = "PartnerType";
        sheet.Range["D1"].Text = "PartnerGroupName";
        sheet.Range["E1"].Text = "DepartmentName";
        sheet.Range["F1"].Text = "BillingAddress";
        sheet.Range["G1"].Text = "ContactName";
        sheet.Range["H1"].Text = "ContactEmail";
        sheet.Range["I1"].Text = "ContactPhone";
        sheet.Range["J1"].Text = "IsActive";

        sheet.Range["A2"].Text = "TIME Main";
        sheet.Range["C2"].Text = "Telco";
        sheet.Range["D2"].Text = "TIME Group";
        sheet.Range["J2"].Text = "TRUE";
    }

    private void CreateMaterialsSheet(IWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Create();
        sheet.Name = "Materials";
        sheet.Range["A1"].Text = "Code";
        sheet.Range["B1"].Text = "Description";
        sheet.Range["C1"].Text = "CategoryName";
        sheet.Range["D1"].Text = "UnitOfMeasure";
        sheet.Range["E1"].Text = "UnitCost";
        sheet.Range["F1"].Text = "IsSerialised";
        sheet.Range["G1"].Text = "MinStockLevel";
        sheet.Range["H1"].Text = "ReorderPoint";
        sheet.Range["I1"].Text = "IsActive";
    }

    private void CreateBuildingsSheet(IWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Create();
        sheet.Name = "Buildings";
        sheet.Range["A1"].Text = "Name";
        sheet.Range["B1"].Text = "Code";
        sheet.Range["C1"].Text = "PropertyType";
        sheet.Range["D1"].Text = "InstallationMethodName";
        sheet.Range["E1"].Text = "DepartmentName";
        sheet.Range["F1"].Text = "AddressLine1";
        sheet.Range["G1"].Text = "AddressLine2";
        sheet.Range["H1"].Text = "City";
        sheet.Range["I1"].Text = "State";
        sheet.Range["J1"].Text = "Postcode";
        sheet.Range["K1"].Text = "Area";
        sheet.Range["L1"].Text = "Notes";
        sheet.Range["M1"].Text = "IsActive";
    }

    private void CreateSplittersSheet(IWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Create();
        sheet.Name = "Splitters";
        sheet.Range["A1"].Text = "BuildingName";
        sheet.Range["B1"].Text = "BuildingCode";
        sheet.Range["C1"].Text = "Name";
        sheet.Range["D1"].Text = "Code";
        sheet.Range["E1"].Text = "SplitterTypeName";
        sheet.Range["F1"].Text = "Location";
        sheet.Range["G1"].Text = "Block";
        sheet.Range["H1"].Text = "Floor";
        sheet.Range["I1"].Text = "DepartmentName";
        sheet.Range["J1"].Text = "IsActive";
    }

    private void CreateServiceInstallersSheet(IWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Create();
        sheet.Name = "Service Installers";
        sheet.Range["A1"].Text = "Name";
        sheet.Range["B1"].Text = "Code";
        sheet.Range["C1"].Text = "DepartmentName";
        sheet.Range["D1"].Text = "Phone";
        sheet.Range["E1"].Text = "Email";
        sheet.Range["F1"].Text = "Level";
        sheet.Range["G1"].Text = "IcNumber";
        sheet.Range["H1"].Text = "BankName";
        sheet.Range["I1"].Text = "BankAccountNumber";
        sheet.Range["J1"].Text = "Address";
        sheet.Range["K1"].Text = "EmergencyContact";
        sheet.Range["L1"].Text = "IsActive";
    }

    private void CreateUsersSheet(IWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Create();
        sheet.Name = "Users";
        sheet.Range["A1"].Text = "Name";
        sheet.Range["B1"].Text = "Email";
        sheet.Range["C1"].Text = "Phone";
        sheet.Range["D1"].Text = "Password";
        sheet.Range["E1"].Text = "RoleName";
        sheet.Range["F1"].Text = "DepartmentName";
        sheet.Range["G1"].Text = "IsActive";
    }

    private void CreateRateCardsSheet(IWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Create();
        sheet.Name = "Rate Cards";
        sheet.Range["A1"].Text = "PartnerName";
        sheet.Range["B1"].Text = "PartnerGroupName";
        sheet.Range["C1"].Text = "DepartmentName";
        sheet.Range["D1"].Text = "OrderTypeName";
        sheet.Range["E1"].Text = "ServiceCategory";
        sheet.Range["F1"].Text = "InstallationMethodName";
        sheet.Range["G1"].Text = "Description";
        sheet.Range["H1"].Text = "Amount";
        sheet.Range["I1"].Text = "TaxRate";
        sheet.Range["J1"].Text = "IsActive";
    }

    private async Task ValidateSingleFileAsync(IFormFile file, DeploymentValidationResult result, CancellationToken cancellationToken)
    {
        // Implementation for validating single file with multiple sheets
        // This would parse the Excel file and validate each sheet
        _logger.LogInformation("Validating single file deployment: {FileName}", file.FileName);
    }

    private async Task ValidateSeparateFilesAsync(IFormFileCollection files, DeploymentValidationResult result, CancellationToken cancellationToken)
    {
        // Implementation for validating separate files
        _logger.LogInformation("Validating separate files deployment: {FileCount} files", files.Count);
    }

    private async Task ImportSingleFileAsync(IFormFile file, DeploymentImportOptions options, DeploymentImportResult result, CancellationToken cancellationToken)
    {
        // Implementation for importing single file
        _logger.LogInformation("Importing single file deployment: {FileName}", file.FileName);
    }

    private async Task ImportSeparateFilesAsync(IFormFileCollection files, DeploymentImportOptions options, DeploymentImportResult result, CancellationToken cancellationToken)
    {
        // Implementation for importing separate files
        _logger.LogInformation("Importing separate files deployment: {FileCount} files", files.Count);
    }
}

