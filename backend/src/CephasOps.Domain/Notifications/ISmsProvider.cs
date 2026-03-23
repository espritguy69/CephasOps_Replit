namespace CephasOps.Domain.Notifications;

/// <summary>
/// SMS provider interface for sending SMS messages
/// </summary>
public interface ISmsProvider
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
    /// Get status of a previously sent SMS message
    /// </summary>
    /// <param name="messageId">Message ID from SendSmsAsync result</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SMS result with current status</returns>
    Task<SmsResult> GetStatusAsync(string messageId, CancellationToken cancellationToken = default);
}

