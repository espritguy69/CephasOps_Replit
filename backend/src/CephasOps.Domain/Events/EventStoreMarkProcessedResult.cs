namespace CephasOps.Domain.Events;

/// <summary>
/// Result of marking an event as processed (or failed/dead-letter). Used for metrics and observability.
/// </summary>
public class EventStoreMarkProcessedResult
{
    public bool Success { get; set; }
    /// <summary>Processed | Failed | DeadLetter</summary>
    public string NewStatus { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string? LastHandler { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
    /// <summary>When true, failure was classified as non-retryable (poison) and event moved to DeadLetter immediately.</summary>
    public bool IsNonRetryable { get; set; }
    /// <summary>Retry count after this attempt (for attempt history AttemptNumber).</summary>
    public int RetryCount { get; set; }
}
