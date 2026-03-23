using CephasOps.Domain.Billing.Enums;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.SaaS;

/// <summary>
/// Resolves feature flags from TenantFeatureFlags (override) and BillingPlanFeatures (plan).
/// Tenant override wins. If no override: active subscription plan features determine access; no subscription => allow (backward compat).
/// </summary>
public sealed class PlanBasedFeatureFlagService : IFeatureFlagService
{
    private readonly ApplicationDbContext _context;

    public PlanBasedFeatureFlagService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsEnabledAsync(Guid tenantId, string featureKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(featureKey))
            return true;

        var overrideFlag = await _context.TenantFeatureFlags
            .AsNoTracking()
            .Where(f => f.TenantId == tenantId && f.FeatureKey == featureKey)
            .Select(f => (bool?)f.IsEnabled)
            .FirstOrDefaultAsync(cancellationToken);
        if (overrideFlag.HasValue)
            return overrideFlag.Value;

        var subscription = await _context.TenantSubscriptions
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && (s.Status == TenantSubscriptionStatus.Active || s.Status == TenantSubscriptionStatus.Trialing))
            .OrderByDescending(s => s.StartedAtUtc)
            .Select(s => s.BillingPlanId)
            .FirstOrDefaultAsync(cancellationToken);
        if (subscription == default)
            return true;

        var planHasFeature = await _context.BillingPlanFeatures
            .AsNoTracking()
            .AnyAsync(f => f.BillingPlanId == subscription && f.FeatureKey == featureKey, cancellationToken);
        return planHasFeature;
    }
}
