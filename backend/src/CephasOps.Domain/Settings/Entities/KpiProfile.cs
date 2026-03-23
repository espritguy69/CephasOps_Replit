using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// KPI profile entity - defines configurable KPI rules for scheduler and payroll
/// </summary>
public class KpiProfile : CompanyScopedEntity
{
    /// <summary>
    /// Profile name (e.g. "TIME Prelaid KPI", "TIME SDU KPI")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Partner ID (nullable)
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// Order type (Activation, Assurance, FTTR, FTTC, SDU, RDFPole, etc.)
    /// </summary>
    public string OrderType { get; set; } = string.Empty;

    /// <summary>
    /// Installation method ID (Prelaid, Non-Prelaid, etc.) - site condition
    /// </summary>
    public Guid? InstallationMethodId { get; set; }

    /// <summary>
    /// Building type ID (DEPRECATED - use InstallationMethodId instead)
    /// This field is kept for backward compatibility only and will be removed in a future version.
    /// Building types (Prelaid, Non-Prelaid, SDU, RDF_POLE) are now represented as Installation Methods.
    /// </summary>
    [Obsolete("BuildingType entity is deprecated. Use InstallationMethodId for site conditions.")]
    public Guid? BuildingTypeId { get; set; }

    /// <summary>
    /// Maximum job duration in minutes (target from Assigned → OrderCompleted)
    /// </summary>
    public int MaxJobDurationMinutes { get; set; }

    /// <summary>
    /// Docket KPI in minutes (target time from OrderCompleted → DocketsReceived)
    /// </summary>
    public int DocketKpiMinutes { get; set; }

    /// <summary>
    /// Maximum reschedules allowed (optional)
    /// </summary>
    public int? MaxReschedulesAllowed { get; set; }

    /// <summary>
    /// Whether this is the default profile for (CompanyId, OrderType)
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Effective from date
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// Effective to date (nullable for ongoing profiles)
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// User ID who created this profile
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// User ID who last updated this profile
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }
}

