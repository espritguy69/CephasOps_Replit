using CephasOps.Application.Billing.Subscription.DTOs;

namespace CephasOps.Application.Billing.Subscription.Services;

public interface ITenantSubscriptionService
{
    Task<List<TenantSubscriptionDto>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantSubscriptionDto?> GetActiveAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantSubscriptionDto?> SubscribeAsync(Guid tenantId, string planSlug, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
