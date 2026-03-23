namespace CephasOps.Domain.Workflow.Entities;

/// <summary>
/// Observability record for a single job execution (queue-based or in-process).
/// Used for run history, failure diagnostics, metrics, and retry.
/// </summary>
public class JobRun
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Tenant; null for global/system jobs.</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>Display name (e.g. "Email Ingest", "P&amp;L Rebuild").</summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>Category/type (e.g. EmailIngest, pnlrebuild).</summary>
    public string JobType { get; set; } = string.Empty;

    /// <summary>Scheduler, Manual, System, Retry, Repair.</summary>
    public string TriggerSource { get; set; } = "System";

    /// <summary>For tracing and copy-to-clipboard.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>e.g. "BackgroundJobs".</summary>
    public string? QueueOrChannel { get; set; }

    /// <summary>Safe summary only; no secrets; truncated.</summary>
    public string? PayloadSummary { get; set; }

    /// <summary>Pending, Running, Succeeded, Failed, Cancelled, Retrying, DeadLetter.</summary>
    public string Status { get; set; } = "Pending";

    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public long? DurationMs { get; set; }
    public int RetryCount { get; set; }

    /// <summary>Host/node name if available.</summary>
    public string? WorkerNode { get; set; }

    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    /// <summary>Sanitized and truncated (e.g. 2000 chars).</summary>
    public string? ErrorDetails { get; set; }

    /// <summary>If triggered by user (manual/API).</summary>
    public Guid? InitiatedByUserId { get; set; }

    /// <summary>If this run was triggered by another (e.g. retry).</summary>
    public Guid? ParentJobRunId { get; set; }

    public string? RelatedEntityType { get; set; }
    public string? RelatedEntityId { get; set; }

    /// <summary>When this run is backed by a BackgroundJob.</summary>
    public Guid? BackgroundJobId { get; set; }

    /// <summary>When this run is for an event handler (EventBus). Links to EventStore.</summary>
    public Guid? EventId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
