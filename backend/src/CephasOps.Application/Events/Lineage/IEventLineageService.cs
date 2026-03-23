namespace CephasOps.Application.Events.Lineage;

/// <summary>
/// Reconstructs event correlation trees (root, children, siblings) for debugging and observability.
/// </summary>
public interface IEventLineageService
{
    /// <summary>
    /// Gets the event lineage tree for an event: the event itself, its parent chain, and children by EventId, RootEventId, or CorrelationId.
    /// </summary>
    Task<EventLineageTreeDto?> GetTreeByEventIdAsync(Guid eventId, Guid? scopeCompanyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all events that share the same root (full causality tree under a root event).
    /// </summary>
    Task<EventLineageTreeDto?> GetTreeByRootEventIdAsync(Guid rootEventId, Guid? scopeCompanyId, int maxNodes = 500, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all events that share the same correlation id (workflow/flow group).
    /// </summary>
    Task<EventLineageTreeDto?> GetTreeByCorrelationIdAsync(string correlationId, Guid? scopeCompanyId, int maxNodes = 500, CancellationToken cancellationToken = default);
}
