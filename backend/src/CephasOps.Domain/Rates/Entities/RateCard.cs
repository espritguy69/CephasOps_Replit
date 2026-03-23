using CephasOps.Domain.Common;
using CephasOps.Domain.Rates.Enums;

namespace CephasOps.Domain.Rates.Entities;

/// <summary>
/// RateCard entity - defines the context and type of a rate card.
/// This is the header table that groups rate card lines.
/// Per RATE_ENGINE.md specification.
/// </summary>
public class RateCard : CompanyScopedEntity
{
    /// <summary>
    /// Vertical ID (ISP, BARBERSHOP, TRAVEL, SPA, HQ)
    /// </summary>
    public Guid? VerticalId { get; set; }

    /// <summary>
    /// Department ID (GPON, NWO, CWO, BARBER_OPS, TRAVEL_OPS, etc.)
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Rate context - what domain this rate card applies to
    /// (GPON_JOB, NWO_SCOPE, BARBER_SERVICE, TRAVEL_PACKAGE, etc.)
    /// </summary>
    public RateContext RateContext { get; set; }

    /// <summary>
    /// Rate kind - type of rate (REVENUE, PAYOUT, BONUS, COMMISSION)
    /// </summary>
    public RateKind RateKind { get; set; }

    /// <summary>
    /// Human-readable name (e.g., "GPON Revenue v1", "Barber Payout 2025")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Effective from date (null = always valid)
    /// </summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// Effective to date (null = no end date)
    /// </summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Whether this rate card is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property - rate card lines
    /// </summary>
    public virtual ICollection<RateCardLine> Lines { get; set; } = new List<RateCardLine>();
}

