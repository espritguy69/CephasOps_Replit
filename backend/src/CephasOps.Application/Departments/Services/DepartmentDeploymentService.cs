using CephasOps.Application.Departments.DTOs;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Buildings.Services;
using CephasOps.Application.Inventory.Services;
using CephasOps.Application.Billing.Services;
using CephasOps.Application.Departments.Services;
using CephasOps.Application.Companies.Services;
using CephasOps.Application.Parser.Services;
using CephasOps.Domain.Departments.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Syncfusion.XlsIO;
using System.Text;

namespace CephasOps.Application.Departments.Services;

/// <summary>
/// Service for department deployment via Excel import/export
/// Configuration-driven approach - supports GPON, CWO, NWO, and future departments
/// </summary>
public class DepartmentDeploymentService : IDepartmentDeploymentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DepartmentDeploymentService> _logger;
    private readonly IOrderTypeService _orderTypeService;
    private readonly IInstallationMethodService _installationMethodService;
    private readonly IBuildingTypeService _buildingTypeService;
    private readonly ISplitterTypeService _splitterTypeService;
    private readonly IMaterialCategoryService _materialCategoryService;
    private readonly IBillingRatecardService _billingRatecardService;
    private readonly IDepartmentService _departmentService;
    private readonly IPartnerService _partnerService;
    private readonly IPartnerGroupService _partnerGroupService;
    private readonly IParserTemplateService _parserTemplateService;
    private readonly IEmailAccountService _emailAccountService;

    // Department configurations - defines what data types each department needs
    private static readonly Dictionary<string, DepartmentDeploymentConfig> DepartmentConfigs = new()
    {
        ["GPON"] = new DepartmentDeploymentConfig
        {
            DepartmentCode = "GPON",
            DepartmentName = "GPON (Gigabit Passive Optical Network)",
            RequiredDataTypes = new List<string>
            {
                "OrderTypes",
                "InstallationMethods",
                "BuildingTypes",
                "SplitterTypes",
                "MaterialCategories",
                "RateCards"
            },
            OptionalDataTypes = new List<string>
            {
                "ParserTemplates",
                "EmailAccounts"
            },
            DataTypeDescriptions = new Dictionary<string, string>
            {
                ["OrderTypes"] = "Order categories: Activation, Modification, Assurance, etc.",
                ["InstallationMethods"] = "Site conditions: Prelaid, Non-Prelaid, SDU, RDF Pole",
                ["BuildingTypes"] = "Building classifications: Prelaid, Non-Prelaid, SDU, RDF_POLE",
                ["SplitterTypes"] = "Splitter configurations: 1:8, 1:12, 1:32",
                ["MaterialCategories"] = "Material classification hierarchy",
                ["RateCards"] = "Billing rates by OrderType + InstallationMethod + Partner",
                ["ParserTemplates"] = "Email parsing templates for partner orders",
                ["EmailAccounts"] = "Email inboxes for order ingestion"
            }
        },
        ["CWO"] = new DepartmentDeploymentConfig
        {
            DepartmentCode = "CWO",
            DepartmentName = "CWO (Customer Work Orders - Enterprise)",
            RequiredDataTypes = new List<string>
            {
                "OrderTypes",
                "MaterialCategories",
                "RateCards"
            },
            OptionalDataTypes = new List<string>
            {
                "ParserTemplates",
                "EmailAccounts"
            },
            DataTypeDescriptions = new Dictionary<string, string>
            {
                ["OrderTypes"] = "Enterprise order types: Core Pull, Rack Setup, etc.",
                ["MaterialCategories"] = "Material classification hierarchy",
                ["RateCards"] = "Enterprise rates by Scope + Difficulty + FloorCount",
                ["ParserTemplates"] = "Email parsing templates for enterprise orders",
                ["EmailAccounts"] = "Email inboxes for order ingestion"
            }
        },
        ["NWO"] = new DepartmentDeploymentConfig
        {
            DepartmentCode = "NWO",
            DepartmentName = "NWO (Network Work Orders)",
            RequiredDataTypes = new List<string>
            {
                "OrderTypes",
                "MaterialCategories",
                "RateCards"
            },
            OptionalDataTypes = new List<string>
            {
                "ParserTemplates",
                "EmailAccounts"
            },
            DataTypeDescriptions = new Dictionary<string, string>
            {
                ["OrderTypes"] = "Network order types: Fibre Pull, Chamber, Manhole, etc.",
                ["MaterialCategories"] = "Material classification hierarchy",
                ["RateCards"] = "Network rates by ScopeType + Complexity + Region",
                ["ParserTemplates"] = "Email parsing templates for network orders",
                ["EmailAccounts"] = "Email inboxes for order ingestion"
            }
        }
    };

    public DepartmentDeploymentService(
        ApplicationDbContext context,
        ILogger<DepartmentDeploymentService> logger,
        IOrderTypeService orderTypeService,
        IInstallationMethodService installationMethodService,
        IBuildingTypeService buildingTypeService,
        ISplitterTypeService splitterTypeService,
        IMaterialCategoryService materialCategoryService,
        IBillingRatecardService billingRatecardService,
        IDepartmentService departmentService,
        IPartnerService partnerService,
        IPartnerGroupService partnerGroupService,
        IParserTemplateService parserTemplateService,
        IEmailAccountService emailAccountService)
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
        _parserTemplateService = parserTemplateService;
        _emailAccountService = emailAccountService;
    }

    public Task<List<DepartmentDeploymentConfig>> GetAvailableConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DepartmentConfigs.Values.ToList());
    }

    public Task<DepartmentDeploymentConfig?> GetConfigurationAsync(string departmentCode, CancellationToken cancellationToken = default)
    {
        var config = DepartmentConfigs.TryGetValue(departmentCode.ToUpperInvariant(), out var found) ? found : null;
        return Task.FromResult(config);
    }

    public async Task<byte[]> GenerateTemplateAsync(string departmentCode, CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationAsync(departmentCode, cancellationToken);
        if (config == null)
        {
            throw new ArgumentException($"Unknown department code: {departmentCode}");
        }

        using var excelEngine = new ExcelEngine();
        var application = excelEngine.Excel;
        application.DefaultVersion = ExcelVersion.Excel2016;

        IWorkbook workbook = application.Workbooks.Create(1);

        // Create instruction sheet
        CreateInstructionsSheet(workbook, config);

        // Create sheets based on configuration
        if (config.RequiredDataTypes.Contains("OrderTypes") || config.OptionalDataTypes.Contains("OrderTypes"))
        {
            CreateOrderTypesSheet(workbook);
        }

        if (config.RequiredDataTypes.Contains("InstallationMethods") || config.OptionalDataTypes.Contains("InstallationMethods"))
        {
            CreateInstallationMethodsSheet(workbook);
        }

        if (config.RequiredDataTypes.Contains("BuildingTypes") || config.OptionalDataTypes.Contains("BuildingTypes"))
        {
            CreateBuildingTypesSheet(workbook);
        }

        if (config.RequiredDataTypes.Contains("SplitterTypes") || config.OptionalDataTypes.Contains("SplitterTypes"))
        {
            CreateSplitterTypesSheet(workbook);
        }

        if (config.RequiredDataTypes.Contains("MaterialCategories") || config.OptionalDataTypes.Contains("MaterialCategories"))
        {
            CreateMaterialCategoriesSheet(workbook);
        }

        if (config.RequiredDataTypes.Contains("RateCards") || config.OptionalDataTypes.Contains("RateCards"))
        {
            CreateRateCardsSheet(workbook, config);
        }

        if (config.OptionalDataTypes.Contains("ParserTemplates"))
        {
            CreateParserTemplatesSheet(workbook);
        }

        if (config.OptionalDataTypes.Contains("EmailAccounts"))
        {
            CreateEmailAccountsSheet(workbook);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        workbook.Close();

        return stream.ToArray();
    }

    public async Task<DepartmentDeploymentValidationResult> ValidateDeploymentAsync(
        IFormFileCollection files,
        string departmentCode,
        CancellationToken cancellationToken = default)
    {
        var result = new DepartmentDeploymentValidationResult
        {
            DepartmentCode = departmentCode
        };

        try
        {
            var config = await GetConfigurationAsync(departmentCode, cancellationToken);
            if (config == null)
            {
                result.Errors.Add(new DepartmentDeploymentError
                {
                    DataType = "System",
                    RowNumber = 0,
                    Message = $"Unknown department code: {departmentCode}"
                });
                result.IsValid = false;
                return result;
            }

            if (files == null || files.Count == 0)
            {
                result.Errors.Add(new DepartmentDeploymentError
                {
                    DataType = "System",
                    RowNumber = 0,
                    Message = "No files uploaded"
                });
                result.IsValid = false;
                return result;
            }

            // Parse and validate Excel file based on configuration
            await ValidateExcelFileAsync(files[0], config, result, cancellationToken);

            result.IsValid = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating department deployment for {DepartmentCode}", departmentCode);
            result.Errors.Add(new DepartmentDeploymentError
            {
                DataType = "System",
                RowNumber = 0,
                Message = $"Validation failed: {ex.Message}"
            });
            result.IsValid = false;
        }

        return result;
    }

    public async Task<DepartmentDeploymentImportResult> ImportDeploymentAsync(
        IFormFileCollection files,
        DepartmentDeploymentImportOptions options,
        CancellationToken cancellationToken = default)
    {
        var result = new DepartmentDeploymentImportResult
        {
            DepartmentCode = options.DepartmentCode
        };

        try
        {
            var config = await GetConfigurationAsync(options.DepartmentCode, cancellationToken);
            if (config == null)
            {
                result.Errors.Add(new DepartmentDeploymentError
                {
                    DataType = "System",
                    RowNumber = 0,
                    Message = $"Unknown department code: {options.DepartmentCode}"
                });
                result.Success = false;
                return result;
            }

            if (files == null || files.Count == 0)
            {
                result.Errors.Add(new DepartmentDeploymentError
                {
                    DataType = "System",
                    RowNumber = 0,
                    Message = "No files uploaded"
                });
                result.Success = false;
                return result;
            }

            // Validate first
            var validation = await ValidateDeploymentAsync(files, options.DepartmentCode, cancellationToken);
            if (!validation.IsValid && !options.CreateMissingDependencies)
            {
                result.Errors.AddRange(validation.Errors);
                result.Success = false;
                return result;
            }

            // Ensure department exists
            var department = await EnsureDepartmentExistsAsync(options, cancellationToken);
            if (department != null)
            {
                result.DepartmentId = department.Id;
            }

            // Import in correct order based on configuration
            await ImportExcelFileAsync(files[0], config, options, result, cancellationToken);

            result.Success = result.ErrorCount == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing department deployment for {DepartmentCode}", options.DepartmentCode);
            result.Errors.Add(new DepartmentDeploymentError
            {
                DataType = "System",
                RowNumber = 0,
                Message = $"Import failed: {ex.Message}"
            });
            result.Success = false;
        }

        return result;
    }

    public async Task<byte[]> ExportDepartmentDataAsync(
        string departmentCode,
        Guid? departmentId,
        CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationAsync(departmentCode, cancellationToken);
        if (config == null)
        {
            throw new ArgumentException($"Unknown department code: {departmentCode}");
        }

        using var excelEngine = new ExcelEngine();
        var application = excelEngine.Excel;
        application.DefaultVersion = ExcelVersion.Excel2016;

        IWorkbook workbook = application.Workbooks.Create(1);

        // Export department data to Excel based on configuration
        _logger.LogInformation("Exporting {DepartmentCode} data for department: {DepartmentId}", departmentCode, departmentId);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        workbook.Close();

        return stream.ToArray();
    }

    // Private helper methods

    private void CreateInstructionsSheet(IWorkbook workbook, DepartmentDeploymentConfig config)
    {
        var sheet = workbook.Worksheets[0];
        sheet.Name = "Instructions";
        sheet.Range["A1"].Text = $"Department Deployment - {config.DepartmentName}";
        sheet.Range["A3"].Text = "This template includes the following data types:";
        
        int row = 4;
        foreach (var dataType in config.RequiredDataTypes)
        {
            sheet.Range[$"A{row}"].Text = $"✓ {dataType} (Required)";
            if (config.DataTypeDescriptions.TryGetValue(dataType, out var desc))
            {
                sheet.Range[$"B{row}"].Text = desc;
            }
            row++;
        }

        foreach (var dataType in config.OptionalDataTypes)
        {
            sheet.Range[$"A{row}"].Text = $"○ {dataType} (Optional)";
            if (config.DataTypeDescriptions.TryGetValue(dataType, out var desc))
            {
                sheet.Range[$"B{row}"].Text = desc;
            }
            row++;
        }

        sheet.Range["A1"].CellStyle.Font.Bold = true;
        sheet.Range["A1"].CellStyle.Font.Size = 14;
    }

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

    private void CreateRateCardsSheet(IWorkbook workbook, DepartmentDeploymentConfig config)
    {
        var sheet = workbook.Worksheets.Create();
        sheet.Name = "Rate Cards";
        
        // Common columns
        sheet.Range["A1"].Text = "PartnerName";
        sheet.Range["B1"].Text = "PartnerGroupName";
        sheet.Range["C1"].Text = "DepartmentName";
        sheet.Range["D1"].Text = "OrderTypeName";
        sheet.Range["E1"].Text = "Description";
        sheet.Range["F1"].Text = "Amount";
        sheet.Range["G1"].Text = "TaxRate";
        sheet.Range["H1"].Text = "EffectiveFrom";
        sheet.Range["I1"].Text = "EffectiveTo";
        sheet.Range["J1"].Text = "IsActive";

        // Department-specific columns
        int col = 11;
        if (config.DepartmentCode == "GPON")
        {
            sheet.Range[GetColumnLetter(col++) + "1"].Text = "ServiceCategory";
            sheet.Range[GetColumnLetter(col++) + "1"].Text = "InstallationMethodName";
            sheet.Range[GetColumnLetter(col++) + "1"].Text = "BuildingType";
        }
        else if (config.DepartmentCode == "NWO")
        {
            sheet.Range[GetColumnLetter(col++) + "1"].Text = "ScopeType";
            sheet.Range[GetColumnLetter(col++) + "1"].Text = "Complexity";
            sheet.Range[GetColumnLetter(col++) + "1"].Text = "Region";
            sheet.Range[GetColumnLetter(col++) + "1"].Text = "Unit";
        }
        else if (config.DepartmentCode == "CWO")
        {
            sheet.Range[GetColumnLetter(col++) + "1"].Text = "EnterpriseScope";
            sheet.Range[GetColumnLetter(col++) + "1"].Text = "Difficulty";
            sheet.Range[GetColumnLetter(col++) + "1"].Text = "FloorCount";
            sheet.Range[GetColumnLetter(col++) + "1"].Text = "CabinetCount";
        }
    }

    private void CreateParserTemplatesSheet(IWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Create();
        sheet.Name = "Parser Templates";
        sheet.Range["A1"].Text = "Name";
        sheet.Range["B1"].Text = "PartnerName";
        sheet.Range["C1"].Text = "PartnerGroupName";
        sheet.Range["D1"].Text = "DepartmentName";
        sheet.Range["E1"].Text = "ParserType";
        sheet.Range["F1"].Text = "SupportedFormat";
        sheet.Range["G1"].Text = "Description";
        sheet.Range["H1"].Text = "IsActive";
    }

    private void CreateEmailAccountsSheet(IWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Create();
        sheet.Name = "Email Accounts";
        sheet.Range["A1"].Text = "Name";
        sheet.Range["B1"].Text = "DepartmentName";
        sheet.Range["C1"].Text = "Provider";
        sheet.Range["D1"].Text = "Host";
        sheet.Range["E1"].Text = "Port";
        sheet.Range["F1"].Text = "UseSsl";
        sheet.Range["G1"].Text = "Username";
        sheet.Range["H1"].Text = "Password";
        sheet.Range["I1"].Text = "PollIntervalSec";
        sheet.Range["J1"].Text = "ParserTemplateName";
        sheet.Range["K1"].Text = "IsActive";
    }

    private string GetColumnLetter(int columnNumber)
    {
        string columnName = "";
        while (columnNumber > 0)
        {
            columnNumber--;
            columnName = (char)('A' + columnNumber % 26) + columnName;
            columnNumber /= 26;
        }
        return columnName;
    }

    private async Task<Domain.Departments.Entities.Department?> EnsureDepartmentExistsAsync(
        DepartmentDeploymentImportOptions options,
        CancellationToken cancellationToken)
    {
        if (!options.CreateDepartmentIfNotExists)
        {
            var existing = await _context.Departments
                .FirstOrDefaultAsync(d => d.Code == options.DepartmentCode.ToUpperInvariant(), cancellationToken);
            return existing;
        }

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Code == options.DepartmentCode.ToUpperInvariant(), cancellationToken);

        if (department == null)
        {
            var config = await GetConfigurationAsync(options.DepartmentCode, cancellationToken);
            department = new Domain.Departments.Entities.Department
            {
                Id = Guid.NewGuid(),
                Code = options.DepartmentCode.ToUpperInvariant(),
                Name = options.DepartmentName ?? config?.DepartmentName ?? options.DepartmentCode,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created department: {DepartmentCode} ({DepartmentName})", department.Code, department.Name);
        }

        return department;
    }

    private async Task ValidateExcelFileAsync(
        IFormFile file,
        DepartmentDeploymentConfig config,
        DepartmentDeploymentValidationResult result,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating {DepartmentCode} deployment file: {FileName}", config.DepartmentCode, file.FileName);
        // Implementation for validating Excel file based on configuration
        // This would parse the Excel file and validate each sheet according to config
    }

    private async Task ImportExcelFileAsync(
        IFormFile file,
        DepartmentDeploymentConfig config,
        DepartmentDeploymentImportOptions options,
        DepartmentDeploymentImportResult result,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Importing {DepartmentCode} deployment file: {FileName}", config.DepartmentCode, file.FileName);
        // Implementation for importing Excel file based on configuration
        // This would parse and import data in correct order:
        // 1. Department (if creating new)
        // 2. Order Types
        // 3. Installation Methods (if GPON)
        // 4. Building Types (if GPON)
        // 5. Splitter Types (if GPON)
        // 6. Material Categories
        // 7. Rate Cards (depends on all above)
        // 8. Parser Templates (optional)
        // 9. Email Accounts (optional)
    }
}

