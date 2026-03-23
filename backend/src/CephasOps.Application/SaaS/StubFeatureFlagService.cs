namespace CephasOps.Application.SaaS;

/// <summary>
/// Stub implementation: all features enabled for all tenants until plan-based or override logic is added.
/// </summary>
public sealed class StubFeatureFlagService : IFeatureFlagService
{
    public Task<bool> IsEnabledAsync(Guid tenantId, string featureKey, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
