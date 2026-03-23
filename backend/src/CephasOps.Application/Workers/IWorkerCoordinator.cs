using CephasOps.Application.Workers.DTOs;

namespace CephasOps.Application.Workers;

/// <summary>
/// Worker coordination: registration, heartbeat, job claim/release, stale recovery, and visibility.
/// </summary>
public interface IWorkerCoordinator
{
    /// <summary>Register a new worker instance. Returns its Id. Idempotent per (hostName, processId, role) when re-registering after crash.</summary>
    Task<Guid> RegisterAsync(string hostName, int processId, string role, CancellationToken cancellationToken = default);

    /// <summary>Update LastHeartbeatUtc for the worker. No-op if worker not found or inactive.</summary>
    Task HeartbeatAsync(Guid workerId, CancellationToken cancellationToken = default);

    /// <summary>Try to claim the replay operation for this worker. Returns true if claimed, false if already owned by another active worker.</summary>
    Task<bool> TryClaimReplayOperationAsync(Guid workerId, Guid replayOperationId, CancellationToken cancellationToken = default);

    /// <summary>Try to claim the rebuild operation for this worker. Returns true if claimed, false if already owned by another active worker.</summary>
    Task<bool> TryClaimRebuildOperationAsync(Guid workerId, Guid rebuildOperationId, CancellationToken cancellationToken = default);

    /// <summary>Release replay operation ownership (on completion or failure).</summary>
    Task ReleaseReplayOperationAsync(Guid replayOperationId, CancellationToken cancellationToken = default);

    /// <summary>Release rebuild operation ownership (on completion or failure).</summary>
    Task ReleaseRebuildOperationAsync(Guid rebuildOperationId, CancellationToken cancellationToken = default);

    /// <summary>Mark workers that have not heartbeaten within timeout as inactive and release their job ownership. Safe: does not touch recently active workers.</summary>
    Task<int> RecoverStaleWorkersAsync(CancellationToken cancellationToken = default);

    /// <summary>List all worker instances (active and inactive) for diagnostics.</summary>
    Task<IReadOnlyList<WorkerInstanceDto>> ListWorkersAsync(CancellationToken cancellationToken = default);

    /// <summary>Get a single worker by id with owned jobs.</summary>
    Task<WorkerInstanceDetailDto?> GetWorkerAsync(Guid workerId, CancellationToken cancellationToken = default);

    /// <summary>Try to atomically claim a background job for this worker. Job must be Queued and (unclaimed or owner inactive). On success sets WorkerId, ClaimedAtUtc, State=Running, StartedAt. Returns true if claimed.</summary>
    Task<bool> TryClaimBackgroundJobAsync(Guid workerId, Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>Release background job ownership (clear WorkerId and ClaimedAtUtc). Does not change State.</summary>
    Task ReleaseBackgroundJobAsync(Guid jobId, CancellationToken cancellationToken = default);
}
