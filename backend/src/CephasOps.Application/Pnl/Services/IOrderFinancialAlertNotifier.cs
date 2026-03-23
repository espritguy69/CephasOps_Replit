using CephasOps.Application.Pnl.DTOs;

namespace CephasOps.Application.Pnl.Services;

/// <summary>
/// Hook for notifying when Critical financial alerts exist (e.g. email, in-app). No-op by default.
/// </summary>
public interface IOrderFinancialAlertNotifier
{
    /// <summary>
    /// Called when an order has one or more Critical financial alerts (e.g. after evaluate-and-save).
    /// </summary>
    Task NotifyCriticalAlertsAsync(Guid orderId, OrderFinancialAlertsResultDto result, CancellationToken cancellationToken = default);
}
