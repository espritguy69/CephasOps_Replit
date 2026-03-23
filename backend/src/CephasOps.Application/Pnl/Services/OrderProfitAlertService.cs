using CephasOps.Application.Pnl.DTOs;
using CephasOps.Domain.Pnl.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Pnl.Services;

/// <summary>
/// Derives financial alerts from profitability results. Does not duplicate revenue/payout resolution.
/// Alerts can be computed only or evaluated and persisted (with optional Critical notification).
/// </summary>
public class OrderProfitAlertService : IOrderProfitAlertService
{
    private readonly IOrderProfitabilityService _profitabilityService;
    private readonly ProfitabilityAlertsOptions _options;
    private readonly ApplicationDbContext _context;
    private readonly IOrderFinancialAlertNotifier _notifier;
    private readonly ILogger<OrderProfitAlertService> _logger;

    public OrderProfitAlertService(
        IOrderProfitabilityService profitabilityService,
        IOptions<ProfitabilityAlertsOptions> options,
        ApplicationDbContext context,
        IOrderFinancialAlertNotifier notifier,
        ILogger<OrderProfitAlertService> logger)
    {
        _profitabilityService = profitabilityService;
        _options = options?.Value ?? new ProfitabilityAlertsOptions();
        _context = context;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<OrderFinancialAlertsResultDto> EvaluateOrderAlertsAsync(Guid orderId, Guid companyId, DateTime? referenceDate = null, CancellationToken cancellationToken = default)
    {
        var prof = await _profitabilityService.CalculateOrderProfitabilityAsync(orderId, companyId, referenceDate, cancellationToken);
        return DeriveAlerts(prof, orderId);
    }

    public async Task<List<OrderFinancialAlertsResultDto>> EvaluateOrdersAlertsAsync(IReadOnlyList<Guid> orderIds, Guid companyId, DateTime? referenceDate = null, CancellationToken cancellationToken = default)
    {
        if (orderIds == null || orderIds.Count == 0)
            return new List<OrderFinancialAlertsResultDto>();
        var profList = await _profitabilityService.CalculateOrdersProfitabilityAsync(orderIds, companyId, referenceDate, cancellationToken);
        return profList.Select(p => DeriveAlerts(p, p.OrderId)).ToList();
    }

    public async Task<OrderFinancialAlertsResultDto> EvaluateAndSaveOrderAlertsAsync(Guid orderId, Guid companyId, DateTime? referenceDate = null, CancellationToken cancellationToken = default)
    {
        var result = await EvaluateOrderAlertsAsync(orderId, companyId, referenceDate, cancellationToken);

        var existing = await _context.OrderFinancialAlerts
            .Where(a => a.OrderId == orderId && a.CompanyId == companyId && !a.IsDeleted)
            .ToListAsync(cancellationToken);
        _context.OrderFinancialAlerts.RemoveRange(existing);

        var now = DateTime.UtcNow;
        foreach (var a in result.Alerts)
        {
            _context.OrderFinancialAlerts.Add(new OrderFinancialAlert
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                OrderId = a.OrderId,
                AlertCode = a.AlertCode,
                Severity = a.Severity,
                Message = a.Message,
                RevenueAmount = a.RevenueAmount,
                PayoutAmount = a.PayoutAmount,
                ProfitAmount = a.ProfitAmount,
                MarginPercent = a.MarginPercent,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (result.HighestSeverity == OrderFinancialAlertSeverity.Critical)
        {
            try
            {
                await _notifier.NotifyCriticalAlertsAsync(orderId, result, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "NotifyCriticalAlertsAsync failed for order {OrderId}", orderId);
            }
        }

        return result;
    }

    public async Task<List<PersistedOrderFinancialAlertDto>> GetPersistedAlertsAsync(ListOrderFinancialAlertsQuery query, CancellationToken cancellationToken = default)
    {
        var q = _context.OrderFinancialAlerts
            .AsNoTracking()
            .Where(a => a.CompanyId == query.CompanyId && !a.IsDeleted);

        if (query.OrderId.HasValue)
            q = q.Where(a => a.OrderId == query.OrderId.Value);
        if (!string.IsNullOrEmpty(query.Severity))
            q = q.Where(a => a.Severity == query.Severity);
        if (query.FromUtc.HasValue)
            q = q.Where(a => a.CreatedAt >= query.FromUtc.Value);
        if (query.ToUtc.HasValue)
            q = q.Where(a => a.CreatedAt <= query.ToUtc.Value);
        if (query.ActiveOnly)
            q = q.Where(a => a.IsActive);

        var list = await q.OrderByDescending(a => a.CreatedAt).ToListAsync(cancellationToken);
        return list.Select(a => new PersistedOrderFinancialAlertDto
        {
            Id = a.Id,
            OrderId = a.OrderId,
            AlertCode = a.AlertCode,
            Severity = a.Severity,
            Message = a.Message,
            RevenueAmount = a.RevenueAmount,
            PayoutAmount = a.PayoutAmount,
            ProfitAmount = a.ProfitAmount,
            MarginPercent = a.MarginPercent,
            CreatedAtUtc = a.CreatedAt,
            IsActive = a.IsActive
        }).ToList();
    }

    public async Task<IReadOnlyList<OrderFinancialAlertSummaryDto>> GetOrderFinancialAlertSummariesAsync(Guid companyId, IReadOnlyList<Guid> orderIds, CancellationToken cancellationToken = default)
    {
        if (orderIds == null || orderIds.Count == 0)
            return Array.Empty<OrderFinancialAlertSummaryDto>();

        var rows = await _context.OrderFinancialAlerts
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId && orderIds.Contains(a.OrderId) && a.IsActive && !a.IsDeleted)
            .Select(a => new { a.OrderId, a.Severity })
            .ToListAsync(cancellationToken);

        var byOrder = rows.GroupBy(r => r.OrderId).Select(g =>
        {
            var count = g.Count();
            var highest = g.Select(x => x.Severity).MaxBy(SeverityRank) ?? null;
            return new OrderFinancialAlertSummaryDto { OrderId = g.Key, ActiveAlertCount = count, HighestAlertSeverity = highest };
        }).ToList();

        return byOrder;
    }

    /// <summary>Severity priority: Critical (3) &gt; Warning (2) &gt; Info (1).</summary>
    private static int SeverityRank(string? severity)
    {
        if (string.IsNullOrEmpty(severity)) return 0;
        if (string.Equals(severity, OrderFinancialAlertSeverity.Critical, StringComparison.OrdinalIgnoreCase)) return 3;
        if (string.Equals(severity, OrderFinancialAlertSeverity.Warning, StringComparison.OrdinalIgnoreCase)) return 2;
        if (string.Equals(severity, OrderFinancialAlertSeverity.Info, StringComparison.OrdinalIgnoreCase)) return 1;
        return 0;
    }

    private OrderFinancialAlertsResultDto DeriveAlerts(OrderProfitabilityDto? prof, Guid orderId)
    {
        var result = new OrderFinancialAlertsResultDto { OrderId = orderId };
        var now = DateTime.UtcNow;

        if (prof == null)
        {
            result.Alerts.Add(new OrderFinancialAlertDto
            {
                OrderId = orderId,
                AlertCode = OrderFinancialAlertCodes.ProfitabilityUnresolved,
                Severity = OrderFinancialAlertSeverity.Critical,
                Message = "Order not found or profitability could not be calculated.",
                CreatedAtUtc = now
            });
            result.HighestSeverity = OrderFinancialAlertSeverity.Critical;
            return result;
        }

        var reasons = prof.ReasonCodes.ToHashSet();

        // Critical: category missing
        if (reasons.Contains(OrderProfitabilityReasonCodes.OrderCategoryMissing))
        {
            result.Alerts.Add(CreateAlert(prof, orderId, OrderFinancialAlertCodes.OrderCategoryMissing, OrderFinancialAlertSeverity.Critical,
                "Order category is missing; revenue and payout cannot be resolved.", now));
        }

        // Unresolved / missing rates
        if (prof.Status == OrderProfitabilityStatus.Unresolved)
        {
            result.Alerts.Add(CreateAlert(prof, orderId, OrderFinancialAlertCodes.ProfitabilityUnresolved, OrderFinancialAlertSeverity.Critical,
                "Profitability is unresolved; one or both of revenue and payout could not be determined.", now));
        }

        if (reasons.Contains(OrderProfitabilityReasonCodes.NoBillingRatecardFound))
        {
            result.Alerts.Add(CreateAlert(prof, orderId, OrderFinancialAlertCodes.NoBillingRateFound, OrderFinancialAlertSeverity.Critical,
                "No BillingRatecard found for this order; revenue unknown.", now));
        }

        if (reasons.Contains(OrderProfitabilityReasonCodes.NoSiRateFound) || reasons.Contains(OrderProfitabilityReasonCodes.SiLevelMissing) || reasons.Contains(OrderProfitabilityReasonCodes.NoAssignedSi))
        {
            result.Alerts.Add(CreateAlert(prof, orderId, OrderFinancialAlertCodes.NoPayoutRateFound, OrderFinancialAlertSeverity.Warning,
                "No SI payout rate found or SI level missing; payout unknown.", now));
        }

        // Installation method missing (warning when order has category but no method — rate may be less specific)
        if (prof.OrderCategoryId.HasValue && !prof.InstallationMethodId.HasValue)
        {
            result.Alerts.Add(CreateAlert(prof, orderId, OrderFinancialAlertCodes.InstallationMethodMissing, OrderFinancialAlertSeverity.Warning,
                "Installation method not set; rate resolution may use a less specific rate.", now));
        }

        // Resolved profitability: check profit and margin
        if (prof.RevenueAmount.HasValue && prof.PayoutAmount.HasValue)
        {
            if (prof.ProfitAmount.HasValue && prof.ProfitAmount.Value < 0)
            {
                result.Alerts.Add(CreateAlert(prof, orderId, OrderFinancialAlertCodes.NegativeProfit, OrderFinancialAlertSeverity.Critical,
                    $"Profit is negative: {prof.ProfitAmount.Value:N2}.", now));
            }

            if (prof.PayoutAmount.Value > prof.RevenueAmount.Value)
            {
                result.Alerts.Add(CreateAlert(prof, orderId, OrderFinancialAlertCodes.PayoutExceedsRevenue, OrderFinancialAlertSeverity.Critical,
                    "Payout exceeds revenue; order is loss-making.", now));
            }

            if (prof.MarginPercent.HasValue && prof.RevenueAmount.Value > 0)
            {
                var threshold = _options.LowMarginThresholdPercent;
                if (prof.MarginPercent.Value < threshold)
                {
                    result.Alerts.Add(CreateAlert(prof, orderId, OrderFinancialAlertCodes.LowMargin, OrderFinancialAlertSeverity.Warning,
                        $"Margin {prof.MarginPercent.Value:N1}% is below threshold {threshold}%.", now));
                }
            }
        }

        result.HighestSeverity = HighestSeverity(result.Alerts.Select(a => a.Severity));
        return result;
    }

    private static OrderFinancialAlertDto CreateAlert(OrderProfitabilityDto prof, Guid orderId, string code, string severity, string message, DateTime createdAtUtc)
    {
        return new OrderFinancialAlertDto
        {
            OrderId = orderId,
            AlertCode = code,
            Severity = severity,
            Message = message,
            RevenueAmount = prof.RevenueAmount,
            PayoutAmount = prof.PayoutAmount,
            ProfitAmount = prof.ProfitAmount,
            MarginPercent = prof.MarginPercent,
            CreatedAtUtc = createdAtUtc
        };
    }

    private static string? HighestSeverity(IEnumerable<string> severities)
    {
        var list = severities.ToList();
        if (list.Count == 0) return null;
        if (list.Contains(OrderFinancialAlertSeverity.Critical)) return OrderFinancialAlertSeverity.Critical;
        if (list.Contains(OrderFinancialAlertSeverity.Warning)) return OrderFinancialAlertSeverity.Warning;
        return OrderFinancialAlertSeverity.Info;
    }
}
