using CephasOps.Domain.Billing.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Platform.FeatureFlags;

/// <summary>Enterprise: tenant-scoped feature flags. Platform-only keys cannot be enabled by tenant admins.</summary>
public class FeatureFlagService : IFeatureFlagService
{
    private static readonly HashSet<string> PlatformOnlyFeaturePrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Platform.",
        "Admin."
    };

    private readonly ApplicationDbContext _context;

    public FeatureFlagService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsEnabledAsync(string featureKey, Guid? companyId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(featureKey)) return false;

        var tenantId = await ResolveTenantIdAsync(companyId, cancellationToken);
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty) return false;

        var flag = await _context.TenantFeatureFlags
            .AsNoTracking()
            .Where(f => f.TenantId == tenantId.Value && f.FeatureKey == featureKey)
            .Select(f => new { f.IsEnabled })
            .FirstOrDefaultAsync(cancellationToken);

        return flag?.IsEnabled ?? false;
    }

    public async Task RequireEnabledAsync(string featureKey, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var enabled = await IsEnabledAsync(featureKey, companyId, cancellationToken);
        if (!enabled)
            throw new FeatureNotEnabledException(featureKey);
    }

    public async Task<IReadOnlyList<TenantFeatureFlagDto>> GetFlagsForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var list = await _context.TenantFeatureFlags
            .AsNoTracking()
            .Where(f => f.TenantId == tenantId)
            .OrderBy(f => f.FeatureKey)
            .Select(f => new TenantFeatureFlagDto
            {
                FeatureKey = f.FeatureKey,
                IsEnabled = f.IsEnabled,
                UpdatedAtUtc = f.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);
        return list;
    }

    public async Task SetFlagAsync(Guid tenantId, string featureKey, bool isEnabled, bool isPlatformAdmin, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(featureKey))
            throw new ArgumentException("Feature key is required.", nameof(featureKey));

        if (!isPlatformAdmin && IsPlatformOnlyFeature(featureKey))
            throw new InvalidOperationException($"Feature '{featureKey}' is a platform-only feature and cannot be enabled by tenant admins.");

        var existing = await _context.TenantFeatureFlags
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.FeatureKey == featureKey, cancellationToken);

        if (existing != null)
        {
            existing.IsEnabled = isEnabled;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            _context.TenantFeatureFlags.Add(new TenantFeatureFlag
            {
                TenantId = tenantId,
                FeatureKey = featureKey,
                IsEnabled = isEnabled,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static bool IsPlatformOnlyFeature(string featureKey)
    {
        return PlatformOnlyFeaturePrefixes.Any(prefix => featureKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<Guid?> ResolveTenantIdAsync(Guid? companyId, CancellationToken cancellationToken)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return null;

        var tenantId = await _context.Companies
            .AsNoTracking()
            .Where(c => c.Id == effectiveCompanyId.Value)
            .Select(c => (Guid?)c.TenantId)
            .FirstOrDefaultAsync(cancellationToken);
        return tenantId;
    }
}
