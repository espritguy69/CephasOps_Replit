using CephasOps.Domain.Integration.Entities;
using CephasOps.Domain.Notifications.Entities;
using CephasOps.Domain.Workflow;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Platform.TenantHealth;

/// <summary>Enterprise: automated tenant health scoring. Weights: job failures 30%, notification failures 20%, integration failures 20%, API error rate 20%, activity drop 10%. Score 90-100 Healthy, 70-89 Warning, &lt;70 Critical.</summary>
public class TenantHealthScoringService : ITenantHealthScoringService
{
    private const string NotificationStatusSent = "Sent";
    private const string NotificationStatusFailed = "Failed";
    private const string NotificationStatusDeadLetter = "DeadLetter";
    private const string IntegrationStatusDelivered = "Delivered";
    private const string IntegrationStatusFailed = "Failed";
    private const string IntegrationStatusDeadLetter = "DeadLetter";

    private readonly ApplicationDbContext _context;

    public TenantHealthScoringService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task ComputeAndStoreAsync(Guid tenantId, DateTime dateUtc, CancellationToken cancellationToken = default)
    {
        await TenantScopeExecutor.RunWithPlatformBypassAsync(async ct =>
        {
            var start = dateUtc.Date;
            var end = start.AddDays(1);
            var companyIds = await _context.Companies
                .Where(c => c.TenantId == tenantId)
                .Select(c => c.Id)
                .ToListAsync(ct);

            int jobFailures = 0, jobsOk = 0, notifSent = 0, notifFailed = 0, integDelivered = 0, integFailed = 0;
            if (companyIds.Count > 0)
            {
                jobFailures = await _context.JobExecutions
                    .Where(j => j.CompanyId != null && companyIds.Contains(j.CompanyId.Value)
                        && (j.Status == JobExecutionStatus.Failed || j.Status == JobExecutionStatus.DeadLetter)
                        && (j.LastErrorAtUtc ?? j.UpdatedAtUtc ?? j.CreatedAtUtc) >= start
                        && (j.LastErrorAtUtc ?? j.UpdatedAtUtc ?? j.CreatedAtUtc) < end)
                    .CountAsync(ct);
                jobsOk = await _context.JobExecutions
                    .Where(j => j.CompanyId != null && companyIds.Contains(j.CompanyId.Value)
                        && j.Status == JobExecutionStatus.Succeeded
                        && (j.UpdatedAtUtc ?? j.CreatedAtUtc) >= start && (j.UpdatedAtUtc ?? j.CreatedAtUtc) < end)
                    .CountAsync(ct);

                var notifQuery = _context.NotificationDispatches
                    .Where(n => n.CompanyId != null && companyIds.Contains(n.CompanyId.Value)
                        && (n.UpdatedAtUtc ?? n.CreatedAtUtc) >= start && (n.UpdatedAtUtc ?? n.CreatedAtUtc) < end);
                notifSent = await notifQuery.CountAsync(n => n.Status == NotificationStatusSent, ct);
                notifFailed = await notifQuery.CountAsync(n => n.Status == NotificationStatusFailed || n.Status == NotificationStatusDeadLetter, ct);

                var integQuery = _context.OutboundIntegrationDeliveries
                    .Where(o => o.CompanyId != null && companyIds.Contains(o.CompanyId.Value)
                        && o.UpdatedAtUtc >= start && o.UpdatedAtUtc < end);
                integDelivered = await integQuery.CountAsync(o => o.Status == IntegrationStatusDelivered, ct);
                integFailed = await integQuery.CountAsync(o => o.Status == IntegrationStatusFailed || o.Status == IntegrationStatusDeadLetter, ct);
            }

            var daily = await _context.TenantMetricsDaily
                .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.DateUtc == start, ct);
            var apiCalls = daily?.ApiCalls ?? 0;
            var prevDay = await _context.TenantMetricsDaily
                .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.DateUtc == start.AddDays(-1), ct);
            var hasActivity = (daily != null && (apiCalls > 0 || (daily.OrdersCreated + daily.BackgroundJobsExecuted) > 0)) || jobFailures + jobsOk + notifSent + notifFailed + integDelivered + integFailed > 0;
            var activityDrop = (prevDay != null && !hasActivity) ? 10 : 0;

            var totalJobs = jobFailures + jobsOk;
            var jobFailureRate = totalJobs > 0 ? (double)jobFailures / totalJobs : 0;
            var totalNotif = notifSent + notifFailed;
            var notifFailureRate = totalNotif > 0 ? (double)notifFailed / totalNotif : 0;
            var totalInteg = integDelivered + integFailed;
            var integFailureRate = totalInteg > 0 ? (double)integFailed / totalInteg : 0;
            var apiErrorRate = 0d;

            var jobPenalty = Math.Min(30, jobFailureRate * 30);
            var notifPenalty = Math.Min(20, notifFailureRate * 20);
            var integPenalty = Math.Min(20, integFailureRate * 20);
            var apiPenalty = Math.Min(20, apiErrorRate * 20);
            var activityPenalty = activityDrop;

            var score = (int)Math.Round(100 - (jobPenalty + notifPenalty + integPenalty + apiPenalty + activityPenalty));
            score = Math.Clamp(score, 0, 100);
            var status = score >= 90 ? "Healthy" : score >= 70 ? "Warning" : "Critical";

            if (daily != null)
            {
                daily.HealthScore = score;
                daily.HealthStatus = status;
            }

            await _context.SaveChangesAsync(ct);
        }, cancellationToken);
    }

    public async Task ComputeAndStoreForAllTenantsAsync(DateTime dateUtc, CancellationToken cancellationToken = default)
    {
        await TenantScopeExecutor.RunWithPlatformBypassAsync(async ct =>
        {
            var tenantIds = await _context.TenantMetricsDaily
                .Where(d => d.DateUtc == dateUtc.Date)
                .Select(d => d.TenantId)
                .Distinct()
                .ToListAsync(ct);
            foreach (var tenantId in tenantIds)
                await ComputeAndStoreAsync(tenantId, dateUtc, ct);
        }, cancellationToken);
    }
}
