using CephasOps.Application.Pnl.DTOs;

namespace CephasOps.Application.Pnl.Services;

/// <summary>
/// No-op implementation of financial alert notification. Wire a real implementation for email/in-app notifications.
/// </summary>
public sealed class NoOpOrderFinancialAlertNotifier : IOrderFinancialAlertNotifier
{
    public Task NotifyCriticalAlertsAsync(Guid orderId, OrderFinancialAlertsResultDto result, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
