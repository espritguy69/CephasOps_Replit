namespace CephasOps.Application.Workflow.JobOrchestration;

/// <summary>Summary counts for JobExecution operational visibility (Phase 4).</summary>
public class JobExecutionSummaryDto
{
    public int PendingCount { get; set; }
    public int RunningCount { get; set; }
    public int FailedRetryScheduledCount { get; set; }
    public int DeadLetterCount { get; set; }
    public int SucceededCount { get; set; }
}

/// <summary>Single job row for list views (Phase 4).</summary>
public class JobExecutionListItemDto
{
    public Guid Id { get; set; }
    public string JobType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; }
    public DateTime? NextRunAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? LastError { get; set; }
    public DateTime? LastErrorAtUtc { get; set; }
    public Guid? CompanyId { get; set; }
    public string? ProcessingNodeId { get; set; }
    public DateTime? ProcessingLeaseExpiresAtUtc { get; set; }
}
