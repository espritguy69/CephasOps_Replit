namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Replay safety window: events with OccurredAtUtc newer than (now - window) are excluded from replay
/// to reduce replay/live overlap and race conditions. Safe by default.
/// </summary>
public static class ReplaySafetyWindow
{
    /// <summary>Default safety window in minutes. Events newer than (UtcNow - this) are excluded.</summary>
    public const int DefaultWindowMinutes = 5;

    /// <summary>Effective cutoff timestamp: events with OccurredAtUtc &gt; this are excluded.</summary>
    public static DateTime GetCutoffUtc(int windowMinutes = DefaultWindowMinutes) =>
        DateTime.UtcNow.AddMinutes(-Math.Max(1, windowMinutes));
}
