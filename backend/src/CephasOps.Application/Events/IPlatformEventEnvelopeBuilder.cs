using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>
/// Builds platform event envelope metadata for appending to the event store.
/// Ensures consistent PartitionKey, RootEventId, SourceService, etc. across publishers.
/// </summary>
public interface IPlatformEventEnvelopeBuilder
{
    /// <summary>
    /// Build envelope metadata for the given domain event (e.g. for AppendAsync / AppendInCurrentTransaction).
    /// </summary>
    EventStoreEnvelopeMetadata Build(IDomainEvent domainEvent);
}
