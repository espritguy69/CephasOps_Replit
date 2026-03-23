namespace CephasOps.Domain.Integration.Entities;

/// <summary>
/// Deduplication record for inbound webhooks: one successful processing per external idempotency key.
/// Enables safe replay and duplicate suppression.
/// </summary>
public class ExternalIdempotencyRecord
{
    public Guid Id { get; set; }

    /// <summary>Connector key + external id (e.g. provider message id or body hash).</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    public string ConnectorKey { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }

    /// <summary>Reference to the receipt that completed.</summary>
    public Guid InboundWebhookReceiptId { get; set; }

    /// <summary>Null = claimed/in progress; set = completed.</summary>
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
