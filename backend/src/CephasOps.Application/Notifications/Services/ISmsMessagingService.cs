using CephasOps.Domain.Notifications;

namespace CephasOps.Application.Notifications.Services;

/// <summary>
/// SMS messaging service interface for sending SMS messages
/// </summary>
public interface ISmsMessagingService
{
    /// <summary>
    /// Send SMS message to a phone number
    /// </summary>
    /// <param name="to">Recipient phone number (E.164 format)</param>
    /// <param name="message">Message text</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SMS result with message ID and status</returns>
    Task<SmsResult> SendSmsAsync(string to, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send SMS using a template with placeholders. Company context required (companyId or TenantScope).
    /// </summary>
    /// <param name="to">Recipient phone number</param>
    /// <param name="templateCode">Template code</param>
    /// <param name="placeholders">Placeholder values to replace in template</param>
    /// <param name="companyId">Optional company context; when null uses TenantScope.CurrentTenantId</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SMS result with message ID and status</returns>
    Task<SmsResult> SendTemplateSmsAsync(
        string to,
        string templateCode,
        Dictionary<string, string>? placeholders = null,
        Guid? companyId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get status of a previously sent SMS message
    /// </summary>
    /// <param name="messageId">Message ID from SendSmsAsync result</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SMS result with current status</returns>
    Task<SmsResult> GetStatusAsync(string messageId, CancellationToken cancellationToken = default);
}

