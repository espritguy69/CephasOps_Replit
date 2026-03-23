namespace CephasOps.Application.Workers;

/// <summary>
/// Configuration for worker heartbeat and stale detection.
/// </summary>
public class WorkerOptions
{
    public const string SectionName = "Workers";

    /// <summary>Heartbeat interval in seconds. Default 15. Recommended 10–30.</summary>
    public int HeartbeatIntervalSeconds { get; set; } = 15;

    /// <summary>After this many minutes without heartbeat, worker is considered inactive (stale). Default 3. Recommended 2–5.</summary>
    public int InactiveTimeoutMinutes { get; set; } = 3;
}
