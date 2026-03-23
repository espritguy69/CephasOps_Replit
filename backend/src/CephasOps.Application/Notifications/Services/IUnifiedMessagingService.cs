using CephasOps.Application.Notifications.DTOs;

namespace CephasOps.Application.Notifications.Services;

/// <summary>
/// Unified messaging service interface
/// Handles smart routing between SMS and WhatsApp based on customer preferences
/// </summary>
public interface IUnifiedMessagingService
{
    /// <summary>
    /// Send job update notification (routed based on customer preference and urgency)
    /// </summary>
    Task<MessagingResult> SendJobUpdateAsync(
        string customerPhone,
        string orderNumber,
        string status,
        string? appointmentDate = null,
        string? installerName = null,
        bool isUrgent = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send SI (Service Installer) on-the-way alert (routed based on customer preference)
    /// </summary>
    Task<MessagingResult> SendSiOnTheWayAlertAsync(
        string customerPhone,
        string orderNumber,
        string installerName,
        string? estimatedArrival = null,
        bool isUrgent = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send TTKT (Ticket) notification (routed based on customer preference)
    /// </summary>
    Task<MessagingResult> SendTtktNotificationAsync(
        string customerPhone,
        string ticketNumber,
        string issueDescription,
        string? resolution = null,
        bool isUrgent = false,
        CancellationToken cancellationToken = default);
}

