using CephasOps.Application.Events;

namespace CephasOps.Application.Integration;

/// <summary>
/// Maps platform event envelope to outbound integration payload (JSON). Connector-specific mappers can implement this.
/// </summary>
public interface IIntegrationPayloadMapper
{
    /// <summary>Whether this mapper handles the given event type (and optional connector key).</summary>
    bool CanMap(string eventType, string? connectorKey = null);

    /// <summary>Produce the JSON payload to send. Should not include internal-only fields.</summary>
    string MapToPayload(PlatformEventEnvelope envelope, string? connectorKey = null);
}
