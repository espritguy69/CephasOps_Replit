using Microsoft.Extensions.Options;

namespace CephasOps.Application.Events.Backpressure;

/// <summary>
/// Adaptive backpressure based on event store counts (from metrics snapshot) and configured thresholds.
/// Gradual degradation: reduces batch size and increases delay as pending/failed/dead-letter grow.
/// </summary>
public sealed class EventBusBackpressureService : IEventBusBackpressureService
{
    private readonly EventBusMetricsSnapshot? _snapshot;
    private readonly EventBusDispatcherOptions _options;
    private readonly BackpressureOptions _backpressureOptions;

    public EventBusBackpressureService(
        IOptions<EventBusDispatcherOptions> options,
        IOptions<BackpressureOptions>? backpressureOptions = null,
        EventBusMetricsSnapshot? snapshot = null)
    {
        _options = options?.Value ?? new EventBusDispatcherOptions();
        _backpressureOptions = backpressureOptions?.Value ?? new BackpressureOptions();
        _snapshot = snapshot;
    }

    /// <inheritdoc />
    public EventBusBackpressureState GetState()
    {
        var (pending, failed, deadLetter, ageSeconds, processing) = GetSnapshot();

        var level = BackpressureLevel.None;
        var reason = (string?)null;

        if (pending >= _backpressureOptions.PausedPendingThreshold)
        {
            level = BackpressureLevel.Paused;
            reason = $"Pending count {pending} >= {_backpressureOptions.PausedPendingThreshold}";
        }
        else if (deadLetter >= _options.DeadLetterUnhealthyThreshold)
        {
            level = BackpressureLevel.Throttled;
            reason = $"DeadLetter count {deadLetter} >= {_options.DeadLetterUnhealthyThreshold}";
        }
        else if (pending >= _backpressureOptions.ThrottledPendingThreshold || ageSeconds >= _backpressureOptions.OldestPendingAgeThrottleSeconds)
        {
            level = BackpressureLevel.Throttled;
            reason = pending >= _backpressureOptions.ThrottledPendingThreshold
                ? $"Pending count {pending} >= {_backpressureOptions.ThrottledPendingThreshold}"
                : $"Oldest pending age {ageSeconds:F0}s >= {_backpressureOptions.OldestPendingAgeThrottleSeconds}s";
        }
        else if (pending >= _backpressureOptions.ReducedPendingThreshold || failed >= _backpressureOptions.FailedReducedThreshold)
        {
            level = BackpressureLevel.Reduced;
            reason = pending >= _backpressureOptions.ReducedPendingThreshold
                ? $"Pending count {pending} >= {_backpressureOptions.ReducedPendingThreshold}"
                : $"Failed count {failed} >= {_backpressureOptions.FailedReducedThreshold}";
        }

        return new EventBusBackpressureState
        {
            Level = level,
            PendingCount = pending,
            FailedCount = failed,
            DeadLetterCount = deadLetter,
            OldestPendingAgeSeconds = ageSeconds,
            ProcessingCount = processing,
            Reason = reason
        };
    }

    /// <inheritdoc />
    public int? GetSuggestedBatchSize(int configuredBatchSize)
    {
        var state = GetState();
        return state.Level switch
        {
            BackpressureLevel.Paused => 0,
            BackpressureLevel.Throttled => Math.Max(1, configuredBatchSize / 4),
            BackpressureLevel.Reduced => Math.Max(1, configuredBatchSize / 2),
            _ => null
        };
    }

    /// <inheritdoc />
    public int GetSuggestedDelayMs()
    {
        var state = GetState();
        return state.Level switch
        {
            BackpressureLevel.Paused => _backpressureOptions.PausedDelayMs,
            BackpressureLevel.Throttled => _backpressureOptions.ThrottledDelayMs,
            BackpressureLevel.Reduced => _backpressureOptions.ReducedDelayMs,
            _ => 0
        };
    }

    private (int Pending, int Failed, int DeadLetter, double OldestAgeSeconds, int Processing) GetSnapshot()
    {
        if (_snapshot == null)
            return (0, 0, 0, 0, 0);
        return (_snapshot.PendingEventCount, _snapshot.FailedEventCount, _snapshot.DeadLetterEventCount,
            _snapshot.OldestPendingEventAgeSeconds, _snapshot.ParallelWorkerCount);
    }
}

/// <summary>
/// Options for adaptive backpressure thresholds and delays.
/// </summary>
public class BackpressureOptions
{
    public const string SectionName = "EventBus:Backpressure";

    /// <summary>Pending count at or above this: reduce batch size by half. Default 1000.</summary>
    public int ReducedPendingThreshold { get; set; } = 1000;

    /// <summary>Failed count at or above this: reduce batch size. Default 200.</summary>
    public int FailedReducedThreshold { get; set; } = 200;

    /// <summary>Pending count at or above this: throttle (batch/4, extra delay). Default 5000.</summary>
    public int ThrottledPendingThreshold { get; set; } = 5000;

    /// <summary>Oldest pending event age (seconds) at or above this: throttle. Default 600 (10 min).</summary>
    public int OldestPendingAgeThrottleSeconds { get; set; } = 600;

    /// <summary>Pending count at or above this: pause (batch=0, max delay). Default 20000.</summary>
    public int PausedPendingThreshold { get; set; } = 20000;

    /// <summary>Extra delay in ms when Reduced. Default 500.</summary>
    public int ReducedDelayMs { get; set; } = 500;

    /// <summary>Extra delay in ms when Throttled. Default 2000.</summary>
    public int ThrottledDelayMs { get; set; } = 2000;

    /// <summary>Extra delay in ms when Paused. Default 10000.</summary>
    public int PausedDelayMs { get; set; } = 10000;
}
