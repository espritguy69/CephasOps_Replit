using CephasOps.Application.Billing.Subscription.DTOs;

namespace CephasOps.Application.Billing.Subscription.Services;

public interface IBillingPlanService
{
    Task<List<BillingPlanDto>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<BillingPlanDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
