using CephasOps.Domain.Common;

namespace CephasOps.Domain.Payroll.Entities;

/// <summary>
/// Job earning record entity (per order/job)
/// </summary>
public class JobEarningRecord : CompanyScopedEntity
{
    /// <summary>
    /// Payroll run ID
    /// </summary>
    public Guid? PayrollRunId { get; set; }

    /// <summary>
    /// Order ID
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Service installer ID
    /// </summary>
    public Guid ServiceInstallerId { get; set; }

    /// <summary>
    /// Order type ID (FK to OrderType) - replaces JobType
    /// </summary>
    public Guid OrderTypeId { get; set; }

    /// <summary>
    /// Order type code (denormalized for reporting) - e.g., "ACTIVATION", "MODIFICATION_INDOOR"
    /// </summary>
    public string OrderTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// Order type name (denormalized for reporting) - e.g., "Activation", "Modification Indoor"
    /// </summary>
    public string OrderTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Job type (Activation, Modification, Assurance, etc.) - DEPRECATED: Use OrderTypeId/OrderTypeCode instead
    /// This field is no longer populated. Kept in database schema for backward compatibility only.
    /// All new code must use OrderTypeId, OrderTypeCode, and OrderTypeName.
    /// </summary>
    [Obsolete("Use OrderTypeId, OrderTypeCode, and OrderTypeName instead. This field is no longer populated.")]
    public string JobType { get; set; } = string.Empty;

    /// <summary>
    /// KPI result (OnTime, Late, Rework, etc.)
    /// </summary>
    public string? KpiResult { get; set; }

    /// <summary>
    /// Base rate for this job type
    /// </summary>
    public decimal BaseRate { get; set; }

    /// <summary>
    /// KPI adjustment (bonus or penalty)
    /// </summary>
    public decimal KpiAdjustment { get; set; }

    /// <summary>
    /// Final pay (BaseRate + KpiAdjustment)
    /// </summary>
    public decimal FinalPay { get; set; }

    /// <summary>
    /// Period (e.g., "2025-01")
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Status (Draft, Confirmed, Paid)
    /// </summary>
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// Rate source (GponSiCustomRate, GponSiJobRate, RateCard, etc.) - for audit trail
    /// Per RATE_ENGINE.md specification
    /// </summary>
    public string? RateSource { get; set; }

    /// <summary>
    /// Rate ID used for this calculation - for audit trail
    /// </summary>
    public Guid? RateId { get; set; }

    /// <summary>
    /// Confirmed timestamp
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// Paid timestamp
    /// </summary>
    public DateTime? PaidAt { get; set; }

    // Navigation properties
    public PayrollRun? PayrollRun { get; set; }
}

