using System.Text.Json;
using CephasOps.Application.Events.Ledger;
using CephasOps.Application.Rebuild.DTOs;
using CephasOps.Domain.Events;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Rebuild;

/// <summary>
/// Rebuilds WorkflowTransitionHistory from Event Ledger (WorkflowTransition family).
/// FullReplace: delete rows in scope, then insert from ledger. No side effects.
/// </summary>
public sealed class WorkflowTransitionHistoryFromLedgerRebuildRunner : IRebuildRunner
{
    private const int MaxEntriesDefault = 50_000;

    private readonly ILogger<WorkflowTransitionHistoryFromLedgerRebuildRunner> _logger;

    public string TargetId => RebuildTargetIds.WorkflowTransitionHistoryLedger;

    public WorkflowTransitionHistoryFromLedgerRebuildRunner(
        ILogger<WorkflowTransitionHistoryFromLedgerRebuildRunner> logger)
    {
        _logger = logger;
    }

    public async Task<RebuildPreviewResultDto> PreviewAsync(
        ApplicationDbContext context,
        RebuildRequestDto request,
        Guid? scopeCompanyId,
        CancellationToken cancellationToken = default)
    {
        var q = BuildLedgerQuery(context, request, scopeCompanyId);
        var sourceCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);
        var targetCount = await GetCurrentTargetCountAsync(context, request, scopeCompanyId, cancellationToken).ConfigureAwait(false);

        return new RebuildPreviewResultDto
        {
            RebuildTargetId = TargetId,
            DisplayName = "Workflow transition history (from Event Ledger)",
            SourceRecordCount = sourceCount,
            CurrentTargetRowCount = targetCount,
            RebuildStrategy = RebuildStrategies.FullReplace,
            ScopeDescription = BuildScopeDescription(request, scopeCompanyId),
            DryRun = true
        };
    }

    public async Task ExecuteAsync(
        ApplicationDbContext context,
        RebuildOperation operation,
        RebuildRequestDto request,
        Guid? scopeCompanyId,
        CancellationToken cancellationToken = default)
    {
        operation.State = RebuildOperationStates.Running;
        operation.StartedAtUtc = DateTime.UtcNow;

        var q = BuildLedgerQuery(context, request, scopeCompanyId);
        var entries = await q
            .OrderBy(e => e.OccurredAtUtc)
            .ThenBy(e => e.Id)
            .Take(MaxEntriesDefault)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        operation.SourceRecordCount = entries.Count;

        if (request.DryRun)
        {
            operation.State = RebuildOperationStates.Completed;
            operation.CompletedAtUtc = DateTime.UtcNow;
            operation.DurationMs = (long)(operation.CompletedAtUtc.Value - operation.StartedAtUtc!.Value).TotalMilliseconds;
            operation.Notes = "Dry run; no changes applied.";
            return;
        }

        var company = request.CompanyId ?? scopeCompanyId;
        var deleteQuery = context.WorkflowTransitionHistory.AsQueryable();
        if (company.HasValue)
            deleteQuery = deleteQuery.Where(h => h.CompanyId == company.Value);
        if (request.FromOccurredAtUtc.HasValue)
            deleteQuery = deleteQuery.Where(h => h.OccurredAtUtc >= request.FromOccurredAtUtc.Value);
        if (request.ToOccurredAtUtc.HasValue)
            deleteQuery = deleteQuery.Where(h => h.OccurredAtUtc <= request.ToOccurredAtUtc.Value);

        var rowsDeleted = await deleteQuery.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        operation.RowsDeleted = rowsDeleted;

        var rowsInserted = 0;
        foreach (var e in entries)
        {
            if (!e.SourceEventId.HasValue)
                continue;

            var (fromStatus, toStatus, workflowJobId) = ParsePayload(e.PayloadSnapshot);

            context.WorkflowTransitionHistory.Add(new WorkflowTransitionHistoryEntry
            {
                EventId = e.SourceEventId.Value,
                WorkflowJobId = workflowJobId ?? Guid.Empty,
                CompanyId = e.CompanyId,
                EntityType = e.EntityType ?? "",
                EntityId = e.EntityId ?? Guid.Empty,
                FromStatus = fromStatus ?? "",
                ToStatus = toStatus ?? "",
                OccurredAtUtc = e.OccurredAtUtc,
                CreatedAtUtc = DateTime.UtcNow
            });
            rowsInserted++;
        }

        operation.RowsInserted = rowsInserted;
        operation.State = RebuildOperationStates.Completed;
        operation.CompletedAtUtc = DateTime.UtcNow;
        operation.DurationMs = (long)(operation.CompletedAtUtc.Value - operation.StartedAtUtc!.Value).TotalMilliseconds;
        _logger.LogInformation(
            "Rebuild {TargetId} completed. RowsDeleted={Deleted}, RowsInserted={Inserted}",
            TargetId, operation.RowsDeleted, operation.RowsInserted);
    }

    private static IQueryable<LedgerEntry> BuildLedgerQuery(
        ApplicationDbContext context,
        RebuildRequestDto request,
        Guid? scopeCompanyId)
    {
        var q = context.LedgerEntries.AsNoTracking()
            .Where(e => e.LedgerFamily == LedgerFamilies.WorkflowTransition && e.SourceEventId != null);

        var company = request.CompanyId ?? scopeCompanyId;
        if (company.HasValue)
            q = q.Where(e => e.CompanyId == company.Value);
        if (request.FromOccurredAtUtc.HasValue)
            q = q.Where(e => e.OccurredAtUtc >= request.FromOccurredAtUtc.Value);
        if (request.ToOccurredAtUtc.HasValue)
            q = q.Where(e => e.OccurredAtUtc <= request.ToOccurredAtUtc.Value);

        return q;
    }

    private static async Task<int> GetCurrentTargetCountAsync(
        ApplicationDbContext context,
        RebuildRequestDto request,
        Guid? scopeCompanyId,
        CancellationToken cancellationToken)
    {
        var q = context.WorkflowTransitionHistory.AsNoTracking().AsQueryable();
        var company = request.CompanyId ?? scopeCompanyId;
        if (company.HasValue)
            q = q.Where(h => h.CompanyId == company.Value);
        if (request.FromOccurredAtUtc.HasValue)
            q = q.Where(h => h.OccurredAtUtc >= request.FromOccurredAtUtc.Value);
        if (request.ToOccurredAtUtc.HasValue)
            q = q.Where(h => h.OccurredAtUtc <= request.ToOccurredAtUtc.Value);
        return await q.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    private static (string? FromStatus, string? ToStatus, Guid? WorkflowJobId) ParsePayload(string? payloadSnapshot)
    {
        if (string.IsNullOrEmpty(payloadSnapshot))
            return (null, null, null);
        try
        {
            var doc = JsonDocument.Parse(payloadSnapshot);
            var root = doc.RootElement;
            string? fromStatus = null, toStatus = null;
            Guid? workflowJobId = null;
            if (root.TryGetProperty("FromStatus", out var fs))
                fromStatus = fs.GetString();
            if (root.TryGetProperty("ToStatus", out var ts))
                toStatus = ts.GetString();
            if (root.TryGetProperty("WorkflowJobId", out var wj) && wj.ValueKind == JsonValueKind.String && Guid.TryParse(wj.GetString(), out var wjGuid))
                workflowJobId = wjGuid;
            return (fromStatus, toStatus, workflowJobId);
        }
        catch
        {
            return (null, null, null);
        }
    }

    private static string? BuildScopeDescription(RebuildRequestDto request, Guid? scopeCompanyId)
    {
        var parts = new List<string>();
        if (request.CompanyId.HasValue || scopeCompanyId.HasValue)
            parts.Add("Company scoped");
        if (request.FromOccurredAtUtc.HasValue)
            parts.Add($"From {request.FromOccurredAtUtc:O}");
        if (request.ToOccurredAtUtc.HasValue)
            parts.Add($"To {request.ToOccurredAtUtc:O}");
        return parts.Count > 0 ? string.Join("; ", parts) : "Full rebuild";
    }
}
