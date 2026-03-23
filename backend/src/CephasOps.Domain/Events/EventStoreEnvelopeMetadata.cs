namespace CephasOps.Domain.Events;

/// <summary>
/// Optional metadata for the platform event envelope when appending to the store.
/// Built by the application layer (e.g. from IPartitionKeyResolver and options) and passed to IEventStore.
/// </summary>
public sealed class EventStoreEnvelopeMetadata
{
    public string? PartitionKey { get; set; }
    public Guid? RootEventId { get; set; }
    public string? ReplayId { get; set; }
    public string? SourceService { get; set; }
    public string? SourceModule { get; set; }
    public DateTime? CapturedAtUtc { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? Priority { get; set; }
}
