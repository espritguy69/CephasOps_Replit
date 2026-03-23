using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>
/// Enqueues a background job to run async event handlers for the given event. One job per event; the processor runs all IAsyncEventSubscriber handlers for that event.
/// </summary>
public interface IAsyncEventEnqueuer
{
    /// <summary>Enqueue a job to process async handlers for the event. Payload includes eventId for loading from store.</summary>
    Task EnqueueAsync(Guid eventId, IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
