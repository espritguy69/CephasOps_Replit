using CephasOps.Application.Events;
using CephasOps.Application.Events.Replay;
using CephasOps.Application.Rebuild.DTOs;
using CephasOps.Domain.Events;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Rebuild;

/// <summary>
/// Rebuilds WorkflowTransitionHistory from Event Store (WorkflowTransitionCompleted events).
/// FullReplace: delete rows in scope, then insert from source. Supports checkpoint/resume (Phase 2). No side effects.
/// </summary>
public sealed class WorkflowTransitionHistoryFromEventStoreRebuildRunner : IRebuildRunner
{
    private const string EventType = "WorkflowTransitionCompleted";
    private const int MaxEventsDefault = 50_000;
    private const int CheckpointBatchSize = 1000;

    private readonly IEventTypeRegistry _typeRegistry;
    private readonly ILogger<WorkflowTransitionHistoryFromEventStoreRebuildRunner> _logger;

    public string TargetId => RebuildTargetIds.WorkflowTransitionHistoryEventStore;

    public WorkflowTransitionHistoryFromEventStoreRebuildRunner(
        IEventTypeRegistry typeRegistry,
        ILogger<WorkflowTransitionHistoryFromEventStoreRebuildRunner> logger)
    {
        _typeRegistry = typeRegistry;
        _logger = logger;
    }

    public async Task<RebuildPreviewResultDto> PreviewAsync(
        ApplicationDbContext context,
        RebuildRequestDto request,
        Guid? scopeCompanyId,
        CancellationToken cancellationToken = default)
    {
        var q = BuildEventQuery(context, request, scopeCompanyId);
        var sourceCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);
        var targetCount = await GetCurrentTargetCountAsync(context, request, scopeCompanyId, cancellationToken).ConfigureAwait(false);

        return new RebuildPreviewResultDto
        {
            RebuildTargetId = TargetId,
            DisplayName = "Workflow transition history (from Event Store)",
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
        operation.StartedAtUtc ??= DateTime.UtcNow;

        if (request.DryRun)
        {
            var q = BuildEventQuery(context, request, scopeCompanyId);
            operation.SourceRecordCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);
            operation.State = RebuildOperationStates.Completed;
            operation.CompletedAtUtc = DateTime.UtcNow;
            operation.DurationMs = (long)(operation.CompletedAtUtc.Value - operation.StartedAtUtc!.Value).TotalMilliseconds;
            operation.Notes = "Dry run; no changes applied.";
            return;
        }

        var isResume = operation.ResumeRequired && operation.LastProcessedEventId.HasValue && operation.LastProcessedOccurredAtUtc.HasValue;

        if (!isResume)
        {
            // FullReplace: delete target rows in scope (same scope as source query)
            var deleteQuery = context.WorkflowTransitionHistory.AsQueryable();
            var company = request.CompanyId ?? scopeCompanyId;
            if (company.HasValue)
                deleteQuery = deleteQuery.Where(h => h.CompanyId == company.Value);
            if (request.FromOccurredAtUtc.HasValue)
                deleteQuery = deleteQuery.Where(h => h.OccurredAtUtc >= request.FromOccurredAtUtc.Value);
            if (request.ToOccurredAtUtc.HasValue)
                deleteQuery = deleteQuery.Where(h => h.OccurredAtUtc <= request.ToOccurredAtUtc.Value);

            var rowsDeleted = await deleteQuery.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            operation.RowsDeleted = rowsDeleted;
            operation.ProcessedCountAtLastCheckpoint = 0;
            operation.ResumeRequired = false;
        }

        var companyScope = request.CompanyId ?? scopeCompanyId;
        var totalInserted = operation.RowsInserted;
        DateTime? lastOccurred = operation.LastProcessedOccurredAtUtc;
        Guid? lastEventId = operation.LastProcessedEventId;

        while (true)
        {
            var q = BuildEventQuery(context, request, scopeCompanyId);
            if (lastOccurred.HasValue && lastEventId.HasValue)
                q = q.Where(e => e.OccurredAtUtc > lastOccurred.Value || (e.OccurredAtUtc == lastOccurred.Value && e.EventId.CompareTo(lastEventId.Value) > 0));

            var batch = await q
                .OrderBy(e => e.OccurredAtUtc)
                .ThenBy(e => e.EventId)
                .Take(CheckpointBatchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (batch.Count == 0)
                break;

            if (!operation.SourceRecordCount.HasValue)
                operation.SourceRecordCount = await BuildEventQuery(context, request, scopeCompanyId).CountAsync(cancellationToken).ConfigureAwait(false);

            foreach (var entry in batch)
            {
                var domainEvent = _typeRegistry.Deserialize(entry.EventType, entry.Payload) as WorkflowTransitionCompletedEvent;
                if (domainEvent == null)
                    continue;

                context.WorkflowTransitionHistory.Add(new WorkflowTransitionHistoryEntry
                {
                    EventId = domainEvent.EventId,
                    WorkflowJobId = domainEvent.WorkflowJobId,
                    CompanyId = domainEvent.CompanyId,
                    EntityType = domainEvent.EntityType ?? "",
                    EntityId = domainEvent.EntityId,
                    FromStatus = domainEvent.FromStatus ?? "",
                    ToStatus = domainEvent.ToStatus ?? "",
                    OccurredAtUtc = domainEvent.OccurredAtUtc,
                    CreatedAtUtc = DateTime.UtcNow
                });
                totalInserted++;
            }

            var lastInBatch = batch[^1];
            lastOccurred = lastInBatch.OccurredAtUtc;
            lastEventId = lastInBatch.EventId;

            operation.RowsInserted = totalInserted;
            operation.LastProcessedEventId = lastEventId;
            operation.LastProcessedOccurredAtUtc = lastOccurred;
            operation.ProcessedCountAtLastCheckpoint = totalInserted;
            operation.CheckpointCount++;
            operation.LastCheckpointAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        operation.State = RebuildOperationStates.Completed;
        operation.CompletedAtUtc = DateTime.UtcNow;
        operation.DurationMs = (long)(operation.CompletedAtUtc.Value - operation.StartedAtUtc!.Value).TotalMilliseconds;
        operation.ResumeRequired = false;
        _logger.LogInformation(
            "Rebuild {TargetId} completed. RowsDeleted={Deleted}, RowsInserted={Inserted}, Checkpoints={Checkpoints}",
            TargetId, operation.RowsDeleted, operation.RowsInserted, operation.CheckpointCount);
    }

    private static IQueryable<EventStoreEntry> BuildEventQuery(
        ApplicationDbContext context,
        RebuildRequestDto request,
        Guid? scopeCompanyId)
    {
        var q = context.EventStore.AsNoTracking()
            .Where(e => e.EventType == EventType);

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
