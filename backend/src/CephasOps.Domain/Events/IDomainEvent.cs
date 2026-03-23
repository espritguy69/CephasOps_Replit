namespace CephasOps.Domain.Events;

/// <summary>
/// Platform-standard envelope for domain events. Enables event bus, handlers, and replay to work with any event type.
/// All events must support: EventId, EventType, Version, CompanyId, CorrelationId, CausationId, EntityType, EntityId, OccurredAtUtc, Source.
/// ActorType/ActorId optional via IHasActor; TriggeredByUserId is legacy actor (user) id.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    string EventType { get; }
    /// <summary>Contract version for payload (e.g. "1"). Used for version-safe handling and PayloadVersion in store.</summary>
    string? Version { get; }
    DateTime OccurredAtUtc { get; }
    string? CorrelationId { get; }
    Guid? CompanyId { get; }
    /// <summary>Event or command that caused this event (causation chain).</summary>
    Guid? CausationId { get; }
    Guid? TriggeredByUserId { get; }
    string? Source { get; }
}
