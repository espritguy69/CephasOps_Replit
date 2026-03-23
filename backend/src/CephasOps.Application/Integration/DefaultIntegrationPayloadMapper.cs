using System.Text.Json;
using CephasOps.Application.Events;

namespace CephasOps.Application.Integration;

/// <summary>
/// Maps platform event envelope to a generic JSON payload for outbound integration.
/// Handles all event types unless a more specific mapper is registered.
/// </summary>
public class DefaultIntegrationPayloadMapper : IIntegrationPayloadMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public bool CanMap(string eventType, string? connectorKey = null) => true;

    public string MapToPayload(PlatformEventEnvelope envelope, string? connectorKey = null)
    {
        var dto = new
        {
            envelope.EventId,
            envelope.EventName,
            envelope.EventVersion,
            envelope.OccurredAtUtc,
            envelope.CompanyId,
            envelope.CorrelationId,
            envelope.RootEventId,
            envelope.ParentEventId,
            envelope.SourceService,
            envelope.SourceModule,
            envelope.AggregateType,
            envelope.AggregateId,
            Payload = envelope.Payload
        };
        return JsonSerializer.Serialize(dto, JsonOptions);
    }
}
