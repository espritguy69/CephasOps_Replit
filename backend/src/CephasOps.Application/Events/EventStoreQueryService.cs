using CephasOps.Application.Events.DTOs;
using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Events;

/// <summary>
/// Query and list events from the event store. Respects company scope when scopeCompanyId is set.
/// </summary>
public class EventStoreQueryService : IEventStoreQueryService
{
    private readonly ApplicationDbContext _context;
    private readonly IEventStore _eventStore;

    public EventStoreQueryService(ApplicationDbContext context, IEventStore eventStore)
    {
        _context = context;
        _eventStore = eventStore;
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<EventStoreListItemDto> Items, int TotalCount)> GetEventsAsync(EventStoreFilterDto filter, Guid? scopeCompanyId, CancellationToken cancellationToken = default)
    {
        var q = _context.EventStore.AsNoTracking().AsQueryable();
        if (scopeCompanyId.HasValue)
            q = q.Where(e => e.CompanyId == scopeCompanyId.Value);
        if (filter.CompanyId.HasValue)
            q = q.Where(e => e.CompanyId == filter.CompanyId.Value);
        if (!string.IsNullOrEmpty(filter.EventType))
            q = q.Where(e => e.EventType == filter.EventType);
        if (!string.IsNullOrEmpty(filter.Status))
            q = q.Where(e => e.Status == filter.Status);
        if (!string.IsNullOrEmpty(filter.CorrelationId))
            q = q.Where(e => e.CorrelationId != null && e.CorrelationId.Contains(filter.CorrelationId));
        if (!string.IsNullOrEmpty(filter.EntityType))
            q = q.Where(e => e.EntityType == filter.EntityType);
        if (filter.EntityId.HasValue)
            q = q.Where(e => e.EntityId == filter.EntityId.Value);
        if (filter.FromUtc.HasValue)
            q = q.Where(e => e.OccurredAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue)
            q = q.Where(e => e.OccurredAtUtc <= filter.ToUtc.Value);
        if (filter.RetryCountMin.HasValue)
            q = q.Where(e => e.RetryCount >= filter.RetryCountMin.Value);
        if (filter.RetryCountMax.HasValue)
            q = q.Where(e => e.RetryCount <= filter.RetryCountMax.Value);

        var total = await q.CountAsync(cancellationToken);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var skip = (Math.Max(1, filter.Page) - 1) * pageSize;
        var items = await q.OrderByDescending(e => e.OccurredAtUtc)
            .Skip(skip)
            .Take(pageSize)
            .Select(e => new EventStoreListItemDto
            {
                EventId = e.EventId,
                EventType = e.EventType,
                OccurredAtUtc = e.OccurredAtUtc,
                CreatedAtUtc = e.CreatedAtUtc,
                ProcessedAtUtc = e.ProcessedAtUtc,
                RetryCount = e.RetryCount,
                Status = e.Status,
                CorrelationId = e.CorrelationId,
                CompanyId = e.CompanyId,
                EntityType = e.EntityType,
                EntityId = e.EntityId,
                LastError = e.LastError,
                LastHandler = e.LastHandler,
                ParentEventId = e.ParentEventId,
                NextRetryAtUtc = e.NextRetryAtUtc,
                ProcessingNodeId = e.ProcessingNodeId,
                ProcessingLeaseExpiresAtUtc = e.ProcessingLeaseExpiresAtUtc,
                LastClaimedAtUtc = e.LastClaimedAtUtc,
                LastClaimedBy = e.LastClaimedBy,
                LastErrorType = e.LastErrorType,
                RootEventId = e.RootEventId,
                PartitionKey = e.PartitionKey,
                ReplayId = e.ReplayId,
                SourceService = e.SourceService,
                SourceModule = e.SourceModule,
                CapturedAtUtc = e.CapturedAtUtc
            })
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<EventStoreListItemDto> Items, int TotalCount)> GetEventsForReplayAsync(
        ReplayRequestDto request,
        Guid? scopeCompanyId,
        int? maxEvents,
        Guid? resumeAfterEventId = null,
        DateTime? resumeAfterOccurredAtUtc = null,
        DateTime? safetyCutoffOccurredAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var q = _context.EventStore.AsNoTracking().AsQueryable();
        if (scopeCompanyId.HasValue)
            q = q.Where(e => e.CompanyId == scopeCompanyId.Value);
        if (request.CompanyId.HasValue)
            q = q.Where(e => e.CompanyId == request.CompanyId.Value);
        if (!string.IsNullOrEmpty(request.EventType))
            q = q.Where(e => e.EventType == request.EventType);
        if (!string.IsNullOrEmpty(request.Status))
            q = q.Where(e => e.Status == request.Status);
        if (!string.IsNullOrEmpty(request.CorrelationId))
            q = q.Where(e => e.CorrelationId != null && e.CorrelationId.Contains(request.CorrelationId));
        if (!string.IsNullOrEmpty(request.EntityType))
            q = q.Where(e => e.EntityType == request.EntityType);
        if (request.EntityId.HasValue)
            q = q.Where(e => e.EntityId == request.EntityId.Value);
        if (request.FromOccurredAtUtc.HasValue)
            q = q.Where(e => e.OccurredAtUtc >= request.FromOccurredAtUtc.Value);
        if (request.ToOccurredAtUtc.HasValue)
            q = q.Where(e => e.OccurredAtUtc <= request.ToOccurredAtUtc.Value);

        // Replay safety window: exclude events newer than cutoff (reduces replay/live overlap and race conditions)
        if (safetyCutoffOccurredAtUtc.HasValue)
            q = q.Where(e => e.OccurredAtUtc <= safetyCutoffOccurredAtUtc.Value);

        // Resume cursor: only events strictly after (OccurredAtUtc, EventId) in ASC order
        if (resumeAfterEventId.HasValue && resumeAfterOccurredAtUtc.HasValue)
        {
            var afterOccurred = resumeAfterOccurredAtUtc.Value;
            var afterId = resumeAfterEventId.Value;
            q = q.Where(e => e.OccurredAtUtc > afterOccurred || (e.OccurredAtUtc == afterOccurred && e.EventId.CompareTo(afterId) > 0));
        }

        var total = await q.CountAsync(cancellationToken);
        var take = Math.Min(maxEvents ?? 5000, 10000);
        // Ascending order for deterministic replay (oldest first)
        var items = await q.OrderBy(e => e.OccurredAtUtc).ThenBy(e => e.EventId)
            .Take(take)
            .Select(e => new EventStoreListItemDto
            {
                EventId = e.EventId,
                EventType = e.EventType,
                OccurredAtUtc = e.OccurredAtUtc,
                CreatedAtUtc = e.CreatedAtUtc,
                ProcessedAtUtc = e.ProcessedAtUtc,
                RetryCount = e.RetryCount,
                Status = e.Status,
                CorrelationId = e.CorrelationId,
                CompanyId = e.CompanyId,
                EntityType = e.EntityType,
                EntityId = e.EntityId,
                LastError = e.LastError,
                LastHandler = e.LastHandler
            })
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    /// <inheritdoc />
    public async Task<EventStoreDetailDto?> GetByEventIdAsync(Guid eventId, Guid? scopeCompanyId, CancellationToken cancellationToken = default)
    {
        var entry = await _eventStore.GetByEventIdAsync(eventId, cancellationToken);
        if (entry == null) return null;
        if (scopeCompanyId.HasValue && entry.CompanyId != scopeCompanyId.Value)
            return null;
        return new EventStoreDetailDto
        {
            EventId = entry.EventId,
            EventType = entry.EventType,
            Payload = entry.Payload,
            OccurredAtUtc = entry.OccurredAtUtc,
            CreatedAtUtc = entry.CreatedAtUtc,
            ProcessedAtUtc = entry.ProcessedAtUtc,
            RetryCount = entry.RetryCount,
            Status = entry.Status,
            CorrelationId = entry.CorrelationId,
            CompanyId = entry.CompanyId,
            TriggeredByUserId = entry.TriggeredByUserId,
            Source = entry.Source,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            LastError = entry.LastError,
            LastErrorAtUtc = entry.LastErrorAtUtc,
            LastHandler = entry.LastHandler,
            ParentEventId = entry.ParentEventId,
            ProcessingNodeId = entry.ProcessingNodeId,
            ProcessingLeaseExpiresAtUtc = entry.ProcessingLeaseExpiresAtUtc,
            LastClaimedAtUtc = entry.LastClaimedAtUtc,
            LastClaimedBy = entry.LastClaimedBy,
            LastErrorType = entry.LastErrorType,
            RootEventId = entry.RootEventId,
            PartitionKey = entry.PartitionKey,
            ReplayId = entry.ReplayId,
            SourceService = entry.SourceService,
            SourceModule = entry.SourceModule,
            CapturedAtUtc = entry.CapturedAtUtc
        };
    }

    /// <inheritdoc />
    public async Task<EventStoreDashboardDto> GetDashboardAsync(DateTime? fromUtc, DateTime? toUtc, Guid? scopeCompanyId, CancellationToken cancellationToken = default)
    {
        var start = fromUtc ?? DateTime.UtcNow.Date;
        var end = toUtc ?? DateTime.UtcNow.AddDays(1);
        var q = _context.EventStore.AsNoTracking()
            .Where(e => e.OccurredAtUtc >= start && e.OccurredAtUtc < end);
        if (scopeCompanyId.HasValue)
            q = q.Where(e => e.CompanyId == scopeCompanyId.Value);

        var eventsToday = await q.CountAsync(cancellationToken);
        var processed = await q.Where(e => e.Status == "Processed").CountAsync(cancellationToken);
        var failed = await q.Where(e => e.Status == "Failed").CountAsync(cancellationToken);
        var deadLetter = await q.Where(e => e.Status == "DeadLetter").CountAsync(cancellationToken);
        var totalRetries = await q.SumAsync(e => e.RetryCount, cancellationToken);

        var topFailing = await q.Where(e => e.Status == "Failed" || e.Status == "DeadLetter")
            .GroupBy(e => e.EventType)
            .Select(g => new EventTypeCountDto { EventType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(cancellationToken);

        var topCompanies = await q.Where(e => e.Status == "Failed" || e.Status == "DeadLetter")
            .GroupBy(e => e.CompanyId)
            .Select(g => new CompanyEventCountDto
            {
                CompanyId = g.Key,
                FailedCount = g.Count(e => e.Status == "Failed"),
                DeadLetterCount = g.Count(e => e.Status == "DeadLetter")
            })
            .OrderByDescending(x => x.FailedCount + x.DeadLetterCount)
            .Take(10)
            .ToListAsync(cancellationToken);

        return new EventStoreDashboardDto
        {
            EventsToday = eventsToday,
            ProcessedCount = processed,
            FailedCount = failed,
            DeadLetterCount = deadLetter,
            ProcessedPercent = eventsToday > 0 ? 100.0 * processed / eventsToday : 0,
            FailedPercent = eventsToday > 0 ? 100.0 * (failed + deadLetter) / eventsToday : 0,
            TotalRetryCount = totalRetries,
            TopFailingEventTypes = topFailing,
            TopFailingCompanies = topCompanies
        };
    }

    /// <inheritdoc />
    public async Task<EventStoreRelatedLinksDto?> GetRelatedLinksAsync(Guid eventId, Guid? scopeCompanyId, CancellationToken cancellationToken = default)
    {
        var entry = await _eventStore.GetByEventIdAsync(eventId, cancellationToken);
        if (entry == null) return null;
        if (scopeCompanyId.HasValue && entry.CompanyId != scopeCompanyId.Value)
            return null;

        var correlationId = entry.CorrelationId;
        var jobRunsQuery = _context.JobRuns.AsNoTracking()
            .Where(r => r.EventId == eventId || (correlationId != null && r.CorrelationId == correlationId));
        if (scopeCompanyId.HasValue)
            jobRunsQuery = jobRunsQuery.Where(r => r.CompanyId == scopeCompanyId.Value);
        var jobRuns = await jobRunsQuery
            .OrderByDescending(r => r.StartedAtUtc)
            .Take(50)
            .Select(r => new EventStoreRelatedJobRunDto
            {
                Id = r.Id,
                JobName = r.JobName,
                JobType = r.JobType,
                Status = r.Status,
                CorrelationId = r.CorrelationId,
                EventId = r.EventId,
                StartedAtUtc = r.StartedAtUtc,
                CompletedAtUtc = r.CompletedAtUtc,
                ErrorMessage = r.ErrorMessage
            })
            .ToListAsync(cancellationToken);

        var workflowJobsQuery = _context.WorkflowJobs.AsNoTracking()
            .Where(w => correlationId != null && w.CorrelationId == correlationId);
        if (scopeCompanyId.HasValue)
            workflowJobsQuery = workflowJobsQuery.Where(w => w.CompanyId == scopeCompanyId.Value);
        var workflowJobs = await workflowJobsQuery
            .OrderByDescending(w => w.CreatedAt)
            .Take(50)
            .Select(w => new EventStoreRelatedWorkflowJobDto
            {
                Id = w.Id,
                CorrelationId = w.CorrelationId,
                EntityType = w.EntityType,
                EntityId = w.EntityId,
                CurrentStatus = w.CurrentStatus,
                TargetStatus = w.TargetStatus,
                State = w.State.ToString(),
                CreatedAt = w.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new EventStoreRelatedLinksDto { JobRuns = jobRuns, WorkflowJobs = workflowJobs };
    }

    /// <inheritdoc />
    public async Task<EventStoreCountsSnapshot> GetEventStoreCountsAsync(Guid? scopeCompanyId, CancellationToken cancellationToken = default)
    {
        var q = _context.EventStore.AsNoTracking().AsQueryable();
        if (scopeCompanyId.HasValue)
            q = q.Where(e => e.CompanyId == scopeCompanyId.Value);

        var pendingCount = await q.Where(e => e.Status == "Pending").CountAsync(cancellationToken);
        var failedCount = await q.Where(e => e.Status == "Failed").CountAsync(cancellationToken);
        var deadLetterCount = await q.Where(e => e.Status == "DeadLetter").CountAsync(cancellationToken);
        var pendingQ = q.Where(e => e.Status == "Pending");
        var oldestPending = await pendingQ.AnyAsync(cancellationToken)
            ? await pendingQ.MinAsync(e => e.CreatedAtUtc, cancellationToken)
            : (DateTime?)null;

        var now = DateTime.UtcNow;
        var processingCount = await q.Where(e => e.Status == "Processing").CountAsync(cancellationToken);
        var expiredLeasesCount = await q.Where(e => e.Status == "Processing" && e.ProcessingLeaseExpiresAtUtc != null && e.ProcessingLeaseExpiresAtUtc < now).CountAsync(cancellationToken);

        return new EventStoreCountsSnapshot
        {
            PendingCount = pendingCount,
            FailedCount = failedCount,
            DeadLetterCount = deadLetterCount,
            OldestPendingCreatedAtUtc = oldestPending,
            ProcessingCount = processingCount,
            ExpiredLeasesCount = expiredLeasesCount
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventStoreAttemptHistoryItemDto>> GetAttemptHistoryByEventIdAsync(Guid eventId, Guid? scopeCompanyId, CancellationToken cancellationToken = default)
    {
        var entry = await _eventStore.GetByEventIdAsync(eventId, cancellationToken);
        if (entry == null) return Array.Empty<EventStoreAttemptHistoryItemDto>();
        if (scopeCompanyId.HasValue && entry.CompanyId != scopeCompanyId.Value) return Array.Empty<EventStoreAttemptHistoryItemDto>();

        return await _context.EventStoreAttemptHistory.AsNoTracking()
            .Where(a => a.EventId == eventId)
            .OrderBy(a => a.AttemptNumber)
            .Select(a => new EventStoreAttemptHistoryItemDto
            {
                Id = a.Id,
                EventId = a.EventId,
                EventType = a.EventType,
                CompanyId = a.CompanyId,
                HandlerName = a.HandlerName,
                AttemptNumber = a.AttemptNumber,
                Status = a.Status,
                StartedAtUtc = a.StartedAtUtc,
                FinishedAtUtc = a.FinishedAtUtc,
                DurationMs = a.DurationMs,
                ProcessingNodeId = a.ProcessingNodeId,
                ErrorType = a.ErrorType,
                ErrorMessage = a.ErrorMessage,
                StackTraceSummary = a.StackTraceSummary,
                WasRetried = a.WasRetried,
                WasDeadLettered = a.WasDeadLettered
            })
            .ToListAsync(cancellationToken);
    }
}
