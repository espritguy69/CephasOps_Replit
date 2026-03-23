using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>
/// Marker for handlers that are projection/read-model updaters only. When replay target is Projection, only these handlers run; other handlers are skipped.
/// Implementations must be idempotent and replay-safe (no side effects beyond updating read models).
/// </summary>
public interface IProjectionEventHandler<in TEvent> : IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
}
