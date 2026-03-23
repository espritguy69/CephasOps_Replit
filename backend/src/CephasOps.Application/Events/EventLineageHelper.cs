using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>
/// Helpers for creating child events with correct ParentEventId, RootEventId, and CausationId.
/// Use when a handler emits a follow-up event so lineage remains intact.
/// </summary>
public static class EventLineageHelper
{
    /// <summary>
    /// Sets lineage on a child event from the causing (parent) event: ParentEventId, RootEventId, CorrelationId, CausationId.
    /// Call before publishing the child event.
    /// </summary>
    public static void SetLineageFrom<TChild>(TChild childEvent, IDomainEvent causingEvent) where TChild : DomainEvent
    {
        childEvent.ParentEventId = causingEvent.EventId;
        childEvent.CausationId = causingEvent.EventId;
        childEvent.CorrelationId = causingEvent.CorrelationId ?? causingEvent.EventId.ToString("N");
        if (causingEvent is IHasRootEvent root && root.RootEventId.HasValue)
            childEvent.RootEventId = root.RootEventId;
        else
            childEvent.RootEventId = causingEvent.EventId;
    }
}
