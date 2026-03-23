namespace CephasOps.Domain.Events;

/// <summary>
/// Records per-attempt execution history for event dispatch (Phase 7). One record per attempt (success, retry, dead-letter, skipped, recovered).
/// </summary>
public interface IEventStoreAttemptHistoryStore
{
    /// <summary>
    /// Append one attempt record. Call after each dispatch attempt (success, retry, dead-letter, skipped duplicate, recovered from stuck).
    /// </summary>
    Task RecordAttemptAsync(EventStoreAttemptRecord record, CancellationToken cancellationToken = default);
}

/// <summary>
/// Data for one dispatch attempt to be written to EventStoreAttemptHistory.
/// </summary>
public class EventStoreAttemptRecord
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string HandlerName { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public string Status { get; set; } = string.Empty; // Success | Retry | DeadLetter | SkippedDuplicate | RecoveredFromStuck
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
