using CephasOps.Domain.Integration.Entities;

namespace CephasOps.Application.Integration;

/// <summary>
/// Resolves connector definitions and endpoints for routing. Company-scoped and global.
/// </summary>
public interface IConnectorRegistry
{
    Task<ConnectorDefinition?> GetDefinitionByKeyAsync(string connectorKey, CancellationToken cancellationToken = default);
    Task<ConnectorEndpoint?> GetEndpointAsync(Guid endpointId, CancellationToken cancellationToken = default);
    /// <summary>Get endpoints that subscribe to this event type (outbound). Optionally filter by company.</summary>
    Task<IReadOnlyList<ConnectorEndpoint>> GetOutboundEndpointsForEventAsync(string eventType, Guid? companyId, CancellationToken cancellationToken = default);
    /// <summary>Get endpoint for inbound webhook by connector key (and optional company).</summary>
    Task<ConnectorEndpoint?> GetInboundEndpointAsync(string connectorKey, Guid? companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConnectorDefinition>> ListDefinitionsAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ConnectorEndpoint> Items, int TotalCount)> ListEndpointsAsync(Guid? connectorDefinitionId, Guid? companyId, int skip, int take, CancellationToken cancellationToken = default);
}
