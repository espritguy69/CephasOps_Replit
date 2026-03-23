using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>
/// Central event bus for the internal event platform. Publishes domain events (persist then dispatch or dispatch-only),
/// preserves tenant context (CompanyId), and supports same-transaction emission via IEventStore.AppendInCurrentTransaction.
/// Subscribe by registering <see cref="IDomainEventHandler{TEvent}"/> (or <see cref="IEventHandler{TEvent}"/>) in DI.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publish an event: persist to event store (if not already stored), then dispatch to all registered handlers.
    /// Tenant context (CompanyId) is carried on the event; callers must set it when creating the event.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent;

    /// <summary>
    /// Dispatch an event to handlers only, without persisting. Use when the event was already stored
    /// (e.g. by EventStoreDispatcherHostedService or when replaying). Tenant context is on the event.
    /// </summary>
    Task DispatchAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent;
}
