using CephasOps.Application.Platform.Guardian;

namespace CephasOps.Application.Platform;

/// <summary>Admin dashboard: aggregated tenant analytics from TenantMetricsDaily/Monthly.</summary>
public class PlatformDashboardAnalyticsDto
{
    public int ActiveTenantsCount { get; set; }
    public int TotalTenantsCount { get; set; }
    public MonthlyUsageSummaryDto? CurrentMonth { get; set; }
    public MonthlyUsageSummaryDto? PreviousMonth { get; set; }
    public long TotalStorageBytes { get; set; }
    public int TotalJobVolumeLast30Days { get; set; }
}

public class MonthlyUsageSummaryDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int TenantCount { get; set; }
    public long TotalStorageBytes { get; set; }
    public int TotalApiCalls { get; set; }
    public int TotalOrdersCreated { get; set; }
    public int TotalBackgroundJobsExecuted { get; set; }
}

/// <summary>SaaS scaling: per-tenant health for GET /api/platform/analytics/tenant-health.</summary>
public class TenantHealthDto
{
    public Guid TenantId { get; set; }
    public int ApiRequestsLast24h { get; set; }
    public int JobFailuresLast24h { get; set; }
    public long StorageBytes { get; set; }
    public int ActiveUsers { get; set; }
    public DateTime? LastActivityUtc { get; set; }
    /// <summary>Healthy | Warning | Critical</summary>
    public string HealthStatus { get; set; } = "Healthy";
}

/// <summary>Platform observability: single row for tenant operations overview table.</summary>
public class TenantOperationsOverviewItemDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int RequestCountLast24h { get; set; }
    public int JobFailuresLast24h { get; set; }
    public int JobsOkLast24h { get; set; }
    public int NotificationsSentLast24h { get; set; }
    public int NotificationsFailedLast24h { get; set; }
    public int IntegrationsDeliveredLast24h { get; set; }
    public int IntegrationsFailedLast24h { get; set; }
    public DateTime? LastActivityUtc { get; set; }
    /// <summary>0–100 health score from TenantHealthScoringService.</summary>
    public int? HealthScore { get; set; }
    /// <summary>Healthy | Warning | Critical</summary>
    public string HealthStatus { get; set; } = "Healthy";
    /// <summary>True when HealthStatus is not Healthy.</summary>
    public bool HasWarnings { get; set; }
}

/// <summary>Platform observability: tenant detail with trends and recent anomalies.</summary>
public class TenantOperationsDetailDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    /// <summary>Last 7 days: date (UTC date), request count, job failures, jobs ok, notifications sent/failed, integrations delivered/failed.</summary>
    public IReadOnlyList<TenantOperationsDailyBucketDto> DailyBuckets { get; set; } = Array.Empty<TenantOperationsDailyBucketDto>();
    /// <summary>Recent anomaly/warning events for this tenant (newest first).</summary>
    public IReadOnlyList<TenantAnomalyDto> RecentAnomalies { get; set; } = Array.Empty<TenantAnomalyDto>();
}

/// <summary>One day bucket for tenant operations trend.</summary>
public class TenantOperationsDailyBucketDto
{
    public DateTime DateUtc { get; set; }
    public int RequestCount { get; set; }
    public int JobFailures { get; set; }
    public int JobsOk { get; set; }
    public int NotificationsSent { get; set; }
    public int NotificationsFailed { get; set; }
    public int IntegrationsDelivered { get; set; }
    public int IntegrationsFailed { get; set; }
}

/// <summary>Platform observability: summary for dashboard cards (failed counts today, tenant warning count).</summary>
public class PlatformOperationsSummaryDto
{
    public int ActiveTenantsCount { get; set; }
    public int TotalTenantsCount { get; set; }
    public int FailedJobsToday { get; set; }
    public int FailedNotificationsToday { get; set; }
    public int FailedIntegrationsToday { get; set; }
    public int TenantsWithWarningsCount { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
}
