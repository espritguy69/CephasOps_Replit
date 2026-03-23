namespace CephasOps.Application.Rates.DTOs;

/// <summary>
/// Base work rate DTO for GPON layered pricing (Phase 2).
/// </summary>
public class BaseWorkRateDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid RateGroupId { get; set; }
    public string? RateGroupName { get; set; }
    public string? RateGroupCode { get; set; }
    public Guid? OrderCategoryId { get; set; }
    public string? OrderCategoryName { get; set; }
    public string? OrderCategoryCode { get; set; }
    public Guid? ServiceProfileId { get; set; }
    public string? ServiceProfileName { get; set; }
    public string? ServiceProfileCode { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public string? InstallationMethodName { get; set; }
    public string? InstallationMethodCode { get; set; }
    public Guid? OrderSubtypeId { get; set; }
    public string? OrderSubtypeName { get; set; }
    public string? OrderSubtypeCode { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "MYR";
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Create base work rate request.
/// </summary>
public class CreateBaseWorkRateDto
{
    public Guid RateGroupId { get; set; }
    /// <summary>Exact order category (takes precedence in resolution). At most one of OrderCategoryId or ServiceProfileId.</summary>
    public Guid? OrderCategoryId { get; set; }
    /// <summary>Service profile for shared pricing. At most one of OrderCategoryId or ServiceProfileId.</summary>
    public Guid? ServiceProfileId { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public Guid? OrderSubtypeId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "MYR";
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

/// <summary>
/// Update base work rate request.
/// Set clear flags to remove optional dimension (e.g. clear Order Subtype override).
/// </summary>
public class UpdateBaseWorkRateDto
{
    public Guid? RateGroupId { get; set; }
    public Guid? OrderCategoryId { get; set; }
    public Guid? ServiceProfileId { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public Guid? OrderSubtypeId { get; set; }
    /// <summary>When true, set OrderCategoryId to null (broad fallback).</summary>
    public bool ClearOrderCategoryId { get; set; }
    /// <summary>When true, set ServiceProfileId to null.</summary>
    public bool ClearServiceProfileId { get; set; }
    /// <summary>When true, set InstallationMethodId to null (broad fallback).</summary>
    public bool ClearInstallationMethodId { get; set; }
    /// <summary>When true, set OrderSubtypeId to null (parent-only mapping).</summary>
    public bool ClearOrderSubtypeId { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int? Priority { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Filter for listing base work rates.
/// </summary>
public class BaseWorkRateListFilter
{
    public Guid? RateGroupId { get; set; }
    public Guid? OrderCategoryId { get; set; }
    public Guid? ServiceProfileId { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public Guid? OrderSubtypeId { get; set; }
    public bool? IsActive { get; set; }
}
