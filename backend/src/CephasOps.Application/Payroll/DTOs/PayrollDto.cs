namespace CephasOps.Application.Payroll.DTOs;

/// <summary>
/// Payroll period DTO
/// </summary>
public class PayrollPeriodDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Period { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Payroll run DTO
/// </summary>
public class PayrollRunDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public Guid? PayrollPeriodId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public string? ExportReference { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public List<PayrollLineDto> Lines { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Payroll line DTO
/// </summary>
public class PayrollLineDto
{
    public Guid Id { get; set; }
    public Guid ServiceInstallerId { get; set; }
    public string ServiceInstallerName { get; set; } = string.Empty;
    public int TotalJobs { get; set; }
    public decimal TotalPay { get; set; }
    public decimal Adjustments { get; set; }
    public decimal NetPay { get; set; }
    public string? ExportReference { get; set; }
}

/// <summary>
/// Job earning record DTO
/// </summary>
public class JobEarningRecordDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string OrderUniqueId { get; set; } = string.Empty;
    public Guid ServiceInstallerId { get; set; }
    public string ServiceInstallerName { get; set; } = string.Empty;
    public Guid OrderTypeId { get; set; }
    public string OrderTypeCode { get; set; } = string.Empty;
    public string OrderTypeName { get; set; } = string.Empty;
    /// <summary>
    /// DEPRECATED: This field is no longer populated. Use OrderTypeId, OrderTypeCode, and OrderTypeName instead.
    /// Kept in DTO for backward compatibility with existing API consumers.
    /// </summary>
    [Obsolete("Use OrderTypeId, OrderTypeCode, and OrderTypeName instead. This field is no longer populated.")]
    public string JobType { get; set; } = string.Empty;
    public string? KpiResult { get; set; }
    public decimal BaseRate { get; set; }
    public decimal KpiAdjustment { get; set; }
    public decimal FinalPay { get; set; }
    public string Period { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

/// <summary>
/// SI rate plan DTO
/// </summary>
public class SiRatePlanDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid ServiceInstallerId { get; set; }
    public string ServiceInstallerName { get; set; } = string.Empty;
    public Guid? InstallationMethodId { get; set; }
    public string? InstallationMethodName { get; set; }
    public string RateType { get; set; } = "Junior"; // Junior, Senior, Custom
    public string Level { get; set; } = string.Empty;
    
    // Rates by Installation Method
    public decimal? PrelaidRate { get; set; }
    public decimal? NonPrelaidRate { get; set; }
    public decimal? SduRate { get; set; }
    public decimal? RdfPoleRate { get; set; }
    
    // Rates by Order Type (Activation, Modification, Assurance, etc.)
    public decimal? ActivationRate { get; set; }
    public decimal? ModificationRate { get; set; }
    public decimal? AssuranceRate { get; set; }
    public decimal? AssuranceRepullRate { get; set; }
    
    // Service Category specific
    public decimal? FttrRate { get; set; }
    public decimal? FttcRate { get; set; }
    
    // Bonus/Penalty
    public decimal? OnTimeBonus { get; set; }
    public decimal? LatePenalty { get; set; }
    public decimal? ReworkRate { get; set; }
    
    public bool IsActive { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Create payroll run request DTO
/// </summary>
public class CreatePayrollRunDto
{
    public Guid? PayrollPeriodId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Create SI rate plan request DTO
/// </summary>
public class CreateSiRatePlanDto
{
    public Guid? DepartmentId { get; set; }
    public Guid ServiceInstallerId { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public string RateType { get; set; } = "Junior"; // Junior, Senior, Custom
    public string Level { get; set; } = string.Empty;
    
    // Rates by Installation Method
    public decimal? PrelaidRate { get; set; }
    public decimal? NonPrelaidRate { get; set; }
    public decimal? SduRate { get; set; }
    public decimal? RdfPoleRate { get; set; }
    
    // Rates by Order Type (Activation, Modification, Assurance, etc.)
    public decimal? ActivationRate { get; set; }
    public decimal? ModificationRate { get; set; }
    public decimal? AssuranceRate { get; set; }
    public decimal? AssuranceRepullRate { get; set; }
    
    // Service Category specific
    public decimal? FttrRate { get; set; }
    public decimal? FttcRate { get; set; }
    
    // Bonus/Penalty
    public decimal? OnTimeBonus { get; set; }
    public decimal? LatePenalty { get; set; }
    public decimal? ReworkRate { get; set; }
    
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

