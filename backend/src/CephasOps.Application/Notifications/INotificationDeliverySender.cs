using CephasOps.Domain.Notifications.Entities;

namespace CephasOps.Application.Notifications;

/// <summary>
/// Sends a single notification dispatch via the appropriate channel (Phase 2).
/// Implementation uses existing template and provider services.
/// </summary>
public interface INotificationDeliverySender
{
    /// <summary>Send the dispatch; returns true if sent successfully.</summary>
    Task<(bool Success, string? ErrorMessage)> SendAsync(NotificationDispatch dispatch, CancellationToken cancellationToken = default);
}
