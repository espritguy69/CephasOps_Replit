namespace CephasOps.Application.Files;

/// <summary>SaaS storage lifecycle: tier transition rules (days since last access or creation).</summary>
public class StorageLifecycleOptions
{
    public const string SectionName = "SaaS:StorageLifecycle";

    public bool Enabled { get; set; } = true;
    /// <summary>Run interval (e.g. daily).</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(24);
    /// <summary>Days without access after which to mark Warm. 0 = disable.</summary>
    public int WarmAfterDays { get; set; } = 90;
    /// <summary>Days without access after which to mark Cold. 0 = disable.</summary>
    public int ColdAfterDays { get; set; } = 180;
    /// <summary>Days without access after which to mark Archive. 0 = disable.</summary>
    public int ArchiveAfterDays { get; set; } = 365;
    /// <summary>Max files to update per tenant per run (batch size).</summary>
    public int MaxFilesPerTenantPerRun { get; set; } = 500;
}
