namespace CephasOps.Application.Subscription;

/// <summary>Evaluates tenant/subscription state for access control (Phase 3).</summary>
public interface ISubscriptionAccessService
{
    /// <summary>Evaluate access for the current request context (company from user). Returns Allow or Deny with reason.</summary>
    Task<SubscriptionAccessResult> GetAccessForCompanyAsync(Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>Evaluate access for a tenant. Use when you have TenantId (e.g. from job context).</summary>
    Task<SubscriptionAccessResult> GetAccessForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Returns true if the company/tenant is allowed to perform write/operational actions.</summary>
    Task<bool> CanPerformWritesAsync(Guid? companyId, CancellationToken cancellationToken = default);
}
