using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Events.Lineage;

/// <summary>
/// Reconstructs event correlation trees from EventStore by RootEventId, ParentEventId, and CorrelationId.
/// </summary>
public sealed class EventLineageService : IEventLineageService
{
    private readonly ApplicationDbContext _context;

    public EventLineageService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<EventLineageTreeDto?> GetTreeByEventIdAsync(Guid eventId, Guid? scopeCompanyId, CancellationToken cancellationToken = default)
    {
        var entry = await _context.EventStore.AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (entry == null) return null;
        if (scopeCompanyId.HasValue && entry.CompanyId != scopeCompanyId.Value) return null;

        var rootId = entry.RootEventId ?? entry.EventId;
        return await GetTreeByRootEventIdAsync(rootId, scopeCompanyId, 500, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EventLineageTreeDto?> GetTreeByRootEventIdAsync(Guid rootEventId, Guid? scopeCompanyId, int maxNodes = 500, CancellationToken cancellationToken = default)
    {
        var q = _context.EventStore.AsNoTracking()
            .Where(e => e.RootEventId == rootEventId || e.EventId == rootEventId);
        if (scopeCompanyId.HasValue)
            q = q.Where(e => e.CompanyId == scopeCompanyId.Value);

        var list = await q.OrderBy(e => e.OccurredAtUtc).ThenBy(e => e.EventId)
            .Take(maxNodes + 1)
            .Select(e => new EventLineageNodeDto
            {
                EventId = e.EventId,
                EventType = e.EventType,
                OccurredAtUtc = e.OccurredAtUtc,
                Status = e.Status,
                ParentEventId = e.ParentEventId,
                CausationId = e.CausationId,
                PartitionKey = e.PartitionKey,
                ReplayId = e.ReplayId,
                Depth = 0
            })
            .ToListAsync(cancellationToken);

        var truncated = list.Count > maxNodes;
        if (truncated) list = list.Take(maxNodes).ToList();

        var correlationId = list.FirstOrDefault()?.EventId == rootEventId
            ? (await _context.EventStore.AsNoTracking().Where(e => e.EventId == rootEventId).Select(e => e.CorrelationId).FirstOrDefaultAsync(cancellationToken))
            : list.FirstOrDefault()?.EventId.ToString();

        return new EventLineageTreeDto
        {
            RootEventId = rootEventId,
            CorrelationId = correlationId,
            Nodes = list,
            TotalCount = list.Count,
            Truncated = truncated
        };
    }

    /// <inheritdoc />
    public async Task<EventLineageTreeDto?> GetTreeByCorrelationIdAsync(string correlationId, Guid? scopeCompanyId, int maxNodes = 500, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(correlationId)) return null;

        var q = _context.EventStore.AsNoTracking()
            .Where(e => e.CorrelationId != null && e.CorrelationId == correlationId);
        if (scopeCompanyId.HasValue)
            q = q.Where(e => e.CompanyId == scopeCompanyId.Value);

        var list = await q.OrderBy(e => e.OccurredAtUtc).ThenBy(e => e.EventId)
            .Take(maxNodes + 1)
            .Select(e => new EventLineageNodeDto
            {
                EventId = e.EventId,
                EventType = e.EventType,
                OccurredAtUtc = e.OccurredAtUtc,
                Status = e.Status,
                ParentEventId = e.ParentEventId,
                CausationId = e.CausationId,
                PartitionKey = e.PartitionKey,
                ReplayId = e.ReplayId,
                Depth = 0
            })
            .ToListAsync(cancellationToken);

        var truncated = list.Count > maxNodes;
        if (truncated) list = list.Take(maxNodes).ToList();

        var rootId = list.OrderBy(e => e.OccurredAtUtc).FirstOrDefault()?.EventId;

        return new EventLineageTreeDto
        {
            RootEventId = rootId,
            CorrelationId = correlationId,
            Nodes = list,
            TotalCount = list.Count,
            Truncated = truncated
        };
    }
}
