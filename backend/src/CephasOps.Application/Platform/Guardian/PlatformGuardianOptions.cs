namespace CephasOps.Application.Platform.Guardian;

/// <summary>Platform Guardian: scheduling and feature flags.</summary>
public class PlatformGuardianOptions
{
    public const string SectionName = "PlatformGuardian";

    public bool Enabled { get; set; } = true;
    /// <summary>Run interval in minutes (e.g. 15 or 60). Minimum 5 to avoid overload.</summary>
    public int RunIntervalMinutes { get; set; } = 60;
    /// <summary>When true, run anomaly detection in the scheduled cycle.</summary>
    public bool RunAnomalyDetection { get; set; } = true;
    /// <summary>When true, run drift detection in the scheduled cycle.</summary>
    public bool RunDriftDetection { get; set; } = true;
    /// <summary>When true, run performance watchdog in the scheduled cycle.</summary>
    public bool RunPerformanceWatchdog { get; set; } = true;
}
