using CephasOps.Application.Rebuild.DTOs;
using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Rebuild;

/// <summary>
/// Resolves rebuild target, runs the appropriate runner, persists RebuildOperation.
/// Phase 2: lock, async enqueue, checkpoint/resume, progress. Replay-safe; no side effects.
/// </summary>
public sealed class OperationalRebuildService : IOperationalRebuildService
{
    private readonly ApplicationDbContext _context;
    private readonly IRebuildTargetRegistry _registry;
    private readonly IEnumerable<IRebuildRunner> _runners;
    private readonly IRebuildExecutionLockStore _lockStore;
    private readonly IRebuildJobEnqueuer _jobEnqueuer;
    private readonly ILogger<OperationalRebuildService> _logger;

    public OperationalRebuildService(
        ApplicationDbContext context,
        IRebuildTargetRegistry registry,
        IEnumerable<IRebuildRunner> runners,
        IRebuildExecutionLockStore lockStore,
        IRebuildJobEnqueuer jobEnqueuer,
        ILogger<OperationalRebuildService> logger)
    {
        _context = context;
        _registry = registry;
        _runners = runners;
        _lockStore = lockStore;
        _jobEnqueuer = jobEnqueuer;
        _logger = logger;
    }

    public async Task<RebuildPreviewResultDto?> PreviewAsync(
        RebuildRequestDto request,
        Guid? scopeCompanyId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RebuildTargetId))
            return null;

        var descriptor = _registry.GetById(request.RebuildTargetId);
        if (descriptor == null)
            return null;

        var runner = _runners.FirstOrDefault(r => string.Equals(r.TargetId, request.RebuildTargetId, StringComparison.OrdinalIgnoreCase));
        if (runner == null)
            return null;

        return await runner.PreviewAsync(_context, request, scopeCompanyId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RebuildExecutionResultDto> ExecuteAsync(
        RebuildRequestDto request,
        Guid? scopeCompanyId,
        Guid? requestedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RebuildTargetId))
            return new RebuildExecutionResultDto { RebuildOperationId = Guid.Empty, State = RebuildOperationStates.Failed, ErrorMessage = "RebuildTargetId is required." };

        var descriptor = _registry.GetById(request.RebuildTargetId);
        if (descriptor == null)
            return new RebuildExecutionResultDto { RebuildOperationId = Guid.Empty, State = RebuildOperationStates.Failed, ErrorMessage = $"Unknown rebuild target: {request.RebuildTargetId}." };

        var runner = _runners.FirstOrDefault(r => string.Equals(r.TargetId, request.RebuildTargetId, StringComparison.OrdinalIgnoreCase));
        if (runner == null)
            return new RebuildExecutionResultDto { RebuildOperationId = Guid.Empty, State = RebuildOperationStates.Failed, ErrorMessage = $"No runner registered for target: {request.RebuildTargetId}." };

        var scopeKey = _lockStore.GetScopeKey(request.CompanyId ?? scopeCompanyId);
        var operation = new RebuildOperation
        {
            Id = Guid.NewGuid(),
            RebuildTargetId = request.RebuildTargetId,
            RequestedByUserId = requestedByUserId,
            RequestedAtUtc = DateTime.UtcNow,
            ScopeCompanyId = request.CompanyId ?? scopeCompanyId,
            FromOccurredAtUtc = request.FromOccurredAtUtc,
            ToOccurredAtUtc = request.ToOccurredAtUtc,
            DryRun = request.DryRun,
            State = RebuildOperationStates.Running
        };

        _context.RebuildOperations.Add(operation);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (!request.DryRun)
        {
            var acquired = await _lockStore.TryAcquireAsync(request.RebuildTargetId, scopeKey, operation.Id, cancellationToken).ConfigureAwait(false);
            if (!acquired)
            {
                operation.State = RebuildOperationStates.Failed;
                operation.ErrorMessage = "Another rebuild is running for this target/scope. Wait for it to complete or check operations.";
                operation.CompletedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return new RebuildExecutionResultDto { RebuildOperationId = operation.Id, State = operation.State, ErrorMessage = operation.ErrorMessage };
            }
        }

        try
        {
            await runner.ExecuteAsync(_context, operation, request, scopeCompanyId, cancellationToken).ConfigureAwait(false);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            operation.State = RebuildOperationStates.Failed;
            operation.CompletedAtUtc = DateTime.UtcNow;
            operation.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            if (operation.StartedAtUtc.HasValue)
                operation.DurationMs = (long)(operation.CompletedAtUtc.Value - operation.StartedAtUtc.Value).TotalMilliseconds;
            if (operation.CheckpointCount > 0)
                operation.ResumeRequired = true;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError(ex, "Rebuild {TargetId} failed. OperationId={OpId}", request.RebuildTargetId, operation.Id);
        }
        finally
        {
            if (!request.DryRun)
                await _lockStore.ReleaseAsync(request.RebuildTargetId, scopeKey, operation.Id, cancellationToken).ConfigureAwait(false);
        }

        return new RebuildExecutionResultDto
        {
            RebuildOperationId = operation.Id,
            RebuildTargetId = operation.RebuildTargetId,
            State = operation.State,
            DryRun = operation.DryRun,
            RowsDeleted = operation.RowsDeleted,
            RowsInserted = operation.RowsInserted,
            RowsUpdated = operation.RowsUpdated,
            SourceRecordCount = operation.SourceRecordCount,
            DurationMs = operation.DurationMs,
            ErrorMessage = operation.ErrorMessage
        };
    }

    public async Task<Guid> EnqueueRebuildAsync(
        RebuildRequestDto request,
        Guid? scopeCompanyId,
        Guid? requestedByUserId,
        CancellationToken cancellationToken = default)
    {
        return await _jobEnqueuer.EnqueueRebuildAsync(request, scopeCompanyId, requestedByUserId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RebuildExecutionResultDto> ExecuteByOperationIdAsync(
        Guid operationId,
        Guid? scopeCompanyId,
        Guid? requestedByUserId,
        CancellationToken cancellationToken = default)
    {
        var operation = await _context.RebuildOperations.FirstOrDefaultAsync(o => o.Id == operationId, cancellationToken).ConfigureAwait(false);
        if (operation == null)
            return new RebuildExecutionResultDto { RebuildOperationId = Guid.Empty, State = RebuildOperationStates.Failed, ErrorMessage = "Rebuild operation not found." };
        if (scopeCompanyId.HasValue && operation.ScopeCompanyId.HasValue && operation.ScopeCompanyId != scopeCompanyId)
            return new RebuildExecutionResultDto { RebuildOperationId = operationId, State = RebuildOperationStates.Failed, ErrorMessage = "Operation not in scope." };

        var request = new RebuildRequestDto
        {
            RebuildTargetId = operation.RebuildTargetId,
            CompanyId = operation.ScopeCompanyId,
            FromOccurredAtUtc = operation.FromOccurredAtUtc,
            ToOccurredAtUtc = operation.ToOccurredAtUtc,
            DryRun = operation.DryRun
        };

        var runner = _runners.FirstOrDefault(r => string.Equals(r.TargetId, operation.RebuildTargetId, StringComparison.OrdinalIgnoreCase));
        if (runner == null)
        {
            operation.State = RebuildOperationStates.Failed;
            operation.ErrorMessage = "No runner registered for target.";
            operation.CompletedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new RebuildExecutionResultDto { RebuildOperationId = operationId, State = operation.State, ErrorMessage = operation.ErrorMessage };
        }

        var scopeKey = _lockStore.GetScopeKey(operation.ScopeCompanyId);
        var acquired = await _lockStore.TryAcquireAsync(operation.RebuildTargetId, scopeKey, operation.Id, cancellationToken).ConfigureAwait(false);
        if (!acquired)
        {
            operation.State = RebuildOperationStates.Failed;
            operation.ErrorMessage = "Another rebuild is running for this target/scope (lock conflict).";
            operation.CompletedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new RebuildExecutionResultDto { RebuildOperationId = operationId, State = operation.State, ErrorMessage = operation.ErrorMessage };
        }

        try
        {
            operation.State = RebuildOperationStates.Running;
            operation.StartedAtUtc = operation.StartedAtUtc ?? DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await runner.ExecuteAsync(_context, operation, request, scopeCompanyId, cancellationToken).ConfigureAwait(false);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            operation.State = RebuildOperationStates.Failed;
            operation.CompletedAtUtc = DateTime.UtcNow;
            operation.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            if (operation.StartedAtUtc.HasValue)
                operation.DurationMs = (long)(operation.CompletedAtUtc.Value - operation.StartedAtUtc.Value).TotalMilliseconds;
            if (operation.CheckpointCount > 0)
                operation.ResumeRequired = true;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError(ex, "Rebuild by operation id failed. OperationId={OpId}", operationId);
        }
        finally
        {
            await _lockStore.ReleaseAsync(operation.RebuildTargetId, scopeKey, operation.Id, cancellationToken).ConfigureAwait(false);
        }

        return new RebuildExecutionResultDto
        {
            RebuildOperationId = operation.Id,
            RebuildTargetId = operation.RebuildTargetId,
            State = operation.State,
            DryRun = operation.DryRun,
            RowsDeleted = operation.RowsDeleted,
            RowsInserted = operation.RowsInserted,
            RowsUpdated = operation.RowsUpdated,
            SourceRecordCount = operation.SourceRecordCount,
            DurationMs = operation.DurationMs,
            ErrorMessage = operation.ErrorMessage
        };
    }

    public async Task<RebuildExecutionResultDto> ExecuteResumeAsync(
        Guid operationId,
        Guid? scopeCompanyId,
        Guid? requestedByUserId,
        string? rerunReason,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteByOperationIdAsync(operationId, scopeCompanyId, requestedByUserId, cancellationToken).ConfigureAwait(false);
    }

    public async Task EnqueueResumeAsync(
        Guid operationId,
        Guid? scopeCompanyId,
        Guid? requestedByUserId,
        string? rerunReason,
        CancellationToken cancellationToken = default)
    {
        await _jobEnqueuer.EnqueueResumeAsync(operationId, scopeCompanyId, requestedByUserId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RebuildOperationSummaryDto?> GetOperationAsync(
        Guid operationId,
        Guid? scopeCompanyId,
        CancellationToken cancellationToken = default)
    {
        var op = await _context.RebuildOperations.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == operationId, cancellationToken)
            .ConfigureAwait(false);
        if (op == null)
            return null;
        if (scopeCompanyId.HasValue && op.ScopeCompanyId.HasValue && op.ScopeCompanyId != scopeCompanyId)
            return null;

        return new RebuildOperationSummaryDto
        {
            Id = op.Id,
            RebuildTargetId = op.RebuildTargetId,
            RequestedByUserId = op.RequestedByUserId,
            RequestedAtUtc = op.RequestedAtUtc,
            ScopeCompanyId = op.ScopeCompanyId,
            FromOccurredAtUtc = op.FromOccurredAtUtc,
            ToOccurredAtUtc = op.ToOccurredAtUtc,
            DryRun = op.DryRun,
            State = op.State,
            StartedAtUtc = op.StartedAtUtc,
            CompletedAtUtc = op.CompletedAtUtc,
            DurationMs = op.DurationMs,
            BackgroundJobId = op.BackgroundJobId,
            RowsDeleted = op.RowsDeleted,
            RowsInserted = op.RowsInserted,
            RowsUpdated = op.RowsUpdated,
            SourceRecordCount = op.SourceRecordCount,
            ErrorMessage = op.ErrorMessage,
            Notes = op.Notes,
            ResumeRequired = op.ResumeRequired,
            LastCheckpointAtUtc = op.LastCheckpointAtUtc,
            ProcessedCountAtLastCheckpoint = op.ProcessedCountAtLastCheckpoint,
            CheckpointCount = op.CheckpointCount,
            RetriedFromOperationId = op.RetriedFromOperationId,
            RerunReason = op.RerunReason
        };
    }

    public async Task<RebuildProgressDto?> GetProgressAsync(
        Guid operationId,
        Guid? scopeCompanyId,
        CancellationToken cancellationToken = default)
    {
        var op = await _context.RebuildOperations.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == operationId, cancellationToken)
            .ConfigureAwait(false);
        if (op == null)
            return null;
        if (scopeCompanyId.HasValue && op.ScopeCompanyId.HasValue && op.ScopeCompanyId != scopeCompanyId)
            return null;

        return new RebuildProgressDto
        {
            OperationId = op.Id,
            State = op.State,
            ResumeRequired = op.ResumeRequired,
            LastCheckpointAtUtc = op.LastCheckpointAtUtc,
            ProcessedCountAtLastCheckpoint = op.ProcessedCountAtLastCheckpoint,
            CheckpointCount = op.CheckpointCount,
            RowsDeleted = op.RowsDeleted,
            RowsInserted = op.RowsInserted,
            RowsUpdated = op.RowsUpdated,
            SourceRecordCount = op.SourceRecordCount
        };
    }

    public async Task<(IReadOnlyList<RebuildOperationSummaryDto> Items, int Total)> ListOperationsAsync(
        int page,
        int pageSize,
        Guid? scopeCompanyId,
        string? state = null,
        string? rebuildTargetId = null,
        CancellationToken cancellationToken = default)
    {
        var q = _context.RebuildOperations.AsNoTracking().AsQueryable();
        if (scopeCompanyId.HasValue)
            q = q.Where(o => o.ScopeCompanyId == scopeCompanyId.Value);
        if (!string.IsNullOrWhiteSpace(state))
            q = q.Where(o => o.State == state);
        if (!string.IsNullOrWhiteSpace(rebuildTargetId))
            q = q.Where(o => o.RebuildTargetId == rebuildTargetId);

        var total = await q.CountAsync(cancellationToken).ConfigureAwait(false);
        var size = Math.Clamp(pageSize, 1, 100);
        var skip = (Math.Max(1, page) - 1) * size;
        var list = await q.OrderByDescending(o => o.RequestedAtUtc)
            .Skip(skip)
            .Take(size)
            .Select(o => new RebuildOperationSummaryDto
            {
                Id = o.Id,
                RebuildTargetId = o.RebuildTargetId,
                RequestedByUserId = o.RequestedByUserId,
                RequestedAtUtc = o.RequestedAtUtc,
                ScopeCompanyId = o.ScopeCompanyId,
                FromOccurredAtUtc = o.FromOccurredAtUtc,
                ToOccurredAtUtc = o.ToOccurredAtUtc,
                DryRun = o.DryRun,
                State = o.State,
                StartedAtUtc = o.StartedAtUtc,
                CompletedAtUtc = o.CompletedAtUtc,
                DurationMs = o.DurationMs,
                BackgroundJobId = o.BackgroundJobId,
                RowsDeleted = o.RowsDeleted,
                RowsInserted = o.RowsInserted,
                RowsUpdated = o.RowsUpdated,
                SourceRecordCount = o.SourceRecordCount,
                ErrorMessage = o.ErrorMessage,
                Notes = o.Notes,
                ResumeRequired = o.ResumeRequired,
                LastCheckpointAtUtc = o.LastCheckpointAtUtc,
                ProcessedCountAtLastCheckpoint = o.ProcessedCountAtLastCheckpoint,
                CheckpointCount = o.CheckpointCount,
                RetriedFromOperationId = o.RetriedFromOperationId,
                RerunReason = o.RerunReason
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (list, total);
    }
}
