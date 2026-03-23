namespace CephasOps.Application.Billing.Abstractions;

/// <summary>Phase 12: No-op payment provider for development / when no provider configured.</summary>
public class NoOpPaymentProvider : IPaymentProvider
{
    public Task<PaymentProviderResult> CreateOrUpdateSubscriptionAsync(
        Guid tenantId,
        string planSlug,
        string? existingExternalSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        var id = existingExternalSubscriptionId ?? $"sub_noop_{tenantId:N}";
        return Task.FromResult(new PaymentProviderResult { Success = true, ExternalId = id });
    }

    public Task<PaymentProviderResult> CancelSubscriptionAsync(
        string externalSubscriptionId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new PaymentProviderResult { Success = true });

    public Task<PaymentProviderResult> ChargeAsync(
        Guid tenantId,
        decimal amount,
        string currency,
        string description,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new PaymentProviderResult { Success = true, ExternalId = idempotencyKey ?? $"ch_noop_{Guid.NewGuid():N}" });
}
