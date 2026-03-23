using CephasOps.Application.Workers.DTOs;
using CephasOps.Domain.Events;
using CephasOps.Domain.Workers;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Workers;

public sealed class WorkerCoordinatorService : IWorkerCoordinator
{
    private readonly ApplicationDbContext _context;
    private readonly WorkerOptions _options;
    private readonly ILogger<WorkerCoordinatorService> _logger;

    public WorkerCoordinatorService(
        ApplicationDbContext context,
        IOptions<WorkerOptions> options,
        ILogger<WorkerCoordinatorService> logger)
    {
        _context = context;
        _options = options?.Value ?? new WorkerOptions();
        _logger = logger;
    }

    public async Task<Guid> RegisterAsync(string hostName, int processId, string role, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var worker = new WorkerInstance
        {
            Id = Guid.NewGuid(),
            HostName = hostName,
            ProcessId = processId,
            Role = role,
            StartedAtUtc = now,
            LastHeartbeatUtc = now,
            IsActive = true
        };
        _context.WorkerInstances.Add(worker);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Worker registered. WorkerId={WorkerId}, HostName={HostName}, ProcessId={ProcessId}, Role={Role}",
            worker.Id, worker.HostName, worker.ProcessId, worker.Role);
        return worker.Id;
    }

    public async Task HeartbeatAsync(Guid workerId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var updated = await _context.WorkerInstances
            .Where(w => w.Id == workerId && w.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(w => w.LastHeartbeatUtc, now), cancellationToken).ConfigureAwait(false);
        if (updated == 0)
            _logger.LogDebug("Heartbeat skipped: worker {WorkerId} not found or inactive", workerId);
    }

    public async Task<bool> TryClaimReplayOperationAsync(Guid workerId, Guid replayOperationId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddMinutes(-_options.InactiveTimeoutMinutes);

        var op = await _context.ReplayOperations
            .FirstOrDefaultAsync(o => o.Id == replayOperationId, cancellationToken).ConfigureAwait(false);
        if (op == null) return false;

        if (op.WorkerId.HasValue && op.ClaimedAtUtc.HasValue)
        {
            var owner = await _context.WorkerInstances.FindAsync(new object[] { op.WorkerId.Value }, cancellationToken).ConfigureAwait(false);
            if (owner != null && owner.IsActive && owner.LastHeartbeatUtc > cutoff)
                return false;
        }

        op.WorkerId = workerId;
        op.ClaimedAtUtc = now;
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Replay operation claimed. ReplayOperationId={OpId}, WorkerId={WorkerId}", replayOperationId, workerId);
        return true;
    }

    public async Task<bool> TryClaimRebuildOperationAsync(Guid workerId, Guid rebuildOperationId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddMinutes(-_options.InactiveTimeoutMinutes);

        var op = await _context.RebuildOperations
            .FirstOrDefaultAsync(o => o.Id == rebuildOperationId, cancellationToken).ConfigureAwait(false);
        if (op == null) return false;

        if (op.WorkerId.HasValue && op.ClaimedAtUtc.HasValue)
        {
            var owner = await _context.WorkerInstances.FindAsync(new object[] { op.WorkerId.Value }, cancellationToken).ConfigureAwait(false);
            if (owner != null && owner.IsActive && owner.LastHeartbeatUtc > cutoff)
                return false;
        }

        op.WorkerId = workerId;
        op.ClaimedAtUtc = now;
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Rebuild operation claimed. RebuildOperationId={OpId}, WorkerId={WorkerId}", rebuildOperationId, workerId);
        return true;
    }

    public async Task ReleaseReplayOperationAsync(Guid replayOperationId, CancellationToken cancellationToken = default)
    {
        var op = await _context.ReplayOperations.FirstOrDefaultAsync(o => o.Id == replayOperationId, cancellationToken).ConfigureAwait(false);
        if (op == null) return;
        op.WorkerId = null;
        op.ClaimedAtUtc = null;
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Replay operation released. ReplayOperationId={OpId}", replayOperationId);
    }

    public async Task ReleaseRebuildOperationAsync(Guid rebuildOperationId, CancellationToken cancellationToken = default)
    {
        var op = await _context.RebuildOperations.FirstOrDefaultAsync(o => o.Id == rebuildOperationId, cancellationToken).ConfigureAwait(false);
        if (op == null) return;
        op.WorkerId = null;
        op.ClaimedAtUtc = null;
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Rebuild operation released. RebuildOperationId={OpId}", rebuildOperationId);
    }

    public async Task<int> RecoverStaleWorkersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddMinutes(-_options.InactiveTimeoutMinutes);

        var stale = await _context.WorkerInstances
            .Where(w => w.IsActive && w.LastHeartbeatUtc < cutoff)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (stale.Count == 0) return 0;

        foreach (var w in stale)
        {
            w.IsActive = false;
            _logger.LogWarning("Worker marked stale (no heartbeat). WorkerId={WorkerId}, HostName={HostName}, LastHeartbeat={LastHeartbeat}",
                w.Id, w.HostName, w.LastHeartbeatUtc);
        }
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var w in stale)
        {
            await _context.ReplayOperations
                .Where(o => o.WorkerId == w.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.WorkerId, (Guid?)null)
                    .SetProperty(o => o.ClaimedAtUtc, (DateTime?)null), cancellationToken).ConfigureAwait(false);
            await _context.RebuildOperations
                .Where(o => o.WorkerId == w.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.WorkerId, (Guid?)null)
                    .SetProperty(o => o.ClaimedAtUtc, (DateTime?)null), cancellationToken).ConfigureAwait(false);
            await _context.BackgroundJobs
                .Where(j => j.WorkerId == w.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(j => j.WorkerId, (Guid?)null)
                    .SetProperty(j => j.ClaimedAtUtc, (DateTime?)null)
                    .SetProperty(j => j.State, BackgroundJobState.Queued)
                    .SetProperty(j => j.StartedAt, (DateTime?)null), cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Stale worker recovery completed. Marked {Count} workers inactive and released their job ownership", stale.Count);
        return stale.Count;
    }

    public async Task<IReadOnlyList<WorkerInstanceDto>> ListWorkersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var timeoutMinutes = _options.InactiveTimeoutMinutes;
        var workers = await _context.WorkerInstances
            .OrderByDescending(w => w.LastHeartbeatUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return workers.Select(w =>
        {
            var age = (now - w.LastHeartbeatUtc).TotalSeconds;
            var isStale = !w.IsActive || (now - w.LastHeartbeatUtc).TotalMinutes > timeoutMinutes;
            return new WorkerInstanceDto
            {
                Id = w.Id,
                HostName = w.HostName,
                ProcessId = w.ProcessId,
                Role = w.Role,
                StartedAtUtc = w.StartedAtUtc,
                LastHeartbeatUtc = w.LastHeartbeatUtc,
                IsActive = w.IsActive,
                HeartbeatAgeSeconds = age,
                IsStale = isStale
            };
        }).ToList();
    }

    public async Task<WorkerInstanceDetailDto?> GetWorkerAsync(Guid workerId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var timeoutMinutes = _options.InactiveTimeoutMinutes;
        var worker = await _context.WorkerInstances.FindAsync(new object[] { workerId }, cancellationToken).ConfigureAwait(false);
        if (worker == null) return null;

        var replays = await _context.ReplayOperations
            .Where(o => o.WorkerId == workerId)
            .Select(o => new OwnedReplayOperationDto { OperationId = o.Id, State = o.State, ClaimedAtUtc = o.ClaimedAtUtc })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var rebuilds = await _context.RebuildOperations
            .Where(o => o.WorkerId == workerId)
            .Select(o => new OwnedRebuildOperationDto { OperationId = o.Id, State = o.State, ClaimedAtUtc = o.ClaimedAtUtc })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var age = (now - worker.LastHeartbeatUtc).TotalSeconds;
        var isStale = !worker.IsActive || (now - worker.LastHeartbeatUtc).TotalMinutes > timeoutMinutes;

        return new WorkerInstanceDetailDto
        {
            Id = worker.Id,
            HostName = worker.HostName,
            ProcessId = worker.ProcessId,
            Role = worker.Role,
            StartedAtUtc = worker.StartedAtUtc,
            LastHeartbeatUtc = worker.LastHeartbeatUtc,
            IsActive = worker.IsActive,
            HeartbeatAgeSeconds = age,
            IsStale = isStale,
            OwnedReplayOperations = replays,
            OwnedRebuildOperations = rebuilds
        };
    }

    public async Task<bool> TryClaimBackgroundJobAsync(Guid workerId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddMinutes(-_options.InactiveTimeoutMinutes);
        const string stateQueued = "Queued";
        const string stateRunning = "Running";

        // Atomic claim: update only if Queued and (unclaimed or owner inactive)
        var rows = await _context.Database.ExecuteSqlRawAsync(
            """
            UPDATE "BackgroundJobs"
            SET "WorkerId" = {0}, "ClaimedAtUtc" = {1}, "State" = {2}, "StartedAt" = {1}, "UpdatedAt" = {1}
            WHERE "Id" = {3} AND "State" = {4}
            AND ("WorkerId" IS NULL OR "WorkerId" IN (
                SELECT "Id" FROM "WorkerInstances"
                WHERE NOT ("IsActive" = true AND "LastHeartbeatUtc" > {5})
            ))
            """,
            new object[] { workerId, now, stateRunning, jobId, stateQueued, cutoff },
            cancellationToken).ConfigureAwait(false);

        if (rows > 0)
            _logger.LogDebug("Background job claimed. JobId={JobId}, WorkerId={WorkerId}", jobId, workerId);
        return rows > 0;
    }

    public async Task ReleaseBackgroundJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        await _context.BackgroundJobs
            .Where(j => j.Id == jobId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(j => j.WorkerId, (Guid?)null)
                .SetProperty(j => j.ClaimedAtUtc, (DateTime?)null), cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Background job released. JobId={JobId}", jobId);
    }
}
