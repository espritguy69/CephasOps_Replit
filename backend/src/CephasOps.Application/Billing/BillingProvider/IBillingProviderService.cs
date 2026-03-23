namespace CephasOps.Application.Billing.BillingProvider;

/// <summary>Abstraction for billing provider integration (Stripe, etc.): customer lifecycle, subscription attachment, status, webhooks. Implement for concrete providers.</summary>
public interface IBillingProviderService
{
    /// <summary>Create a billing customer for the tenant. Returns external customer ID.</summary>
    Task<BillingProviderResult> CreateCustomerAsync(Guid tenantId, string email, string? companyName, CancellationToken cancellationToken = default);

    /// <summary>Attach a subscription (plan) to the customer. Returns external subscription ID.</summary>
    Task<BillingProviderResult> AttachSubscriptionAsync(Guid tenantId, string externalCustomerId, string planSlug, CancellationToken cancellationToken = default);

    /// <summary>Get current billing status for the tenant (active, past_due, cancelled, etc.).</summary>
    Task<BillingStatusResult> GetBillingStatusAsync(Guid tenantId, string? externalSubscriptionId, CancellationToken cancellationToken = default);

    /// <summary>Handle incoming payment webhook (signature verification and payload handling). Returns 200 if processed.</summary>
    Task<WebhookHandleResult> HandleWebhookAsync(string payload, string signatureHeader, string webhookSecret, CancellationToken cancellationToken = default);
}

/// <summary>Result of a billing provider call.</summary>
public class BillingProviderResult
{
    public bool Success { get; set; }
    public string? ExternalId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>Billing status for a tenant.</summary>
public class BillingStatusResult
{
    public bool Success { get; set; }
    public string? Status { get; set; }
    public DateTime? CurrentPeriodEndUtc { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>Result of webhook handling.</summary>
public class WebhookHandleResult
{
    public bool Processed { get; set; }
    public int HttpStatus { get; set; } = 200;
    public string? ErrorMessage { get; set; }
}
