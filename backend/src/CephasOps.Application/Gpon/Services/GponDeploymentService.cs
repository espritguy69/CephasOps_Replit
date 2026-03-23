using CephasOps.Application.Gpon.DTOs;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Buildings.Services;
using CephasOps.Application.Inventory.Services;
using CephasOps.Application.Billing.Services;
using CephasOps.Application.Departments.Services;
using CephasOps.Application.Companies.Services;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Syncfusion.XlsIO;
using System.Text;

namespace CephasOps.Application.Gpon.Services;

/// <summary>
/// Service for GPON data deployment via Excel import/export
/// </summary>
public class GponDeploymentService : IGponDeploymentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GponDeploymentService> _logger;
    private readonly IOrderTypeService _orderTypeService;
    private readonly IInstallationMethodService _installationMethodService;
    private readonly IBuildingTypeService _buildingTypeService;
    private readonly ISplitterTypeService _splitterTypeService;
    private readonly IMaterialCategoryService _materialCategoryService;
    private readonly IBillingRatecardService _billingRatecardService;
    private readonly IDepartmentService _departmentService;
    private readonly IPartnerService _partnerService;
    private readonly IPartnerGroupService _partnerGroupService;

    public GponDeploymentService(
        ApplicationDbContext context,
        ILogger<GponDeploymentService> logger,
        IOrderTypeService orderTypeService,
        IInstallationMethodService installationMethodService,
        IBuildingTypeService buildingTypeService,
        ISplitterTypeService splitterTypeService,
        IMaterialCategoryService materialCategoryService,
        IBillingRatecardService billingRatecardService,
        IDepartmentService departmentService,
        IPartnerService partnerService,
        IPartnerGroupService partnerGroupService)
    {
        _context = context;
        _logger = logger;
        _orderTypeService = orderTypeService;
        _installationMethodService = installationMethodService;
        _buildingTypeService = buildingTypeService;
        _splitterTypeService = splitterTypeService;
        _materialCategoryService = materialCategoryService;
        _billingRatecardService = billingRatecardService;
        _departmentService = departmentService;
        _partnerService = partnerService;
        _partnerGroupService = partnerGroupService;
    }

    public async Task<byte[]> GenerateTemplateAsync(CancellationToken cancellationToken = default)
    {
        using var excelEngine = new ExcelEngine();
        var application = excelEngine.Excel;
        application.DefaultVersion = ExcelVersion.Excel2016;

        IWorkbook workbook = application.Workbooks.Create(1);

        // Create sheets for each GPON data type
        CreateOrderTypesSheet(workbook);
        CreateInstallationMethodsSheet(workbook);
        CreateBuildingTypesSheet(workbook);
        CreateSplitterTypesSheet(workbook);
        CreateMaterialCategoriesSheet(workbook);
        CreateRateCardsSheet(workbook);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        workbook.Close();

        return stream.ToArray();
    }

    public async Task<GponDeploymentValidationResult> ValidateDeploymentAsync(
        IFormFileCollection files,
        CancellationToken cancellationToken = default)
    {
        var result = new GponDeploymentValidationResult();

        try
        {
            if (files == null || files.Count == 0)
            {
                result.Errors.Add(new GponDeploymentError
                {
                    DataType = "System",
                    RowNumber = 0,
                    Message = "No files uploaded"
                });
                result.IsValid = false;
                return result;
            }

            // Parse and validate Excel file
            await ValidateExcelFileAsync(files[0], result, cancellationToken);

            result.IsValid = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating GPON deployment");
            result.Errors.Add(new GponDeploymentError
            {
                DataType = "System",
                RowNumber = 0,
                Message = $"Validation failed: {ex.Message}"
            });
            result.IsValid = false;
        }

        return result;
    }

    public async Task<GponDeploymentImportResult> ImportDeploymentAsync(
        IFormFileCollection files,
        GponDeploymentImportOptions options,
        CancellationToken cancellationToken = default)
    {
        var result = new GponDeploymentImportResult();

        try
        {
            if (files == null || files.Count == 0)
            {
                result.Errors.Add(new GponDeploymentError
                {
                    DataType = "System",
                    RowNumber = 0,
                    Message = "No files uploaded"
                });
                result.Success = false;
                return result;
            }

            // Validate first
            var validation = await ValidateDeploymentAsync(files, cancellationToken);
            if (!validation.IsValid && !options.CreateMissingDependencies)
            {
                result.Errors.AddRange(validation.Errors);
                result.Success = false;
                return result;
            }

            // Import in correct order
            await ImportExcelFileAsync(files[0], options, result, cancellationToken);

            result.Success = result.ErrorCount == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing GPON deployment");
            result.Errors.Add(new GponDeploymentError
            {
                DataType = "System",
                RowNumber = 0,
                Message = $"Import failed: {ex.Message}"
            });
            result.Success = false;
        }

        return result;
    }

    public async Task<byte[]> ExportGponDataAsync(
        Guid? departmentId,
        CancellationToken cancellationToken = default)
    {
        using var excelEngine = new ExcelEngine();
        var application = excelEngine.Excel;
        application.DefaultVersion = ExcelVersion.Excel2016;

        IWorkbook workbook = application.Workbooks.Create(1);

        // Export GPON data to Excel
        // Implementation would populate sheets with actual data from database
        _logger.LogInformation("Exporting GPON data for department: {DepartmentId}", departmentId);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        workbook.Close();

        return stream.ToArray();
    }

    // Private helper methods

    private void CreateOrderTypesSheet(IWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Create();
        sheet.Name = "Order Types";
        sheet.Range["A1"].Text = "Name";
        sheet.Range["B1"].Text = "Code";
        sheet.Range["C1"].Text = "Description";
        sheet.Range["D1"].Text = "DepartmentName";
        sheet.Range["E1"].Text = "DisplayOrder";
        sheet.Range["F1"].Text = "IsActive";

        // Example row
        sheet.Range["A2"].Text = "Activation";
        sheet.Range["B2"].Text = "ACTIVATION";
        sheet.Range["E2"].Text = "1";
        sheet.Range["F2"].Text = "TRUE";
    }

    private void CreateInstallationMethodsSheet(IWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Create();
        sheet.Name = "Installation Methods";
        sheet.Range["A1"].Text = "Name";
        sheet.Range["B1"].Text = "Code";
        sheet.Range["C1"].Text = "Category";
        sheet.Range["D1"].Text = "Description";
        sheet.Range["E1"].Text = "DepartmentName";
        sheet.Range["F1"].Text = "DisplayOrder";
        sheet.Range["G1"].Text = "IsActive";

        sheet.Range["A2"].Text = "Prelaid";
        sheet.Range["B2"].Text = "PRELAID";
        sheet.Range["C2"].Text = "FTTH";
        sheet.Range["F2"].Text = "1";
        sheet.Range["G2"].Text = "TRUE";
    }

    private void CreateBuildingTypesSheet(IWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Create();
        sheet.Name = "Building Types";
        sheet.Range["A1"].Text = "Name";
        sheet.Range["B1"].Text = "Code";
        sheet.Range["C1"].Text = "Description";
        sheet.Range["D1"].Text = "DepartmentName";
        sheet.Range["E1"].Text = "DisplayOrder";
        sheet.Range["F1"].Text = "IsActive";

        sheet.Range["A2"].Text = "Prelaid";
        sheet.Range["B2"].Text = "PRELAID";
        sheet.Range["E2"].Text = "1";
        sheet.Range["F2"].Text = "TRUE";
    }

    private void CreateSplitterTypesSheet(IWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Create();
        sheet.Name = "Splitter Types";
        sheet.Range["A1"].Text = "Name";
        sheet.Range["B1"].Text = "Code";
        sheet.Range["C1"].Text = "TotalPorts";
        sheet.Range["D1"].Text = "StandbyPortNumber";
        sheet.Range["E1"].Text = "Description";
        sheet.Range["F1"].Text = "DepartmentName";
        sheet.Range["G1"].Text = "DisplayOrder";
        sheet.Range["H1"].Text = "IsActive";

        sheet.Range["A2"].Text = "1:32";
        sheet.Range["B2"].Text = "1_32";
        sheet.Range["C2"].Text = "32";
        sheet.Range["D2"].Text = "32";
        sheet.Range["G2"].Text = "1";
        sheet.Range["H2"].Text = "TRUE";
    }

    private void CreateMaterialCategoriesSheet(IWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Create();
        sheet.Name = "Material Categories";
        sheet.Range["A1"].Text = "Name";
        sheet.Range["B1"].Text = "Description";
        sheet.Range["C1"].Text = "DisplayOrder";
        sheet.Range["D1"].Text = "IsActive";

        sheet.Range["A2"].Text = "Cables";
        sheet.Range["C2"].Text = "1";
        sheet.Range["D2"].Text = "TRUE";
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
        sheet.Range["G1"].Text = "BuildingType";
        sheet.Range["H1"].Text = "Description";
        sheet.Range["I1"].Text = "Amount";
        sheet.Range["J1"].Text = "TaxRate";
        sheet.Range["K1"].Text = "EffectiveFrom";
        sheet.Range["L1"].Text = "EffectiveTo";
        sheet.Range["M1"].Text = "IsActive";
    }

    private async Task ValidateExcelFileAsync(
        IFormFile file,
        GponDeploymentValidationResult result,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating GPON deployment file: {FileName}", file.FileName);
        // Implementation for validating Excel file
        // This would parse the Excel file and validate each sheet
    }

    private async Task ImportExcelFileAsync(
        IFormFile file,
        GponDeploymentImportOptions options,
        GponDeploymentImportResult result,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Importing GPON deployment file: {FileName}", file.FileName);
        // Implementation for importing Excel file
        // This would parse and import data in correct order:
        // 1. Departments (if referenced)
        // 2. Order Types
        // 3. Installation Methods
        // 4. Building Types
        // 5. Splitter Types
        // 6. Material Categories
        // 7. Rate Cards (depends on all above)
    }
}

