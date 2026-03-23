using CephasOps.Domain.PlatformGuardian;
using CephasOps.Domain.Workflow;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Platform.Guardian;

/// <summary>Platform Guardian: evaluates per-tenant metrics and persists anomaly events.</summary>
public class TenantAnomalyDetectionService : ITenantAnomalyDetectionService
{
    private readonly ApplicationDbContext _context;
    private readonly TenantAnomalyDetectionOptions _options;

    public TenantAnomalyDetectionService(ApplicationDbContext context, IOptions<TenantAnomalyDetectionOptions>? options = null)
    {
        _context = context;
        _options = options?.Value ?? new TenantAnomalyDetectionOptions();
    }

    public async Task RunDetectionAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return;

        await TenantScopeExecutor.RunWithPlatformBypassAsync(async ct =>
        {
            var now = DateTime.UtcNow;
            var dayAgo = now.AddDays(-1).Date;
            var sevenDaysAgo = now.AddDays(-7).Date;

            var tenantIds = await _context.Tenants.Where(t => t.IsActive).Select(t => t.Id).ToListAsync(ct);
            var eventsAdded = 0;

            foreach (var tenantId in tenantIds)
            {
                if (eventsAdded >= _options.MaxEventsPerTenantPerRun * Math.Max(1, tenantIds.Count)) break;

                var dailyRecent = await _context.TenantMetricsDaily
                    .Where(d => d.TenantId == tenantId && d.DateUtc >= dayAgo)
                    .ToListAsync(ct);
                var dailyBaseline = await _context.TenantMetricsDaily
                    .Where(d => d.TenantId == tenantId && d.DateUtc >= sevenDaysAgo && d.DateUtc < dayAgo)
                    .ToListAsync(ct);

                var apiLast24 = dailyRecent.Sum(d => d.ApiCalls);
                var apiAvg7 = dailyBaseline.Count > 0 ? dailyBaseline.Average(d => d.ApiCalls) : 0;
                if (apiAvg7 > 0 && apiLast24 >= _options.ApiSpikeCriticalMultiple * apiAvg7)
                {
                    await AddAnomalyAsync(tenantId, "ApiSpike", "Critical", $"ApiCalls last 24h={apiLast24}, 7d avg={apiAvg7:F0}", ct);
                    eventsAdded++;
                }
                else if (apiAvg7 > 0 && apiLast24 >= _options.ApiSpikeWarningMultiple * apiAvg7)
                {
                    await AddAnomalyAsync(tenantId, "ApiSpike", "Warning", $"ApiCalls last 24h={apiLast24}, 7d avg={apiAvg7:F0}", ct);
                    eventsAdded++;
                }

                var storageLast = dailyRecent.Count > 0 ? dailyRecent.Max(d => d.StorageBytes) : 0L;
                var storageBaseline = dailyBaseline.Count > 0 ? dailyBaseline.Max(d => d.StorageBytes) : 0L;
                if (storageBaseline > 0 && storageLast > 0)
                {
                    var growth = (double)(storageLast - storageBaseline) / storageBaseline;
                    if (growth >= _options.StorageGrowthCriticalFraction)
                    {
                        await AddAnomalyAsync(tenantId, "StorageSpike", "Critical", $"Storage growth {(growth * 100):F1}%", ct);
                        eventsAdded++;
                    }
                    else if (growth >= _options.StorageGrowthWarningFraction)
                    {
                        await AddAnomalyAsync(tenantId, "StorageSpike", "Warning", $"Storage growth {(growth * 100):F1}%", ct);
                        eventsAdded++;
                    }
                }

                var companyIds = await _context.Companies.Where(c => c.TenantId == tenantId).Select(c => c.Id).ToListAsync(ct);
                if (companyIds.Count > 0)
                {
                    var jobFailures = await _context.JobExecutions
                        .Where(j => j.CompanyId != null && companyIds.Contains(j.CompanyId.Value)
                            && (j.Status == JobExecutionStatus.Failed || j.Status == JobExecutionStatus.DeadLetter)
                            && (j.LastErrorAtUtc ?? j.UpdatedAtUtc ?? j.CreatedAtUtc) >= dayAgo)
                        .CountAsync(ct);
                    if (jobFailures >= _options.JobFailureSpikeCritical)
                    {
                        await AddAnomalyAsync(tenantId, "JobFailureSpike", "Critical", $"Job failures last 24h={jobFailures}", ct);
                        eventsAdded++;
                    }
                    else if (jobFailures >= _options.JobFailureSpikeWarning)
                    {
                        await AddAnomalyAsync(tenantId, "JobFailureSpike", "Warning", $"Job failures last 24h={jobFailures}", ct);
                        eventsAdded++;
                    }
                }
            }

            if (eventsAdded > 0)
                await _context.SaveChangesAsync(ct);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<TenantAnomalyDto>> GetAnomaliesAsync(DateTime? sinceUtc = null, Guid? tenantId = null, string? severity = null, int take = 500, CancellationToken cancellationToken = default)
    {
        return await TenantScopeExecutor.RunWithPlatformBypassAsync(async ct =>
        {
            var query = _context.TenantAnomalyEvents.AsNoTracking().AsQueryable();
            if (sinceUtc.HasValue)
                query = query.Where(e => e.OccurredAtUtc >= sinceUtc.Value);
            if (tenantId.HasValue)
                query = query.Where(e => e.TenantId == tenantId.Value);
            if (!string.IsNullOrEmpty(severity))
                query = query.Where(e => e.Severity == severity);
            var list = await query.OrderByDescending(e => e.OccurredAtUtc).Take(take).ToListAsync(ct);
            return list.Select(e => new TenantAnomalyDto
            {
                Id = e.Id,
                TenantId = e.TenantId,
                Kind = e.Kind,
                Severity = e.Severity,
                OccurredAtUtc = e.OccurredAtUtc,
                Details = e.Details,
                ResolvedAtUtc = e.ResolvedAtUtc
            }).ToList();
        }, cancellationToken);
    }

    private async Task AddAnomalyAsync(Guid tenantId, string kind, string severity, string details, CancellationToken ct)
    {
        var recent = await _context.TenantAnomalyEvents
            .Where(e => e.TenantId == tenantId && e.Kind == kind && e.OccurredAtUtc >= DateTime.UtcNow.AddHours(-1))
            .CountAsync(ct);
        if (recent > 0) return; // avoid duplicate in same hour
        _context.TenantAnomalyEvents.Add(new TenantAnomalyEvent
        {
            TenantId = tenantId,
            Kind = kind,
            Severity = severity,
            Details = details.Length > 2000 ? details[..2000] : details
        });
    }
}
