namespace CephasOps.Application.Platform.Guardian;

/// <summary>Platform Guardian: options for drift detection (baseline and reporting).</summary>
public class PlatformDriftDetectionOptions
{
    public const string SectionName = "PlatformGuardian:DriftDetection";

    public bool Enabled { get; set; } = true;
    /// <summary>If set, write machine-readable report to this path (e.g. tools/architecture/platform_guardian_report.json).</summary>
    public string? ReportPath { get; set; }
}
