using CephasOps.Application.Rates.DTOs;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Records and reads payout anomaly alert run history. Read-only with respect to payout logic.
/// </summary>
public interface IAlertRunHistoryService
{
    /// <summary>Record a run (scheduler or manual).</summary>
    Task RecordRunAsync(
        RunPayoutAnomalyAlertsResultDto result,
        string triggerSource,
        DateTime startedAt,
        DateTime completedAt,
        CancellationToken cancellationToken = default);

    /// <summary>Get the most recent alert run, or null if none.</summary>
    Task<PayoutAnomalyAlertRunDto?> GetLatestAsync(CancellationToken cancellationToken = default);
}
