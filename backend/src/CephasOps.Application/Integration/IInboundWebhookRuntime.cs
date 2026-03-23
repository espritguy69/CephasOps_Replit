namespace CephasOps.Application.Integration;

/// <summary>
/// Inbound webhook runtime: receive request → verify → persist → idempotency → normalize → dispatch to application.
/// </summary>
public interface IInboundWebhookRuntime
{
    /// <summary>
    /// Process an inbound webhook request. Verification and idempotency are applied; then handler is invoked.
    /// Returns receipt id and status for response.
    /// </summary>
    Task<InboundWebhookResult> ProcessAsync(InboundWebhookRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Raw inbound request as received at the transport.
/// </summary>
public sealed class InboundWebhookRequest
{
    public string ConnectorKey { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string? SignatureHeader { get; set; }
    public string? TimestampHeader { get; set; }
    public string? ExternalEventId { get; set; }
    public string RequestBody { get; set; } = string.Empty;
    public string? ContentType { get; set; }
}

/// <summary>
/// Result of processing one inbound webhook.
/// </summary>
public sealed class InboundWebhookResult
{
    public Guid ReceiptId { get; set; }
    public bool Accepted { get; set; }
    public bool VerificationPassed { get; set; }
    public bool IdempotencyReused { get; set; }
    public string? FailureReason { get; set; }
    public int? SuggestedHttpStatusCode { get; set; }
}
