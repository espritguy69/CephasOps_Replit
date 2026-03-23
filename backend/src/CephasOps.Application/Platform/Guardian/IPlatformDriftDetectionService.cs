namespace CephasOps.Application.Platform.Guardian;

/// <summary>Platform Guardian: compare current config to baseline and report drift.</summary>
public interface IPlatformDriftDetectionService
{
    /// <summary>Run drift detection and return report. Does not throw; classification only.</summary>
    Task<PlatformDriftResultDto> DetectAsync(CancellationToken cancellationToken = default);
}
