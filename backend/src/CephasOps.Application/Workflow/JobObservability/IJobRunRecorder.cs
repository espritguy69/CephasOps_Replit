namespace CephasOps.Application.Workflow.JobObservability;

/// <summary>
/// Records job run lifecycle for observability (history, metrics, failure diagnostics).
/// Used by the background job processor and optionally by in-process schedulers.
/// </summary>
public interface IJobRunRecorder
{
    /// <summary>
    /// Start recording a run (e.g. when processor picks a job). Returns the JobRun Id for later completion.
    /// </summary>
    Task<Guid> StartAsync(StartJobRunDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark the run as succeeded and set duration.
    /// </summary>
    Task CompleteAsync(Guid jobRunId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark the run as failed with sanitized error info.
    /// </summary>
    Task FailAsync(Guid jobRunId, FailJobRunDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark the run as cancelled (e.g. shutdown).
    /// </summary>
    Task CancelAsync(Guid jobRunId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Data to start recording a job run.
/// </summary>
public class StartJobRunDto
{
    public Guid? BackgroundJobId { get; set; }
    public Guid? CompanyId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string TriggerSource { get; set; } = "System";
    public string? CorrelationId { get; set; }
    public string? QueueOrChannel { get; set; }
    public string? PayloadSummary { get; set; }
    public int RetryCount { get; set; }
    public string? WorkerNode { get; set; }
    public Guid? InitiatedByUserId { get; set; }
    public Guid? ParentJobRunId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? RelatedEntityId { get; set; }
    /// <summary>When this run is for event handling; links to EventStore.</summary>
    public Guid? EventId { get; set; }
}

/// <summary>
/// Data to record a failed run.
/// </summary>
public class FailJobRunDto
{
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }
    public string? Status { get; set; } = "Failed"; // Failed, DeadLetter, etc.
}
