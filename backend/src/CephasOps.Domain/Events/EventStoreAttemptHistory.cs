namespace CephasOps.Domain.Events;

/// <summary>
/// One record per dispatch attempt for an event. Used for audit, replay, and poison analysis (Phase 7).
/// </summary>
public class EventStoreAttemptHistory
{
    public long Id { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string HandlerName { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    /// <summary>Success | Retry | DeadLetter | SkippedDuplicate | RecoveredFromStuck</summary>
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public int? DurationMs { get; set; }
    public string? ProcessingNodeId { get; set; }
    public string? ErrorType { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StackTraceSummary { get; set; }
    public bool WasRetried { get; set; }
    public bool WasDeadLettered { get; set; }
}
