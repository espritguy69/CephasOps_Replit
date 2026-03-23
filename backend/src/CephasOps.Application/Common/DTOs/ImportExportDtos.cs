namespace CephasOps.Application.Common.DTOs;

/// <summary>
/// CSV export/import DTO for SI Rate Plans
/// </summary>
public class SiRatePlanCsvDto
{
    public string ServiceInstallerName { get; set; } = string.Empty;
    public string ServiceInstallerCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string InstallationMethodName { get; set; } = string.Empty;
    public string RateType { get; set; } = "Junior";
    public string Level { get; set; } = "Junior";
    public decimal? PrelaidRate { get; set; }
    public decimal? NonPrelaidRate { get; set; }
    public decimal? SduRate { get; set; }
    public decimal? RdfPoleRate { get; set; }
    public decimal? ActivationRate { get; set; }
    public decimal? ModificationRate { get; set; }
    public decimal? AssuranceRate { get; set; }
    public decimal? AssuranceRepullRate { get; set; }
    public decimal? FttrRate { get; set; }
    public decimal? FttcRate { get; set; }
    public decimal? OnTimeBonus { get; set; }
    public decimal? LatePenalty { get; set; }
    public decimal? ReworkRate { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// CSV export/import DTO for Partner Rates (Billing Ratecards)
/// </summary>
public class PartnerRateCsvDto
{
    public string PartnerName { get; set; } = string.Empty;
    public string PartnerCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string OrderTypeName { get; set; } = string.Empty;
    public string ServiceCategory { get; set; } = string.Empty;
    public string InstallationMethodName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TaxRate { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// CSV export/import DTO for Service Installers
/// </summary>
public class ServiceInstallerCsvDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string IcNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string EmergencyContact { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// CSV export/import DTO for Partners
/// </summary>
public class PartnerCsvDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// CSV export/import DTO for Materials
/// </summary>
public class MaterialCsvDto
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public bool IsSerialised { get; set; }
    public int? MinStockLevel { get; set; }
    public int? ReorderPoint { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// CSV export/import DTO for Buildings
/// </summary>
public class BuildingCsvDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public string InstallationMethodName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Postcode { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    /// <summary>When null or missing in CSV, treated as true.</summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// CSV export/import DTO for Installation Methods
/// </summary>
public class InstallationMethodCsvDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// CSV export/import DTO for Order Types
/// </summary>
public class OrderTypeCsvDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// CSV export/import DTO for Departments
/// </summary>
public class DepartmentCsvDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Import result with success/error tracking
/// </summary>
public class ImportResult<T>
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<T> ImportedRecords { get; set; } = new();
    public List<ImportError> Errors { get; set; } = new();
}

/// <summary>
/// Import error details
/// </summary>
public class ImportError
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RawValue { get; set; } = string.Empty;
}

