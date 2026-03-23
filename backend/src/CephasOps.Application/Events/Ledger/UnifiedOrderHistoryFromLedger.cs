using CephasOps.Application.Events.Ledger.DTOs;
using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CephasOps.Application.Events.Ledger;

public sealed class UnifiedOrderHistoryFromLedger : IUnifiedOrderHistoryFromLedger
{
    private readonly ApplicationDbContext _context;

    public UnifiedOrderHistoryFromLedger(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UnifiedOrderHistoryItemDto>> GetByOrderIdAsync(
        Guid orderId,
        Guid? companyId,
        DateTime? fromOccurredUtc,
        DateTime? toOccurredUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var families = new[] { LedgerFamilies.WorkflowTransition, LedgerFamilies.OrderLifecycle };
        var q = _context.LedgerEntries.AsNoTracking()
            .Where(e => families.Contains(e.LedgerFamily) && e.EntityType == "Order" && e.EntityId == orderId);
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

        return entries.Select(e => MapToDto(e)).ToList();
    }

    private static UnifiedOrderHistoryItemDto MapToDto(LedgerEntry e)
    {
        string? prior = null, @new = null;
        if (!string.IsNullOrEmpty(e.PayloadSnapshot))
        {
            try
            {
                var root = JsonDocument.Parse(e.PayloadSnapshot).RootElement;
                if (root.TryGetProperty("FromStatus", out var fs)) prior = fs.GetString();
                else if (root.TryGetProperty("PreviousStatus", out var ps)) prior = ps.GetString();
                if (root.TryGetProperty("ToStatus", out var ts)) @new = ts.GetString();
                else if (root.TryGetProperty("NewStatus", out var ns)) @new = ns.GetString();
            }
            catch { /* ignore */ }
        }
        return new UnifiedOrderHistoryItemDto
        {
            LedgerEntryId = e.Id,
            OccurredAtUtc = e.OccurredAtUtc,
            RecordedAtUtc = e.RecordedAtUtc,
            LedgerFamily = e.LedgerFamily,
            EventType = e.EventType,
            Category = e.Category,
            PriorStatus = prior,
            NewStatus = @new,
            SourceEventId = e.SourceEventId,
            OrderingStrategyId = e.OrderingStrategyId,
            TriggeredByUserId = e.TriggeredByUserId
        };
    }
}
