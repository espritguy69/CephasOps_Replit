using CephasOps.Application.Events;

namespace CephasOps.Application.Integration;

/// <summary>
/// Outbound integration bus: publishes internal platform events to external systems via connector endpoints.
/// Creates delivery records, applies mapping/signing, dispatches with retry and dead-letter.
/// </summary>
public interface IOutboundIntegrationBus
{
    /// <summary>
    /// Publish a platform event to all matching connector endpoints (by event type and company).
    /// Creates one OutboundIntegrationDelivery per endpoint; actual HTTP dispatch may be async.
    /// </summary>
    Task PublishAsync(PlatformEventEnvelope envelope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatch a single delivery (by id). Used by retry worker or replay. Idempotent by delivery idempotency key.
    /// </summary>
    Task<OutboundDispatchResult> DispatchDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Request replay of failed or dead-letter deliveries matching the filter.
    /// </summary>
    Task<ReplayOutboundResult> ReplayAsync(ReplayOutboundRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of dispatching one outbound delivery.
/// </summary>
public sealed class OutboundDispatchResult
{
    public bool Success { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public bool MovedToDeadLetter { get; set; }
}

/// <summary>
/// Request to replay outbound deliveries.
/// </summary>
public sealed class ReplayOutboundRequest
{
    public Guid? ConnectorEndpointId { get; set; }
    public Guid? CompanyId { get; set; }
    public string? EventType { get; set; }
    public string? Status { get; set; } // Failed, DeadLetter
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int MaxCount { get; set; } = 100;
}

/// <summary>
/// Result of a replay operation.
/// </summary>
public sealed class ReplayOutboundResult
{
    public int Dispatched { get; set; }
    public int Failed { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = new List<string>();
}
