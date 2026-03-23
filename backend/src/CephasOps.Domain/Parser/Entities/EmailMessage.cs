using CephasOps.Domain.Common;

namespace CephasOps.Domain.Parser.Entities;

/// <summary>
/// Email message entity - represents both ingested (inbound) and sent (outbound) emails
/// </summary>
public class EmailMessage : CompanyScopedEntity
{
    public Guid EmailAccountId { get; set; }
    public string MessageId { get; set; } = string.Empty; // Provider message-id
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddresses { get; set; } = string.Empty;
    public string? CcAddresses { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? BodyPreview { get; set; }
    /// <summary>
    /// Full email body as plain text (for viewing and parsing)
    /// </summary>
    public string? BodyText { get; set; }
    /// <summary>
    /// Full email body as HTML (for viewing with formatting)
    /// </summary>
    public string? BodyHtml { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? SentAt { get; set; } // For outbound emails
    public string Direction { get; set; } = "Inbound"; // Inbound, Outbound
    public string? RawStoragePath { get; set; }
    public bool HasAttachments { get; set; }
    public string ParserStatus { get; set; } = "Pending"; // Pending, Parsed, Error, Ignored
    public string? ParserError { get; set; }

    // VIP fields
    /// <summary>
    /// Whether this email is from a VIP sender
    /// </summary>
    public bool IsVip { get; set; }

    /// <summary>
    /// The ID of the rule that matched this email (if any)
    /// </summary>
    public Guid? MatchedRuleId { get; set; }

    /// <summary>
    /// The ID of the VIP email entry that matched (if any)
    /// </summary>
    public Guid? MatchedVipEmailId { get; set; }

    /// <summary>
    /// Department this email message belongs to (inherited from EmailAccount or VIP rule)
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// When this email expires (48 hours from CreatedAt/ReceivedAt)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether this email has expired (computed property, not stored)
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Navigation property for attachments
    /// </summary>
    public ICollection<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
}

