using CephasOps.Domain.Common;

namespace CephasOps.Domain.Payroll.Entities;

/// <summary>
/// Payroll line entity (per SI in a payroll run)
/// </summary>
public class PayrollLine : CompanyScopedEntity
{
    /// <summary>
    /// Payroll run ID
    /// </summary>
    public Guid PayrollRunId { get; set; }

    /// <summary>
    /// Service installer ID
    /// </summary>
    public Guid ServiceInstallerId { get; set; }

    /// <summary>
    /// Total number of jobs
    /// </summary>
    public int TotalJobs { get; set; }

    /// <summary>
    /// Total pay before adjustments
    /// </summary>
    public decimal TotalPay { get; set; }

    /// <summary>
    /// Adjustments (positive or negative)
    /// </summary>
    public decimal Adjustments { get; set; }

    /// <summary>
    /// Net pay (TotalPay + Adjustments)
    /// </summary>
    public decimal NetPay { get; set; }

    /// <summary>
    /// Export reference (e.g., GIRO batch ID)
    /// </summary>
    public string? ExportReference { get; set; }

    // Navigation properties
    public PayrollRun? PayrollRun { get; set; }
}

