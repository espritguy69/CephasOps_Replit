namespace CephasOps.Application.Events;

/// <summary>
/// Canonical platform event envelope for transport, persistence, and observability.
/// Aligns with EventStoreEntry and supports version-tolerant serialization.
/// </summary>
public sealed class PlatformEventEnvelope
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? EventVersion { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime? CapturedAtUtc { get; set; }
    public string? SourceService { get; set; }
    public string? SourceModule { get; set; }
    public Guid? CompanyId { get; set; }
    public string? AggregateType { get; set; }
    public Guid? AggregateId { get; set; }
    public string? PartitionKey { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? CausationId { get; set; }
    public Guid? ParentEventId { get; set; }
    public Guid? RootEventId { get; set; }
    public string? ReplayId { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? Priority { get; set; }
    public int AttemptCount { get; set; }
    public string? IdempotencyKey { get; set; }
    public IReadOnlyDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public string Payload { get; set; } = "{}";
    public string? PayloadSchemaVersion { get; set; }
}
