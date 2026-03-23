using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>
/// Dispatches domain events to registered handlers. Supports in-process and optional background dispatch.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Publish an event: persist (if event store enabled and not alreadyStored), then dispatch to all handlers for this event type.
    /// When alreadyStored is true, skip append and assume event is already in the store (e.g. claimed by EventStoreDispatcherWorker).
    /// </summary>
    Task PublishAsync<TEvent>(TEvent domainEvent, bool alreadyStored = false, CancellationToken cancellationToken = default) where TEvent : IDomainEvent;

    /// <summary>
    /// Dispatch to handlers only (no persistence). Use when event was already stored or persistence is not used.
    /// </summary>
    Task DispatchToHandlersAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent;
}
