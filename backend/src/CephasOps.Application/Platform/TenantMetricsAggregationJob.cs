using CephasOps.Domain.Billing.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Platform;

/// <summary>SaaS scaling: aggregates TenantUsageRecord into TenantMetricsDaily and TenantMetricsMonthly.</summary>
public class TenantMetricsAggregationJob
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TenantMetricsAggregationJob> _logger;

    public TenantMetricsAggregationJob(ApplicationDbContext context, ILogger<TenantMetricsAggregationJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>Aggregate usage records for the month containing dateUtc into TenantMetricsDaily (one row per tenant for that date).</summary>
    public async Task AggregateDailyAsync(DateTime dateUtc, CancellationToken cancellationToken = default)
    {
        var start = dateUtc.Date;
        var monthStart = new DateTime(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);
        var tenantIds = await _context.TenantUsageRecords
            .Where(r => r.PeriodStartUtc >= monthStart && r.PeriodStartUtc < monthEnd)
            .Select(r => r.TenantId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var tenantId in tenantIds)
        {
            var records = await _context.TenantUsageRecords
                .Where(r => r.TenantId == tenantId && r.PeriodStartUtc >= monthStart && r.PeriodStartUtc < monthEnd)
                .ToListAsync(cancellationToken);

            var totalUsers = (int)(records.FirstOrDefault(r => r.MetricKey == "TotalUsers")?.Quantity ?? 0);
            var activeUsers = (int)(records.FirstOrDefault(r => r.MetricKey == "ActiveUsers")?.Quantity ?? 0);
            var ordersCreated = (int)(records.FirstOrDefault(r => r.MetricKey == "OrdersCreated")?.Quantity ?? 0);
            var jobsExecuted = (int)(records.FirstOrDefault(r => r.MetricKey == "BackgroundJobsExecuted")?.Quantity ?? 0);
            var storageBytes = (long)(records.FirstOrDefault(r => r.MetricKey == "StorageBytes")?.Quantity ?? 0);
            var apiCalls = (int)(records.FirstOrDefault(r => r.MetricKey == "ApiCalls")?.Quantity ?? 0);
            var rateLimitExceeded = (int)(records.FirstOrDefault(r => r.MetricKey == "RateLimitExceeded")?.Quantity ?? 0);

            var existing = await _context.TenantMetricsDaily
                .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.DateUtc == start, cancellationToken);
            if (existing != null)
            {
                existing.ActiveUsers = activeUsers;
                existing.TotalUsers = totalUsers;
                existing.OrdersCreated = ordersCreated;
                existing.BackgroundJobsExecuted = jobsExecuted;
                existing.StorageBytes = storageBytes;
                existing.ApiCalls = apiCalls;
                existing.RateLimitExceededCount = rateLimitExceeded;
            }
            else
            {
                _context.TenantMetricsDaily.Add(new TenantMetricsDaily
                {
                    TenantId = tenantId,
                    DateUtc = start,
                    ActiveUsers = activeUsers,
                    TotalUsers = totalUsers,
                    OrdersCreated = ordersCreated,
                    BackgroundJobsExecuted = jobsExecuted,
                    StorageBytes = storageBytes,
                    ApiCalls = apiCalls,
                    RateLimitExceededCount = rateLimitExceeded
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("TenantMetricsDaily aggregated for {DateUtc}, {Count} tenants", start, tenantIds.Count);
    }

    /// <summary>Aggregate usage records for a given year/month into TenantMetricsMonthly.</summary>
    public async Task AggregateMonthlyAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var tenantIds = await _context.TenantUsageRecords
            .Where(r => r.PeriodStartUtc >= start && r.PeriodStartUtc < end)
            .Select(r => r.TenantId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var tenantId in tenantIds)
        {
            var records = await _context.TenantUsageRecords
                .Where(r => r.TenantId == tenantId && r.PeriodStartUtc >= start && r.PeriodStartUtc < end)
                .ToListAsync(cancellationToken);

            var totalUsers = (int)(records.FirstOrDefault(r => r.MetricKey == "TotalUsers")?.Quantity ?? 0);
            var activeUsers = (int)(records.FirstOrDefault(r => r.MetricKey == "ActiveUsers")?.Quantity ?? 0);
            var ordersCreated = (int)(records.Sum(r => r.MetricKey == "OrdersCreated" ? r.Quantity : 0));
            var jobsExecuted = (int)(records.Sum(r => r.MetricKey == "BackgroundJobsExecuted" ? r.Quantity : 0));
            var storageBytes = (long)(records.FirstOrDefault(r => r.MetricKey == "StorageBytes")?.Quantity ?? 0);
            var apiCalls = (int)(records.Sum(r => r.MetricKey == "ApiCalls" ? r.Quantity : 0));

            var existing = await _context.TenantMetricsMonthly
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Year == year && m.Month == month, cancellationToken);
            if (existing != null)
            {
                existing.ActiveUsers = activeUsers;
                existing.TotalUsers = totalUsers;
                existing.OrdersCreated = ordersCreated;
                existing.BackgroundJobsExecuted = jobsExecuted;
                existing.StorageBytes = storageBytes;
                existing.ApiCalls = apiCalls;
            }
            else
            {
                _context.TenantMetricsMonthly.Add(new TenantMetricsMonthly
                {
                    TenantId = tenantId,
                    Year = year,
                    Month = month,
                    ActiveUsers = activeUsers,
                    TotalUsers = totalUsers,
                    OrdersCreated = ordersCreated,
                    BackgroundJobsExecuted = jobsExecuted,
                    StorageBytes = storageBytes,
                    ApiCalls = apiCalls
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("TenantMetricsMonthly aggregated for {Year}-{Month}, {Count} tenants", year, month, tenantIds.Count);
    }
}
