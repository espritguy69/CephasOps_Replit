namespace CephasOps.Application.Departments.DTOs;

/// <summary>
/// Department deployment DTOs for Excel import/export
/// Supports GPON, CWO, NWO, and future departments
/// </summary>

/// <summary>
/// Department deployment configuration - defines what data types a department needs
/// </summary>
public class DepartmentDeploymentConfig
{
    public string DepartmentCode { get; set; } = string.Empty; // GPON, CWO, NWO
    public string DepartmentName { get; set; } = string.Empty;
    public List<string> RequiredDataTypes { get; set; } = new(); // OrderTypes, InstallationMethods, etc.
    public List<string> OptionalDataTypes { get; set; } = new();
    public Dictionary<string, string> DataTypeDescriptions { get; set; } = new();
}

/// <summary>
/// Department deployment validation result (dry-run)
/// </summary>
public class DepartmentDeploymentValidationResult
{
    public bool IsValid { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public Dictionary<string, int> RecordCounts { get; set; } = new();
    public List<DepartmentDeploymentError> Errors { get; set; } = new();
    public List<DepartmentDeploymentWarning> Warnings { get; set; } = new();
    public Dictionary<string, List<string>> MissingDependencies { get; set; } = new();
}

/// <summary>
/// Department deployment import result
/// </summary>
public class DepartmentDeploymentImportResult
{
    public bool Success { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public Dictionary<string, DepartmentImportSummary> Summaries { get; set; } = new();
    public List<DepartmentDeploymentError> Errors { get; set; } = new();
    public List<DepartmentDeploymentWarning> Warnings { get; set; } = new();
}

/// <summary>
/// Import summary for each data type
/// </summary>
public class DepartmentImportSummary
{
    public string DataType { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Success { get; set; }
    public int Errors { get; set; }
    public int Warnings { get; set; }
}

/// <summary>
/// Department deployment error
/// </summary>
public class DepartmentDeploymentError
{
    public string DataType { get; set; } = string.Empty;
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RawValue { get; set; }
    public string? SheetName { get; set; }
}

/// <summary>
/// Department deployment warning
/// </summary>
public class DepartmentDeploymentWarning
{
    public string DataType { get; set; } = string.Empty;
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? SheetName { get; set; }
}

/// <summary>
/// Department deployment import options
/// </summary>
public class DepartmentDeploymentImportOptions
{
    public string DepartmentCode { get; set; } = string.Empty; // GPON, CWO, NWO
    public string? DepartmentName { get; set; } // If creating new department
    public DuplicateHandling DuplicateHandling { get; set; } = DuplicateHandling.Skip;
    public bool CreateMissingDependencies { get; set; } = false;
    public bool CreateDepartmentIfNotExists { get; set; } = true;
    public List<string> DataTypesToImport { get; set; } = new(); // Empty = import all
}

/// <summary>
/// Duplicate handling strategy
/// </summary>
public enum DuplicateHandling
{
    Skip,           // Don't import duplicates
    Update,         // Update existing records
    CreateNew,      // Force create (may cause errors)
}

// Common Excel Row DTOs (shared across all departments)

/// <summary>
/// Order Type row from Excel (common to all departments)
/// </summary>
public class OrderTypeRow
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DepartmentName { get; set; } // Reference to Department
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Installation Method row from Excel (GPON-specific)
/// </summary>
public class InstallationMethodRow
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Category { get; set; } // FTTH, FTTO, FTTR, FTTC
    public string? Description { get; set; }
    public string? DepartmentName { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Building Type row from Excel (GPON-specific)
/// </summary>
public class BuildingTypeRow
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DepartmentName { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Splitter Type row from Excel (GPON-specific)
/// </summary>
public class SplitterTypeRow
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int TotalPorts { get; set; }
    public int? StandbyPortNumber { get; set; }
    public string? Description { get; set; }
    public string? DepartmentName { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Material Category row from Excel (common to all departments)
/// </summary>
public class MaterialCategoryRow
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Rate Card row from Excel (department-specific structure)
/// </summary>
public class RateCardRow
{
    // Common fields
    public string? PartnerName { get; set; }
    public string? PartnerGroupName { get; set; }
    public string? DepartmentName { get; set; }
    public string? OrderTypeName { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxRate { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    // GPON-specific fields
    public string? ServiceCategory { get; set; } // FTTH, FTTO, FTTR, FTTC
    public string? InstallationMethodName { get; set; }
    public string? BuildingType { get; set; }

    // NWO-specific fields (future)
    public string? ScopeType { get; set; } // FIBRE_PULL, CHAMBER, MANHOLE
    public string? Complexity { get; set; } // NORMAL, HARD, NIGHT
    public string? Region { get; set; }
    public string? Unit { get; set; } // METER, UNIT, JOB

    // CWO-specific fields (future)
    public string? EnterpriseScope { get; set; } // CORE_PULL, RACK_SETUP
    public string? Difficulty { get; set; }
    public int? FloorCount { get; set; }
    public int? CabinetCount { get; set; }
}

/// <summary>
/// Parser Template row from Excel (department-specific)
/// </summary>
public class ParserTemplateRow
{
    public string Name { get; set; } = string.Empty;
    public string? PartnerName { get; set; }
    public string? PartnerGroupName { get; set; }
    public string? DepartmentName { get; set; }
    public string ParserType { get; set; } = string.Empty; // Activation, Assurance, Modification
    public string? SupportedFormat { get; set; } // Excel, PDF, Email
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Email Account row from Excel (department-specific)
/// </summary>
public class EmailAccountRow
{
    public string Name { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string Provider { get; set; } = "POP3"; // POP3, IMAP
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 995;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int PollIntervalSec { get; set; } = 60;
    public string? ParserTemplateName { get; set; } // Reference to ParserTemplate
    public bool IsActive { get; set; } = true;
}

