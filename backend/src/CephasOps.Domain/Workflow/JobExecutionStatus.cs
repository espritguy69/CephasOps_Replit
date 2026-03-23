namespace CephasOps.Domain.Workflow;

/// <summary>
/// JobExecution lifecycle states (Phase 4). Transitions:
/// Pending → (claim) → Running → Succeeded | Failed | DeadLetter.
/// Failed + NextRunAtUtc set = retry scheduled; becomes Pending when NextRunAtUtc &lt;= now.
/// DeadLetter = terminal; no further retries.
/// </summary>
public static class JobExecutionStatus
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
    public const string DeadLetter = "DeadLetter";
}
