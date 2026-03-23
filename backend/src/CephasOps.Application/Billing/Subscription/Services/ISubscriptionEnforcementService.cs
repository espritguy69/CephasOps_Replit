namespace CephasOps.Application.Billing.Subscription.Services;

/// <summary>Enforces subscription validity: expired, trial ended, seat/storage limits. Used by middleware and APIs.</summary>
public interface ISubscriptionEnforcementService
{
    /// <summary>Returns null if tenant has a valid subscription; otherwise returns a short reason (e.g. "Subscription expired").</summary>
    Task<string?> ValidateTenantSubscriptionAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    /// <summary>Returns true if tenant is within seat limit (or unlimited).</summary>
    Task<bool> IsWithinSeatLimitAsync(Guid tenantId, int currentUserCount, CancellationToken cancellationToken = default);

    /// <summary>Returns true if tenant is within storage limit (or unlimited).</summary>
    Task<bool> IsWithinStorageLimitAsync(Guid tenantId, long currentStorageBytes, CancellationToken cancellationToken = default);
}
