using CephasOps.Application.Rates.DTOs;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Read-only response tracking: summary counts of alerted anomalies by review status, stale list, time-to-action.
/// Does not change payout, detection, or alert logic.
/// </summary>
public interface IPayoutAnomalyResponseTrackingService
{
    /// <summary>Get alert response summary (alerted+open, alerted+acknowledged, ... stale count, avg time to first action).</summary>
    /// <param name="filter">Filter by date range, installer, anomaly type, severity, company.</param>
    /// <param name="staleThresholdHoursOverride">Optional override for stale threshold (hours). When null, config default is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<AlertResponseSummaryDto> GetAlertResponseSummaryAsync(PayoutAnomalyFilterDto filter, int? staleThresholdHoursOverride = null, CancellationToken cancellationToken = default);

    /// <summary>Get alerted anomalies with no action after threshold (stale).</summary>
    /// <param name="filter">Filter by date range, installer, anomaly type, severity, company. Optional.</param>
    /// <param name="limit">Maximum number of anomalies to return (default 50).</param>
    /// <param name="staleThresholdHoursOverride">Optional override for stale threshold (hours). When null, config default is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<PayoutAnomalyDto>> GetStaleAlertedAnomaliesAsync(PayoutAnomalyFilterDto? filter, int limit = 50, int? staleThresholdHoursOverride = null, CancellationToken cancellationToken = default);
}
