namespace CephasOps.Application.Workflow;

/// <summary>
/// Options for tenant-aware job fairness: limit how many jobs per tenant are processed per cycle so one tenant cannot monopolize workers.
/// </summary>
public class BackgroundJobFairnessOptions
{
    public const string SectionName = "BackgroundJobs:Fairness";

    /// <summary>Max jobs per tenant per processing cycle (round-robin style). Default 5. 0 = no limit.</summary>
    public int MaxJobsPerTenantPerCycle { get; set; } = 5;
}
