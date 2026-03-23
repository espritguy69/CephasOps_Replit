using CephasOps.Application.Notifications.DTOs;

using CephasOps.Domain.Notifications;

namespace CephasOps.Application.Notifications.Services;

/// <summary>
/// WhatsApp messaging service for sending template messages with dynamic parameters
/// </summary>
public interface IWhatsAppMessagingService
{
    /// <summary>
    /// Send a WhatsApp template message with dynamic parameters
    /// </summary>
    /// <param name="to">Recipient phone number (E.164 format)</param>
    /// <param name="templateName">WhatsApp Business API template name</param>
    /// <param name="parameters">Dynamic parameters for template placeholders</param>
    /// <param name="languageCode">Language code (default: "en")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>WhatsApp result with message ID and status</returns>
    Task<WhatsAppResult> SendTemplateMessageAsync(
        string to,
        string templateName,
        Dictionary<string, string>? parameters = null,
        string? languageCode = "en",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send job update notification to customer
    /// </summary>
    Task<WhatsAppResult> SendJobUpdateAsync(
        string customerPhone,
        string orderNumber,
        string status,
        string? appointmentDate = null,
        string? installerName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send SI (Service Installer) on-the-way alert to customer
    /// </summary>
    Task<WhatsAppResult> SendSiOnTheWayAlertAsync(
        string customerPhone,
        string orderNumber,
        string installerName,
        string? estimatedArrival = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send TTKT (Tiket/Ticket) notification
    /// </summary>
    Task<WhatsAppResult> SendTtktNotificationAsync(
        string customerPhone,
        string ticketNumber,
        string issueDescription,
        string? resolution = null,
        CancellationToken cancellationToken = default);
}

