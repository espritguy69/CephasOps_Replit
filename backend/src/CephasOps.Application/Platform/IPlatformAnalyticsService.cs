namespace CephasOps.Application.Platform;

/// <summary>Platform admin: dashboard analytics from TenantMetricsDaily/Monthly and Tenants.</summary>
public interface IPlatformAnalyticsService
{
    Task<PlatformDashboardAnalyticsDto> GetDashboardAnalyticsAsync(CancellationToken cancellationToken = default);
    /// <summary>Per-tenant health for observability (API requests, job failures, storage, active users, status).</summary>
    Task<IReadOnlyList<TenantHealthDto>> GetTenantHealthAsync(CancellationToken cancellationToken = default);
    /// <summary>Tenant operations overview for platform observability dashboard (name, status, requests, jobs, notifications, integrations, last activity, warnings).</summary>
    Task<IReadOnlyList<TenantOperationsOverviewItemDto>> GetTenantOperationsOverviewAsync(CancellationToken cancellationToken = default);
    /// <summary>Tenant operations detail: daily trend buckets and recent anomalies for one tenant.</summary>
    Task<TenantOperationsDetailDto?> GetTenantOperationsDetailAsync(Guid tenantId, CancellationToken cancellationToken = default);
    /// <summary>Platform operations summary: active tenants, failed jobs/notifications/integrations today, tenants with warnings.</summary>
    Task<PlatformOperationsSummaryDto> GetPlatformOperationsSummaryAsync(CancellationToken cancellationToken = default);
}
