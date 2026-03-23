using Microsoft.Extensions.Options;

namespace CephasOps.Application.Workflow;

/// <summary>
/// Configurable thresholds (minutes) after which a Running background job is considered stale and reaped.
/// </summary>
public class BackgroundJobStaleOptions
{
    public const string SectionName = "BackgroundJobs:StaleRunning";

    /// <summary>Stale threshold for EmailIngest jobs (minutes). Default 10.</summary>
    public int EmailIngestMinutes { get; set; } = 10;

    /// <summary>Stale threshold for all other job types (minutes). Default 120 (2 hours).</summary>
    public int DefaultMinutes { get; set; } = 120;
}
