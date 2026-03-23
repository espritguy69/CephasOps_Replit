namespace CephasOps.Domain.Notifications;

/// <summary>
/// Result of WhatsApp message sending operation
/// </summary>
public class WhatsAppResult
{
    /// <summary>
    /// Whether the WhatsApp message was sent successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message ID from provider (for tracking status)
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Status of the message (e.g., "sent", "delivered", "read", "failed")
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Error message if sending failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Provider-specific error code (if any)
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Timestamp when the message was sent
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Create a successful result
    /// </summary>
    public static WhatsAppResult SuccessResult(string messageId, string status = "sent", DateTime? sentAt = null)
    {
        return new WhatsAppResult
        {
            Success = true,
            MessageId = messageId,
            Status = status,
            SentAt = sentAt ?? DateTime.UtcNow
        };
    }

    /// <summary>
    /// Create a failed result
    /// </summary>
    public static WhatsAppResult FailedResult(string errorMessage, string? errorCode = null)
    {
        return new WhatsAppResult
        {
            Success = false,
            Status = "failed",
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

