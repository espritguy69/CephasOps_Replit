namespace CephasOps.Application.Platform.Guardian;

/// <summary>Platform Guardian: detect and persist tenant anomalies; list for platform admin.</summary>
public interface ITenantAnomalyDetectionService
{
    /// <summary>Run detection for all active tenants and persist new anomaly events.</summary>
    Task RunDetectionAsync(CancellationToken cancellationToken = default);
    /// <summary>List recent anomaly events (e.g. last 7 days), optionally by tenant or severity.</summary>
    Task<IReadOnlyList<TenantAnomalyDto>> GetAnomaliesAsync(DateTime? sinceUtc = null, Guid? tenantId = null, string? severity = null, int take = 500, CancellationToken cancellationToken = default);
}
