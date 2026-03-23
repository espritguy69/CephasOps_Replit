namespace CephasOps.Application.Scheduler;

/// <summary>
/// Configuration for the distributed job polling coordinator.
/// </summary>
public class SchedulerOptions
{
    public const string SectionName = "Scheduler";

    /// <summary>Polling interval in seconds. Default 15. Recommended 10–30.</summary>
    public int PollIntervalSeconds { get; set; } = 15;

    /// <summary>Maximum number of jobs to claim per poll cycle. Default 10.</summary>
    public int MaxJobsPerPoll { get; set; } = 10;
}
