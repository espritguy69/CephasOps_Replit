namespace CephasOps.Application.Workers;

/// <summary>
/// Provides the current process's worker instance id after registration.
/// Set by WorkerHeartbeatHostedService on startup; null until registration completes.
/// </summary>
public interface IWorkerIdentity
{
    /// <summary>Current worker instance id, or null if not yet registered.</summary>
    Guid? WorkerId { get; }
}
