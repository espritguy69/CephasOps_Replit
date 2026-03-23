using CephasOps.Domain.Common;

namespace CephasOps.Domain.Pnl.Entities;

/// <summary>
/// P&amp;L period entity
/// </summary>
public class PnlPeriod : CompanyScopedEntity
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
    /// Whether this period is locked
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// User ID who created this period
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Last recalculated timestamp
    /// </summary>
    public DateTime? LastRecalculatedAt { get; set; }

    // Navigation properties
    public ICollection<PnlFact> PnlFacts { get; set; } = new List<PnlFact>();
}

