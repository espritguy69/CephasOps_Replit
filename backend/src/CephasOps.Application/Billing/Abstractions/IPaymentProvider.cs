namespace CephasOps.Application.Billing.Abstractions;

/// <summary>Phase 12: Abstraction for tenant subscription payment (Stripe, etc.).</summary>
public interface IPaymentProvider
{
    /// <summary>Create or update a subscription; returns external subscription ID.</summary>
    Task<PaymentProviderResult> CreateOrUpdateSubscriptionAsync(
        Guid tenantId,
        string planSlug,
        string? existingExternalSubscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>Cancel subscription at period end.</summary>
    Task<PaymentProviderResult> CancelSubscriptionAsync(
        string externalSubscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>Charge a one-time amount (e.g. for invoice).</summary>
    Task<PaymentProviderResult> ChargeAsync(
        Guid tenantId,
        decimal amount,
        string currency,
        string description,
        string? idempotencyKey,
        CancellationToken cancellationToken = default);
}

/// <summary>Phase 12: Result of a payment provider call.</summary>
public class PaymentProviderResult
{
    public bool Success { get; set; }
    public string? ExternalId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}
