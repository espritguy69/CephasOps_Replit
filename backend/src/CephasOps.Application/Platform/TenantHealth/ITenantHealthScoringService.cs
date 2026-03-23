namespace CephasOps.Application.Platform.TenantHealth;

/// <summary>Enterprise: compute and store tenant health score from operational metrics (job/notif/integration failures, API error rate, activity).</summary>
public interface ITenantHealthScoringService
{
    /// <summary>Compute health score for a tenant for the given date and update TenantMetricsDaily. Platform bypass only.</summary>
    Task ComputeAndStoreAsync(Guid tenantId, DateTime dateUtc, CancellationToken cancellationToken = default);

    /// <summary>Compute and store health for all tenants that have daily metrics for the date. Platform bypass only.</summary>
    Task ComputeAndStoreForAllTenantsAsync(DateTime dateUtc, CancellationToken cancellationToken = default);
}
