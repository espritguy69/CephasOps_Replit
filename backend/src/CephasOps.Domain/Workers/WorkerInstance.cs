namespace CephasOps.Domain.Workers;

/// <summary>
/// Durable identity of a running worker process (API, dedicated worker, or scheduler).
/// Used for distributed coordination: heartbeat, job ownership, and stale recovery.
/// </summary>
public class WorkerInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string HostName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    /// <summary>API, Worker, or Scheduler.</summary>
    public string Role { get; set; } = string.Empty;

    public DateTime StartedAtUtc { get; set; }
    public DateTime LastHeartbeatUtc { get; set; }

    /// <summary>False when worker has not heartbeaten within configured timeout (stale).</summary>
    public bool IsActive { get; set; } = true;
}
