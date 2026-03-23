using CephasOps.Domain.Common;

namespace CephasOps.Domain.Payroll.Entities;

/// <summary>
/// Payroll period entity
/// </summary>
public class PayrollPeriod : CompanyScopedEntity
{
    /// <summary>
    /// Period identifier (e.g., "2025-01")
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Period start date
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Period end date
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Status (Draft, Final, Locked)
    /// </summary>
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// Whether this period is locked
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// User ID who created this period
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    // Navigation properties
    public ICollection<PayrollRun> PayrollRuns { get; set; } = new List<PayrollRun>();
}

