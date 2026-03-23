using System.Diagnostics;
using System.Text.Json;
using CephasOps.Application.Events.DTOs;
using CephasOps.Application.Events.Ledger;
using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Executes operational replay: persists ReplayOperation, sets replay context (side-effect suppression), loads eligible events in deterministic order, replays in batches. Does not mutate original EventStore rows.
/// </summary>
public class OperationalReplayExecutionService : IOperationalReplayExecutionService
{
    private const int DefaultBatchSize = 50;
    private const int DefaultMaxReplayCount = 1000;
    private const int MaxErrorSummaryLength = 1500;

    private readonly ApplicationDbContext _context;
    private readonly IEventStoreQueryService _queryService;
    private readonly IOperationalReplayPolicy _policy;
    private readonly IEventReplayService _replayService;
    private readonly IReplayExecutionContextAccessor _replayContextAccessor;
    private readonly ReplayMetrics _metrics;
    private readonly IReplayTargetRegistry _targetRegistry;
    private readonly ILedgerWriter? _ledgerWriter;
    private readonly IReplayExecutionLockStore? _replayLockStore;
    private readonly ILogger<OperationalReplayExecutionService> _logger;

    public OperationalReplayExecutionService(
        ApplicationDbContext context,
        IEventStoreQueryService queryService,
        IOperationalReplayPolicy policy,
        IEventReplayService replayService,
        IReplayExecutionContextAccessor replayContextAccessor,
        ReplayMetrics metrics,
        IReplayTargetRegistry targetRegistry,
        ILogger<OperationalReplayExecutionService> logger,
        ILedgerWriter? ledgerWriter = null,
        IReplayExecutionLockStore? replayLockStore = null)
    {
        _context = context;
        _queryService = queryService;
        _policy = policy;
        _replayService = replayService;
        _replayContextAccessor = replayContextAccessor;
        _metrics = metrics;
        _targetRegistry = targetRegistry;
        _ledgerWriter = ledgerWriter;
        _replayLockStore = replayLockStore;
        _logger = logger;
    }

    public async Task<OperationalReplayExecutionResultDto> ExecuteAsync(
        ReplayRequestDto request,
        Guid? scopeCompanyId,
        Guid? requestedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.DryRun)
        {
            _logger.LogInformation("Operational replay skipped (DryRun=true). Use preview API for dry-run.");
            return new OperationalReplayExecutionResultDto
            {
                ReplayOperationId = Guid.Empty,
                DryRun = true,
                ErrorMessage = "DryRun is set; use preview API instead."
            };
        }

        var utcNow = DateTime.UtcNow;
        var replayCorrelationId = Guid.NewGuid().ToString("N")[..16];

        var replayTarget = request.ReplayTarget ?? ReplayTargets.EventStore;
        var replayMode = request.ReplayMode ?? ReplayModes.Apply;
        if (!_targetRegistry.IsSupported(replayTarget))
            return new OperationalReplayExecutionResultDto { ReplayOperationId = Guid.Empty, ErrorMessage = $"Replay target '{replayTarget}' is not supported." };

        var descriptor = _targetRegistry.GetById(replayTarget);
        var orderingStrategyId = descriptor?.OrderingStrategyId ?? OrderingStrategies.OccurredAtUtcAscendingEventIdAscending;

        var operation = new ReplayOperation
        {
            Id = Guid.NewGuid(),
            RequestedByUserId = requestedByUserId,
            RequestedAtUtc = utcNow,
            DryRun = false,
            ReplayReason = request.ReplayReason,
            CompanyId = request.CompanyId,
            EventType = request.EventType,
            Status = request.Status,
            FromOccurredAtUtc = request.FromOccurredAtUtc,
            ToOccurredAtUtc = request.ToOccurredAtUtc,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            CorrelationId = request.CorrelationId,
            MaxEvents = request.MaxEvents,
            State = ReplayOperationStates.Running,
            ReplayCorrelationId = replayCorrelationId,
            ReplayTarget = replayTarget,
            ReplayMode = replayMode,
            StartedAtUtc = utcNow,
            OrderingStrategyId = orderingStrategyId
        };

        _context.ReplayOperations.Add(operation);
        await _context.SaveChangesAsync(cancellationToken);

        if (operation.CompanyId.HasValue && _replayLockStore != null)
        {
            var acquired = await _replayLockStore.TryAcquireAsync(operation.CompanyId.Value, operation.Id, cancellationToken).ConfigureAwait(false);
            if (!acquired)
            {
                operation.State = ReplayOperationStates.Failed;
                operation.CompletedAtUtc = DateTime.UtcNow;
                operation.ErrorSummary = "Another replay is already running for this company. Wait for it to complete or cancel it before starting a new one.";
                if (operation.StartedAtUtc.HasValue)
                    operation.DurationMs = (long)(operation.CompletedAtUtc.Value - operation.StartedAtUtc.Value).TotalMilliseconds;
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogWarning("Replay not started: company lock not acquired. ReplayOperationId={OpId}, CompanyId={CompanyId}", operation.Id, operation.CompanyId);
                return new OperationalReplayExecutionResultDto
                {
                    ReplayOperationId = operation.Id,
                    State = ReplayOperationStates.Failed,
                    ErrorMessage = operation.ErrorSummary
                };
            }
        }

        var safetyCutoff = ReplaySafetyWindow.GetCutoffUtc();
        _logger.LogInformation("Replay safety cutoff applied: OccurredAtUtc <= {Cutoff:O} (window: {Window} min). ReplayOperationId={OpId}", safetyCutoff, ReplaySafetyWindow.DefaultWindowMinutes, operation.Id);

        try
        {
            return await RunReplayCoreAsync(operation, request, scopeCompanyId, requestedByUserId, replayCorrelationId, resumeAfterEventId: null, resumeAfterOccurredAtUtc: null, safetyCutoffOccurredAtUtc: safetyCutoff, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (operation.CompanyId.HasValue && _replayLockStore != null)
                await _replayLockStore.ReleaseAsync(operation.CompanyId.Value, operation.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<OperationalReplayExecutionResultDto> ExecuteByOperationIdAsync(
        Guid operationId,
        Guid? scopeCompanyId,
        Guid? requestedByUserId,
        CancellationToken cancellationToken = default)
    {
        // Tenant-safe: scope by scopeCompanyId when provided so FindAsync bypass is avoided
        var operation = (scopeCompanyId.HasValue && scopeCompanyId.Value != Guid.Empty)
            ? await _context.ReplayOperations.FirstOrDefaultAsync(op => op.Id == operationId && op.CompanyId == scopeCompanyId.Value, cancellationToken)
            : await _context.ReplayOperations.FirstOrDefaultAsync(op => op.Id == operationId, cancellationToken);
        if (operation == null)
            return new OperationalReplayExecutionResultDto { ReplayOperationId = operationId, ErrorMessage = "Replay operation not found." };
        var allowedStates = new[] { ReplayOperationStates.Pending, ReplayOperationStates.PartiallyCompleted };
        if (!allowedStates.Contains(operation.State))
            return new OperationalReplayExecutionResultDto { ReplayOperationId = operationId, ErrorMessage = $"Replay operation cannot be run or resumed (State={operation.State})." };

        if (operation.CompanyId.HasValue && _replayLockStore != null)
        {
            var acquired = await _replayLockStore.TryAcquireAsync(operation.CompanyId.Value, operation.Id, cancellationToken).ConfigureAwait(false);
            if (!acquired)
            {
                _logger.LogWarning("Replay resume/start not executed: company lock not acquired. ReplayOperationId={OpId}, CompanyId={CompanyId}", operationId, operation.CompanyId);
                return new OperationalReplayExecutionResultDto
                {
                    ReplayOperationId = operationId,
                    ErrorMessage = "Another replay is already running for this company. Wait for it to complete or cancel it before starting or resuming another."
                };
            }
        }

        var request = new ReplayRequestDto
        {
            CompanyId = operation.CompanyId,
            EventType = operation.EventType,
            Status = operation.Status,
            FromOccurredAtUtc = operation.FromOccurredAtUtc,
            ToOccurredAtUtc = operation.ToOccurredAtUtc,
            EntityType = operation.EntityType,
            EntityId = operation.EntityId,
            CorrelationId = operation.CorrelationId,
            MaxEvents = operation.MaxEvents,
            DryRun = false,
            ReplayReason = operation.ReplayReason,
            ReplayTarget = operation.ReplayTarget,
            ReplayMode = operation.ReplayMode ?? ReplayModes.Apply
        };

        operation.State = ReplayOperationStates.Running;
        if (!operation.StartedAtUtc.HasValue)
            operation.StartedAtUtc = DateTime.UtcNow;
        if (string.IsNullOrEmpty(operation.ReplayCorrelationId))
            operation.ReplayCorrelationId = Guid.NewGuid().ToString("N")[..16];
        operation.ResumeRequired = false;
        await _context.SaveChangesAsync(cancellationToken);

        var safetyCutoff = ReplaySafetyWindow.GetCutoffUtc();
        _logger.LogInformation("Replay safety cutoff applied: OccurredAtUtc <= {Cutoff:O} (window: {Window} min). ReplayOperationId={OpId}", safetyCutoff, ReplaySafetyWindow.DefaultWindowMinutes, operation.Id);

        try
        {
            return await RunReplayCoreAsync(
                operation,
                request,
                scopeCompanyId,
                requestedByUserId,
                operation.ReplayCorrelationId,
                operation.LastProcessedEventId,
                operation.LastProcessedOccurredAtUtc,
                safetyCutoffOccurredAtUtc: safetyCutoff,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (operation.CompanyId.HasValue && _replayLockStore != null)
                await _replayLockStore.ReleaseAsync(operation.CompanyId.Value, operation.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<OperationalReplayExecutionResultDto> ExecuteRerunFailedAsync(
        Guid originalOperationId,
        Guid? scopeCompanyId,
        Guid? requestedByUserId,
        string? rerunReason,
        CancellationToken cancellationToken = default)
    {
        var original = await _context.ReplayOperations.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == originalOperationId, cancellationToken);
        if (original == null)
            return new OperationalReplayExecutionResultDto { ReplayOperationId = originalOperationId, ErrorMessage = "Original replay operation not found." };
        var allowedStates = new[] { ReplayOperationStates.Completed, ReplayOperationStates.PartiallyCompleted, ReplayOperationStates.Failed };
        if (!allowedStates.Contains(original.State))
            return new OperationalReplayExecutionResultDto { ReplayOperationId = originalOperationId, ErrorMessage = $"Original operation state '{original.State}' does not allow rerun-failed." };

        var failedEvents = await _context.ReplayOperationEvents.AsNoTracking()
            .Where(e => e.ReplayOperationId == originalOperationId && !e.Succeeded)
            .OrderBy(e => e.ProcessedAtUtc)
            .Select(e => new { e.EventId, e.EventType, e.EntityType, e.EntityId })
            .ToListAsync(cancellationToken);
        if (failedEvents.Count == 0)
            return new OperationalReplayExecutionResultDto { ReplayOperationId = originalOperationId, ErrorMessage = "No failed events to rerun." };

        var safetyCutoff = ReplaySafetyWindow.GetCutoffUtc();
        var eventsWithOccurredAt = new List<(Guid EventId, string EventType, string? EntityType, Guid? EntityId, DateTime OccurredAtUtc)>();
        foreach (var e in failedEvents)
        {
            var detail = await _queryService.GetByEventIdAsync(e.EventId, scopeCompanyId, cancellationToken).ConfigureAwait(false);
            if (detail != null)
                eventsWithOccurredAt.Add((e.EventId, e.EventType ?? "", e.EntityType, e.EntityId, detail.OccurredAtUtc));
        }
        var eligibleForRerun = eventsWithOccurredAt.Where(x => x.OccurredAtUtc <= safetyCutoff).ToList();
        var excludedBySafety = eventsWithOccurredAt.Count - eligibleForRerun.Count;
        if (eligibleForRerun.Count == 0)
        {
            _logger.LogWarning("Rerun-failed: all {Count} failed events are newer than replay safety cutoff {Cutoff:O}; none will be replayed. OriginalOperationId={OpId}", failedEvents.Count, safetyCutoff, originalOperationId);
            return new OperationalReplayExecutionResultDto
            {
                ReplayOperationId = originalOperationId,
                ErrorMessage = $"All {failedEvents.Count} failed event(s) are newer than the replay safety cutoff ({safetyCutoff:O}). Wait until events are at least {ReplaySafetyWindow.DefaultWindowMinutes} minutes old, then try again.",
                SafetyWindowApplied = true,
                SafetyCutoffOccurredAtUtc = safetyCutoff,
                SafetyWindowMinutes = ReplaySafetyWindow.DefaultWindowMinutes
            };
        }
        if (excludedBySafety > 0)
            _logger.LogInformation("Rerun-failed: {Excluded} of {Total} failed events excluded by replay safety window (OccurredAtUtc > {Cutoff:O}). Replaying {Eligible} events. OriginalOperationId={OpId}", excludedBySafety, failedEvents.Count, safetyCutoff, eligibleForRerun.Count, originalOperationId);

        var replayTarget = (original.ReplayTarget ?? ReplayTargets.EventStore).Trim();
        if (string.IsNullOrEmpty(replayTarget))
            replayTarget = ReplayTargets.EventStore;
        if (!_targetRegistry.IsSupported(replayTarget))
            return new OperationalReplayExecutionResultDto { ReplayOperationId = originalOperationId, ErrorMessage = $"Replay target '{replayTarget}' is not supported for rerun-failed. Only supported targets can be retried." };

        _metrics.RecordRerun(replayTarget, "failed_only");

        var utcNow = DateTime.UtcNow;
        var newOp = new ReplayOperation
        {
            Id = Guid.NewGuid(),
            RequestedByUserId = requestedByUserId,
            RequestedAtUtc = utcNow,
            DryRun = false,
            ReplayReason = rerunReason ?? ("Rerun failed events from " + originalOperationId),
            CompanyId = original.CompanyId,
            EventType = original.EventType,
            Status = original.Status,
            FromOccurredAtUtc = original.FromOccurredAtUtc,
            ToOccurredAtUtc = original.ToOccurredAtUtc,
            EntityType = original.EntityType,
            EntityId = original.EntityId,
            CorrelationId = original.CorrelationId,
            MaxEvents = eligibleForRerun.Count,
            State = ReplayOperationStates.Running,
            ReplayCorrelationId = Guid.NewGuid().ToString("N")[..16],
            ReplayTarget = replayTarget,
            ReplayMode = original.ReplayMode ?? ReplayModes.Apply,
            StartedAtUtc = utcNow,
            OrderingStrategyId = original.OrderingStrategyId,
            RetriedFromOperationId = originalOperationId,
            RerunReason = rerunReason,
            TotalMatched = eligibleForRerun.Count,
            TotalEligible = eligibleForRerun.Count,
            SkippedCount = 0,
            SafetyCutoffOccurredAtUtc = safetyCutoff,
            SafetyWindowMinutes = ReplaySafetyWindow.DefaultWindowMinutes
        };
        _context.ReplayOperations.Add(newOp);
        await _context.SaveChangesAsync(cancellationToken);

        if (newOp.CompanyId.HasValue && _replayLockStore != null)
        {
            var acquired = await _replayLockStore.TryAcquireAsync(newOp.CompanyId.Value, newOp.Id, cancellationToken).ConfigureAwait(false);
            if (!acquired)
            {
                newOp.State = ReplayOperationStates.Failed;
                newOp.CompletedAtUtc = DateTime.UtcNow;
                newOp.ErrorSummary = "Another replay is already running for this company. Wait for it to complete or cancel it before rerunning failed events.";
                newOp.DurationMs = 0;
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogWarning("Rerun-failed not started: company lock not acquired. NewOpId={NewOpId}, CompanyId={CompanyId}", newOp.Id, newOp.CompanyId);
                return new OperationalReplayExecutionResultDto
                {
                    ReplayOperationId = newOp.Id,
                    State = ReplayOperationStates.Failed,
                    ErrorMessage = newOp.ErrorSummary
                };
            }
        }

        try
        {
            var executed = 0;
            var succeeded = 0;
            var failed = 0;
            var errorMessages = new List<string>();
            var childRecords = new List<ReplayOperationEvent>();
            var replayContext = ReplayExecutionContext.ForReplay(newOp.Id, replayTarget, newOp.ReplayMode ?? ReplayModes.Apply, true);

            try
            {
                _replayContextAccessor.Set(replayContext);
                foreach (var e in eligibleForRerun)
                {
                    var sw = Stopwatch.StartNew();
                    var result = await _replayService.ReplayAsync(e.EventId, scopeCompanyId, requestedByUserId, cancellationToken);
                    sw.Stop();
                    executed++;
                    if (result.Success) succeeded++; else { failed++; if (errorMessages.Count < 5) errorMessages.Add(result.ErrorMessage ?? result.BlockedReason ?? ""); }
                    childRecords.Add(new ReplayOperationEvent
                    {
                        ReplayOperationId = newOp.Id,
                        EventId = e.EventId,
                        EventType = e.EventType,
                        EntityType = e.EntityType,
                        EntityId = e.EntityId,
                        Succeeded = result.Success,
                        ErrorMessage = result.ErrorMessage ?? result.BlockedReason,
                        ProcessedAtUtc = DateTime.UtcNow,
                        DurationMs = sw.ElapsedMilliseconds
                    });
                }
            }
            catch (Exception ex)
            {
                newOp.State = ReplayOperationStates.Failed;
                newOp.CompletedAtUtc = DateTime.UtcNow;
                var errMsg = ex.Message ?? "";
                newOp.ErrorSummary = errMsg.Length > MaxErrorSummaryLength ? errMsg[..MaxErrorSummaryLength] + "..." : errMsg;
                newOp.TotalExecuted = executed;
                newOp.TotalSucceeded = succeeded;
                newOp.TotalFailed = failed;
                newOp.DurationMs = newOp.StartedAtUtc.HasValue ? (long)(newOp.CompletedAtUtc.Value - newOp.StartedAtUtc.Value).TotalMilliseconds : null;
                await _context.ReplayOperationEvents.AddRangeAsync(childRecords, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogWarning(ex, "Rerun-failed error. NewOpId={NewOpId}, Executed={Executed}", newOp.Id, executed);
                throw;
            }
            finally
            {
                _replayContextAccessor.Set(null);
            }

            var completedAt = DateTime.UtcNow;
        newOp.TotalExecuted = executed;
        newOp.TotalSucceeded = succeeded;
        newOp.TotalFailed = failed;
        newOp.State = ReplayOperationStates.Completed;
        newOp.CompletedAtUtc = completedAt;
        newOp.DurationMs = (long)(completedAt - utcNow).TotalMilliseconds;
        newOp.ErrorSummary = errorMessages.Count > 0 ? string.Join("; ", errorMessages.Take(3)) : null;
        if (newOp.ErrorSummary != null && newOp.ErrorSummary.Length > MaxErrorSummaryLength)
            newOp.ErrorSummary = newOp.ErrorSummary.Substring(0, MaxErrorSummaryLength) + "...";
        await _context.ReplayOperationEvents.AddRangeAsync(childRecords, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await TryWriteReplayOperationCompletedLedgerAsync(newOp, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Rerun-failed completed. NewOpId={NewId}, OriginalOpId={OrigId}, Executed={Executed}, Succeeded={Ok}, Failed={Fail}", newOp.Id, originalOperationId, executed, succeeded, failed);
            return WithSafetyWindow(new OperationalReplayExecutionResultDto
            {
                ReplayOperationId = newOp.Id,
                DryRun = false,
                TotalMatched = eligibleForRerun.Count,
                TotalEligible = eligibleForRerun.Count,
                TotalExecuted = executed,
                TotalSucceeded = succeeded,
                TotalFailed = failed,
                ReplayCorrelationId = newOp.ReplayCorrelationId,
                State = newOp.State
            }, newOp);
        }
        finally
        {
            if (newOp.CompanyId.HasValue && _replayLockStore != null)
                await _replayLockStore.ReleaseAsync(newOp.CompanyId.Value, newOp.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<OperationalReplayExecutionResultDto> RequestCancelAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        var tenantId = CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        var operation = (tenantId.HasValue && tenantId.Value != Guid.Empty)
            ? await _context.ReplayOperations.FirstOrDefaultAsync(op => op.Id == operationId && op.CompanyId == tenantId.Value, cancellationToken)
            : await _context.ReplayOperations.FirstOrDefaultAsync(op => op.Id == operationId, cancellationToken);
        if (operation == null)
            return new OperationalReplayExecutionResultDto { ReplayOperationId = operationId, ErrorMessage = "Replay operation not found." };
        var allowedStates = new[] { ReplayOperationStates.Pending, ReplayOperationStates.Running, ReplayOperationStates.PartiallyCompleted };
        if (!allowedStates.Contains(operation.State))
            return new OperationalReplayExecutionResultDto { ReplayOperationId = operationId, ErrorMessage = $"Operation cannot be cancelled (State={operation.State}). Only Pending, Running, or PartiallyCompleted can be cancelled." };

        var utcNow = DateTime.UtcNow;
        if (operation.State == ReplayOperationStates.Running)
        {
            operation.CancelRequestedAtUtc = utcNow;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cancel requested for running replay. ReplayOperationId={OpId}; job will stop at next checkpoint.", operationId);
            return new OperationalReplayExecutionResultDto { ReplayOperationId = operationId, State = operation.State };
        }
        // Pending or PartiallyCompleted: no job running; mark Cancelled immediately.
        operation.State = ReplayOperationStates.Cancelled;
        operation.CompletedAtUtc = utcNow;
        operation.CancelRequestedAtUtc = utcNow;
        operation.ResumeRequired = false;
        if (operation.StartedAtUtc.HasValue)
            operation.DurationMs = (long)(utcNow - operation.StartedAtUtc.Value).TotalMilliseconds;
        operation.ErrorSummary = "Cancelled by user.";
        await _context.SaveChangesAsync(cancellationToken);
        await TryWriteReplayOperationCompletedLedgerAsync(operation, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Replay operation cancelled. ReplayOperationId={OpId}", operationId);
        return new OperationalReplayExecutionResultDto { ReplayOperationId = operationId, State = ReplayOperationStates.Cancelled };
    }

    private async Task TryWriteReplayOperationCompletedLedgerAsync(ReplayOperation operation, CancellationToken cancellationToken)
    {
        if (_ledgerWriter == null) return;
        var state = operation.State ?? "Unknown";
        if (state != ReplayOperationStates.Completed && state != ReplayOperationStates.Cancelled && state != ReplayOperationStates.Failed)
            return;
        var occurredAt = operation.CompletedAtUtc ?? DateTime.UtcNow;
        var payload = JsonSerializer.Serialize(new
        {
            State = state,
            operation.ReplayTarget,
            operation.TotalMatched,
            operation.TotalEligible,
            operation.TotalExecuted,
            operation.TotalSucceeded,
            operation.TotalFailed,
            operation.DurationMs,
            operation.RetriedFromOperationId
        });
        try
        {
            await _ledgerWriter.AppendFromReplayOperationAsync(
                operation.Id,
                LedgerFamilies.ReplayOperationCompleted,
                "ReplayOperationCompleted",
                occurredAt,
                operation.CompanyId,
                payload,
                operation.OrderingStrategyId,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write ReplayOperationCompleted ledger entry for ReplayOperationId={OpId}", operation.Id);
        }
    }

    private async Task<OperationalReplayExecutionResultDto> RunReplayCoreAsync(
        ReplayOperation operation,
        ReplayRequestDto request,
        Guid? scopeCompanyId,
        Guid? requestedByUserId,
        string replayCorrelationId,
        Guid? resumeAfterEventId,
        DateTime? resumeAfterOccurredAtUtc,
        DateTime? safetyCutoffOccurredAtUtc,
        CancellationToken cancellationToken)
    {
        var utcNow = operation.StartedAtUtc ?? DateTime.UtcNow;
        var replayTarget = operation.ReplayTarget ?? ReplayTargets.EventStore;
        var replayMode = operation.ReplayMode ?? ReplayModes.Apply;
        var dryRun = operation.DryRun;
        var isResume = resumeAfterEventId.HasValue && resumeAfterOccurredAtUtc.HasValue;

        if (safetyCutoffOccurredAtUtc.HasValue)
        {
            operation.SafetyCutoffOccurredAtUtc = safetyCutoffOccurredAtUtc;
            operation.SafetyWindowMinutes = ReplaySafetyWindow.DefaultWindowMinutes;
        }

        _metrics.RecordRunStarted(replayTarget, dryRun);
        if (isResume)
            _metrics.RecordRunResumed(replayTarget);

        var maxEvents = request.MaxEvents ?? _policy.MaxReplayCountPerRequest ?? DefaultMaxReplayCount;
        var (items, totalMatched) = await _queryService.GetEventsForReplayAsync(
            request, scopeCompanyId, maxEvents,
            resumeAfterEventId, resumeAfterOccurredAtUtc,
            safetyCutoffOccurredAtUtc,
            cancellationToken);

        var eligibleList = new List<(Guid EventId, string EventType, string? EntityType, Guid? EntityId, DateTime OccurredAtUtc)>();
        var childRecords = new List<ReplayOperationEvent>();
        var skippedCount = isResume ? (operation.SkippedCount ?? 0) : 0;

        foreach (var item in items)
        {
            var input = new ReplayEligibilityInputDto
            {
                EventId = item.EventId,
                EventType = item.EventType,
                CompanyId = item.CompanyId,
                OccurredAtUtc = item.OccurredAtUtc
            };
            var eligibility = _policy.CheckEligibility(input, request, utcNow);
            if (eligibility.Eligible)
                eligibleList.Add((item.EventId, item.EventType, item.EntityType, item.EntityId, item.OccurredAtUtc));
            else
            {
                skippedCount++;
                childRecords.Add(new ReplayOperationEvent
                {
                    ReplayOperationId = operation.Id,
                    EventId = item.EventId,
                    EventType = item.EventType,
                    EntityType = item.EntityType,
                    EntityId = item.EntityId,
                    Succeeded = false,
                    SkippedReason = eligibility.BlockedReason,
                    ProcessedAtUtc = DateTime.UtcNow
                });
            }
        }

        var segmentEligible = eligibleList.Count;
        var executed = isResume ? (operation.TotalExecuted ?? 0) : 0;
        var succeeded = isResume ? (operation.TotalSucceeded ?? 0) : 0;
        var failed = isResume ? (operation.TotalFailed ?? 0) : 0;
        var totalEligible = isResume ? (operation.TotalEligible ?? 0) : segmentEligible;
        if (!isResume)
        {
            operation.TotalMatched = totalMatched;
            operation.TotalEligible = totalEligible;
            operation.SkippedCount = skippedCount;
            operation.OrderingStrategyId ??= OrderingStrategies.OccurredAtUtcAscendingEventIdAscending;
            await _context.SaveChangesAsync(cancellationToken);
        }

        var batchSize = DefaultBatchSize;
        var errorMessages = new List<string>();
        var replayContext = ReplayExecutionContext.ForReplay(operation.Id, replayTarget, replayMode, suppressSideEffects: true);
        var cancelled = false;
        var lastEventId = (Guid?)null;
        var lastOccurredAtUtc = (DateTime?)null;

        try
        {
            _replayContextAccessor.Set(replayContext);
            var batchIndex = 0;
            foreach (var batch in eligibleList.Chunk(batchSize))
            {
                foreach (var (eventId, eventType, entityType, entityId, occurredAtUtc) in batch)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancelled = true;
                        break;
                    }
                    var sw = Stopwatch.StartNew();
                    var result = await _replayService.ReplayAsync(eventId, scopeCompanyId, requestedByUserId, cancellationToken);
                    sw.Stop();
                    executed++;
                    if (result.Success)
                        succeeded++;
                    else
                    {
                        failed++;
                        var err = result.ErrorMessage ?? result.BlockedReason;
                        if (!string.IsNullOrEmpty(err) && errorMessages.Count < 5)
                            errorMessages.Add(err);
                    }
                    lastEventId = eventId;
                    lastOccurredAtUtc = occurredAtUtc;
                    childRecords.Add(new ReplayOperationEvent
                    {
                        ReplayOperationId = operation.Id,
                        EventId = eventId,
                        EventType = eventType,
                        EntityType = entityType,
                        EntityId = entityId,
                        Succeeded = result.Success,
                        ErrorMessage = result.ErrorMessage ?? result.BlockedReason,
                        ProcessedAtUtc = DateTime.UtcNow,
                        DurationMs = sw.ElapsedMilliseconds
                    });
                }
                if (cancelled) break;

                // Cooperative cancel: check if operator requested cancel (persisted by cancel endpoint).
                await _context.Entry(operation).ReloadAsync(cancellationToken);
                if (operation.CancelRequestedAtUtc.HasValue)
                {
                    cancelled = true;
                    break;
                }

                batchIndex++;
                operation.TotalExecuted = executed;
                operation.TotalSucceeded = succeeded;
                operation.TotalFailed = failed;
                operation.LastProcessedEventId = lastEventId;
                operation.LastProcessedOccurredAtUtc = lastOccurredAtUtc;
                operation.ProcessedCountAtLastCheckpoint = executed;
                operation.CheckpointCount++;
                operation.LastCheckpointAtUtc = DateTime.UtcNow;
                _metrics.RecordCheckpoint(replayTarget);
                await _context.ReplayOperationEvents.AddRangeAsync(childRecords, cancellationToken);
                childRecords.Clear();
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Replay run error. ReplayOperationId={OpId}, Executed={Executed}", operation.Id, executed);
            operation.TotalExecuted = executed;
            operation.TotalSucceeded = succeeded;
            operation.TotalFailed = failed;
            operation.LastProcessedEventId = lastEventId;
            operation.LastProcessedOccurredAtUtc = lastOccurredAtUtc;
            operation.ProcessedCountAtLastCheckpoint = executed;
            operation.CheckpointCount++;
            operation.LastCheckpointAtUtc = DateTime.UtcNow;
            operation.State = ReplayOperationStates.PartiallyCompleted;
            operation.ResumeRequired = true;
            operation.ErrorSummary = (ex.Message?.Length ?? 0) > MaxErrorSummaryLength ? ex.Message![..MaxErrorSummaryLength] + "..." : ex.Message;
            await _context.ReplayOperationEvents.AddRangeAsync(childRecords, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _replayContextAccessor.Set(null);
            _metrics.RecordRunFailed(replayTarget, dryRun);
            return WithSafetyWindow(new OperationalReplayExecutionResultDto { ReplayOperationId = operation.Id, State = operation.State, ErrorMessage = ex.Message }, operation);
        }
        finally
        {
            _replayContextAccessor.Set(null);
        }

        if (cancelled)
        {
            await _context.Entry(operation).ReloadAsync(cancellationToken);
            var cancelRequested = operation.CancelRequestedAtUtc.HasValue;

            operation.TotalExecuted = executed;
            operation.TotalSucceeded = succeeded;
            operation.TotalFailed = failed;
            operation.LastProcessedEventId = lastEventId;
            operation.LastProcessedOccurredAtUtc = lastOccurredAtUtc;
            operation.ProcessedCountAtLastCheckpoint = executed;
            operation.CheckpointCount++;
            operation.LastCheckpointAtUtc = DateTime.UtcNow;
            await _context.ReplayOperationEvents.AddRangeAsync(childRecords, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Operator-requested cancel: set Cancelled; otherwise interrupted (token) = PartiallyCompleted + ResumeRequired.
            if (cancelRequested)
            {
                operation.State = ReplayOperationStates.Cancelled;
                operation.ResumeRequired = false;
                operation.CompletedAtUtc = DateTime.UtcNow;
                operation.DurationMs = operation.StartedAtUtc.HasValue ? (long)(operation.CompletedAtUtc.Value - operation.StartedAtUtc.Value).TotalMilliseconds : null;
                operation.ErrorSummary = "Cancelled by user.";
                _logger.LogInformation("Replay cancelled by user. ReplayOperationId={OpId}, Executed={Executed}", operation.Id, executed);
            }
            else
            {
                operation.State = ReplayOperationStates.PartiallyCompleted;
                operation.ResumeRequired = true;
                _logger.LogInformation("Replay cancelled/resumable (token). ReplayOperationId={OpId}, Executed={Executed}", operation.Id, executed);
            }
            await _context.SaveChangesAsync(cancellationToken);
            if (cancelRequested)
                await TryWriteReplayOperationCompletedLedgerAsync(operation, cancellationToken).ConfigureAwait(false);
            return WithSafetyWindow(new OperationalReplayExecutionResultDto { ReplayOperationId = operation.Id, State = operation.State, TotalExecuted = executed, TotalSucceeded = succeeded, TotalFailed = failed }, operation);
        }

        var completedAt = DateTime.UtcNow;
        if (!isResume)
        {
            operation.TotalMatched = totalMatched;
            operation.TotalEligible = totalEligible;
        }
        operation.TotalExecuted = executed;
        operation.TotalSucceeded = succeeded;
        operation.TotalFailed = failed;
        operation.SkippedCount = skippedCount;
        operation.State = ReplayOperationStates.Completed;
        operation.CompletedAtUtc = completedAt;
        operation.StartedAtUtc = operation.StartedAtUtc ?? utcNow;
        operation.DurationMs = (long)(completedAt - operation.StartedAtUtc.Value).TotalMilliseconds;
        var errorSummaryRaw = errorMessages.Count > 0 ? string.Join("; ", errorMessages) : null;
        operation.ErrorSummary = errorSummaryRaw != null && errorSummaryRaw.Length > MaxErrorSummaryLength
            ? errorSummaryRaw.Substring(0, MaxErrorSummaryLength) + "..."
            : errorSummaryRaw;
        await _context.ReplayOperationEvents.AddRangeAsync(childRecords, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var durationSeconds = operation.DurationMs.HasValue ? operation.DurationMs.Value / 1000.0 : 0;
        _metrics.RecordRunCompleted(replayTarget, dryRun, executed, succeeded, failed, durationSeconds);

        _logger.LogInformation(
            "Operational replay completed. ReplayOperationId={OpId}, ReplayTarget={Target}, TotalMatched={Matched}, Eligible={Eligible}, Executed={Executed}, Succeeded={Ok}, Failed={Fail}, DurationMs={DurationMs}",
            operation.Id, replayTarget, totalMatched, totalEligible, executed, succeeded, failed, operation.DurationMs);

        await TryWriteReplayOperationCompletedLedgerAsync(operation, cancellationToken).ConfigureAwait(false);

        return WithSafetyWindow(new OperationalReplayExecutionResultDto
        {
            ReplayOperationId = operation.Id,
            DryRun = false,
            TotalMatched = totalMatched,
            TotalEligible = totalEligible,
            TotalExecuted = executed,
            TotalSucceeded = succeeded,
            TotalFailed = failed,
            ReplayCorrelationId = replayCorrelationId,
            State = operation.State
        }, operation);
    }

    private static OperationalReplayExecutionResultDto WithSafetyWindow(OperationalReplayExecutionResultDto dto, ReplayOperation operation)
    {
        dto.SafetyWindowApplied = operation.SafetyCutoffOccurredAtUtc.HasValue;
        dto.SafetyCutoffOccurredAtUtc = operation.SafetyCutoffOccurredAtUtc;
        dto.SafetyWindowMinutes = operation.SafetyWindowMinutes;
        return dto;
    }
}
