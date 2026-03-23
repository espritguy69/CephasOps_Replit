using CephasOps.Application.Events.DTOs;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Executes operational replay for eligible events in batches. Creates audit record and does not mutate original EventStore rows.
/// </summary>
public interface IOperationalReplayExecutionService
{
    /// <summary>
    /// Execute replay for the given request. Persists ReplayOperation and processes eligible events in batches. Does not run if request.DryRun is true.
    /// </summary>
    Task<OperationalReplayExecutionResultDto> ExecuteAsync(
        ReplayRequestDto request,
        Guid? scopeCompanyId,
        Guid? requestedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute replay for an existing operation (e.g. queued via background job). Loads operation, builds request from it, runs and updates the same operation. Supports resume when state is PartiallyCompleted.
    /// </summary>
    Task<OperationalReplayExecutionResultDto> ExecuteByOperationIdAsync(
        Guid operationId,
        Guid? scopeCompanyId,
        Guid? requestedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new replay operation that replays only the events that failed in the original operation. New operation is linked via RetriedFromOperationId.
    /// </summary>
    Task<OperationalReplayExecutionResultDto> ExecuteRerunFailedAsync(
        Guid originalOperationId,
        Guid? scopeCompanyId,
        Guid? requestedByUserId,
        string? rerunReason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Request cancellation of an operation. Pending/PartiallyCompleted: set State = Cancelled immediately. Running: set CancelRequestedAtUtc so the job stops at next checkpoint.
    /// </summary>
    Task<OperationalReplayExecutionResultDto> RequestCancelAsync(Guid operationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an operational replay execution.
/// </summary>
public class OperationalReplayExecutionResultDto
{
    public Guid ReplayOperationId { get; set; }
    public bool DryRun { get; set; }
    public int TotalMatched { get; set; }
    public int TotalEligible { get; set; }
    public int TotalExecuted { get; set; }
    public int TotalSucceeded { get; set; }
    public int TotalFailed { get; set; }
    public string? ReplayCorrelationId { get; set; }
    public string? State { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>True when replay safety window was applied (events newer than cutoff excluded).</summary>
    public bool SafetyWindowApplied { get; set; }
    /// <summary>Effective cutoff: events with OccurredAtUtc after this were excluded.</summary>
    public DateTime? SafetyCutoffOccurredAtUtc { get; set; }
    /// <summary>Safety window in minutes used for this run (e.g. 5).</summary>
    public int? SafetyWindowMinutes { get; set; }
}
