using CephasOps.Application.Pnl.DTOs;
using CephasOps.Application.Rates.DTOs;

namespace CephasOps.Application.Pnl.Services;

/// <summary>
/// Per-order profitability calculation for GPON orders.
/// Reuses BillingRatecard (revenue) and RateEngineService (SI payout).
/// </summary>
public interface IOrderProfitabilityService
{
    /// <summary>
    /// Calculate profitability for a single order.
    /// </summary>
    Task<OrderProfitabilityDto?> CalculateOrderProfitabilityAsync(Guid orderId, Guid companyId, DateTime? referenceDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate profitability for multiple orders.
    /// </summary>
    Task<List<OrderProfitabilityDto>> CalculateOrdersProfitabilityAsync(IReadOnlyList<Guid> orderIds, Guid companyId, DateTime? referenceDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get full payout breakdown for an order (base rate, modifiers, trace). Read-only; uses existing rate resolution.
    /// </summary>
    Task<GponRateResolutionResult?> GetOrderPayoutBreakdownAsync(Guid orderId, Guid? companyId, DateTime? referenceDate = null, CancellationToken cancellationToken = default);
}
