using CephasOps.Domain.Workflow;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Platform.Guardian;

/// <summary>Platform Guardian: aggregates performance indicators from tenant health, job queue, and anomalies.</summary>
public class PerformanceWatchdogService : IPerformanceWatchdogService
{
    private readonly ApplicationDbContext _context;
    private readonly IPlatformAnalyticsService _analyticsService;

    public PerformanceWatchdogService(ApplicationDbContext context, IPlatformAnalyticsService analyticsService)
    {
        _context = context;
        _analyticsService = analyticsService;
    }

    public async Task<PerformanceHealthDto> GetPerformanceHealthAsync(CancellationToken cancellationToken = default)
    {
        var result = new PerformanceHealthDto { GeneratedAtUtc = DateTime.UtcNow };

        await TenantScopeExecutor.RunWithPlatformBypassAsync(async ct =>
        {
            var health = await _analyticsService.GetTenantHealthAsync(ct);
            var warningOrCritical = health.Where(h => h.HealthStatus == "Warning" || h.HealthStatus == "Critical").ToList();
            result.TenantsWithHighLatencyImpact = warningOrCritical.Select(h => h.TenantId).ToList();

            var pendingCount = await _context.JobExecutions
                .Where(j => j.Status == JobExecutionStatus.Pending && (j.NextRunAtUtc == null || j.NextRunAtUtc <= DateTime.UtcNow))
                .CountAsync(ct);
            result.PendingJobCount = pendingCount;

            if (warningOrCritical.Count > 0 || pendingCount > 100)
                result.Summary = $"{warningOrCritical.Count} tenant(s) in Warning/Critical; {pendingCount} pending job(s).";
            else
                result.Summary = "No significant performance issues detected.";
        }, cancellationToken);

        return result;
    }
}
