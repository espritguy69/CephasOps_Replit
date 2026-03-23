namespace CephasOps.Application.Platform.FeatureFlags;

/// <summary>Enterprise: resolve and enforce tenant-level feature flags. Tenant admins cannot enable platform-only features.</summary>
public interface IFeatureFlagService
{
    /// <summary>Returns true if the feature is enabled for the tenant. Resolves tenant from companyId or TenantScope.</summary>
    Task<bool> IsEnabledAsync(string featureKey, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>Throws <see cref="FeatureNotEnabledException"/> if the feature is not enabled for the tenant.</summary>
    Task RequireEnabledAsync(string featureKey, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>Get all flags for a tenant (platform bypass only).</summary>
    Task<IReadOnlyList<TenantFeatureFlagDto>> GetFlagsForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Set a flag for a tenant. Platform admin may set any key; tenant scope cannot set platform-only keys.</summary>
    Task SetFlagAsync(Guid tenantId, string featureKey, bool isEnabled, bool isPlatformAdmin, CancellationToken cancellationToken = default);
}

public class TenantFeatureFlagDto
{
    public string FeatureKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
