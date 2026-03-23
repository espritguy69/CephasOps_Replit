namespace CephasOps.Domain.Events;

/// <summary>
/// Optional interface for domain events that are spawned from another event (child events).
/// When set, the event store persists ParentEventId for lineage and correlation.
/// </summary>
public interface IHasParentEvent
{
    Guid? ParentEventId { get; }
}
