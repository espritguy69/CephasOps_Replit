namespace CephasOps.Domain.Workflow.Entities;

/// <summary>
/// Job orchestration record (Phase 3). Persisted work item; worker claims, executes, and marks completed/failed.
/// Retry via NextRunAtUtc; lease for claim ownership.
/// </summary>
public class JobExecution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string JobType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    /// <summary>Pending | Running | Succeeded | Failed | DeadLetter.</summary>
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 5;
    /// <summary>When to run (null = immediately). For retries, set on failure.</summary>
    public DateTime? NextRunAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? LastError { get; set; }
    public DateTime? LastErrorAtUtc { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? CausationId { get; set; }
    /// <summary>Worker/node that claimed this job.</summary>
    public string? ProcessingNodeId { get; set; }
    public DateTime? ProcessingLeaseExpiresAtUtc { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
    /// <summary>Optional priority (higher = run first).</summary>
    public int Priority { get; set; }
}
