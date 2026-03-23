using CephasOps.Application.Events.Ledger.DTOs;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CephasOps.Application.Events.Ledger;

public sealed class OrderTimelineFromLedger : IOrderTimelineFromLedger
{
    private readonly ApplicationDbContext _context;

    public OrderTimelineFromLedger(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<OrderTimelineItemDto>> GetByOrderIdAsync(
        Guid orderId,
        Guid? companyId,
        DateTime? fromOccurredUtc,
        DateTime? toOccurredUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var q = _context.LedgerEntries.AsNoTracking()
            .Where(e => e.LedgerFamily == LedgerFamilies.OrderLifecycle && e.EntityType == "Order" && e.EntityId == orderId);
        if (companyId.HasValue)
            q = q.Where(e => e.CompanyId == companyId.Value);
        if (fromOccurredUtc.HasValue)
            q = q.Where(e => e.OccurredAtUtc >= fromOccurredUtc.Value);
        if (toOccurredUtc.HasValue)
            q = q.Where(e => e.OccurredAtUtc <= toOccurredUtc.Value);

        var entries = await q
            .OrderBy(e => e.OccurredAtUtc)
            .ThenBy(e => e.Id)
            .Take(Math.Clamp(limit, 1, 500))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return entries.Select(e =>
        {
            string? priorStatus = null, newStatus = null;
            if (!string.IsNullOrEmpty(e.PayloadSnapshot))
            {
                try
                {
                    var doc = JsonDocument.Parse(e.PayloadSnapshot);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("PreviousStatus", out var ps)) priorStatus = ps.GetString();
                    if (root.TryGetProperty("NewStatus", out var ns)) newStatus = ns.GetString();
                }
                catch { /* ignore */ }
            }
            return new OrderTimelineItemDto
            {
                LedgerEntryId = e.Id,
                SourceEventId = e.SourceEventId,
                OccurredAtUtc = e.OccurredAtUtc,
                RecordedAtUtc = e.RecordedAtUtc,
                LedgerFamily = e.LedgerFamily,
                EventType = e.EventType,
                Category = e.Category,
                PriorStatus = priorStatus,
                NewStatus = newStatus,
                TriggeredByUserId = e.TriggeredByUserId,
                OrderingStrategyId = e.OrderingStrategyId,
                PayloadSnapshot = e.PayloadSnapshot
            };
        }).ToList();
    }
}
