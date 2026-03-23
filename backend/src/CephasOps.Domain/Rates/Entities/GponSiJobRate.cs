using CephasOps.Domain.Common;

namespace CephasOps.Domain.Rates.Entities;

/// <summary>
/// GponSiJobRate entity - default payout rates to Service Installers for GPON jobs.
/// This is what Cephas pays to SIs by their level (Junior, Senior, Subcon).
/// Per GPON_RATECARDS.md specification.
/// 
/// Rate keying: OrderType + OrderCategory + InstallationMethod + SiLevel
/// </summary>
public class GponSiJobRate : CompanyScopedEntity
{
    /// <summary>
    /// Order Type ID (Activation, Modification, Assurance)
    /// </summary>
    public Guid OrderTypeId { get; set; }

    /// <summary>
    /// Order Category ID (FTTH, FTTO, FTTR, FTTC) - represents the service/technology category
    /// Previously known as InstallationTypeId.
    /// </summary>
    public Guid OrderCategoryId { get; set; }

    /// <summary>
    /// Installation Method ID (Prelaid, Non-Prelaid, SDU, RDF_POLE) - represents site condition
    /// </summary>
    public Guid? InstallationMethodId { get; set; }

    /// <summary>
    /// SI Level (Junior, Senior, Subcon)
    /// </summary>
    public string SiLevel { get; set; } = string.Empty;

    /// <summary>
    /// Partner Group ID (optional - for partner-specific payout rates)
    /// </summary>
    public Guid? PartnerGroupId { get; set; }

    /// <summary>
    /// Payout amount in MYR (what SI earns)
    /// </summary>
    public decimal PayoutAmount { get; set; }

    /// <summary>
    /// Currency (default MYR)
    /// </summary>
    public string Currency { get; set; } = "MYR";

    /// <summary>
    /// Effective from date
    /// </summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// Effective to date (null = no end date)
    /// </summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Whether this rate is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Notes/description
    /// </summary>
    public string? Notes { get; set; }
}

