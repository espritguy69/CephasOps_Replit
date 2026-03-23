using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Billing.Usage;

/// <summary>Reads tenant usage from TenantUsageRecords.</summary>
public class TenantUsageQueryService : ITenantUsageQueryService
{
    private readonly ApplicationDbContext _context;

    public TenantUsageQueryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TenantUsageEntryDto>> GetUsageAsync(Guid tenantId, DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken cancellationToken = default)
    {
        var list = await _context.TenantUsageRecords
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.PeriodStartUtc >= periodStartUtc && r.PeriodStartUtc < periodEndUtc)
            .OrderBy(r => r.MetricKey)
            .Select(r => new TenantUsageEntryDto
            {
                TenantId = r.TenantId,
                MetricKey = r.MetricKey,
                Quantity = r.Quantity,
                PeriodStartUtc = r.PeriodStartUtc,
                PeriodEndUtc = r.PeriodEndUtc,
                UpdatedAtUtc = r.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);
        return list;
    }

    public async Task<IReadOnlyList<TenantUsageEntryDto>> GetCurrentMonthUsageAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        return await GetUsageAsync(tenantId, start, end, cancellationToken);
    }

    public async Task<TenantUsageEntryDto?> GetMetricUsageAsync(Guid tenantId, string metricKey, DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken cancellationToken = default)
    {
        var record = await _context.TenantUsageRecords
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.MetricKey == metricKey && r.PeriodStartUtc == periodStartUtc)
            .Select(r => new TenantUsageEntryDto
            {
                TenantId = r.TenantId,
                MetricKey = r.MetricKey,
                Quantity = r.Quantity,
                PeriodStartUtc = r.PeriodStartUtc,
                PeriodEndUtc = r.PeriodEndUtc,
                UpdatedAtUtc = r.UpdatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);
        return record;
    }
}
