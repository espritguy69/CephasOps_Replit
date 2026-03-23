using CephasOps.Domain.Common;

namespace CephasOps.Domain.Billing.Entities;

/// <summary>
/// Billing ratecard entity - defines billing rates (what partner pays us)
/// Rates are determined by: Department + Partner + Service Category + Installation Method + Job Type
/// </summary>
public class BillingRatecard : CompanyScopedEntity
{
    /// <summary>
    /// Department ID - rates can vary by department
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Partner Group ID (TIME, CELCOM_DIGI, U_MOBILE) - for group-level rates
    /// Per PARTNER_MODULE.md: Rate lookup uses partnerGroupId first, then partnerId override
    /// </summary>
    public Guid? PartnerGroupId { get; set; }

    /// <summary>
    /// Partner ID (TM Direct, Celcom Fibre, Digi SME, etc.) - for channel-specific overrides
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// Order type ID (Job Type - Activation, Assurance, etc.)
    /// </summary>
    public Guid? OrderTypeId { get; set; }

    /// <summary>
    /// Service Category - FTTH, FTTO, FTTR, FTTC
    /// Different pricing tiers for different service products
    /// </summary>
    public string? ServiceCategory { get; set; }

    /// <summary>
    /// Installation Method ID - Prelaid, Non-Prelaid, SDU, RDF Pole
    /// Links to InstallationMethod entity (Site Condition)
    /// </summary>
    public Guid? InstallationMethodId { get; set; }

    /// <summary>
    /// Building type (legacy - for backward compatibility)
    /// </summary>
    public string? BuildingType { get; set; }

    /// <summary>
    /// Description / Notes
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Rate amount per job (in RM)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Tax rate (0, 0.06 for SST, etc.)
    /// </summary>
    public decimal TaxRate { get; set; } = 0;

    /// <summary>
    /// Whether this ratecard is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Effective from date
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// Effective to date (null = still valid)
    /// </summary>
    public DateTime? EffectiveTo { get; set; }
}

