namespace CephasOps.Application.Workflow.DTOs;

/// <summary>
/// DTO for a single job run (observability record).
/// </summary>
public class JobRunDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string TriggerSource { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? QueueOrChannel { get; set; }
    public string? PayloadSummary { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public long? DurationMs { get; set; }
    public int RetryCount { get; set; }
    public string? WorkerNode { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }
    public Guid? InitiatedByUserId { get; set; }
    public Guid? ParentJobRunId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? RelatedEntityId { get; set; }
    public Guid? BackgroundJobId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    /// <summary>Whether this run can be retried from the UI (failed queue-based job in allow-list).</summary>
    public bool CanRetry { get; set; }
    /// <summary>Effective stuck threshold in seconds used for this run (when returned from stuck endpoint).</summary>
    public int? EffectiveStuckThresholdSeconds { get; set; }
}

/// <summary>
/// Dashboard summary metrics for job observability.
/// </summary>
public class JobRunDashboardDto
{
    public int TotalRunsLast24h { get; set; }
    public int SucceededLast24h { get; set; }
    public int FailedLast24h { get; set; }
    public double SuccessRateLast24h { get; set; }
    public int RunningNow { get; set; }
    public int StuckCount { get; set; }
    public int QueuedNow { get; set; }
    public long? P95DurationMsLast24h { get; set; }
    public double JobsPerHourLast24h { get; set; }
    public double RetryRateLast24h { get; set; }
    public List<JobTypeMetricDto> ByJobType { get; set; } = new();
    public List<TopFailingCompanyDto> TopFailingCompanies { get; set; } = new();
    public List<TopFailingJobTypeDto> TopFailingJobTypes { get; set; } = new();
    public List<JobRunDto> RecentFailures { get; set; } = new();
}

public class JobTypeMetricDto
{
    public string JobType { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public double? AvgDurationMs { get; set; }
}

public class TopFailingCompanyDto
{
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int FailedCount { get; set; }
}

public class TopFailingJobTypeDto
{
    public string JobType { get; set; } = string.Empty;
    public int FailedCount { get; set; }
}
