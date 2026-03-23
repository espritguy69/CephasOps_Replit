using CephasOps.Application.Rates.DTOs;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Sends payout anomaly alerts to a channel (Email, Slack, Telegram). Read-only with respect to payout logic.
/// </summary>
public interface IPayoutAnomalyAlertSender
{
    /// <summary>Channel name (e.g. Email, Slack, Telegram).</summary>
    string ChannelName { get; }

    /// <summary>
    /// Send alerts for the given anomalies to the given recipients. Returns count of successful sends.
    /// </summary>
    Task<int> SendAsync(
        IReadOnlyList<PayoutAnomalyDto> anomalies,
        IReadOnlyList<string> recipients,
        CancellationToken cancellationToken = default);
}
