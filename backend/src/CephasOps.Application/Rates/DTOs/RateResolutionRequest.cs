namespace CephasOps.Application.Rates.DTOs;

/// <summary>
/// Request DTO for GPON rate resolution.
/// Per RATE_ENGINE.md: OrderType + OrderCategory + InstallationMethod + PartnerGroup
/// </summary>
public class GponRateResolutionRequest
{
    /// <summary>
    /// Order Type ID (Activation, Modification, Assurance)
    /// </summary>
    public Guid OrderTypeId { get; set; }

    /// <summary>
    /// Order Category ID (FTTH, FTTO, FTTR, FTTC) - represents the service/technology category.
    /// Previously known as InstallationTypeId.
    /// </summary>
    public Guid OrderCategoryId { get; set; }

    /// <summary>
    /// Installation Method ID (Prelaid, Non-Prelaid, SDU, RDF_POLE) - represents site condition
    /// </summary>
    public Guid? InstallationMethodId { get; set; }

    /// <summary>
    /// Partner Group ID (TIME, CELCOM_DIGI, U_MOBILE)
    /// </summary>
    public Guid? PartnerGroupId { get; set; }

    /// <summary>
    /// Partner ID - for channel-specific rate overrides
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// Service Installer ID - for custom rate lookup
    /// </summary>
    public Guid? ServiceInstallerId { get; set; }

    /// <summary>
    /// SI Level (Junior, Senior, Subcon) - for default payout rate lookup
    /// </summary>
    public string? SiLevel { get; set; }

    /// <summary>
    /// Reference date for rate validity check (defaults to now)
    /// </summary>
    public DateTime? ReferenceDate { get; set; }

    /// <summary>
    /// Optional company scope for Base Work Rate and Rate Group mapping lookup.
    /// When null, resolution still runs but matches rows with null CompanyId or any company.
    /// </summary>
    public Guid? CompanyId { get; set; }
}

/// <summary>
/// Universal rate resolution request using flexible dimensions.
/// Per RATE_ENGINE.md specification.
/// </summary>
public class UniversalRateResolutionRequest
{
    /// <summary>
    /// Rate context (GPON_JOB, NWO_SCOPE, BARBER_SERVICE, etc.)
    /// </summary>
    public string RateContext { get; set; } = string.Empty;

    /// <summary>
    /// Rate kind (REVENUE, PAYOUT, BONUS, COMMISSION)
    /// </summary>
    public string RateKind { get; set; } = string.Empty;

    /// <summary>
    /// Vertical ID (optional)
    /// </summary>
    public Guid? VerticalId { get; set; }

    /// <summary>
    /// Department ID (optional)
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// First dimension value (e.g., OrderType for GPON)
    /// </summary>
    public string? Dimension1 { get; set; }

    /// <summary>
    /// Second dimension value (e.g., OrderCategory for GPON - previously InstallationType)
    /// </summary>
    public string? Dimension2 { get; set; }

    /// <summary>
    /// Third dimension value (e.g., InstallationMethod for GPON)
    /// </summary>
    public string? Dimension3 { get; set; }

    /// <summary>
    /// Fourth dimension value (e.g., PartnerGroup for GPON)
    /// </summary>
    public string? Dimension4 { get; set; }

    /// <summary>
    /// Partner Group ID - for partner-specific rates
    /// </summary>
    public Guid? PartnerGroupId { get; set; }

    /// <summary>
    /// Partner ID - for channel-specific rate overrides
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// User ID - for custom rate lookup
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Reference date for rate validity check (defaults to now)
    /// </summary>
    public DateTime? ReferenceDate { get; set; }
}

