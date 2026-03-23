using CephasOps.Domain.Common;

namespace CephasOps.Domain.Scheduler.Entities;

/// <summary>
/// SI availability entity - daily availability per SI
/// </summary>
public class SiAvailability : CompanyScopedEntity
{
    public Guid ServiceInstallerId { get; set; }
    public DateTime Date { get; set; }
    public bool IsWorkingDay { get; set; }
    public TimeSpan? WorkingFrom { get; set; }
    public TimeSpan? WorkingTo { get; set; }
    public int MaxJobs { get; set; }
    public int CurrentJobsCount { get; set; }
    public string? Notes { get; set; }
}

