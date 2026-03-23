namespace CephasOps.Application.SaaS;

/// <summary>
/// Checks whether a feature is enabled for a tenant (e.g. by plan or override).
/// Use for module enablement: Orders, Scheduler, Inventory, Billing, Payroll, Automation, Reports, etc.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Returns true if the feature is enabled for the tenant. When no plan/override exists, implementation may allow by default.
    /// </summary>
    Task<bool> IsEnabledAsync(Guid tenantId, string featureKey, CancellationToken cancellationToken = default);
}
