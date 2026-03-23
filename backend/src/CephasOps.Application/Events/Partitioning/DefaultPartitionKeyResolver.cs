using CephasOps.Domain.Events;

namespace CephasOps.Application.Events.Partitioning;

/// <summary>
/// Default partition key strategy: CompanyId first (tenant isolation), then EntityId (aggregate), then CorrelationId (workflow), then EventId (no sharing).
/// Ensures ordering per tenant/aggregate/workflow while allowing parallelism across them.
/// </summary>
public sealed class DefaultPartitionKeyResolver : IPartitionKeyResolver
{
    private const string PrefixCompany = "c:";
    private const string PrefixEntity = "e:";
    private const string PrefixCorrelation = "k:";
    private const string PrefixEvent = "v:";

    /// <inheritdoc />
    public string? GetPartitionKey(IDomainEvent domainEvent)
    {
        if (domainEvent.CompanyId.HasValue)
            return PrefixCompany + domainEvent.CompanyId.Value.ToString("N");

        if (domainEvent is IHasEntityContext entityContext && entityContext.EntityId.HasValue)
            return PrefixEntity + (entityContext.EntityType ?? "?") + ":" + entityContext.EntityId.Value.ToString("N");

        if (!string.IsNullOrEmpty(domainEvent.CorrelationId))
            return PrefixCorrelation + domainEvent.CorrelationId;

        return PrefixEvent + domainEvent.EventId.ToString("N");
    }
}
