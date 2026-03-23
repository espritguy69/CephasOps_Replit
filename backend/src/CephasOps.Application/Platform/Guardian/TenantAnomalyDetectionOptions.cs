namespace CephasOps.Application.Platform.Guardian;

/// <summary>Platform Guardian: options for tenant anomaly detection.</summary>
public class TenantAnomalyDetectionOptions
{
    public const string SectionName = "PlatformGuardian:AnomalyDetection";

    public bool Enabled { get; set; } = true;
    /// <summary>API calls last 24h above this multiple of 7-day average → Warning.</summary>
    public double ApiSpikeWarningMultiple { get; set; } = 2.0;
    /// <summary>API calls last 24h above this multiple → Critical.</summary>
    public double ApiSpikeCriticalMultiple { get; set; } = 5.0;
    /// <summary>Job failures in last 24h above this → Warning.</summary>
    public int JobFailureSpikeWarning { get; set; } = 10;
    /// <summary>Job failures in last 24h above this → Critical.</summary>
    public int JobFailureSpikeCritical { get; set; } = 50;
    /// <summary>Storage growth (current vs 7 days ago) above this fraction → Warning (e.g. 0.2 = 20%).</summary>
    public double StorageGrowthWarningFraction { get; set; } = 0.2;
    /// <summary>Storage growth above this fraction → Critical.</summary>
    public double StorageGrowthCriticalFraction { get; set; } = 0.5;
    /// <summary>Max anomaly events to persist per run per tenant (to avoid flood).</summary>
    public int MaxEventsPerTenantPerRun { get; set; } = 10;
}
