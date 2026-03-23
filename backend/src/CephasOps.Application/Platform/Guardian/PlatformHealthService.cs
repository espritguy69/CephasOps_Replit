namespace CephasOps.Application.Platform.Guardian;

/// <summary>Platform Guardian: aggregates tenant health, anomalies, and performance into one platform health DTO.</summary>
public class PlatformHealthService : IPlatformHealthService
{
    private readonly IPlatformAnalyticsService _analyticsService;
    private readonly ITenantAnomalyDetectionService _anomalyService;
    private readonly IPerformanceWatchdogService _performanceWatchdog;

    public PlatformHealthService(
        IPlatformAnalyticsService analyticsService,
        ITenantAnomalyDetectionService anomalyService,
        IPerformanceWatchdogService performanceWatchdog)
    {
        _analyticsService = analyticsService;
        _anomalyService = anomalyService;
        _performanceWatchdog = performanceWatchdog;
    }

    public async Task<PlatformHealthDto> GetPlatformHealthAsync(CancellationToken cancellationToken = default)
    {
        var health = await _analyticsService.GetTenantHealthAsync(cancellationToken);
        var warningCount = health.Count(h => h.HealthStatus == "Warning");
        var criticalCount = health.Count(h => h.HealthStatus == "Critical");
        var totalActive = health.Count;

        var criticalAnomalies = await _anomalyService.GetAnomaliesAsync(
            sinceUtc: DateTime.UtcNow.AddDays(-1),
            tenantId: null,
            severity: "Critical",
            take: 1000,
            cancellationToken);
        var tenantsWithCriticalAnomaly = criticalAnomalies.Select(a => a.TenantId).Distinct().Count();

        var perf = await _performanceWatchdog.GetPerformanceHealthAsync(cancellationToken);
        var failedJobs = health.Sum(h => h.JobFailuresLast24h);
        var storageWarning = health.Count(h => h.HealthStatus == "Warning" && h.StorageBytes > 0); // heuristic

        var dto = new PlatformHealthDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            TotalActiveTenants = totalActive,
            TenantsInWarningState = warningCount,
            TenantsInCriticalAnomalyState = tenantsWithCriticalAnomaly,
            RateLimitBreachCountLast24h = 0, // from logs/metrics when available
            StuckJobResetsLast24h = null, // from watchdog counter when available
            FailedJobsLast24h = failedJobs,
            StorageWarningTenants = storageWarning,
            SuspiciousAuthOrImpersonationCount = null,
            PerformanceDegradationFlag = perf.TenantsWithHighLatencyImpact.Count > 0 || (perf.PendingJobCount ?? 0) > 100
        };

        if (dto.TenantsInCriticalAnomalyState > 0 || criticalCount > 0 || dto.PerformanceDegradationFlag)
            dto.Summary = $"{totalActive} active tenant(s); {warningCount} warning, {criticalCount} critical health; {tenantsWithCriticalAnomaly} with critical anomaly; performance flag={dto.PerformanceDegradationFlag}.";
        else
            dto.Summary = $"{totalActive} active tenant(s); no critical issues.";

        return dto;
    }
}
