namespace CephasOps.Application.Platform.Guardian;

/// <summary>Platform Guardian: surface performance degradation and multi-tenant hot path health.</summary>
public interface IPerformanceWatchdogService
{
    /// <summary>Collect performance health (slow queries, job delays, degraded endpoints, tenants with high latency impact).</summary>
    Task<PerformanceHealthDto> GetPerformanceHealthAsync(CancellationToken cancellationToken = default);
}
