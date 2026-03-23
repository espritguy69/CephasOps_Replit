namespace CephasOps.Domain.Notifications;

/// <summary>
/// Result of SMS sending operation
/// </summary>
public class SmsResult
{
    /// <summary>
    /// Whether the SMS was sent successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message ID from provider (for tracking status)
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Status of the message (e.g., "sent", "delivered", "failed")
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
    public static SmsResult SuccessResult(string messageId, string status = "sent", DateTime? sentAt = null)
    {
        return new SmsResult
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
    public static SmsResult FailedResult(string errorMessage, string? errorCode = null)
    {
        return new SmsResult
        {
            Success = false,
            Status = "failed",
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

