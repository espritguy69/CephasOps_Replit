namespace CephasOps.Application.Companies.DTOs;

/// <summary>
/// Company deployment DTOs for Excel import/export
/// </summary>

/// <summary>
/// Deployment validation result (dry-run)
/// </summary>
public class DeploymentValidationResult
{
    public bool IsValid { get; set; }
    public int TotalRecords { get; set; }
    public Dictionary<string, int> RecordCounts { get; set; } = new();
    public List<DeploymentError> Errors { get; set; } = new();
    public List<DeploymentWarning> Warnings { get; set; } = new();
    public Dictionary<string, List<string>> MissingDependencies { get; set; } = new();
    public string Format { get; set; } = string.Empty; // "single" or "separate"
}

/// <summary>
/// Deployment import result
/// </summary>
public class DeploymentImportResult
{
    public bool Success { get; set; }
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public Dictionary<string, ImportSummary> Summaries { get; set; } = new();
    public List<DeploymentError> Errors { get; set; } = new();
    public List<DeploymentWarning> Warnings { get; set; } = new();
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
}

/// <summary>
/// Import summary for each data type
/// </summary>
public class ImportSummary
{
    public string DataType { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Success { get; set; }
    public int Errors { get; set; }
    public int Warnings { get; set; }
}

/// <summary>
/// Deployment error
/// </summary>
public class DeploymentError
{
    public string DataType { get; set; } = string.Empty;
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RawValue { get; set; }
    public string? SheetName { get; set; }
}

/// <summary>
/// Deployment warning
/// </summary>
public class DeploymentWarning
{
    public string DataType { get; set; } = string.Empty;
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? SheetName { get; set; }
}

/// <summary>
/// Deployment import options
/// </summary>
public class DeploymentImportOptions
{
    public DuplicateHandling DuplicateHandling { get; set; } = DuplicateHandling.Skip;
    public bool SkipSplittersIfNotGpon { get; set; } = true;
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
    Ask             // Not used in API, handled in UI
}

// Excel Row DTOs for each data type

/// <summary>
/// Company info row from Excel
/// </summary>
public class CompanyInfoRow
{
    public string LegalName { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string? RegistrationNo { get; set; }
    public string? TaxId { get; set; }
    public string Vertical { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    // Settings
    public string? DefaultTimezone { get; set; }
    public string? DefaultCurrency { get; set; }
    public string? DefaultLanguage { get; set; }
    public string? InvoicePrefix { get; set; }
    public string? WorkOrderPrefix { get; set; }
    public int? BillingDayOfMonth { get; set; }
}

/// <summary>
/// Department row from Excel
/// </summary>
public class DepartmentRow
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CostCentreCode { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Partner Group row from Excel
/// </summary>
public class PartnerGroupRow
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Partner row from Excel
/// </summary>
public class PartnerRow
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string PartnerType { get; set; } = string.Empty; // Telco, Customer, Vendor, Landlord
    public string? PartnerGroupName { get; set; } // Reference to PartnerGroup
    public string? DepartmentName { get; set; } // Optional department
    public string? BillingAddress { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Material row from Excel
/// </summary>
public class MaterialRow
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal? UnitCost { get; set; }
    public bool IsSerialised { get; set; }
    public int? MinStockLevel { get; set; }
    public int? ReorderPoint { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Building row from Excel
/// </summary>
public class BuildingRow
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? PropertyType { get; set; }
    public string? InstallationMethodName { get; set; }
    public string? DepartmentName { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Postcode { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Splitter row from Excel (GPON-specific)
/// </summary>
public class SplitterRow
{
    public string BuildingName { get; set; } = string.Empty; // Reference to Building
    public string? BuildingCode { get; set; } // Alternative reference
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? SplitterTypeName { get; set; }
    public string? Location { get; set; }
    public string? Block { get; set; }
    public string? Floor { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Service Installer row from Excel
/// </summary>
public class ServiceInstallerRow
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? DepartmentName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Level { get; set; }
    public string? IcNumber { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContact { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// User row from Excel
/// </summary>
public class UserRow
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Password { get; set; } = string.Empty; // Will be hashed
    public string? RoleName { get; set; } // Reference to Role
    public string? DepartmentName { get; set; } // Optional department
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Rate Card row from Excel
/// </summary>
public class RateCardRow
{
    public string? PartnerName { get; set; } // Reference to Partner
    public string? PartnerGroupName { get; set; } // Reference to PartnerGroup
    public string? DepartmentName { get; set; }
    public string OrderTypeName { get; set; } = string.Empty;
    public string ServiceCategory { get; set; } = string.Empty;
    public string? InstallationMethodName { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TaxRate { get; set; }
    public bool IsActive { get; set; } = true;
}

