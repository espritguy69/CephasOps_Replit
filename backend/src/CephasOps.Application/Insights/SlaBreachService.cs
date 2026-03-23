using CephasOps.Domain.Orders.Enums;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Insights;

/// <summary>Read-only SLA breach engine. Uses Order.KpiDueAt as authoritative due time. Classifies orders as NoSla, OnTrack, NearingBreach, or Breached with explainable reasons.</summary>
public class SlaBreachService : ISlaBreachService
{
    private readonly ApplicationDbContext _context;
    private readonly OperationalSlaOptions _options;

    public SlaBreachService(ApplicationDbContext context, Microsoft.Extensions.Options.IOptions<OperationalSlaOptions>? options = null)
    {
        _context = context;
        _options = options?.Value ?? new OperationalSlaOptions();
    }

    public async Task<SlaBreachSummaryDto> GetSummaryAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context required: CompanyId cannot be empty.");

        var orders = await GetActiveOrdersWithSlaStateAsync(companyId, cancellationToken);
        var distribution = new SlaBreachDistributionDto
        {
            OnTrackCount = orders.Count(o => o.BreachState == SlaBreachState.OnTrack),
            NearingBreachCount = orders.Count(o => o.BreachState == SlaBreachState.NearingBreach),
            BreachedCount = orders.Count(o => o.BreachState == SlaBreachState.Breached),
            NoSlaCount = orders.Count(o => o.BreachState == SlaBreachState.NoSla)
        };

        return new SlaBreachSummaryDto
        {
            Distribution = distribution,
            GeneratedAtUtc = DateTime.UtcNow
        };
    }

    public async Task<IReadOnlyList<SlaBreachOrderItemDto>> GetOrdersAtRiskAsync(Guid companyId, string? breachState = null, string? severity = null, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context required: CompanyId cannot be empty.");

        var orders = await GetActiveOrdersWithSlaStateAsync(companyId, cancellationToken);
        var atRisk = orders
            .Where(o => o.BreachState == SlaBreachState.NearingBreach || o.BreachState == SlaBreachState.Breached)
            .ToList();

        if (breachState != null)
            atRisk = atRisk.Where(o => string.Equals(o.BreachState, breachState, StringComparison.OrdinalIgnoreCase)).ToList();
        if (severity != null)
            atRisk = atRisk.Where(o => string.Equals(o.Severity, severity, StringComparison.OrdinalIgnoreCase)).ToList();

        return atRisk.OrderBy(o => o.KpiDueAt).Take(_options.MaxOrdersAtRisk).ToList();
    }

    public async Task<SlaBreachSummaryDto> GetPlatformSummaryAsync(CancellationToken cancellationToken = default)
    {
        return await TenantScopeExecutor.RunWithPlatformBypassAsync(async ct =>
        {
            var companyIds = await _context.Companies
                .Where(c => c.TenantId != null)
                .Select(c => c.Id)
                .ToListAsync(ct);

            int onTrack = 0, nearing = 0, breached = 0, noSla = 0;
            foreach (var cid in companyIds)
            {
                try
                {
                    var summary = await GetSummaryAsync(cid, ct);
                    onTrack += summary.Distribution.OnTrackCount;
                    nearing += summary.Distribution.NearingBreachCount;
                    breached += summary.Distribution.BreachedCount;
                    noSla += summary.Distribution.NoSlaCount;
                }
                catch
                {
                    // Skip tenant on error
                }
            }

            return new SlaBreachSummaryDto
            {
                Distribution = new SlaBreachDistributionDto
                {
                    OnTrackCount = onTrack,
                    NearingBreachCount = nearing,
                    BreachedCount = breached,
                    NoSlaCount = noSla
                },
                GeneratedAtUtc = DateTime.UtcNow
            };
        }, cancellationToken);
    }

    private async Task<List<SlaBreachOrderItemDto>> GetActiveOrdersWithSlaStateAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var activeStatuses = new[] { OrderStatus.Completed, OrderStatus.Cancelled, OrderStatus.Rejected };

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CompanyId == companyId && !activeStatuses.Contains(o.Status ?? ""))
            .Select(o => new
            {
                o.Id,
                o.ServiceId,
                o.TicketId,
                o.Status,
                o.AssignedSiId,
                o.KpiDueAt,
                o.UpdatedAt,
                o.RescheduleCount
            })
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(o => o.Id).ToList();
        var blockerOrderIds = await _context.OrderBlockers
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId && orderIds.Contains(b.OrderId))
            .Select(b => b.OrderId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var replacementOrderIds = await _context.OrderMaterialReplacements
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId && orderIds.Contains(r.OrderId))
            .Select(r => r.OrderId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var lastLog = await _context.OrderStatusLogs
            .AsNoTracking()
            .Where(l => l.CompanyId == companyId && orderIds.Contains(l.OrderId))
            .GroupBy(l => l.OrderId)
            .Select(g => new { OrderId = g.Key, LastAt = g.Max(l => l.CreatedAt) })
            .ToListAsync(cancellationToken);
        var lastLogMap = lastLog.ToDictionary(x => x.OrderId, x => x.LastAt);

        var result = new List<SlaBreachOrderItemDto>();
        foreach (var o in orders)
        {
            var hasBlocker = blockerOrderIds.Contains(o.Id);
            var hasReplacement = replacementOrderIds.Contains(o.Id);
            var hasReschedule = o.RescheduleCount > 0;
            var lastActivityAt = lastLogMap.TryGetValue(o.Id, out var lastAt) ? lastAt : o.UpdatedAt;

            if (!o.KpiDueAt.HasValue)
            {
                result.Add(new SlaBreachOrderItemDto
                {
                    OrderId = o.Id,
                    OrderRef = o.ServiceId ?? o.TicketId,
                    CompanyId = companyId,
                    CurrentStatus = o.Status,
                    AssignedSiId = o.AssignedSiId,
                    KpiDueAt = null,
                    NowUtc = now,
                    MinutesToDueOrOverdue = null,
                    BreachState = SlaBreachState.NoSla,
                    Severity = "Info",
                    Explanation = "Order has no SLA because no KPI due time is set.",
                    LastActivityAt = lastActivityAt,
                    HasBlocker = hasBlocker,
                    HasReplacement = hasReplacement,
                    HasReschedule = hasReschedule
                });
                continue;
            }

            var dueUtc = o.KpiDueAt.Value;
            var minutesToDue = (int)(dueUtc - now).TotalMinutes;

            string breachState;
            string severity;
            string explanation;

            if (now >= dueUtc)
            {
                breachState = SlaBreachState.Breached;
                var overdueMinutes = (int)(now - dueUtc).TotalMinutes;
                severity = overdueMinutes >= _options.BreachedCriticalOverdueMinutes ? "Critical" : "Warning";
                explanation = overdueMinutes >= _options.BreachedCriticalOverdueMinutes
                    ? $"Order is in SLA breach: KPI due time passed {overdueMinutes} minutes ago (critical threshold {_options.BreachedCriticalOverdueMinutes} min)."
                    : $"Order is in SLA breach: KPI due time passed {overdueMinutes} minutes ago.";
                result.Add(new SlaBreachOrderItemDto
                {
                    OrderId = o.Id,
                    OrderRef = o.ServiceId ?? o.TicketId,
                    CompanyId = companyId,
                    CurrentStatus = o.Status,
                    AssignedSiId = o.AssignedSiId,
                    KpiDueAt = dueUtc,
                    NowUtc = now,
                    MinutesToDueOrOverdue = -overdueMinutes,
                    BreachState = breachState,
                    Severity = severity,
                    Explanation = explanation,
                    LastActivityAt = lastActivityAt,
                    HasBlocker = hasBlocker,
                    HasReplacement = hasReplacement,
                    HasReschedule = hasReschedule
                });
            }
            else if (minutesToDue <= _options.NearingBreachMinutes)
            {
                breachState = SlaBreachState.NearingBreach;
                severity = "Warning";
                explanation = $"Order is nearing SLA breach: KPI due time is in {minutesToDue} minutes (threshold {_options.NearingBreachMinutes} min).";
                result.Add(new SlaBreachOrderItemDto
                {
                    OrderId = o.Id,
                    OrderRef = o.ServiceId ?? o.TicketId,
                    CompanyId = companyId,
                    CurrentStatus = o.Status,
                    AssignedSiId = o.AssignedSiId,
                    KpiDueAt = dueUtc,
                    NowUtc = now,
                    MinutesToDueOrOverdue = minutesToDue,
                    BreachState = breachState,
                    Severity = severity,
                    Explanation = explanation,
                    LastActivityAt = lastActivityAt,
                    HasBlocker = hasBlocker,
                    HasReplacement = hasReplacement,
                    HasReschedule = hasReschedule
                });
            }
            else
            {
                breachState = SlaBreachState.OnTrack;
                severity = "Info";
                explanation = $"Order is on track: KPI due in {minutesToDue} minutes.";
                result.Add(new SlaBreachOrderItemDto
                {
                    OrderId = o.Id,
                    OrderRef = o.ServiceId ?? o.TicketId,
                    CompanyId = companyId,
                    CurrentStatus = o.Status,
                    AssignedSiId = o.AssignedSiId,
                    KpiDueAt = dueUtc,
                    NowUtc = now,
                    MinutesToDueOrOverdue = minutesToDue,
                    BreachState = breachState,
                    Severity = severity,
                    Explanation = explanation,
                    LastActivityAt = lastActivityAt,
                    HasBlocker = hasBlocker,
                    HasReplacement = hasReplacement,
                    HasReschedule = hasReschedule
                });
            }
        }

        return result;
    }
}
