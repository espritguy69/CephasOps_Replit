using System.Diagnostics.Metrics;

namespace CephasOps.Application.Events;

/// <summary>
/// Structured event processing metrics for the Event Bus dispatcher. Uses System.Diagnostics.Metrics;
/// export via OpenTelemetry or Prometheus if needed.
/// </summary>
public sealed class EventBusDispatcherMetrics
{
    public const string MeterName = "CephasOps.EventBus";

    private readonly Meter _meter;
    private readonly Counter<long> _eventsPersisted;
    private readonly Counter<long> _eventsDispatched;
    private readonly Counter<long> _eventsSucceeded;
    private readonly Counter<long> _eventsFailed;
    private readonly Counter<long> _eventsRetried;
    private readonly Counter<long> _eventsDeadLettered;
    private readonly Counter<long> _eventsRecoveredFromStuck;
    private readonly Counter<long> _dispatcherClaimed;
    private readonly Counter<long> _eventsNonRetryableFailed;
    private readonly Counter<long> _eventsBulkReplayed;
    private readonly Counter<long> _eventsBulkCancelled;
    private readonly Histogram<double> _eventProcessingLatencySeconds;
    private readonly Histogram<double> _attemptDurationSeconds;
    private readonly EventBusMetricsSnapshot _snapshot;

    public EventBusDispatcherMetrics(EventBusMetricsSnapshot snapshot)
    {
        _snapshot = snapshot ?? new EventBusMetricsSnapshot();
        _meter = new Meter(MeterName, "1.0");
        _eventsPersisted = _meter.CreateCounter<long>("eventbus.events.persisted", description: "Events persisted to store");
        _eventsDispatched = _meter.CreateCounter<long>("eventbus.events.dispatched", description: "Events dispatched from store");
        _eventsSucceeded = _meter.CreateCounter<long>("eventbus.events.succeeded", description: "Events processed successfully");
        _eventsFailed = _meter.CreateCounter<long>("eventbus.events.failed", description: "Events failed (will retry or dead-letter)");
        _eventsRetried = _meter.CreateCounter<long>("eventbus.events.retried", description: "Events scheduled for retry");
        _eventsDeadLettered = _meter.CreateCounter<long>("eventbus.events.dead_lettered", description: "Events moved to dead-letter");
        _eventsRecoveredFromStuck = _meter.CreateCounter<long>("eventbus.events.recovered_from_stuck", description: "Events recovered from stuck Processing");
        _dispatcherClaimed = _meter.CreateCounter<long>("eventbus.dispatcher.claimed", description: "Events claimed per batch (Phase 7)");
        _eventsNonRetryableFailed = _meter.CreateCounter<long>("eventbus.events.non_retryable_failed", description: "Events failed as non-retryable (poison)");
        _eventsBulkReplayed = _meter.CreateCounter<long>("eventbus.events.bulk_replayed", description: "Events replayed via bulk action");
        _eventsBulkCancelled = _meter.CreateCounter<long>("eventbus.events.bulk_cancelled", description: "Events cancelled via bulk action");
        _eventProcessingLatencySeconds = _meter.CreateHistogram<double>(
            "eventbus.event.processing_latency_seconds", unit: "s", description: "Time from CreatedAtUtc to ProcessedAtUtc");
        _attemptDurationSeconds = _meter.CreateHistogram<double>(
            "eventbus.event.attempt_duration_seconds", unit: "s", description: "Duration of a single dispatch attempt (Phase 7)");

        _meter.CreateObservableGauge("eventbus.pending_count", () => _snapshot.PendingEventCount, description: "Current pending event count");
        _meter.CreateObservableGauge("eventbus.failed_count", () => _snapshot.FailedEventCount, description: "Current failed event count");
        _meter.CreateObservableGauge("eventbus.dead_letter_count", () => _snapshot.DeadLetterEventCount, description: "Current dead-letter event count");
        _meter.CreateObservableGauge("eventbus.oldest_pending_event_age_seconds", () => _snapshot.OldestPendingEventAgeSeconds, description: "Age in seconds of oldest pending event");
        _meter.CreateObservableGauge("eventbus.dispatcher.parallel_workers", () => _snapshot.ParallelWorkerCount, description: "Number of event workers currently processing a batch");
        _meter.CreateObservableGauge("eventbus.dispatcher.inflight", () => _snapshot.ParallelWorkerCount, description: "Events currently in-flight (same as parallel_workers)");
        _meter.CreateObservableGauge("eventbus.backpressure.level", () => (int)_snapshot.BackpressureLevel, description: "Backpressure level: 0=None, 1=Reduced, 2=Throttled, 3=Paused (Phase 8)");
    }

    public void RecordEventPersisted(string eventType, Guid? companyId)
    {
        var tags = Tags(eventType, companyId, null);
        _eventsPersisted.Add(1, tags);
    }

    public void RecordEventDispatched(string eventType, Guid? companyId, string? handlerName)
    {
        var tags = Tags(eventType, companyId, handlerName);
        _eventsDispatched.Add(1, tags);
    }

    public void RecordEventSucceeded(string eventType, Guid? companyId, string? handlerName, double latencySeconds)
    {
        var tags = Tags(eventType, companyId, handlerName);
        _eventsSucceeded.Add(1, tags);
        _eventProcessingLatencySeconds.Record(latencySeconds, tags);
    }

    public void RecordEventFailed(string eventType, Guid? companyId, string? handlerName)
    {
        var tags = Tags(eventType, companyId, handlerName);
        _eventsFailed.Add(1, tags);
    }

    public void RecordEventRetried(string eventType, Guid? companyId)
    {
        var tags = Tags(eventType, companyId, null);
        _eventsRetried.Add(1, tags);
    }

    public void RecordEventDeadLettered(string eventType, Guid? companyId)
    {
        var tags = Tags(eventType, companyId, null);
        _eventsDeadLettered.Add(1, tags);
    }

    public void RecordEventRecoveredFromStuck(string eventType, Guid? companyId)
    {
        var tags = Tags(eventType, companyId, null);
        _eventsRecoveredFromStuck.Add(1, tags);
    }

    /// <summary>Record multiple events recovered from stuck Processing (e.g. from ResetStuckProcessingAsync).</summary>
    public void RecordEventsRecoveredFromStuck(int count)
    {
        if (count <= 0) return;
        _eventsRecoveredFromStuck.Add(count);
    }

    /// <summary>Record events claimed in a batch (Phase 7).</summary>
    public void RecordClaimed(int count)
    {
        if (count <= 0) return;
        _dispatcherClaimed.Add(count);
    }

    /// <summary>Record non-retryable (poison) failure (Phase 7).</summary>
    public void RecordNonRetryableFailed(string eventType, Guid? companyId)
    {
        var tags = Tags(eventType, companyId, null);
        _eventsNonRetryableFailed.Add(1, tags);
    }

    /// <summary>Record bulk replay count (Phase 7).</summary>
    public void RecordBulkReplayed(int count)
    {
        if (count <= 0) return;
        _eventsBulkReplayed.Add(count);
    }

    /// <summary>Record bulk cancel count (Phase 7).</summary>
    public void RecordBulkCancelled(int count)
    {
        if (count <= 0) return;
        _eventsBulkCancelled.Add(count);
    }

    /// <summary>Record attempt duration in seconds (Phase 7).</summary>
    public void RecordAttemptDuration(double durationSeconds, string eventType, Guid? companyId)
    {
        var tags = Tags(eventType, companyId, null);
        _attemptDurationSeconds.Record(durationSeconds, tags);
    }

    public void UpdateSnapshot(int pendingCount, int failedCount, int deadLetterCount, double oldestPendingEventAgeSeconds)
    {
        _snapshot.Update(pendingCount, failedCount, deadLetterCount, oldestPendingEventAgeSeconds);
    }

    /// <summary>Update backpressure level for gauge (Phase 8).</summary>
    public void UpdateBackpressureLevel(Backpressure.BackpressureLevel level)
    {
        _snapshot.SetBackpressureLevel(level);
    }

    /// <summary>Update the current parallel worker count for the dispatcher (used by observable gauge).</summary>
    public void SetParallelWorkerCount(int count)
    {
        _snapshot.SetParallelWorkerCount(count);
    }

    private static KeyValuePair<string, object?>[] Tags(string eventType, Guid? companyId, string? handlerName)
    {
        var list = new List<KeyValuePair<string, object?>>
        {
            new("event_type", eventType ?? "unknown"),
            new("company_id", companyId?.ToString() ?? "")
        };
        if (!string.IsNullOrEmpty(handlerName))
            list.Add(new KeyValuePair<string, object?>("handler_name", handlerName));
        return list.ToArray();
    }
}

/// <summary>
/// Thread-safe snapshot for Event Bus gauges (pending/failed/dead-letter counts, oldest pending age, parallel workers, backpressure).
/// </summary>
public sealed class EventBusMetricsSnapshot
{
    private int _pendingEventCount;
    private int _failedEventCount;
    private int _deadLetterEventCount;
    private double _oldestPendingEventAgeSeconds;
    private int _parallelWorkerCount;
    private int _backpressureLevel; // 0=None, 1=Reduced, 2=Throttled, 3=Paused
    private readonly object _lock = new();

    public int PendingEventCount { get { lock (_lock) { return _pendingEventCount; } } }
    public int FailedEventCount { get { lock (_lock) { return _failedEventCount; } } }
    public int DeadLetterEventCount { get { lock (_lock) { return _deadLetterEventCount; } } }
    public double OldestPendingEventAgeSeconds { get { lock (_lock) { return _oldestPendingEventAgeSeconds; } } }
    public int ParallelWorkerCount { get { lock (_lock) { return _parallelWorkerCount; } } }
    /// <summary>Backpressure level: 0=None, 1=Reduced, 2=Throttled, 3=Paused (Phase 8).</summary>
    public Backpressure.BackpressureLevel BackpressureLevel { get { lock (_lock) { return (Backpressure.BackpressureLevel)_backpressureLevel; } } }

    public void Update(int pendingCount, int failedCount, int deadLetterCount, double oldestPendingEventAgeSeconds)
    {
        lock (_lock)
        {
            _pendingEventCount = pendingCount;
            _failedEventCount = failedCount;
            _deadLetterEventCount = deadLetterCount;
            _oldestPendingEventAgeSeconds = oldestPendingEventAgeSeconds;
        }
    }

    /// <summary>Set backpressure level from backpressure service (Phase 8).</summary>
    public void SetBackpressureLevel(Backpressure.BackpressureLevel level)
    {
        lock (_lock) { _backpressureLevel = (int)level; }
    }

    public void SetParallelWorkerCount(int count)
    {
        lock (_lock)
        {
            _parallelWorkerCount = Math.Max(0, count);
        }
    }
}
