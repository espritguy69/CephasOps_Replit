using CephasOps.Domain.Buildings.Entities;
using CephasOps.Domain.Common;
using CephasOps.Domain.Orders.Entities;

namespace CephasOps.Domain.Rates.Entities;

/// <summary>
/// Base work rate for GPON layered pricing (Phase 2 + Service Profile).
/// Defines rate amount by Rate Group; optional Order Category (exact) or Service Profile (shared family); Installation Method; optional Order Subtype.
/// Resolution precedence: exact OrderCategoryId match first, then ServiceProfileId match, then broad fallback.
/// At most one of OrderCategoryId or ServiceProfileId should be set per row (or both null for broad).
/// </summary>
public class BaseWorkRate : CompanyScopedEntity
{
    public Guid RateGroupId { get; set; }
    /// <summary>Exact order category. When set, this row applies only to that category. Takes precedence over ServiceProfileId in resolution.</summary>
    public Guid? OrderCategoryId { get; set; }
    /// <summary>Service profile (shared family). When set, this row applies to any order category mapped to this profile. Used when no exact OrderCategoryId row matches.</summary>
    public Guid? ServiceProfileId { get; set; }
    /// <summary>When null, applies as broad fallback across installation methods.</summary>
    public Guid? InstallationMethodId { get; set; }
    /// <summary>When set, overrides parent-only mapping for this subtype; null = applies to whole rate group.</summary>
    public Guid? OrderSubtypeId { get; set; }

    public decimal Amount { get; set; }
    /// <summary>Currency code (e.g. MYR).</summary>
    public string Currency { get; set; } = "MYR";

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    /// <summary>Higher priority wins when multiple rows match; default 0.</summary>
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public RateGroup RateGroup { get; set; } = null!;
    public OrderCategory? OrderCategory { get; set; }
    public ServiceProfile? ServiceProfile { get; set; }
    public InstallationMethod? InstallationMethod { get; set; }
    public OrderType? OrderSubtype { get; set; }
}
