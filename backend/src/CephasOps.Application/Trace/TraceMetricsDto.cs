namespace CephasOps.Application.Trace.DTOs;

/// <summary>
/// Minimal derived metrics for operational trace (e.g. failure counts in a time window).
/// Used for admin UI without overbuilding dashboards.
/// </summary>
public class TraceMetricsDto
{
    /// <summary>Start of the window (UTC).</summary>
    public DateTime FromUtc { get; set; }

    /// <summary>End of the window (UTC).</summary>
    public DateTime ToUtc { get; set; }

    /// <summary>Events in status Failed in the window.</summary>
    public int FailedEventsCount { get; set; }

    /// <summary>Events in status DeadLetter in the window.</summary>
    public int DeadLetterEventsCount { get; set; }

    /// <summary>Job runs in status Failed in the window.</summary>
    public int FailedJobRunsCount { get; set; }

    /// <summary>Job runs in status DeadLetter in the window.</summary>
    public int DeadLetterJobRunsCount { get; set; }

    /// <summary>Distinct correlation IDs that have at least one failed or dead-letter event or job run in the window.</summary>
    public int CorrelationChainsWithFailuresCount { get; set; }
}
