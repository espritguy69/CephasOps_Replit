using CephasOps.Application.Billing.Services;
using CephasOps.Application.Common;
using CephasOps.Application.Pnl.DTOs;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Pnl.Services;

/// <summary>
/// Per-order profitability: Profit = Billing revenue - SI payout - MaterialCost (placeholder) - OtherCost (future).
/// Reuses ResolveInvoiceLineFromOrderAsync (revenue) and RateEngineService (payout).
/// </summary>
public class OrderProfitabilityService : IOrderProfitabilityService
{
    private readonly ApplicationDbContext _context;
    private readonly IBillingService _billingService;
    private readonly IRateEngineService _rateEngineService;
    private readonly ILogger<OrderProfitabilityService> _logger;

    public OrderProfitabilityService(
        ApplicationDbContext context,
        IBillingService billingService,
        IRateEngineService rateEngineService,
        ILogger<OrderProfitabilityService> logger)
    {
        _context = context;
        _billingService = billingService;
        _rateEngineService = rateEngineService;
        _logger = logger;
    }

    public async Task<OrderProfitabilityDto?> CalculateOrderProfitabilityAsync(Guid orderId, Guid companyId, DateTime? referenceDate = null, CancellationToken cancellationToken = default)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("CalculateOrderProfitability");
        FinancialIsolationGuard.RequireCompany(companyId, "CalculateOrderProfitability");
        var refDate = referenceDate ?? DateTime.UtcNow;
        // Explicit company-scoped read (defense-in-depth): do not rely on ambient TenantScope for this lookup.
        var order = await _context.Orders
            .IgnoreQueryFilters()
            .Include(o => o.Partner)
            .Include(o => o.OrderCategory)
            .Where(o => o.Id == orderId && o.CompanyId == companyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for profitability.", orderId);
            return new OrderProfitabilityDto
            {
                OrderId = orderId,
                Status = OrderProfitabilityStatus.Unresolved,
                ReasonCodes = { OrderProfitabilityReasonCodes.OrderNotFound },
                Messages = { "Order not found or not in company scope." }
            };
        }

        var dto = new OrderProfitabilityDto
        {
            OrderId = order.Id,
            OrderNo = order.DocketNumber,
            ServiceId = order.ServiceId,
            OrderTypeId = order.OrderTypeId,
            OrderCategoryId = order.OrderCategoryId,
            InstallationMethodId = order.InstallationMethodId,
            MaterialCostAmount = 0, // Placeholder: material cost not yet implemented
            OtherCostAmount = 0
        };

        // --- Revenue (BillingRatecard) ---
        string? revenueReason = null;
        if (!order.OrderCategoryId.HasValue || order.OrderCategoryId.Value == Guid.Empty)
        {
            revenueReason = OrderProfitabilityReasonCodes.OrderCategoryMissing;
            dto.ReasonCodes.Add(revenueReason);
            dto.Messages.Add("Revenue: Order category is required for BillingRatecard resolution.");
        }
        else if (order.PartnerId == Guid.Empty)
        {
            revenueReason = OrderProfitabilityReasonCodes.PartnerMissing;
            dto.ReasonCodes.Add(revenueReason);
            dto.Messages.Add("Revenue: Order has no partner.");
        }
        else
        {
            var resolvedLine = await _billingService.ResolveInvoiceLineFromOrderAsync(orderId, companyId, refDate, cancellationToken);
            if (resolvedLine != null)
            {
                dto.RevenueAmount = resolvedLine.UnitPrice * resolvedLine.Quantity;
                dto.RevenueSource = "BillingRatecard";
            }
            else
            {
                revenueReason = OrderProfitabilityReasonCodes.NoBillingRatecardFound;
                dto.ReasonCodes.Add(revenueReason);
                dto.Messages.Add("Revenue: No BillingRatecard match for order dimensions.");
            }
        }

        // --- Payout (RateEngineService) ---
        string? payoutReason = null;
        if (!order.OrderCategoryId.HasValue || order.OrderCategoryId.Value == Guid.Empty)
        {
            payoutReason = OrderProfitabilityReasonCodes.OrderCategoryMissing;
            if (!dto.ReasonCodes.Contains(payoutReason))
                dto.ReasonCodes.Add(payoutReason);
            dto.Messages.Add("Payout: Order category is required for SI rate resolution.");
        }
        else
        {
            var partnerGroupId = order.Partner?.GroupId;
            string? siLevel = null;
            if (order.AssignedSiId.HasValue)
            {
                // Explicit company-scoped read (defense-in-depth): order.CompanyId is already validated.
                var si = await _context.ServiceInstallers
                    .IgnoreQueryFilters()
                    .Where(s => s.Id == order.AssignedSiId.Value && s.CompanyId == order.CompanyId)
                    .Select(s => new { s.SiLevel })
                    .FirstOrDefaultAsync(cancellationToken);
                siLevel = si?.SiLevel.ToString();
            }

            if (!order.AssignedSiId.HasValue && string.IsNullOrEmpty(siLevel))
            {
                payoutReason = OrderProfitabilityReasonCodes.NoAssignedSi;
                dto.ReasonCodes.Add(payoutReason);
                dto.Messages.Add("Payout: No assigned SI; SI level unknown. Fallback: use company default SI level when implemented.");
            }
            else if (string.IsNullOrEmpty(siLevel))
            {
                payoutReason = OrderProfitabilityReasonCodes.SiLevelMissing;
                dto.ReasonCodes.Add(payoutReason);
                dto.Messages.Add("Payout: SI level could not be determined.");
            }
            else
            {
                var payoutRequest = new GponRateResolutionRequest
                {
                    CompanyId = order.CompanyId,
                    OrderTypeId = order.OrderTypeId,
                    OrderCategoryId = order.OrderCategoryId!.Value,
                    InstallationMethodId = order.InstallationMethodId,
                    PartnerGroupId = partnerGroupId,
                    PartnerId = order.PartnerId != Guid.Empty ? order.PartnerId : null,
                    ServiceInstallerId = order.AssignedSiId,
                    SiLevel = siLevel,
                    ReferenceDate = refDate
                };
                var rateResult = await _rateEngineService.ResolveGponRatesAsync(payoutRequest);
                if (rateResult.PayoutAmount.HasValue)
                {
                    dto.PayoutAmount = rateResult.PayoutAmount;
                    dto.PayoutSource = rateResult.PayoutSource ?? "GponSiJobRate";
                }
                else
                {
                    payoutReason = OrderProfitabilityReasonCodes.NoSiRateFound;
                    dto.ReasonCodes.Add(payoutReason);
                    dto.Messages.Add("Payout: No SI rate found for order dimensions and SI level.");
                }
            }
        }

        // --- Status & profit ---
        var revenueResolved = dto.RevenueAmount.HasValue;
        var payoutResolved = dto.PayoutAmount.HasValue;
        if (revenueResolved && payoutResolved)
        {
            dto.Status = OrderProfitabilityStatus.Resolved;
            dto.ProfitAmount = dto.RevenueAmount!.Value - dto.PayoutAmount!.Value - dto.MaterialCostAmount - dto.OtherCostAmount;
            if (dto.RevenueAmount.Value > 0)
                dto.MarginPercent = (dto.ProfitAmount.Value / dto.RevenueAmount.Value) * 100;
        }
        else if (revenueResolved || payoutResolved)
        {
            dto.Status = OrderProfitabilityStatus.Partial;
            dto.ProfitAmount = null; // Do not use zero for missing side
            if (dto.RevenueAmount.HasValue && dto.RevenueAmount.Value > 0 && dto.ProfitAmount.HasValue)
                dto.MarginPercent = (dto.ProfitAmount.Value / dto.RevenueAmount.Value) * 100;
        }
        else
        {
            dto.Status = OrderProfitabilityStatus.Unresolved;
        }

        dto.Warning = dto.Messages.Count > 0 ? string.Join(" ", dto.Messages) : null;
        return dto;
    }

    /// <inheritdoc />
    public async Task<GponRateResolutionResult?> GetOrderPayoutBreakdownAsync(Guid orderId, Guid? companyId, DateTime? referenceDate = null, CancellationToken cancellationToken = default)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("GetOrderPayoutBreakdown");
        var refDate = referenceDate ?? DateTime.UtcNow;
        var query = _context.Orders.Include(o => o.Partner).AsQueryable();
        if (companyId.HasValue)
            query = query.Where(o => o.Id == orderId && o.CompanyId == companyId);
        else
            query = query.Where(o => o.Id == orderId);
        var order = await query.FirstOrDefaultAsync(cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for payout breakdown.", orderId);
            return null;
        }
        FinancialIsolationGuard.RequireCompany(order.CompanyId, "GetOrderPayoutBreakdown");
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            FinancialIsolationGuard.RequireSameCompany(order.CompanyId, companyId, "Order", "Request", order.Id, null);
        if (!order.OrderCategoryId.HasValue || order.OrderCategoryId.Value == Guid.Empty)
        {
            return GponRateResolutionResult.Failed("Order category is required for payout resolution.");
        }
        var partnerGroupId = order.Partner?.GroupId;
        string? siLevel = null;
        if (order.AssignedSiId.HasValue)
        {
            var si = await _context.ServiceInstallers
                .Where(s => s.Id == order.AssignedSiId.Value)
                .Select(s => new { s.SiLevel })
                .FirstOrDefaultAsync(cancellationToken);
            siLevel = si?.SiLevel.ToString();
        }
        if (string.IsNullOrEmpty(siLevel))
        {
            return GponRateResolutionResult.Failed("No assigned SI or SI level could not be determined.");
        }
        var request = new GponRateResolutionRequest
        {
            OrderTypeId = order.OrderTypeId,
            OrderCategoryId = order.OrderCategoryId!.Value,
            InstallationMethodId = order.InstallationMethodId,
            PartnerGroupId = partnerGroupId,
            PartnerId = order.PartnerId != Guid.Empty ? order.PartnerId : null,
            ServiceInstallerId = order.AssignedSiId,
            SiLevel = siLevel,
            ReferenceDate = refDate
        };
        var result = await _rateEngineService.ResolveGponRatesAsync(request);
        return result;
    }

    public async Task<List<OrderProfitabilityDto>> CalculateOrdersProfitabilityAsync(IReadOnlyList<Guid> orderIds, Guid companyId, DateTime? referenceDate = null, CancellationToken cancellationToken = default)
    {
        if (orderIds == null || orderIds.Count == 0)
            return new List<OrderProfitabilityDto>();
        var results = new List<OrderProfitabilityDto>();
        foreach (var id in orderIds.Distinct())
        {
            var dto = await CalculateOrderProfitabilityAsync(id, companyId, referenceDate, cancellationToken);
            if (dto != null)
                results.Add(dto);
        }
        return results;
    }
}
