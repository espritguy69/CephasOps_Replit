using CephasOps.Application.Events;
using CephasOps.Domain.Events;

namespace CephasOps.Application.Integration;

/// <summary>
/// Builds a PlatformEventEnvelope from a domain event for outbound integration publishing.
/// Used by integration forwarding handlers to send domain events to the integration bus.
/// </summary>
public interface IDomainEventToPlatformEnvelopeBuilder
{
    /// <summary>
    /// Builds a platform envelope from the given domain event. EventName is set from EventType; Payload is JSON-serialized event.
    /// </summary>
    PlatformEventEnvelope Build(IDomainEvent domainEvent);
}
