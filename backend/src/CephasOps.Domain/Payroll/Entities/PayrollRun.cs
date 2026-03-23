using CephasOps.Domain.Common;

namespace CephasOps.Domain.Payroll.Entities;

/// <summary>
/// Payroll run entity (batch processing)
/// </summary>
public class PayrollRun : CompanyScopedEntity
{
    /// <summary>
    /// Payroll period ID
    /// </summary>
    public Guid PayrollPeriodId { get; set; }

    /// <summary>
    /// Period start date
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Period end date
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Status (Draft, Final, Exported, Paid)
    /// </summary>
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// Total amount for this run
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Notes/remarks
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Export reference (e.g., GIRO batch ID)
    /// </summary>
    public string? ExportReference { get; set; }

    /// <summary>
    /// User ID who created this run
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Finalized timestamp
    /// </summary>
    public DateTime? FinalizedAt { get; set; }

    /// <summary>
    /// Paid timestamp
    /// </summary>
    public DateTime? PaidAt { get; set; }

    // Navigation properties
    public PayrollPeriod? PayrollPeriod { get; set; }
    public ICollection<PayrollLine> PayrollLines { get; set; } = new List<PayrollLine>();
    public ICollection<JobEarningRecord> JobEarningRecords { get; set; } = new List<JobEarningRecord>();
}

