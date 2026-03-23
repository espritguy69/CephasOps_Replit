using CephasOps.Domain.Billing.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Billing.Usage;

/// <summary>
/// Writes tenant usage records for metering (Phase 4). Uses monthly buckets; one row per (tenant, metric, month), Quantity incremented on each event.
/// </summary>
public class TenantUsageService : ITenantUsageService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TenantUsageService> _logger;

    public static class MetricKeys
    {
        public const string OrdersCreated = "OrdersCreated";
        public const string InvoicesGenerated = "InvoicesGenerated";
        public const string BackgroundJobsExecuted = "BackgroundJobsExecuted";
        public const string ReportExports = "ReportExports";
        public const string TotalUsers = "TotalUsers";
        public const string ActiveUsers = "ActiveUsers";
        /// <summary>SaaS scaling: API request count per tenant.</summary>
        public const string ApiCalls = "ApiCalls";
        /// <summary>SaaS scaling: storage usage in bytes (use SetQuantityAsync for snapshot).</summary>
        public const string StorageBytes = "StorageBytes";
        /// <summary>Enterprise: rate limit exceeded events (429) per tenant.</summary>
        public const string RateLimitExceeded = "RateLimitExceeded";
    }

    public TenantUsageService(ApplicationDbContext context, ILogger<TenantUsageService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RecordUsageAsync(Guid? companyId, string metricKey, decimal quantity = 1, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty || string.IsNullOrWhiteSpace(metricKey))
            return;

        var tenantId = await _context.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId.Value)
            .Select(c => c.TenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            return;

        var (periodStart, periodEnd) = GetCurrentMonthUtc();
        await RecordIncrementAsync(tenantId.Value, metricKey.Trim(), periodStart, periodEnd, quantity, cancellationToken);
    }

    public async Task RecordStorageDeltaAsync(Guid? companyId, long deltaBytes, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty || deltaBytes == 0)
            return;

        var tenantId = await _context.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId.Value)
            .Select(c => c.TenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            return;

        var (periodStart, periodEnd) = GetCurrentMonthUtc();
        var record = await _context.TenantUsageRecords
            .FirstOrDefaultAsync(r => r.TenantId == tenantId.Value && r.MetricKey == MetricKeys.StorageBytes && r.PeriodStartUtc == periodStart, cancellationToken);
        var now = DateTime.UtcNow;
        var newQuantity = (record != null ? record.Quantity : 0) + deltaBytes;
        if (newQuantity < 0) newQuantity = 0;

        if (record != null)
        {
            record.Quantity = newQuantity;
            record.UpdatedAtUtc = now;
        }
        else
        {
            _context.TenantUsageRecords.Add(new TenantUsageRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                MetricKey = MetricKeys.StorageBytes,
                Quantity = newQuantity,
                PeriodStartUtc = periodStart,
                PeriodEndUtc = periodEnd,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<long> GetCurrentStorageBytesAsync(Guid? companyId, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            return 0;

        var tenantId = await _context.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId.Value)
            .Select(c => c.TenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            return 0;

        var (periodStart, _) = GetCurrentMonthUtc();
        var record = await _context.TenantUsageRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.TenantId == tenantId.Value && r.MetricKey == MetricKeys.StorageBytes && r.PeriodStartUtc == periodStart, cancellationToken);
        return (long)(record?.Quantity ?? 0);
    }

    public async Task RecordIncrementAsync(Guid tenantId, string metricKey, DateTime periodStartUtc, DateTime periodEndUtc, decimal amount = 1, CancellationToken cancellationToken = default)
    {
        if (amount <= 0) return;
        var record = await _context.TenantUsageRecords
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.MetricKey == metricKey && r.PeriodStartUtc == periodStartUtc, cancellationToken);
        var now = DateTime.UtcNow;
        if (record != null)
        {
            record.Quantity += amount;
            record.UpdatedAtUtc = now;
        }
        else
        {
            _context.TenantUsageRecords.Add(new TenantUsageRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                MetricKey = metricKey,
                Quantity = amount,
                PeriodStartUtc = periodStartUtc,
                PeriodEndUtc = periodEndUtc,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SetQuantityAsync(Guid tenantId, string metricKey, DateTime periodStartUtc, DateTime periodEndUtc, decimal quantity, CancellationToken cancellationToken = default)
    {
        var record = await _context.TenantUsageRecords
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.MetricKey == metricKey && r.PeriodStartUtc == periodStartUtc, cancellationToken);
        var now = DateTime.UtcNow;
        if (record != null)
        {
            record.Quantity = quantity;
            record.UpdatedAtUtc = now;
        }
        else
        {
            _context.TenantUsageRecords.Add(new TenantUsageRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                MetricKey = metricKey,
                Quantity = quantity,
                PeriodStartUtc = periodStartUtc,
                PeriodEndUtc = periodEndUtc,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RecalculateUserMetricsForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var companyIds = await _context.Companies
            .Where(c => c.TenantId == tenantId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        if (companyIds.Count == 0) return;

        var totalUsers = await _context.Users
            .CountAsync(u => u.CompanyId != null && companyIds.Contains(u.CompanyId.Value), cancellationToken);
        var activeUsers = await _context.Users
            .CountAsync(u => u.CompanyId != null && companyIds.Contains(u.CompanyId.Value) && u.IsActive, cancellationToken);

        var (periodStart, periodEnd) = GetCurrentMonthUtc();
        await SetQuantityAsync(tenantId, MetricKeys.TotalUsers, periodStart, periodEnd, totalUsers, cancellationToken);
        await SetQuantityAsync(tenantId, MetricKeys.ActiveUsers, periodStart, periodEnd, activeUsers, cancellationToken);
        _logger.LogDebug("Recalculated user metrics for tenant {TenantId}: TotalUsers={Total}, ActiveUsers={Active}", tenantId, totalUsers, activeUsers);
    }

    private static (DateTime Start, DateTime End) GetCurrentMonthUtc()
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        return (start, end);
    }
}
