using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>
/// Default event bus implementation. Delegates to IDomainEventDispatcher for publish and dispatch.
/// Persistence and handler execution (including idempotency and replay behavior) are unchanged.
/// </summary>
public sealed class EventBus : IEventBus
{
    private readonly IDomainEventDispatcher _dispatcher;

    public EventBus(IDomainEventDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <inheritdoc />
    public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent
    {
        return _dispatcher.PublishAsync(domainEvent, alreadyStored: false, cancellationToken);
    }

    /// <inheritdoc />
    public Task DispatchAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent
    {
        return _dispatcher.DispatchToHandlersAsync(domainEvent, cancellationToken);
    }
}
