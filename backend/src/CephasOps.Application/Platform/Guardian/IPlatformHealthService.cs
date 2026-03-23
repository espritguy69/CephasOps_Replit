namespace CephasOps.Application.Platform.Guardian;

/// <summary>Platform Guardian: aggregated platform health (one operational view).</summary>
public interface IPlatformHealthService
{
    Task<PlatformHealthDto> GetPlatformHealthAsync(CancellationToken cancellationToken = default);
}
