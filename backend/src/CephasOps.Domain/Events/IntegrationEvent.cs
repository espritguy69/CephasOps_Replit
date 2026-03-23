namespace CephasOps.Domain.Events;

/// <summary>
/// Canonical shape for events that cross the internal/external boundary (outbound integration).
/// The application layer builds a rich transport envelope (e.g. PlatformEventEnvelope) from this or from IDomainEvent.
/// Immutable, tenant-scoped (CompanyId), and versionable.
/// </summary>
public sealed record IntegrationEvent
{
    public Guid EventId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public Guid? CompanyId { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public string PayloadJson { get; init; } = "{}";
    public string? Source { get; init; }
    public string? Version { get; init; }
    public string? CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
}
