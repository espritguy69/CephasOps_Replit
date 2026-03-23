using CephasOps.Domain.Events;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Maps event type names to .NET types for deserialization when replaying from store.
/// </summary>
public interface IEventTypeRegistry
{
    /// <summary>Returns the type for the given event type name, or null if unknown.</summary>
    Type? GetEventType(string eventTypeName);

    /// <summary>Deserialize payload JSON to IDomainEvent, or null if type unknown or deserialization fails.</summary>
    IDomainEvent? Deserialize(string eventTypeName, string payloadJson);
}
