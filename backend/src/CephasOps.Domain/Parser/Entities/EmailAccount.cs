using CephasOps.Domain.Common;

namespace CephasOps.Domain.Parser.Entities;

/// <summary>
/// Email account / mailbox configuration used for email ingestion and (optionally) SMTP sending.
/// </summary>
public class EmailAccount : CompanyScopedEntity
{
    // Incoming mail (POP3 / IMAP / O365 etc.)
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = "POP3"; // POP3, IMAP, O365, Gmail, etc.
    public string? Host { get; set; }
    public int? Port { get; set; }
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for incoming mail. Per product request this is stored directly
    /// on the entity instead of in CompanySetting.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    public int PollIntervalSec { get; set; } = 60;
    public bool IsActive { get; set; } = true;
    public DateTime? LastPolledAt { get; set; }

    /// <summary>
    /// Default department to assign to orders parsed from this mailbox
    /// </summary>
    public Guid? DefaultDepartmentId { get; set; }

    /// <summary>
    /// Default parser template to fall back to when no rule matches.
    /// </summary>
    public Guid? DefaultParserTemplateId { get; set; }

    public ParserTemplate? DefaultParserTemplate { get; set; }

    // Outgoing mail (SMTP)
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpUseSsl { get; set; }
    public bool SmtpUseTls { get; set; }
    public string? SmtpFromAddress { get; set; }
    public string? SmtpFromName { get; set; }
}


