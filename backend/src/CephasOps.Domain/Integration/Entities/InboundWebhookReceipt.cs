namespace CephasOps.Domain.Integration.Entities;

/// <summary>
/// One received inbound webhook: persisted for idempotency, diagnostics, and replay-safe ingestion.
/// </summary>
public class InboundWebhookReceipt
{
    public Guid Id { get; set; }

    public Guid ConnectorEndpointId { get; set; }
    public Guid? CompanyId { get; set; }

    /// <summary>External idempotency key: provider message id, or hash of body/signature.</summary>
    public string ExternalIdempotencyKey { get; set; } = string.Empty;

    /// <summary>Provider-supplied event/message id if any.</summary>
    public string? ExternalEventId { get; set; }

    public string ConnectorKey { get; set; } = string.Empty;
    public string? MessageType { get; set; }

    /// <summary>Received | Verified | Processing | Processed | VerificationFailed | HandlerFailed | DeadLetter</summary>
    public string Status { get; set; } = Statuses.Received;

    /// <summary>Raw payload (or truncated). Stored for replay and diagnostics.</summary>
    public string PayloadJson { get; set; } = "{}";

    public string? CorrelationId { get; set; }
    public bool VerificationPassed { get; set; }
    public string? VerificationFailureReason { get; set; }

    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? HandlerErrorMessage { get; set; }
    public int? HandlerAttemptCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public static class Statuses
    {
        public const string Received = "Received";
        public const string Verified = "Verified";
        public const string Processing = "Processing";
        public const string Processed = "Processed";
        public const string VerificationFailed = "VerificationFailed";
        public const string HandlerFailed = "HandlerFailed";
        public const string DeadLetter = "DeadLetter";
    }
}
