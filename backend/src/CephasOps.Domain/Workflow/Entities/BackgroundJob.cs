namespace CephasOps.Domain.Workflow.Entities;

/// <summary>
/// Background job entity - generic for email ingestion, P&amp;L rebuild, etc.
/// </summary>
public class BackgroundJob
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type of background job (e.g., "EmailIngest", "PnlRebuild", "InvoiceSubmission", "PayrollCalculation")
    /// </summary>
    public string JobType { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized job data/payload
    /// </summary>
    public string PayloadJson { get; set; } = "{}";

    /// <summary>
    /// Current state of the job (Queued, Running, Succeeded, Failed)
    /// </summary>
    public BackgroundJobState State { get; set; } = BackgroundJobState.Queued;

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Maximum number of retry attempts allowed
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Last error message if the job failed
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Priority of the job (higher numbers = higher priority)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Scheduled execution time (null = execute immediately)
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Timestamp when the job was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the job started processing
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Timestamp when the job completed (successfully or failed)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Timestamp when the job was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this job was retried from the UI/API, the JobRun id that was retried (so the new run can set ParentJobRunId).
    /// </summary>
    public Guid? RetriedFromJobRunId { get; set; }

    // --- Distributed job scheduler (Phase 1) ---
    /// <summary>Worker that claimed this job for execution. Null when unclaimed.</summary>
    public Guid? WorkerId { get; set; }
    /// <summary>When the job was claimed by the current worker.</summary>
    public DateTime? ClaimedAtUtc { get; set; }

    // --- Multi-tenant (SaaS) ---
    /// <summary>Company (tenant) this job belongs to. Execution must restore this context.</summary>
    public Guid? CompanyId { get; set; }
}

/// <summary>
/// Enumeration of background job states
/// </summary>
public enum BackgroundJobState
{
    /// <summary>
    /// Job is queued and waiting to be processed
    /// </summary>
    Queued = 0,

    /// <summary>
    /// Job is currently being processed
    /// </summary>
    Running = 1,

    /// <summary>
    /// Job completed successfully
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// Job failed with an error
    /// </summary>
    Failed = 3
}

