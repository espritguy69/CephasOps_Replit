using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>
/// Handler for a specific domain event type. Supports async execution, retry safety, and idempotency.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// Handle the event. Implementations should be idempotent and safe for retry.
    /// </summary>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Alias for <see cref="IDomainEventHandler{TEvent}"/>. Use for formal event platform handler contract.
/// Handlers must be idempotent, retry-safe, and tenant-aware (event carries CompanyId).
/// </summary>
public interface IEventHandler<in TEvent> : IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
}
