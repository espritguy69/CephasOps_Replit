namespace CephasOps.Domain.Notifications;

/// <summary>
/// WhatsApp provider interface for sending WhatsApp messages
/// </summary>
public interface IWhatsAppProvider
{
    /// <summary>
    /// Send WhatsApp message to a phone number
    /// </summary>
    /// <param name="to">Recipient phone number (E.164 format)</param>
    /// <param name="message">Message text</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>WhatsApp result with message ID and status</returns>
    Task<WhatsAppResult> SendMessageAsync(string to, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get status of a previously sent WhatsApp message
    /// </summary>
    /// <param name="messageId">Message ID from SendMessageAsync result</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>WhatsApp result with current status</returns>
    Task<WhatsAppResult> GetStatusAsync(string messageId, CancellationToken cancellationToken = default);
}

