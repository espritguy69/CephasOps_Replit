using CephasOps.Domain.Common;
using CephasOps.Domain.Rates.Enums;

namespace CephasOps.Domain.Rates.Entities;

/// <summary>
/// RateCardLine entity - defines individual rate entries within a rate card.
/// Uses flexible dimensions (dimension1-4) to support various rate keying scenarios.
/// Per RATE_ENGINE.md specification.
/// 
/// Dimension meanings vary by context:
/// - GPON: OrderType + InstallationType + InstallationMethod + PartnerGroup
/// - NWO: ScopeType + Complexity + PartnerGroup + Region
/// - Barbershop: ServiceCode + BarberLevel + BranchId + DayType
/// - Travel: PackageCode + Season + RoomType + AgentLevel
/// </summary>
public class RateCardLine : CompanyScopedEntity
{
    /// <summary>
    /// Parent rate card ID
    /// </summary>
    public Guid RateCardId { get; set; }

    /// <summary>
    /// First dimension key (e.g., OrderType for GPON, ServiceCode for Barbershop)
    /// </summary>
    public string? Dimension1 { get; set; }

    /// <summary>
    /// Second dimension key (e.g., InstallationType for GPON, BarberLevel for Barbershop)
    /// </summary>
    public string? Dimension2 { get; set; }

    /// <summary>
    /// Third dimension key (e.g., InstallationMethod for GPON, BranchId for Barbershop)
    /// </summary>
    public string? Dimension3 { get; set; }

    /// <summary>
    /// Fourth dimension key (e.g., PartnerGroup for GPON, DayType for Barbershop)
    /// </summary>
    public string? Dimension4 { get; set; }

    /// <summary>
    /// Partner Group ID - for partner-specific rates (optional)
    /// </summary>
    public Guid? PartnerGroupId { get; set; }

    /// <summary>
    /// Partner ID - for channel-specific rate overrides (optional)
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// Rate amount (in MYR or percentage based on PayoutType)
    /// </summary>
    public decimal RateAmount { get; set; }

    /// <summary>
    /// Unit of measure (JOB, METER, SERVICE, PAX, SESSION, DEVICE, etc.)
    /// </summary>
    public UnitOfMeasure UnitOfMeasure { get; set; } = UnitOfMeasure.Job;

    /// <summary>
    /// Currency (default MYR)
    /// </summary>
    public string Currency { get; set; } = "MYR";

    /// <summary>
    /// Payout type - fixed RM or percentage
    /// </summary>
    public PayoutType PayoutType { get; set; } = PayoutType.FixedAmount;

    /// <summary>
    /// Optional extra metadata as JSON
    /// </summary>
    public string? ExtraJson { get; set; }

    /// <summary>
    /// Whether this rate line is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property - parent rate card
    /// </summary>
    public virtual RateCard? RateCard { get; set; }
}

