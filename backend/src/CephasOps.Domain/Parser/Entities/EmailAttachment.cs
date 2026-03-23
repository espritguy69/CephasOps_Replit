using CephasOps.Domain.Common;

namespace CephasOps.Domain.Parser.Entities;

/// <summary>
/// Email attachment entity - stores metadata for email attachments
/// </summary>
public class EmailAttachment : CompanyScopedEntity
{
    public Guid EmailMessageId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    
    /// <summary>
    /// Storage path/key for the attachment blob
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;
    
    /// <summary>
    /// File entity ID if stored via FileService
    /// </summary>
    public Guid? FileId { get; set; }
    
    /// <summary>
    /// Whether this is an inline/CID image attachment
    /// </summary>
    public bool IsInline { get; set; }
    
    /// <summary>
    /// Content-ID for inline images (CID:xxx)
    /// </summary>
    public string? ContentId { get; set; }
    
    /// <summary>
    /// When this attachment expires (same as parent email)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Navigation property to parent email
    /// </summary>
    public EmailMessage EmailMessage { get; set; } = null!;
}

