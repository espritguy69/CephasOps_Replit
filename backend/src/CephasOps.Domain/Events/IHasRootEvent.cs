namespace CephasOps.Domain.Events;

/// <summary>
/// Optional interface for domain events that know their root event (origin of the causality chain).
/// When set, the event store persists RootEventId for correlation tree reconstruction.
/// </summary>
public interface IHasRootEvent
{
    Guid? RootEventId { get; }
}
