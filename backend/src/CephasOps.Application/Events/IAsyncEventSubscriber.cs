using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>
/// Marker for handlers that should run asynchronously via background job (when async dispatch is enabled).
/// When not enqueued, they are not invoked in-process; the dispatcher may enqueue a job instead.
/// Implementations must be idempotent and safe for retries.
/// </summary>
public interface IAsyncEventSubscriber<in TEvent> : IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
}
