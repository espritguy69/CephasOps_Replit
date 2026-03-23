using CephasOps.Application.Events.Ledger.DTOs;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CephasOps.Application.Events.Ledger;

public sealed class WorkflowTransitionTimelineFromLedger : IWorkflowTransitionTimelineFromLedger
{
    private readonly ApplicationDbContext _context;

    public WorkflowTransitionTimelineFromLedger(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<WorkflowTransitionTimelineItemDto>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        Guid? companyId,
        DateTime? fromOccurredUtc,
        DateTime? toOccurredUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var q = _context.LedgerEntries.AsNoTracking()
            .Where(e => e.LedgerFamily == LedgerFamilies.WorkflowTransition && e.EntityType == entityType && e.EntityId == entityId);
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
            string? fromStatus = null, toStatus = null;
            Guid? workflowJobId = null;
            if (!string.IsNullOrEmpty(e.PayloadSnapshot))
            {
                try
                {
                    var doc = JsonDocument.Parse(e.PayloadSnapshot);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("FromStatus", out var fs)) fromStatus = fs.GetString();
                    if (root.TryGetProperty("ToStatus", out var ts)) toStatus = ts.GetString();
                    if (root.TryGetProperty("WorkflowJobId", out var wj) && wj.ValueKind == JsonValueKind.String && Guid.TryParse(wj.GetString(), out var wjGuid))
                        workflowJobId = wjGuid;
                }
                catch { /* ignore */ }
            }
            return new WorkflowTransitionTimelineItemDto
            {
                LedgerEntryId = e.Id,
                SourceEventId = e.SourceEventId ?? Guid.Empty,
                OccurredAtUtc = e.OccurredAtUtc,
                RecordedAtUtc = e.RecordedAtUtc,
                CompanyId = e.CompanyId,
                EntityType = e.EntityType ?? "",
                EntityId = e.EntityId ?? Guid.Empty,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                WorkflowJobId = workflowJobId,
                PayloadSnapshot = e.PayloadSnapshot
            };
        }).ToList();
    }
}
