using System.Text.Json;
using CephasOps.Application.Events;
using CephasOps.Domain.Events;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Integration;

/// <summary>
/// Builds PlatformEventEnvelope from IDomainEvent for outbound integration. Uses domain EventType as EventName; serializes event to JSON for Payload.
/// </summary>
public sealed class DomainEventToPlatformEnvelopeBuilder : IDomainEventToPlatformEnvelopeBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };
    private readonly PlatformEventEnvelopeOptions? _options;

    public DomainEventToPlatformEnvelopeBuilder(IOptions<PlatformEventEnvelopeOptions>? options = null)
    {
        _options = options?.Value;
    }

    /// <inheritdoc />
    public PlatformEventEnvelope Build(IDomainEvent domainEvent)
    {
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions);
        var envelope = new PlatformEventEnvelope
        {
            EventId = domainEvent.EventId,
            EventName = domainEvent.EventType,
            EventVersion = domainEvent.Version ?? "1",
            OccurredAtUtc = domainEvent.OccurredAtUtc,
            CapturedAtUtc = DateTime.UtcNow,
            CompanyId = domainEvent.CompanyId,
            CorrelationId = domainEvent.CorrelationId,
            CausationId = domainEvent.CausationId,
            Payload = payload,
            PayloadSchemaVersion = domainEvent.Version ?? "1",
            SourceService = _options?.SourceService,
            SourceModule = _options?.SourceModule
        };
        if (domainEvent is IHasParentEvent parent)
            envelope.ParentEventId = parent.ParentEventId;
        if (domainEvent is IHasRootEvent root)
            envelope.RootEventId = root.RootEventId;
        if (domainEvent is IHasEntityContext entity)
        {
            envelope.AggregateType = entity.EntityType;
            envelope.AggregateId = entity.EntityId;
        }
        return envelope;
    }
}
