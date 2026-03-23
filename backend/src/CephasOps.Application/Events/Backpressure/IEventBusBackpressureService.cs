namespace CephasOps.Application.Events.Backpressure;

/// <summary>
/// Provides adaptive backpressure state for the event bus dispatcher.
/// Used to reduce concurrency or intake when queue depth, lag, or failure rate is high.
/// </summary>
public interface IEventBusBackpressureService
{
    /// <summary>
    /// Gets the current backpressure state (e.g. from event store counts and metrics).
    /// </summary>
    EventBusBackpressureState GetState();

    /// <summary>
    /// Suggested batch size for the next claim (may be reduced under pressure). Null = use configured default.
    /// </summary>
    int? GetSuggestedBatchSize(int configuredBatchSize);

    /// <summary>
    /// Suggested extra delay in milliseconds before the next poll when under pressure. Zero = no extra delay.
    /// </summary>
    int GetSuggestedDelayMs();
}

/// <summary>
/// Current backpressure state for observability and throttling decisions.
/// </summary>
public sealed class EventBusBackpressureState
{
    public BackpressureLevel Level { get; set; }
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
    public int DeadLetterCount { get; set; }
    public double OldestPendingAgeSeconds { get; set; }
    public int ProcessingCount { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Backpressure level: None, Reduced, Throttled, Paused.
/// </summary>
public enum BackpressureLevel
{
    None = 0,
    Reduced = 1,
    Throttled = 2,
    Paused = 3
}
