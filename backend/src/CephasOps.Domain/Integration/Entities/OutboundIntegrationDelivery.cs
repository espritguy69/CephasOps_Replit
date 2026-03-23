namespace CephasOps.Domain.Integration.Entities;

/// <summary>
/// One outbound delivery: one internal event/message sent to one connector endpoint.
/// Tracks status, attempts, and dead-letter.
/// </summary>
public class OutboundIntegrationDelivery
{
    public Guid Id { get; set; }

    public Guid ConnectorEndpointId { get; set; }
    public Guid? CompanyId { get; set; }

    /// <summary>Internal event/message identifier (e.g. EventId, or synthetic).</summary>
    public Guid SourceEventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public Guid? RootEventId { get; set; }
    public Guid? WorkflowInstanceId { get; set; }
    public Guid? CommandId { get; set; }

    /// <summary>Idempotency key for this delivery (e.g. eventId + endpointId).</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>Pending | Delivered | Failed | DeadLetter | Replaying</summary>
    public string Status { get; set; } = Statuses.Pending;

    /// <summary>Payload sent (or to send). May be truncated for storage.</summary>
    public string PayloadJson { get; set; } = "{}";

    /// <summary>Optional signature header value (redacted in logs).</summary>
    public string? SignatureHeaderValue { get; set; }

    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public string? LastErrorMessage { get; set; }
    public int? LastHttpStatusCode { get; set; }

    /// <summary>True if this delivery was created by a replay operation.</summary>
    public bool IsReplay { get; set; }
    public Guid? ReplayOperationId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public static class Statuses
    {
        public const string Pending = "Pending";
        public const string Delivered = "Delivered";
        public const string Failed = "Failed";
        public const string DeadLetter = "DeadLetter";
        public const string Replaying = "Replaying";
    }
}
