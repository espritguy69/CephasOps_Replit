using CephasOps.Application.Trace.DTOs;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Trace;

/// <summary>
/// Assembles operational trace timeline from WorkflowJob, EventStore, and JobRun. Company-scoped.
/// </summary>
public class TraceQueryService : ITraceQueryService
{
    private readonly ApplicationDbContext _context;

    public TraceQueryService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<TraceTimelineDto> GetByCorrelationIdAsync(string correlationId, Guid? scopeCompanyId, TraceQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            return new TraceTimelineDto { LookupKind = "CorrelationId", LookupValue = correlationId, Items = new List<TraceTimelineItemDto>() };

        var items = await BuildTimelineFromCorrelationAsync(correlationId, scopeCompanyId, cancellationToken);
        var ordered = items.OrderBy(x => x.TimestampUtc).ToList();
        return ApplyPagination(new TraceTimelineDto { LookupKind = "CorrelationId", LookupValue = correlationId, Items = ordered }, options);
    }

    /// <inheritdoc />
    public async Task<TraceTimelineDto?> GetByEventIdAsync(Guid eventId, Guid? scopeCompanyId, TraceQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var entry = await _context.EventStore.AsNoTracking()
            .Where(e => e.EventId == eventId)
            .Where(e => !scopeCompanyId.HasValue || e.CompanyId == scopeCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);
        if (entry == null) return null;

        var correlationId = entry.CorrelationId;
        var items = new List<TraceTimelineItemDto>();

        if (!string.IsNullOrEmpty(correlationId))
            items = await BuildTimelineFromCorrelationAsync(correlationId, scopeCompanyId, cancellationToken);
        else
            items = await BuildTimelineFromEventOnlyAsync(entry, scopeCompanyId, cancellationToken);

        var ordered = items.OrderBy(x => x.TimestampUtc).ToList();
        return ApplyPagination(new TraceTimelineDto { LookupKind = "EventId", LookupValue = eventId.ToString(), Items = ordered }, options);
    }

    /// <inheritdoc />
    public async Task<TraceTimelineDto?> GetByJobRunIdAsync(Guid jobRunId, Guid? scopeCompanyId, TraceQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var run = await _context.JobRuns.AsNoTracking()
            .Where(r => r.Id == jobRunId)
            .Where(r => !scopeCompanyId.HasValue || r.CompanyId == scopeCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);
        if (run == null) return null;

        var correlationId = run.CorrelationId;
        var items = string.IsNullOrEmpty(correlationId)
            ? await BuildTimelineFromJobRunOnlyAsync(run, cancellationToken)
            : await BuildTimelineFromCorrelationAsync(correlationId, scopeCompanyId, cancellationToken);

        var ordered = items.OrderBy(x => x.TimestampUtc).ToList();
        return ApplyPagination(new TraceTimelineDto { LookupKind = "JobRunId", LookupValue = jobRunId.ToString(), Items = ordered }, options);
    }

    /// <inheritdoc />
    public async Task<TraceTimelineDto?> GetByWorkflowJobIdAsync(Guid workflowJobId, Guid? scopeCompanyId, TraceQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var wj = await _context.WorkflowJobs.AsNoTracking()
            .Where(w => w.Id == workflowJobId)
            .Where(w => !scopeCompanyId.HasValue || w.CompanyId == scopeCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);
        if (wj == null) return null;

        var correlationId = wj.CorrelationId;
        var items = string.IsNullOrEmpty(correlationId)
            ? await BuildTimelineFromWorkflowJobOnlyAsync(wj, cancellationToken)
            : await BuildTimelineFromCorrelationAsync(correlationId, scopeCompanyId, cancellationToken);

        var ordered = items.OrderBy(x => x.TimestampUtc).ToList();
        return ApplyPagination(new TraceTimelineDto { LookupKind = "WorkflowJobId", LookupValue = workflowJobId.ToString(), Items = ordered }, options);
    }

    /// <inheritdoc />
    public async Task<TraceTimelineDto> GetByEntityAsync(string entityType, Guid entityId, Guid? scopeCompanyId, TraceQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var items = new List<TraceTimelineItemDto>();

        var workflowJobs = await _context.WorkflowJobs.AsNoTracking()
            .Where(w => w.EntityType == entityType && w.EntityId == entityId)
            .Where(w => !scopeCompanyId.HasValue || w.CompanyId == scopeCompanyId.Value)
            .ToListAsync(cancellationToken);
        foreach (var w in workflowJobs)
            items.AddRange(WorkflowJobToTimelineItems(w));

        var events = await _context.EventStore.AsNoTracking()
            .Where(e => e.EntityType == entityType && e.EntityId == entityId)
            .Where(e => !scopeCompanyId.HasValue || e.CompanyId == scopeCompanyId.Value)
            .ToListAsync(cancellationToken);
        foreach (var e in events)
            items.AddRange(EventStoreToTimelineItems(e));

        var jobRuns = await _context.JobRuns.AsNoTracking()
            .Where(r => r.RelatedEntityType == entityType && r.RelatedEntityId == entityId.ToString())
            .Where(r => !scopeCompanyId.HasValue || r.CompanyId == scopeCompanyId.Value)
            .ToListAsync(cancellationToken);
        foreach (var r in jobRuns)
            items.AddRange(JobRunToTimelineItems(r));
        items.AddRange(await AddBackgroundJobQueuedItemsAsync(jobRuns, cancellationToken));

        var ordered = items.OrderBy(x => x.TimestampUtc).ToList();
        return ApplyPagination(new TraceTimelineDto { LookupKind = "Entity", LookupValue = $"{entityType}:{entityId}", Items = ordered }, options);
    }

    /// <inheritdoc />
    public async Task<TraceMetricsDto> GetMetricsAsync(DateTime fromUtc, DateTime toUtc, Guid? scopeCompanyId, CancellationToken cancellationToken = default)
    {
        var eventQuery = _context.EventStore.AsNoTracking()
            .Where(e => e.OccurredAtUtc >= fromUtc && e.OccurredAtUtc < toUtc);
        if (scopeCompanyId.HasValue)
            eventQuery = eventQuery.Where(e => e.CompanyId == scopeCompanyId.Value);

        var failedEvents = await eventQuery.Where(e => e.Status == "Failed").CountAsync(cancellationToken);
        var deadLetterEvents = await eventQuery.Where(e => e.Status == "DeadLetter").CountAsync(cancellationToken);

        var runQuery = _context.JobRuns.AsNoTracking()
            .Where(r => r.StartedAtUtc >= fromUtc && r.StartedAtUtc < toUtc);
        if (scopeCompanyId.HasValue)
            runQuery = runQuery.Where(r => r.CompanyId == scopeCompanyId.Value);

        var failedRuns = await runQuery.Where(r => r.Status == "Failed").CountAsync(cancellationToken);
        var deadLetterRuns = await runQuery.Where(r => r.Status == "DeadLetter").CountAsync(cancellationToken);

        var failedEventCorrelations = await eventQuery
            .Where(e => (e.Status == "Failed" || e.Status == "DeadLetter") && e.CorrelationId != null)
            .Select(e => e.CorrelationId!)
            .Distinct()
            .ToListAsync(cancellationToken);
        var failedRunCorrelations = await runQuery
            .Where(r => (r.Status == "Failed" || r.Status == "DeadLetter") && r.CorrelationId != null)
            .Select(r => r.CorrelationId!)
            .Distinct()
            .ToListAsync(cancellationToken);
        var chainsWithFailures = failedEventCorrelations.Union(failedRunCorrelations).Distinct().Count();

        return new TraceMetricsDto
        {
            FromUtc = fromUtc,
            ToUtc = toUtc,
            FailedEventsCount = failedEvents,
            DeadLetterEventsCount = deadLetterEvents,
            FailedJobRunsCount = failedRuns,
            DeadLetterJobRunsCount = deadLetterRuns,
            CorrelationChainsWithFailuresCount = chainsWithFailures
        };
    }

    private static TraceTimelineDto ApplyPagination(TraceTimelineDto dto, TraceQueryOptions? options)
    {
        if (options?.Limit is not { } limit || limit <= 0) return dto;
        var total = dto.Items.Count;
        dto.TotalCount = total;
        dto.Page = 1;
        dto.PageSize = limit;
        dto.Items = dto.Items.Take(limit).ToList();
        return dto;
    }

    private async Task<List<TraceTimelineItemDto>> BuildTimelineFromCorrelationAsync(string correlationId, Guid? scopeCompanyId, CancellationToken cancellationToken)
    {
        var items = new List<TraceTimelineItemDto>();

        var workflowJobs = await _context.WorkflowJobs.AsNoTracking()
            .Where(w => w.CorrelationId == correlationId)
            .Where(w => !scopeCompanyId.HasValue || w.CompanyId == scopeCompanyId.Value)
            .ToListAsync(cancellationToken);
        foreach (var w in workflowJobs)
            items.AddRange(WorkflowJobToTimelineItems(w));

        var events = await _context.EventStore.AsNoTracking()
            .Where(e => e.CorrelationId == correlationId)
            .Where(e => !scopeCompanyId.HasValue || e.CompanyId == scopeCompanyId.Value)
            .ToListAsync(cancellationToken);
        foreach (var e in events)
            items.AddRange(EventStoreToTimelineItems(e));

        var jobRuns = await _context.JobRuns.AsNoTracking()
            .Where(r => r.CorrelationId == correlationId)
            .Where(r => !scopeCompanyId.HasValue || r.CompanyId == scopeCompanyId.Value)
            .ToListAsync(cancellationToken);
        foreach (var r in jobRuns)
            items.AddRange(JobRunToTimelineItems(r));
        items.AddRange(await AddBackgroundJobQueuedItemsAsync(jobRuns, cancellationToken));

        return items;
    }

    private async Task<List<TraceTimelineItemDto>> BuildTimelineFromEventOnlyAsync(Domain.Events.EventStoreEntry entry, Guid? scopeCompanyId, CancellationToken cancellationToken)
    {
        var items = EventStoreToTimelineItems(entry).ToList();
        var runByEvent = await _context.JobRuns.AsNoTracking()
            .Where(r => r.EventId == entry.EventId)
            .Where(r => !scopeCompanyId.HasValue || r.CompanyId == scopeCompanyId.Value)
            .ToListAsync(cancellationToken);
        foreach (var r in runByEvent)
            items.AddRange(JobRunToTimelineItems(r));
        items.AddRange(await AddBackgroundJobQueuedItemsAsync(runByEvent, cancellationToken));
        return items;
    }

    private async Task<List<TraceTimelineItemDto>> BuildTimelineFromJobRunOnlyAsync(JobRun run, CancellationToken cancellationToken)
    {
        var items = JobRunToTimelineItems(run).ToList();
        items.AddRange(await AddBackgroundJobQueuedItemsAsync(new[] { run }, cancellationToken));
        if (run.EventId.HasValue)
        {
            var ev = await _context.EventStore.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == run.EventId.Value, cancellationToken);
            if (ev != null)
                items.AddRange(EventStoreToTimelineItems(ev));
        }
        return items;
    }

    private Task<List<TraceTimelineItemDto>> BuildTimelineFromWorkflowJobOnlyAsync(WorkflowJob wj, CancellationToken cancellationToken)
    {
        return Task.FromResult(WorkflowJobToTimelineItems(wj).ToList());
    }

    private static IEnumerable<TraceTimelineItemDto> WorkflowJobToTimelineItems(WorkflowJob w)
    {
        yield return new TraceTimelineItemDto
        {
            TimestampUtc = w.CreatedAt,
            CorrelationId = w.CorrelationId,
            CompanyId = w.CompanyId,
            ItemType = TraceTimelineItemTypes.WorkflowTransitionRequested,
            Status = w.State.ToString(),
            Source = "WorkflowEngine",
            EntityType = w.EntityType,
            EntityId = w.EntityId,
            Title = $"Workflow: {w.EntityType} {w.CurrentStatus} → {w.TargetStatus}",
            Summary = null,
            DetailSummary = null,
            RelatedId = w.Id,
            RelatedIdKind = "WorkflowJob",
            ParentRelatedId = null,
            ActorUserId = w.InitiatedByUserId
        };
        if (w.StartedAt.HasValue)
            yield return new TraceTimelineItemDto
            {
                TimestampUtc = w.StartedAt.Value,
                CorrelationId = w.CorrelationId,
                CompanyId = w.CompanyId,
                ItemType = TraceTimelineItemTypes.WorkflowTransitionStarted,
                Status = "Running",
                Source = "WorkflowEngine",
                EntityType = w.EntityType,
                EntityId = w.EntityId,
                Title = $"Workflow started: {w.EntityType}",
                Summary = null,
                DetailSummary = null,
                RelatedId = w.Id,
                RelatedIdKind = "WorkflowJob",
                ParentRelatedId = null,
                ActorUserId = w.InitiatedByUserId
            };
        if (w.CompletedAt.HasValue)
            yield return new TraceTimelineItemDto
            {
                TimestampUtc = w.CompletedAt.Value,
                CorrelationId = w.CorrelationId,
                CompanyId = w.CompanyId,
                ItemType = TraceTimelineItemTypes.WorkflowTransitionCompleted,
                Status = w.State.ToString(),
                Source = "WorkflowEngine",
                EntityType = w.EntityType,
                EntityId = w.EntityId,
                Title = $"Workflow completed: {w.EntityType} {w.TargetStatus}",
                Summary = w.LastError,
                DetailSummary = w.LastError,
                RelatedId = w.Id,
                RelatedIdKind = "WorkflowJob",
                ParentRelatedId = null,
                ActorUserId = w.InitiatedByUserId
            };
    }

    private static IEnumerable<TraceTimelineItemDto> EventStoreToTimelineItems(Domain.Events.EventStoreEntry e)
    {
        yield return new TraceTimelineItemDto
        {
            TimestampUtc = e.OccurredAtUtc,
            CorrelationId = e.CorrelationId,
            CompanyId = e.CompanyId,
            ItemType = TraceTimelineItemTypes.EventEmitted,
            Status = e.Status,
            Source = e.Source,
            EntityType = e.EntityType,
            EntityId = e.EntityId,
            Title = $"Event: {e.EventType}",
            Summary = null,
            DetailSummary = null,
            RelatedId = e.EventId,
            RelatedIdKind = "Event",
            ParentRelatedId = e.ParentEventId,
            ActorUserId = e.TriggeredByUserId
        };
        if (e.ProcessedAtUtc.HasValue)
            yield return new TraceTimelineItemDto
            {
                TimestampUtc = e.ProcessedAtUtc.Value,
                CorrelationId = e.CorrelationId,
                CompanyId = e.CompanyId,
                ItemType = TraceTimelineItemTypes.EventProcessed,
                Status = e.Status,
                Source = e.Source,
                EntityType = e.EntityType,
                EntityId = e.EntityId,
                Title = $"Event processed: {e.EventType}",
                Summary = e.LastError,
                DetailSummary = e.LastError,
                RelatedId = e.EventId,
                RelatedIdKind = "Event",
                ParentRelatedId = null,
                ActorUserId = null,
                HandlerName = e.LastHandler
            };
    }

    private static IEnumerable<TraceTimelineItemDto> JobRunToTimelineItems(JobRun r)
    {
        var isEventHandler = r.EventId.HasValue;
        var startType = isEventHandler ? TraceTimelineItemTypes.EventHandlerStarted : TraceTimelineItemTypes.BackgroundJobStarted;
        var titleStart = isEventHandler ? $"Handler: {r.JobName}" : $"Job: {r.JobName}";
        var source = r.TriggerSource == "EventBus" ? "EventBus" : (r.TriggerSource == "Scheduler" ? "Scheduler" : r.TriggerSource ?? "System");
        var endType = isEventHandler
            ? (r.Status == "Succeeded" ? TraceTimelineItemTypes.EventHandlerSucceeded : TraceTimelineItemTypes.EventHandlerFailed)
            : (r.Status == "Succeeded" ? TraceTimelineItemTypes.BackgroundJobCompleted : TraceTimelineItemTypes.BackgroundJobFailed);
        if (r.CompletedAtUtc.HasValue == false)
            endType = isEventHandler ? TraceTimelineItemTypes.EventHandlerSucceeded : TraceTimelineItemTypes.BackgroundJobCompleted; // still running, use completed type for display

        Guid? entityIdParsed = r.RelatedEntityId != null && Guid.TryParse(r.RelatedEntityId, out var eid) ? eid : null;

        yield return new TraceTimelineItemDto
        {
            TimestampUtc = r.StartedAtUtc,
            CorrelationId = r.CorrelationId,
            CompanyId = r.CompanyId,
            ItemType = startType,
            Status = r.Status,
            Source = source,
            EntityType = r.RelatedEntityType,
            EntityId = entityIdParsed,
            Title = titleStart,
            Summary = null,
            DetailSummary = null,
            RelatedId = r.Id,
            RelatedIdKind = "JobRun",
            ParentRelatedId = r.ParentJobRunId,
            ActorUserId = r.InitiatedByUserId
        };
        if (r.CompletedAtUtc.HasValue)
            yield return new TraceTimelineItemDto
            {
                TimestampUtc = r.CompletedAtUtc.Value,
                CorrelationId = r.CorrelationId,
                CompanyId = r.CompanyId,
                ItemType = endType,
                Status = r.Status,
                Source = source,
                EntityType = r.RelatedEntityType,
                EntityId = entityIdParsed,
                Title = $"{titleStart} — {r.Status}",
                Summary = r.ErrorMessage,
                DetailSummary = r.ErrorMessage,
                RelatedId = r.Id,
                RelatedIdKind = "JobRun",
                ParentRelatedId = r.ParentJobRunId,
                ActorUserId = r.InitiatedByUserId
            };
    }

    /// <summary>Add BackgroundJobQueued items for JobRuns that have BackgroundJobId. Caller must merge and re-sort.</summary>
    private async Task<List<TraceTimelineItemDto>> AddBackgroundJobQueuedItemsAsync(IEnumerable<JobRun> runs, CancellationToken cancellationToken)
    {
        var runList = runs.Where(r => r.BackgroundJobId.HasValue).ToList();
        if (runList.Count == 0) return new List<TraceTimelineItemDto>();

        var jobIds = runList.Select(r => r.BackgroundJobId!.Value).Distinct().ToList();
        var jobs = await _context.BackgroundJobs.AsNoTracking()
            .Where(j => jobIds.Contains(j.Id))
            .ToDictionaryAsync(j => j.Id, cancellationToken);

        var items = new List<TraceTimelineItemDto>();
        foreach (var r in runList)
        {
            if (!jobs.TryGetValue(r.BackgroundJobId!.Value, out var job)) continue;
            Guid? entityIdParsed = r.RelatedEntityId != null && Guid.TryParse(r.RelatedEntityId, out var eid) ? eid : null;
            var source = r.TriggerSource == "Retry" ? "Retry" : (r.TriggerSource == "EventBus" ? "EventBus" : r.TriggerSource ?? "Scheduler");
            items.Add(new TraceTimelineItemDto
            {
                TimestampUtc = job.CreatedAt.Kind == DateTimeKind.Utc ? job.CreatedAt : DateTime.SpecifyKind(job.CreatedAt, DateTimeKind.Utc),
                CorrelationId = r.CorrelationId,
                CompanyId = r.CompanyId,
                ItemType = TraceTimelineItemTypes.BackgroundJobQueued,
                Status = "Queued",
                Source = source,
                EntityType = r.RelatedEntityType,
                EntityId = entityIdParsed,
                Title = $"Job queued: {r.JobName}",
                Summary = null,
                DetailSummary = null,
                RelatedId = r.Id,
                RelatedIdKind = "JobRun",
                ParentRelatedId = r.ParentJobRunId,
                ActorUserId = r.InitiatedByUserId
            });
        }
        return items;
    }
}
