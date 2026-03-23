using System.Diagnostics;
using CephasOps.Application.Events.Partitioning;
using CephasOps.Domain.Events;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Events;

/// <summary>
/// Builds EventStoreEnvelopeMetadata from a domain event using partition resolver and configured defaults.
/// Optionally captures TraceId/SpanId from current Activity.
/// </summary>
public sealed class PlatformEventEnvelopeBuilder : IPlatformEventEnvelopeBuilder
{
    private readonly IPartitionKeyResolver _partitionKeyResolver;
    private readonly PlatformEventEnvelopeOptions _options;

    public PlatformEventEnvelopeBuilder(
        IPartitionKeyResolver partitionKeyResolver,
        IOptions<PlatformEventEnvelopeOptions>? options = null)
    {
        _partitionKeyResolver = partitionKeyResolver;
        _options = options?.Value ?? new PlatformEventEnvelopeOptions();
    }

    /// <inheritdoc />
    public EventStoreEnvelopeMetadata Build(IDomainEvent domainEvent)
    {
        var partitionKey = _partitionKeyResolver.GetPartitionKey(domainEvent);
        var rootEventId = domainEvent is IHasRootEvent root ? root.RootEventId : null;
        var activity = Activity.Current;
        return new EventStoreEnvelopeMetadata
        {
            PartitionKey = partitionKey,
            RootEventId = rootEventId,
            CapturedAtUtc = DateTime.UtcNow,
            SourceService = _options.SourceService,
            SourceModule = _options.SourceModule,
            Priority = _options.DefaultPriority,
            TraceId = activity?.TraceId.ToString(),
            SpanId = activity?.SpanId.ToString()
        };
    }
}
