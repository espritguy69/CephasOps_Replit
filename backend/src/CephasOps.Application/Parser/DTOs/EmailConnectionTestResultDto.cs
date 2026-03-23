namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// Detailed result for testing an email mailbox connection (incoming + SMTP).
/// </summary>
public class EmailConnectionTestResultDto
{
    /// <summary>
    /// Overall success (incoming AND SMTP successful).
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// High-level message, e.g. "Connection test successful!".
    /// </summary>
    public string Message { get; set; } = string.Empty;

    // Incoming (POP3 / IMAP) details
    public bool IncomingSuccess { get; set; }
    public string IncomingProtocol { get; set; } = string.Empty; // POP3 / IMAP
    public long IncomingResponseTimeMs { get; set; }
    public string? IncomingError { get; set; }

    // Outgoing (SMTP) details
    public bool SmtpSuccess { get; set; }
    public long SmtpResponseTimeMs { get; set; }
    public string? SmtpError { get; set; }
}


