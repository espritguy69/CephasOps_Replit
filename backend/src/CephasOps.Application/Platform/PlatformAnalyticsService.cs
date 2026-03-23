using CephasOps.Domain.Integration.Entities;
using CephasOps.Domain.Notifications.Entities;
using CephasOps.Domain.PlatformGuardian;
using CephasOps.Domain.Tenants.Entities;
using CephasOps.Domain.Workflow;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Platform;

public class PlatformAnalyticsService : IPlatformAnalyticsService
{
    private const string NotificationStatusSent = "Sent";
    private const string NotificationStatusFailed = "Failed";
    private const string NotificationStatusDeadLetter = "DeadLetter";
    private const string IntegrationStatusDelivered = "Delivered";
    private const string IntegrationStatusFailed = "Failed";
    private const string IntegrationStatusDeadLetter = "DeadLetter";

    private readonly ApplicationDbContext _context;

    public PlatformAnalyticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TenantHealthDto>> GetTenantHealthAsync(CancellationToken cancellationToken = default)
    {
        return await TenantScopeExecutor.RunWithPlatformBypassAsync(async ct =>
        {
            var now = DateTime.UtcNow;
            var dayAgo = now.AddDays(-1);

            var tenantIds = await _context.Tenants.Where(t => t.IsActive).Select(t => t.Id).ToListAsync(ct);
        var result = new List<TenantHealthDto>();

        foreach (var tenantId in tenantIds)
        {
            var daily = await _context.TenantMetricsDaily
                .Where(d => d.TenantId == tenantId && d.DateUtc >= dayAgo.Date)
                .ToListAsync(ct);

            var companyIds = await _context.Companies
                .Where(c => c.TenantId == tenantId)
                .Select(c => c.Id)
                .ToListAsync(ct);

            var jobFailures = 0;
            if (companyIds.Count > 0)
            {
                jobFailures = await _context.JobExecutions
                    .Where(j => j.CompanyId != null && companyIds.Contains(j.CompanyId.Value)
                        && (j.Status == JobExecutionStatus.Failed || j.Status == JobExecutionStatus.DeadLetter)
                        && (j.LastErrorAtUtc ?? j.UpdatedAtUtc ?? j.CreatedAtUtc) >= dayAgo)
                    .CountAsync(ct);
            }

            var apiRequests = daily.Sum(d => d.ApiCalls);
            var storageBytes = daily.Count > 0 ? daily.Max(d => d.StorageBytes) : 0L;
            var activeUsers = daily.Count > 0 ? daily.Max(d => d.ActiveUsers) : 0;
            var lastActivityUtc = daily.Count > 0 ? (DateTime?)daily.Max(d => d.DateUtc) : null;

            var healthStatus = "Healthy";
            if (jobFailures >= 50) healthStatus = "Critical";
            else if (jobFailures >= 10 || (daily.Count == 0 && lastActivityUtc == null)) healthStatus = "Warning";

            result.Add(new TenantHealthDto
            {
                TenantId = tenantId,
                ApiRequestsLast24h = apiRequests,
                JobFailuresLast24h = jobFailures,
                StorageBytes = storageBytes,
                ActiveUsers = activeUsers,
                LastActivityUtc = lastActivityUtc,
                HealthStatus = healthStatus
            });
        }

            return result;
        }, cancellationToken);
    }

    public async Task<PlatformDashboardAnalyticsDto> GetDashboardAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var totalTenants = await _context.Tenants.CountAsync(cancellationToken);
        var activeTenants = await _context.Tenants.CountAsync(t => t.IsActive, cancellationToken);

        var currentYear = now.Year;
        var currentMonth = now.Month;
        var prevMonth = now.Month == 1 ? 12 : now.Month - 1;
        var prevYear = now.Month == 1 ? now.Year - 1 : now.Year;

        var currentMonthly = await _context.TenantMetricsMonthly
            .Where(m => m.Year == currentYear && m.Month == currentMonth)
            .ToListAsync(cancellationToken);
        var prevMonthly = await _context.TenantMetricsMonthly
            .Where(m => m.Year == prevYear && m.Month == prevMonth)
            .ToListAsync(cancellationToken);

        var thirtyDaysAgo = now.Date.AddDays(-30);
        var dailyJobVolume = await _context.TenantMetricsDaily
            .Where(d => d.DateUtc >= thirtyDaysAgo)
            .SumAsync(d => d.BackgroundJobsExecuted, cancellationToken);

        return new PlatformDashboardAnalyticsDto
        {
            ActiveTenantsCount = activeTenants,
            TotalTenantsCount = totalTenants,
            CurrentMonth = currentMonthly.Count > 0 ? new MonthlyUsageSummaryDto
            {
                Year = currentYear,
                Month = currentMonth,
                TenantCount = currentMonthly.Count,
                TotalStorageBytes = currentMonthly.Sum(m => m.StorageBytes),
                TotalApiCalls = currentMonthly.Sum(m => m.ApiCalls),
                TotalOrdersCreated = currentMonthly.Sum(m => m.OrdersCreated),
                TotalBackgroundJobsExecuted = currentMonthly.Sum(m => m.BackgroundJobsExecuted)
            } : null,
            PreviousMonth = prevMonthly.Count > 0 ? new MonthlyUsageSummaryDto
            {
                Year = prevYear,
                Month = prevMonth,
                TenantCount = prevMonthly.Count,
                TotalStorageBytes = prevMonthly.Sum(m => m.StorageBytes),
                TotalApiCalls = prevMonthly.Sum(m => m.ApiCalls),
                TotalOrdersCreated = prevMonthly.Sum(m => m.OrdersCreated),
                TotalBackgroundJobsExecuted = prevMonthly.Sum(m => m.BackgroundJobsExecuted)
            } : null,
            TotalStorageBytes = currentMonthly.Count > 0 ? currentMonthly.Sum(m => m.StorageBytes) : 0,
            TotalJobVolumeLast30Days = dailyJobVolume
        };
    }

    public async Task<IReadOnlyList<TenantOperationsOverviewItemDto>> GetTenantOperationsOverviewAsync(CancellationToken cancellationToken = default)
    {
        return await TenantScopeExecutor.RunWithPlatformBypassAsync(async ct =>
        {
            var now = DateTime.UtcNow;
            var dayAgo = now.AddDays(-1);
            var todayStart = now.Date;

            var tenants = await _context.Tenants.OrderBy(t => t.Name).ToListAsync(ct);
            var result = new List<TenantOperationsOverviewItemDto>();

            foreach (var tenant in tenants)
            {
                var companyIds = await _context.Companies
                    .Where(c => c.TenantId == tenant.Id)
                    .Select(c => c.Id)
                    .ToListAsync(ct);

                var daily = await _context.TenantMetricsDaily
                    .Where(d => d.TenantId == tenant.Id && d.DateUtc >= dayAgo.Date)
                    .ToListAsync(ct);

                var requestCount = daily.Sum(d => d.ApiCalls);
                var jobsOk = daily.Sum(d => d.BackgroundJobsExecuted);

                var jobFailures = 0;
                var notifSent = 0;
                var notifFailed = 0;
                var integDelivered = 0;
                var integFailed = 0;
                DateTime? lastActivity = daily.Count > 0 ? daily.Max(d => d.DateUtc) : null;

                if (companyIds.Count > 0)
                {
                    jobFailures = await _context.JobExecutions
                        .Where(j => j.CompanyId != null && companyIds.Contains(j.CompanyId.Value)
                            && (j.Status == JobExecutionStatus.Failed || j.Status == JobExecutionStatus.DeadLetter)
                            && (j.LastErrorAtUtc ?? j.UpdatedAtUtc ?? j.CreatedAtUtc) >= dayAgo)
                        .CountAsync(ct);

                    var notifQuery = _context.NotificationDispatches
                        .Where(n => n.CompanyId != null && companyIds.Contains(n.CompanyId.Value)
                            && (n.UpdatedAtUtc ?? n.CreatedAtUtc) >= dayAgo);
                    notifSent = await notifQuery.CountAsync(n => n.Status == NotificationStatusSent, ct);
                    notifFailed = await notifQuery.CountAsync(n => n.Status == NotificationStatusFailed || n.Status == NotificationStatusDeadLetter, ct);

                    var integQuery = _context.OutboundIntegrationDeliveries
                        .Where(o => o.CompanyId != null && companyIds.Contains(o.CompanyId.Value)
                            && o.UpdatedAtUtc >= dayAgo);
                    integDelivered = await integQuery.CountAsync(o => o.Status == IntegrationStatusDelivered, ct);
                    integFailed = await integQuery.CountAsync(o => o.Status == IntegrationStatusFailed || o.Status == IntegrationStatusDeadLetter, ct);

                    var latestJob = await _context.JobExecutions
                        .Where(j => j.CompanyId != null && companyIds.Contains(j.CompanyId.Value))
                        .OrderByDescending(j => j.UpdatedAtUtc ?? j.CreatedAtUtc)
                        .Select(j => j.UpdatedAtUtc ?? j.CreatedAtUtc)
                        .FirstOrDefaultAsync(ct);
                    if (latestJob != default && (lastActivity == null || latestJob > lastActivity.Value))
                        lastActivity = latestJob;
                }

                var latestDaily = daily.OrderByDescending(d => d.DateUtc).FirstOrDefault();
                var healthScore = latestDaily?.HealthScore;
                var healthStatus = latestDaily?.HealthStatus ?? "Healthy";
                if (string.IsNullOrEmpty(healthStatus))
                {
                    if (jobFailures >= 50) healthStatus = "Critical";
                    else if (jobFailures >= 10 || (daily.Count == 0 && lastActivity == null)) healthStatus = "Warning";
                    else healthStatus = "Healthy";
                }

                result.Add(new TenantOperationsOverviewItemDto
                {
                    TenantId = tenant.Id,
                    TenantName = tenant.Name,
                    Slug = tenant.Slug,
                    IsActive = tenant.IsActive,
                    RequestCountLast24h = requestCount,
                    JobFailuresLast24h = jobFailures,
                    JobsOkLast24h = jobsOk,
                    NotificationsSentLast24h = notifSent,
                    NotificationsFailedLast24h = notifFailed,
                    IntegrationsDeliveredLast24h = integDelivered,
                    IntegrationsFailedLast24h = integFailed,
                    LastActivityUtc = lastActivity,
                    HealthScore = healthScore,
                    HealthStatus = healthStatus,
                    HasWarnings = healthStatus != "Healthy"
                });
            }

            return result;
        }, cancellationToken);
    }

    public async Task<TenantOperationsDetailDto?> GetTenantOperationsDetailAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await TenantScopeExecutor.RunWithPlatformBypassAsync(async ct =>
        {
            var tenant = await _context.Tenants.FindAsync([tenantId], ct);
            if (tenant == null) return null;

            var companyIds = await _context.Companies
                .Where(c => c.TenantId == tenantId)
                .Select(c => c.Id)
                .ToListAsync(ct);

            var sevenDaysAgo = DateTime.UtcNow.Date.AddDays(-7);
            var dailyBuckets = new List<TenantOperationsDailyBucketDto>();

            for (var d = 0; d < 7; d++)
            {
                var date = sevenDaysAgo.AddDays(d);
                var next = date.AddDays(1);

                var dayDaily = await _context.TenantMetricsDaily
                    .Where(x => x.TenantId == tenantId && x.DateUtc >= date && x.DateUtc < next)
                    .ToListAsync(ct);

                var requestCount = dayDaily.Sum(x => x.ApiCalls);
                var jobsOk = dayDaily.Sum(x => x.BackgroundJobsExecuted);

                var jobFailures = 0;
                var notifSent = 0;
                var notifFailed = 0;
                var integDelivered = 0;
                var integFailed = 0;

                if (companyIds.Count > 0)
                {
                    jobFailures = await _context.JobExecutions
                        .Where(j => j.CompanyId != null && companyIds.Contains(j.CompanyId.Value)
                            && (j.Status == JobExecutionStatus.Failed || j.Status == JobExecutionStatus.DeadLetter)
                            && (j.LastErrorAtUtc ?? j.UpdatedAtUtc ?? j.CreatedAtUtc) >= date
                            && (j.LastErrorAtUtc ?? j.UpdatedAtUtc ?? j.CreatedAtUtc) < next)
                        .CountAsync(ct);

                    var notifQ = _context.NotificationDispatches
                        .Where(n => n.CompanyId != null && companyIds.Contains(n.CompanyId.Value)
                            && (n.UpdatedAtUtc ?? n.CreatedAtUtc) >= date && (n.UpdatedAtUtc ?? n.CreatedAtUtc) < next);
                    notifSent = await notifQ.CountAsync(n => n.Status == NotificationStatusSent, ct);
                    notifFailed = await notifQ.CountAsync(n => n.Status == NotificationStatusFailed || n.Status == NotificationStatusDeadLetter, ct);

                    var integQ = _context.OutboundIntegrationDeliveries
                        .Where(o => o.CompanyId != null && companyIds.Contains(o.CompanyId.Value)
                            && o.UpdatedAtUtc >= date && o.UpdatedAtUtc < next);
                    integDelivered = await integQ.CountAsync(o => o.Status == IntegrationStatusDelivered, ct);
                    integFailed = await integQ.CountAsync(o => o.Status == IntegrationStatusFailed || o.Status == IntegrationStatusDeadLetter, ct);
                }

                dailyBuckets.Add(new TenantOperationsDailyBucketDto
                {
                    DateUtc = date,
                    RequestCount = requestCount,
                    JobFailures = jobFailures,
                    JobsOk = jobsOk,
                    NotificationsSent = notifSent,
                    NotificationsFailed = notifFailed,
                    IntegrationsDelivered = integDelivered,
                    IntegrationsFailed = integFailed
                });
            }

            var recentAnomalies = await _context.TenantAnomalyEvents
                .Where(e => e.TenantId == tenantId)
                .OrderByDescending(e => e.OccurredAtUtc)
                .Take(20)
                .Select(e => new Guardian.TenantAnomalyDto
                {
                    Id = e.Id,
                    TenantId = e.TenantId,
                    Kind = e.Kind,
                    Severity = e.Severity,
                    OccurredAtUtc = e.OccurredAtUtc,
                    Details = e.Details,
                    ResolvedAtUtc = e.ResolvedAtUtc
                })
                .ToListAsync(ct);

            return new TenantOperationsDetailDto
            {
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                IsActive = tenant.IsActive,
                DailyBuckets = dailyBuckets.OrderBy(b => b.DateUtc).ToList(),
                RecentAnomalies = recentAnomalies
            };
        }, cancellationToken);
    }

    public async Task<PlatformOperationsSummaryDto> GetPlatformOperationsSummaryAsync(CancellationToken cancellationToken = default)
    {
        return await TenantScopeExecutor.RunWithPlatformBypassAsync(async ct =>
        {
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var dayAgo = now.AddDays(-1);

            var activeTenants = await _context.Tenants.CountAsync(t => t.IsActive, ct);
            var totalTenants = await _context.Tenants.CountAsync(ct);

            var failedJobsToday = await _context.JobExecutions
                .Where(j => (j.Status == JobExecutionStatus.Failed || j.Status == JobExecutionStatus.DeadLetter)
                    && (j.LastErrorAtUtc ?? j.UpdatedAtUtc ?? j.CreatedAtUtc) >= todayStart)
                .CountAsync(ct);

            var failedNotifToday = await _context.NotificationDispatches
                .Where(n => (n.Status == NotificationStatusFailed || n.Status == NotificationStatusDeadLetter)
                    && (n.UpdatedAtUtc ?? n.CreatedAtUtc) >= dayAgo)
                .CountAsync(ct);

            var failedIntegToday = await _context.OutboundIntegrationDeliveries
                .Where(o => (o.Status == IntegrationStatusFailed || o.Status == IntegrationStatusDeadLetter)
                    && o.UpdatedAtUtc >= dayAgo)
                .CountAsync(ct);

            int tenantsWithWarnings = 0;
            var allTenantIds = await _context.Tenants.Select(t => t.Id).ToListAsync(ct);
            foreach (var tid in allTenantIds)
            {
                var companyIds = await _context.Companies.Where(c => c.TenantId == tid).Select(c => c.Id).ToListAsync(ct);
                var jobFailures = 0;
                if (companyIds.Count > 0)
                    jobFailures = await _context.JobExecutions
                        .Where(j => j.CompanyId != null && companyIds.Contains(j.CompanyId.Value)
                            && (j.Status == JobExecutionStatus.Failed || j.Status == JobExecutionStatus.DeadLetter)
                            && (j.LastErrorAtUtc ?? j.UpdatedAtUtc ?? j.CreatedAtUtc) >= dayAgo)
                        .CountAsync(ct);
                var daily = await _context.TenantMetricsDaily
                    .Where(d => d.TenantId == tid && d.DateUtc >= dayAgo.Date)
                    .ToListAsync(ct);
                var lastActivity = daily.Count > 0 ? (DateTime?)daily.Max(d => d.DateUtc) : null;
                if (jobFailures >= 50 || jobFailures >= 10 || (daily.Count == 0 && lastActivity == null))
                    tenantsWithWarnings++;
            }

            return new PlatformOperationsSummaryDto
            {
                ActiveTenantsCount = activeTenants,
                TotalTenantsCount = totalTenants,
                FailedJobsToday = failedJobsToday,
                FailedNotificationsToday = failedNotifToday,
                FailedIntegrationsToday = failedIntegToday,
                TenantsWithWarningsCount = tenantsWithWarnings,
                GeneratedAtUtc = now
            };
        }, cancellationToken);
    }
}
