namespace CephasOps.Application.Events;

/// <summary>
/// Options for the Event Store dispatcher (Phase 4): polling, batch size, retries, and backoff.
/// </summary>
public class EventBusDispatcherOptions
{
    public const string SectionName = "EventBus:Dispatcher";

    /// <summary>Polling interval in seconds. Default 15.</summary>
    public int PollingIntervalSeconds { get; set; } = 15;

    /// <summary>Identifier for this dispatcher node (e.g. hostname or instance id). Used for lease ownership. Default null (single-node).</summary>
    public string? NodeId { get; set; }

    /// <summary>Processing lease duration in seconds. After this time an uncompleted claim is considered expired and recoverable. Default 300 (5 min).</summary>
    public int ProcessingLeaseSeconds { get; set; } = 300;

    /// <summary>Max concurrent event processors per batch (parallel workers). Default 8.</summary>
    public int MaxConcurrentDispatchers { get; set; } = 8;

    /// <summary>Max events to claim per poll. Default 20.</summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>Alias for BatchSize (Phase 7). Max events per poll.</summary>
    public int MaxEventsPerPoll { get => BatchSize; set => BatchSize = value; }

    /// <summary>Delay in ms when no work was found (idle). Zero = use PollingIntervalSeconds. Default 0.</summary>
    public int DispatcherIdleDelayMs { get; set; }

    /// <summary>Delay in ms when work was processed (backpressure when backlog exists). Zero = use PollingIntervalSeconds. Default 0.</summary>
    public int DispatcherBusyDelayMs { get; set; }

    /// <summary>After this many handler failures, event is marked DeadLetter. Default 5.</summary>
    public int MaxRetriesBeforeDeadLetter { get; set; } = 5;

    /// <summary>Retry delays in seconds per attempt: 1→+1min, 2→+5min, 3→+15min, 4→+60min, 5→dead-letter. Default: 60, 300, 900, 3600.</summary>
    public int[] RetryDelaySeconds { get; set; } = { 60, 300, 900, 3600 };

    /// <summary>Events in Processing longer than this (minutes) are reset to Failed with NextRetryAtUtc = now for recovery after crash/termination. Default 15.</summary>
    public int StuckProcessingTimeoutMinutes { get; set; } = 15;

    /// <summary>Log a warning when oldest pending event age (seconds) exceeds this (minutes). Zero = disabled. Default 30.</summary>
    public int OldestPendingEventAgeWarningMinutes { get; set; } = 30;

    /// <summary>Health: pending count above this is Degraded. Default 5000.</summary>
    public int PendingCountDegradedThreshold { get; set; } = 5000;

    /// <summary>Health: dead-letter count above this is Unhealthy. Default 500.</summary>
    public int DeadLetterUnhealthyThreshold { get; set; } = 500;

    /// <summary>Health: dead-letter count above this is Degraded. Default 100.</summary>
    public int DeadLetterDegradedThreshold { get; set; } = 100;
}
