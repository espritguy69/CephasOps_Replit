using CephasOps.Domain.Events;

namespace CephasOps.Application.Events.Partitioning;

/// <summary>
/// Resolves a partition key for an event to support ordering within a partition and concurrency across partitions.
/// </summary>
public interface IPartitionKeyResolver
{
    /// <summary>
    /// Gets the partition key for the event. Events with the same key are processed in order within that partition;
    /// different partitions can be processed in parallel.
    /// </summary>
    /// <param name="domainEvent">The domain event (may implement IHasEntityContext, have CompanyId, CorrelationId).</param>
    /// <returns>Partition key string, or null to use default (e.g. single partition).</returns>
    string? GetPartitionKey(IDomainEvent domainEvent);
}
