namespace CephasOps.Domain.Notifications.Entities;

/// <summary>
/// Persistent notification delivery work. Created by notification boundary when delivery is requested (e.g. from OrderStatusChangedEvent).
/// Worker claims pending rows, sends via channel adapters, records attempt history and status.
/// Phase 2 Notifications extraction.
/// </summary>
public class NotificationDispatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    /// <summary>Channel: Sms, WhatsApp, Email, InApp.</summary>
    public string Channel { get; set; } = string.Empty;
    /// <summary>Recipient: phone number, email, or UserId (for InApp).</summary>
    public string Target { get; set; } = string.Empty;
    /// <summary>Template code/key for rendering.</summary>
    public string? TemplateKey { get; set; }
    /// <summary>JSON payload for template placeholders and context.</summary>
    public string? PayloadJson { get; set; }
    /// <summary>Pending | Processing | Sent | Failed | DeadLetter.</summary>
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 5;
    public DateTime? NextRetryAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public string? LastError { get; set; }
    public DateTime? LastErrorAtUtc { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? CausationId { get; set; }
    public Guid? SourceEventId { get; set; }
    /// <summary>Optional idempotency key (e.g. eventId + channel + target) to avoid duplicate sends.</summary>
    public string? IdempotencyKey { get; set; }
    /// <summary>Node that last claimed this row (worker lease).</summary>
    public string? ProcessingNodeId { get; set; }
    public DateTime? ProcessingLeaseExpiresAtUtc { get; set; }
}
