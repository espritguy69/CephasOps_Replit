using CephasOps.Application.Rates.DTOs;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Read-only payout anomaly detection. No payout or payroll logic changed; monitoring/flagging only.
/// Future: acknowledge anomaly, notes trail, false positive vs confirmed, link to pricing/override review,
/// optional alerting, optional payroll hold recommendation (see docs/PAYOUT_ANOMALY_DETECTION_DELIVERABLE.md).
/// </summary>
public interface IPayoutAnomalyService
{
    /// <summary>
    /// Get summary counts by anomaly type and severity for dashboard cards.
    /// </summary>
    Task<PayoutAnomalyDetectionSummaryDto> GetAnomalySummaryAsync(PayoutAnomalyFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paged list of anomalies with optional filters.
    /// </summary>
    Task<PayoutAnomalyListResultDto> GetAnomaliesAsync(PayoutAnomalyFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top clusters (e.g. installers with most custom overrides, contexts with most legacy fallback).
    /// </summary>
    Task<IReadOnlyList<PayoutAnomalyClusterDto>> GetTopClustersAsync(PayoutAnomalyFilterDto filter, int top = 10, CancellationToken cancellationToken = default);
}
