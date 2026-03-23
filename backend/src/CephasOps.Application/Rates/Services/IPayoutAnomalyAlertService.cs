using CephasOps.Application.Rates.DTOs;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Runs payout anomaly alerting: detects new high-severity (and optionally repeated medium) anomalies,
/// avoids duplicates, sends via configured channels, and records alert history. Does not change payout or detection logic.
/// </summary>
public interface IPayoutAnomalyAlertService
{
    /// <summary>
    /// Run alerting for current anomalies: filter by severity, apply duplicate-prevention, send, and record.
    /// </summary>
    Task<RunPayoutAnomalyAlertsResultDto> RunAlertsAsync(
        RunPayoutAnomalyAlertsRequestDto request,
        CancellationToken cancellationToken = default);
}
