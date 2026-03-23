using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Billing.BillingProvider;

/// <summary>Stub implementation of billing provider. No external integration; use for development or until a real provider is wired.</summary>
public class StubBillingProviderService : IBillingProviderService
{
    private readonly ILogger<StubBillingProviderService> _logger;

    public StubBillingProviderService(ILogger<StubBillingProviderService> logger)
    {
        _logger = logger;
    }

    public Task<BillingProviderResult> CreateCustomerAsync(Guid tenantId, string email, string? companyName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("StubBillingProvider: CreateCustomer tenantId={TenantId}, email={Email}", tenantId, email);
        return Task.FromResult(new BillingProviderResult
        {
            Success = true,
            ExternalId = "stub-customer-" + tenantId.ToString("N")[..8]
        });
    }

    public Task<BillingProviderResult> AttachSubscriptionAsync(Guid tenantId, string externalCustomerId, string planSlug, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("StubBillingProvider: AttachSubscription tenantId={TenantId}, plan={Plan}", tenantId, planSlug);
        return Task.FromResult(new BillingProviderResult
        {
            Success = true,
            ExternalId = "stub-sub-" + tenantId.ToString("N")[..8]
        });
    }

    public Task<BillingStatusResult> GetBillingStatusAsync(Guid tenantId, string? externalSubscriptionId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("StubBillingProvider: GetBillingStatus tenantId={TenantId}", tenantId);
        return Task.FromResult(new BillingStatusResult
        {
            Success = true,
            Status = "active",
            CurrentPeriodEndUtc = DateTime.UtcNow.AddMonths(1)
        });
    }

    public Task<WebhookHandleResult> HandleWebhookAsync(string payload, string signatureHeader, string webhookSecret, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("StubBillingProvider: HandleWebhook (no-op)");
        return Task.FromResult(new WebhookHandleResult { Processed = true, HttpStatus = 200 });
    }
}
