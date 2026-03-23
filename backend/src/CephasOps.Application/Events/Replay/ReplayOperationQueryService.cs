using CephasOps.Application.Events.DTOs;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Events.Replay;

public class ReplayOperationQueryService : IReplayOperationQueryService
{
    private readonly ApplicationDbContext _context;
    private readonly IReplayTargetRegistry _targetRegistry;

    public ReplayOperationQueryService(ApplicationDbContext context, IReplayTargetRegistry targetRegistry)
    {
        _context = context;
        _targetRegistry = targetRegistry;
    }

    public async Task<(IReadOnlyList<ReplayOperationListItemDto> Items, int Total)> ListAsync(int page, int pageSize, Guid? scopeCompanyId, CancellationToken cancellationToken = default)
    {
        var q = _context.ReplayOperations.AsNoTracking().AsQueryable();
        if (scopeCompanyId.HasValue)
            q = q.Where(o => o.CompanyId == scopeCompanyId.Value);

        var total = await q.CountAsync(cancellationToken);
        var size = Math.Clamp(pageSize, 1, 100);
        var skip = (Math.Max(1, page) - 1) * size;
        var items = await q.OrderByDescending(o => o.RequestedAtUtc)
            .Skip(skip)
            .Take(size)
            .Select(o => new ReplayOperationListItemDto
            {
                Id = o.Id,
                RequestedByUserId = o.RequestedByUserId,
                RequestedAtUtc = o.RequestedAtUtc,
                DryRun = o.DryRun,
                ReplayReason = o.ReplayReason,
                CompanyId = o.CompanyId,
                EventType = o.EventType,
                ReplayTarget = o.ReplayTarget,
                ReplayMode = o.ReplayMode,
                StartedAtUtc = o.StartedAtUtc,
                TotalMatched = o.TotalMatched,
                TotalEligible = o.TotalEligible,
                TotalExecuted = o.TotalExecuted,
                TotalSucceeded = o.TotalSucceeded,
                TotalFailed = o.TotalFailed,
                SkippedCount = o.SkippedCount,
                DurationMs = o.DurationMs,
                ErrorSummary = o.ErrorSummary,
                State = o.State,
                CompletedAtUtc = o.CompletedAtUtc,
                ResumeRequired = o.ResumeRequired,
                OrderingStrategyId = o.OrderingStrategyId,
                RetriedFromOperationId = o.RetriedFromOperationId,
                RerunReason = o.RerunReason,
                SafetyCutoffOccurredAtUtc = o.SafetyCutoffOccurredAtUtc,
                SafetyWindowMinutes = o.SafetyWindowMinutes
            })
            .ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            var descriptor = _targetRegistry.GetById(item.ReplayTarget ?? ReplayTargets.EventStore);
            item.OrderingGuaranteeLevel = descriptor?.OrderingGuaranteeLevel;
            item.OrderingDegradedReason = descriptor?.OrderingDegradedReason;
        }
        return (items, total);
    }

    public async Task<ReplayOperationDetailDto?> GetByIdAsync(Guid id, Guid? scopeCompanyId, CancellationToken cancellationToken = default)
    {
        var o = await _context.ReplayOperations.AsNoTracking()
            .Where(x => x.Id == id && (!scopeCompanyId.HasValue || x.CompanyId == scopeCompanyId.Value))
            .Select(x => new ReplayOperationDetailDto
            {
                Id = x.Id,
                RequestedByUserId = x.RequestedByUserId,
                RequestedAtUtc = x.RequestedAtUtc,
                DryRun = x.DryRun,
                ReplayReason = x.ReplayReason,
                CompanyId = x.CompanyId,
                EventType = x.EventType,
                ReplayTarget = x.ReplayTarget,
                ReplayMode = x.ReplayMode,
                StartedAtUtc = x.StartedAtUtc,
                Status = x.Status,
                FromOccurredAtUtc = x.FromOccurredAtUtc,
                ToOccurredAtUtc = x.ToOccurredAtUtc,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                CorrelationId = x.CorrelationId,
                MaxEvents = x.MaxEvents,
                TotalMatched = x.TotalMatched,
                TotalEligible = x.TotalEligible,
                TotalExecuted = x.TotalExecuted,
                TotalSucceeded = x.TotalSucceeded,
                TotalFailed = x.TotalFailed,
                SkippedCount = x.SkippedCount,
                DurationMs = x.DurationMs,
                ErrorSummary = x.ErrorSummary,
                ReplayCorrelationId = x.ReplayCorrelationId,
                Notes = x.Notes,
                State = x.State,
                CompletedAtUtc = x.CompletedAtUtc,
                ResumeRequired = x.ResumeRequired,
                OrderingStrategyId = x.OrderingStrategyId,
                RetriedFromOperationId = x.RetriedFromOperationId,
                RerunReason = x.RerunReason,
                LastCheckpointAtUtc = x.LastCheckpointAtUtc,
                LastProcessedEventId = x.LastProcessedEventId,
                ProcessedCountAtLastCheckpoint = x.ProcessedCountAtLastCheckpoint,
                SafetyCutoffOccurredAtUtc = x.SafetyCutoffOccurredAtUtc,
                SafetyWindowMinutes = x.SafetyWindowMinutes
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (o == null) return null;
        var descriptor = _targetRegistry.GetById(o.ReplayTarget ?? ReplayTargets.EventStore);
        o.OrderingGuaranteeLevel = descriptor?.OrderingGuaranteeLevel;
        o.OrderingDegradedReason = descriptor?.OrderingDegradedReason;
        if (o.RetriedFromOperationId.HasValue)
        {
            o.OrderingGuaranteeLevel = OrderingGuaranteeLevels.BestEffortDeterministic;
            o.OrderingDegradedReason = "Events replayed in processed order, not event-time order.";
        }

        var eventResults = await _context.ReplayOperationEvents.AsNoTracking()
            .Where(e => e.ReplayOperationId == id)
            .OrderBy(e => e.ProcessedAtUtc)
            .Select(e => new ReplayOperationEventItemDto
            {
                EventId = e.EventId,
                EventType = e.EventType,
                EntityType = e.EntityType,
                EntityId = e.EntityId,
                Succeeded = e.Succeeded,
                ErrorMessage = e.ErrorMessage,
                SkippedReason = e.SkippedReason,
                ProcessedAtUtc = e.ProcessedAtUtc,
                DurationMs = e.DurationMs
            })
            .ToListAsync(cancellationToken);
        o.EventResults = eventResults;
        return o;
    }

    public async Task<ReplayOperationProgressDto?> GetProgressAsync(Guid id, Guid? scopeCompanyId, CancellationToken cancellationToken = default)
    {
        var row = await _context.ReplayOperations.AsNoTracking()
            .Where(x => x.Id == id && (!scopeCompanyId.HasValue || x.CompanyId == scopeCompanyId.Value))
            .Select(x => new
            {
                x.Id,
                x.State,
                x.ResumeRequired,
                x.TotalEligible,
                x.TotalExecuted,
                x.TotalSucceeded,
                x.TotalFailed,
                x.ProcessedCountAtLastCheckpoint,
                x.LastCheckpointAtUtc,
                x.LastProcessedEventId,
                x.ReplayTarget,
                x.OrderingStrategyId,
                x.RetriedFromOperationId
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (row == null) return null;
        var descriptor = _targetRegistry.GetById(row.ReplayTarget ?? ReplayTargets.EventStore);
        var orderingLevel = descriptor?.OrderingGuaranteeLevel;
        var orderingReason = descriptor?.OrderingDegradedReason;
        if (row.RetriedFromOperationId.HasValue)
        {
            orderingLevel = OrderingGuaranteeLevels.BestEffortDeterministic;
            orderingReason = "Events replayed in processed order, not event-time order.";
        }
        return new ReplayOperationProgressDto
        {
            OperationId = row.Id,
            State = row.State,
            ResumeRequired = row.ResumeRequired,
            TotalEligible = row.TotalEligible,
            TotalExecuted = row.TotalExecuted,
            TotalSucceeded = row.TotalSucceeded,
            TotalFailed = row.TotalFailed,
            ProcessedCountAtLastCheckpoint = row.ProcessedCountAtLastCheckpoint,
            LastCheckpointAtUtc = row.LastCheckpointAtUtc,
            LastProcessedEventId = row.LastProcessedEventId,
            ProgressPercent = row.TotalEligible > 0 ? (int?)Math.Min(100, ((row.TotalExecuted ?? row.ProcessedCountAtLastCheckpoint ?? 0) * 100) / row.TotalEligible.Value) : null,
            OrderingStrategyId = row.OrderingStrategyId,
            OrderingGuaranteeLevel = orderingLevel,
            OrderingDegradedReason = orderingReason
        };
    }
}
