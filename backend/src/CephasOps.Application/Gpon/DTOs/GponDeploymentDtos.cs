namespace CephasOps.Application.Gpon.DTOs;

/// <summary>
/// GPON deployment DTOs for Excel import/export
/// </summary>

/// <summary>
/// GPON deployment validation result (dry-run)
/// </summary>
public class GponDeploymentValidationResult
{
    public bool IsValid { get; set; }
    public int TotalRecords { get; set; }
    public Dictionary<string, int> RecordCounts { get; set; } = new();
    public List<GponDeploymentError> Errors { get; set; } = new();
    public List<GponDeploymentWarning> Warnings { get; set; } = new();
    public Dictionary<string, List<string>> MissingDependencies { get; set; } = new();
}

/// <summary>
/// GPON deployment import result
/// </summary>
public class GponDeploymentImportResult
{
    public bool Success { get; set; }
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public Dictionary<string, GponImportSummary> Summaries { get; set; } = new();
    public List<GponDeploymentError> Errors { get; set; } = new();
    public List<GponDeploymentWarning> Warnings { get; set; } = new();
}

/// <summary>
/// Import summary for each data type
/// </summary>
public class GponImportSummary
{
    public string DataType { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Success { get; set; }
    public int Errors { get; set; }
    public int Warnings { get; set; }
}

/// <summary>
/// GPON deployment error
/// </summary>
public class GponDeploymentError
{
    public string DataType { get; set; } = string.Empty;
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RawValue { get; set; }
    public string? SheetName { get; set; }
}

/// <summary>
/// GPON deployment warning
/// </summary>
public class GponDeploymentWarning
{
    public string DataType { get; set; } = string.Empty;
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? SheetName { get; set; }
}

/// <summary>
/// GPON deployment import options
/// </summary>
public class GponDeploymentImportOptions
{
    public DuplicateHandling DuplicateHandling { get; set; } = DuplicateHandling.Skip;
    public bool CreateMissingDependencies { get; set; } = false;
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

// Excel Row DTOs for each GPON data type

/// <summary>
/// Order Type row from Excel
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
/// Installation Method row from Excel
/// </summary>
public class InstallationMethodRow
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Category { get; set; } // FTTH, FTTO, FTTR, FTTC
    public string? Description { get; set; }
    public string? DepartmentName { get; set; } // Reference to Department
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Building Type row from Excel
/// </summary>
public class BuildingTypeRow
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DepartmentName { get; set; } // Reference to Department
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Splitter Type row from Excel
/// </summary>
public class SplitterTypeRow
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int TotalPorts { get; set; }
    public int? StandbyPortNumber { get; set; }
    public string? Description { get; set; }
    public string? DepartmentName { get; set; } // Reference to Department
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Material Category row from Excel
/// </summary>
public class MaterialCategoryRow
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Rate Card row from Excel (Billing Ratecards)
/// </summary>
public class RateCardRow
{
    public string? PartnerName { get; set; } // Reference to Partner
    public string? PartnerGroupName { get; set; } // Reference to PartnerGroup
    public string? DepartmentName { get; set; } // Reference to Department
    public string? OrderTypeName { get; set; } // Reference to OrderType
    public string? ServiceCategory { get; set; } // FTTH, FTTO, FTTR, FTTC
    public string? InstallationMethodName { get; set; } // Reference to InstallationMethod
    public string? BuildingType { get; set; } // Legacy field
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxRate { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

