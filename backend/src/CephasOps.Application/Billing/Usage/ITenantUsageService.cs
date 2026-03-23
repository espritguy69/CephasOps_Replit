namespace CephasOps.Application.Billing.Usage;

/// <summary>
/// Records tenant usage for metering and billing (Phase 4). Safe to call from any tenant-scoped operation.
/// When CompanyId has no TenantId (legacy), recording is skipped. Uses monthly buckets; upserts one row per (tenant, metric, month).
/// </summary>
public interface ITenantUsageService
{
    /// <summary>
    /// Record usage for a metric in the current month. Resolves TenantId from CompanyId; no-op when tenant cannot be resolved.
    /// </summary>
    Task RecordUsageAsync(Guid? companyId, string metricKey, decimal quantity = 1, CancellationToken cancellationToken = default);

    /// <summary>Recalculate TotalUsers and ActiveUsers for a tenant and write to current month bucket.</summary>
    Task RecalculateUserMetricsForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Add or subtract storage bytes for the tenant (current month bucket). Use positive on upload, negative on delete. Resolves TenantId from CompanyId.</summary>
    Task RecordStorageDeltaAsync(Guid? companyId, long deltaBytes, CancellationToken cancellationToken = default);

    /// <summary>Get current month storage bytes for the tenant (from CompanyId). Returns 0 if no record or no tenant.</summary>
    Task<long> GetCurrentStorageBytesAsync(Guid? companyId, CancellationToken cancellationToken = default);
}
