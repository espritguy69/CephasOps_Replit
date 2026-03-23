using CephasOps.Application.Rates.DTOs;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Read-only service for the Payout Health Dashboard. No payout calculation or snapshot creation.
/// </summary>
public interface IPayoutHealthDashboardService
{
    /// <summary>
    /// Get snapshot health, anomaly summary, top unusual payouts, and recent snapshots.
    /// </summary>
    Task<PayoutHealthDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
