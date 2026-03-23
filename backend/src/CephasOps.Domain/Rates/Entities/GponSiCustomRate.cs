using CephasOps.Domain.Common;

namespace CephasOps.Domain.Rates.Entities;

/// <summary>
/// GponSiCustomRate entity - per-SI custom rate overrides for GPON jobs.
/// This takes highest priority in rate resolution (Custom → Payout → Revenue).
/// Per GPON_RATECARDS.md specification.
/// 
/// Rate keying: ServiceInstallerId + OrderType + OrderCategory + InstallationMethod
/// </summary>
public class GponSiCustomRate : CompanyScopedEntity
{
    /// <summary>
    /// Service Installer ID (the specific SI getting the custom rate)
    /// </summary>
    public Guid ServiceInstallerId { get; set; }

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
    /// Partner Group ID (optional - for partner-specific custom rates)
    /// </summary>
    public Guid? PartnerGroupId { get; set; }

    /// <summary>
    /// Custom payout amount in MYR (overrides default payout rate)
    /// </summary>
    public decimal CustomPayoutAmount { get; set; }

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
    /// Whether this custom rate is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Reason for the custom rate (for audit purposes)
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Who approved this custom rate
    /// </summary>
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>
    /// When the custom rate was approved
    /// </summary>
    public DateTime? ApprovedAt { get; set; }
}

