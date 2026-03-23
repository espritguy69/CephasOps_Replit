using CephasOps.Domain.Common;

namespace CephasOps.Domain.Parser.Entities;

/// <summary>
/// Parse session entity - represents one parsing attempt
/// </summary>
public class ParseSession : CompanyScopedEntity
{
    public Guid? EmailMessageId { get; set; }
    public Guid? ParserTemplateId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed
    public string? ErrorMessage { get; set; }
    public Guid? SnapshotFileId { get; set; }
    public int ParsedOrdersCount { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Source type: Email, FileUpload
    /// </summary>
    public string? SourceType { get; set; }
    
    /// <summary>
    /// Description of the source (e.g., file names, email subject)
    /// </summary>
    public string? SourceDescription { get; set; }
}

