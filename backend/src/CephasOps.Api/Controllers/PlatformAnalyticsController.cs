using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Audit;
using CephasOps.Application.Platform;
using CephasOps.Application.Platform.Guardian;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>Platform admin: dashboard analytics (active tenants, monthly usage, storage, job volume).</summary>
[ApiController]
[Route("api/platform/analytics")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class PlatformAnalyticsController : ControllerBase
{
    private readonly IPlatformAnalyticsService _analyticsService;
    private readonly ITenantAnomalyDetectionService _anomalyService;
    private readonly IPlatformDriftDetectionService _driftService;
    private readonly IPerformanceWatchdogService _performanceWatchdog;
    private readonly IPlatformHealthService _platformHealth;
    private readonly ITenantActivityService _tenantActivityService;

    public PlatformAnalyticsController(
        IPlatformAnalyticsService analyticsService,
        ITenantAnomalyDetectionService anomalyService,
        IPlatformDriftDetectionService driftService,
        IPerformanceWatchdogService performanceWatchdog,
        IPlatformHealthService platformHealth,
        ITenantActivityService tenantActivityService)
    {
        _analyticsService = analyticsService;
        _anomalyService = anomalyService;
        _driftService = driftService;
        _performanceWatchdog = performanceWatchdog;
        _platformHealth = platformHealth;
        _tenantActivityService = tenantActivityService;
    }

    /// <summary>Get dashboard analytics: active tenants, monthly usage, storage growth, job volume (from TenantMetricsDaily/Monthly).</summary>
    [HttpGet("dashboard")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<PlatformDashboardAnalyticsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PlatformDashboardAnalyticsDto>>> GetDashboard(CancellationToken cancellationToken = default)
    {
        var result = await _analyticsService.GetDashboardAnalyticsAsync(cancellationToken);
        return this.Success(result);
    }

    /// <summary>Get per-tenant health: API requests, job failures, storage, active users, HealthStatus (Healthy | Warning | Critical).</summary>
    [HttpGet("tenant-health")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TenantHealthDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TenantHealthDto>>>> GetTenantHealth(CancellationToken cancellationToken = default)
    {
        var result = await _analyticsService.GetTenantHealthAsync(cancellationToken);
        return this.Success(result);
    }

    /// <summary>Platform Guardian: list tenant anomaly events (since, tenantId, severity optional).</summary>
    [HttpGet("anomalies")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TenantAnomalyDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TenantAnomalyDto>>>> GetAnomalies(
        [FromQuery] DateTime? sinceUtc = null,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] string? severity = null,
        [FromQuery] int take = 500,
        CancellationToken cancellationToken = default)
    {
        var result = await _anomalyService.GetAnomaliesAsync(sinceUtc ?? DateTime.UtcNow.AddDays(-7), tenantId, severity, take, cancellationToken);
        return this.Success(result);
    }

    /// <summary>Platform Guardian: configuration drift report vs baseline.</summary>
    [HttpGet("drift")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<PlatformDriftResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PlatformDriftResultDto>>> GetDrift(CancellationToken cancellationToken = default)
    {
        var result = await _driftService.DetectAsync(cancellationToken);
        return this.Success(result);
    }

    /// <summary>Platform Guardian: performance health (slow queries, job queue, degraded tenants).</summary>
    [HttpGet("performance-health")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<PerformanceHealthDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PerformanceHealthDto>>> GetPerformanceHealth(CancellationToken cancellationToken = default)
    {
        var result = await _performanceWatchdog.GetPerformanceHealthAsync(cancellationToken);
        return this.Success(result);
    }

    /// <summary>Platform Guardian: aggregated platform health (tenants, anomalies, rate limit, jobs, performance).</summary>
    [HttpGet("platform-health")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<PlatformHealthDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PlatformHealthDto>>> GetPlatformHealth(CancellationToken cancellationToken = default)
    {
        var result = await _platformHealth.GetPlatformHealthAsync(cancellationToken);
        return this.Success(result);
    }

    /// <summary>Platform observability: operations summary (active tenants, failed jobs/notifications/integrations today, tenants with warnings).</summary>
    [HttpGet("operations-summary")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<PlatformOperationsSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PlatformOperationsSummaryDto>>> GetOperationsSummary(CancellationToken cancellationToken = default)
    {
        var result = await _analyticsService.GetPlatformOperationsSummaryAsync(cancellationToken);
        return this.Success(result);
    }

    /// <summary>Platform observability: tenant operations overview table (name, status, requests, jobs, notifications, integrations, last activity, warnings).</summary>
    [HttpGet("tenant-operations-overview")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TenantOperationsOverviewItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TenantOperationsOverviewItemDto>>>> GetTenantOperationsOverview(CancellationToken cancellationToken = default)
    {
        var result = await _analyticsService.GetTenantOperationsOverviewAsync(cancellationToken);
        return this.Success(result);
    }

    /// <summary>Platform observability: single tenant detail (daily trend buckets and recent anomalies).</summary>
    [HttpGet("tenant-operations-detail/{tenantId:guid}")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<TenantOperationsDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantOperationsDetailDto>>> GetTenantOperationsDetail(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var result = await _analyticsService.GetTenantOperationsDetailAsync(tenantId, cancellationToken);
        if (result == null)
            return this.NotFound();
        return this.Success(result);
    }

    /// <summary>Enterprise: activity timeline for a tenant (last 100 events). Platform admin only.</summary>
    [HttpGet("tenants/{tenantId:guid}/activity-timeline")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TenantActivityEventDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TenantActivityEventDto>>>> GetTenantActivityTimeline(Guid tenantId, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        var result = await _tenantActivityService.GetTimelineAsync(tenantId, Math.Min(take, 500), cancellationToken);
        return this.Success(result);
    }
}

