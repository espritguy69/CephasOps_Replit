using CephasOps.Domain.Common;

namespace CephasOps.Domain.Rates.Entities;

/// <summary>
/// GponPartnerJobRate entity - revenue rates from partners for GPON jobs.
/// This is what Cephas earns from the partner (TIME, CELCOM_DIGI, U_MOBILE).
/// Per GPON_RATECARDS.md specification.
/// 
/// Rate keying: OrderType + OrderCategory + InstallationMethod + PartnerGroup
/// </summary>
public class GponPartnerJobRate : CompanyScopedEntity
{
    /// <summary>
    /// Partner Group ID (TIME, CELCOM_DIGI, U_MOBILE)
    /// </summary>
    public Guid PartnerGroupId { get; set; }

    /// <summary>
    /// Partner ID (optional - for channel-specific overrides)
    /// </summary>
    public Guid? PartnerId { get; set; }

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
    /// Revenue amount in MYR (what Cephas earns)
    /// </summary>
    public decimal RevenueAmount { get; set; }

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

