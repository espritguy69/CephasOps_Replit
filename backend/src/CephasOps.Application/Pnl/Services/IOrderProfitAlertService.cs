using CephasOps.Application.Pnl.DTOs;

namespace CephasOps.Application.Pnl.Services;

/// <summary>
/// Evaluates per-order financial alerts from profitability results. Does not reimplement rate resolution.
/// </summary>
public interface IOrderProfitAlertService
{
    /// <summary>
    /// Evaluate financial alerts for a single order (calls profitability service, then derives alerts).
    /// </summary>
    Task<OrderFinancialAlertsResultDto> EvaluateOrderAlertsAsync(Guid orderId, Guid companyId, DateTime? referenceDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluate financial alerts for multiple orders.
    /// </summary>
    Task<List<OrderFinancialAlertsResultDto>> EvaluateOrdersAlertsAsync(IReadOnlyList<Guid> orderIds, Guid companyId, DateTime? referenceDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluate alerts for an order and persist them (replacing any existing for that order). Notifies when Critical alerts exist.
    /// </summary>
    Task<OrderFinancialAlertsResultDto> EvaluateAndSaveOrderAlertsAsync(Guid orderId, Guid companyId, DateTime? referenceDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get persisted financial alerts (for dashboard/list API).
    /// </summary>
    Task<List<PersistedOrderFinancialAlertDto>> GetPersistedAlertsAsync(ListOrderFinancialAlertsQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get financial alert summaries from persisted active alerts for the given order IDs (batch, for enrichment).
    /// Only active alerts are counted. Highest severity: Critical &gt; Warning &gt; Info.
    /// </summary>
    Task<IReadOnlyList<OrderFinancialAlertSummaryDto>> GetOrderFinancialAlertSummariesAsync(Guid companyId, IReadOnlyList<Guid> orderIds, CancellationToken cancellationToken = default);
}
