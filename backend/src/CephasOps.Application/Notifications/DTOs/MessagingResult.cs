using CephasOps.Domain.Notifications;

namespace CephasOps.Application.Notifications.DTOs;

/// <summary>
/// Result of unified messaging operation (SMS + WhatsApp)
/// </summary>
public class MessagingResult
{
    /// <summary>
    /// Whether SMS was sent successfully
    /// </summary>
    public bool SmsSent { get; set; }

    /// <summary>
    /// SMS result details (if sent)
    /// </summary>
    public SmsResult? SmsResult { get; set; }

    /// <summary>
    /// Whether WhatsApp was sent successfully
    /// </summary>
    public bool WhatsAppSent { get; set; }

    /// <summary>
    /// WhatsApp result details (if sent)
    /// </summary>
    public WhatsAppResult? WhatsAppResult { get; set; }

    /// <summary>
    /// Overall success (at least one channel succeeded)
    /// </summary>
    public bool Success => SmsSent || WhatsAppSent;

    /// <summary>
    /// Error message if both channels failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Create successful result
    /// </summary>
    public static MessagingResult SuccessResult(bool smsSent, bool whatsAppSent, SmsResult? smsResult = null, WhatsAppResult? whatsAppResult = null)
    {
        return new MessagingResult
        {
            SmsSent = smsSent,
            SmsResult = smsResult,
            WhatsAppSent = whatsAppSent,
            WhatsAppResult = whatsAppResult
        };
    }

    /// <summary>
    /// Create failed result
    /// </summary>
    public static MessagingResult FailedResult(string errorMessage)
    {
        return new MessagingResult
        {
            SmsSent = false,
            WhatsAppSent = false,
            ErrorMessage = errorMessage
        };
    }
}

