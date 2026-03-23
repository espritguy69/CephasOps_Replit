namespace CephasOps.Api.Health;

/// <summary>Options for JobBacklogHealthCheck thresholds.</summary>
public class JobBacklogHealthCheckOptions
{
    public const string SectionName = "HealthChecks:JobBacklog";

    /// <summary>Pending count above which the check reports Degraded. Default 1000.</summary>
    public int PendingDegradedThreshold { get; set; } = 1000;

    /// <summary>Dead-letter count above which the check reports Unhealthy. Default 100.</summary>
    public int DeadLetterUnhealthyThreshold { get; set; } = 100;

    /// <summary>Dead-letter count above which the check reports Degraded. Default 10.</summary>
    public int DeadLetterDegradedThreshold { get; set; } = 10;
}
