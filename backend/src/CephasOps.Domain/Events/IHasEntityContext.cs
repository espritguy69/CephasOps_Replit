namespace CephasOps.Domain.Events;

/// <summary>
/// Optional interface for domain events that carry entity context (e.g. workflow transition events).
/// Allows event store to index by EntityType/EntityId without depending on concrete event types.
/// </summary>
public interface IHasEntityContext
{
    string? EntityType { get; }
    Guid? EntityId { get; }
}
