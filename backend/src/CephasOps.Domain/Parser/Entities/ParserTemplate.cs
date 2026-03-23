using CephasOps.Domain.Common;

namespace CephasOps.Domain.Parser.Entities;

/// <summary>
/// Parser template entity - defines how to parse emails from specific partners
/// </summary>
public class ParserTemplate : CompanyScopedEntity
{
    /// <summary>
    /// Template name (e.g., "TIME FTTH Activation")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique code for the template (e.g., "TIME_FTTH", "DIGI_HSBB")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Email account/mailbox this template is restricted to (optional).
    /// If null, template applies to all mailboxes.
    /// If set, template only processes emails from this specific mailbox.
    /// </summary>
    public Guid? EmailAccountId { get; set; }

    /// <summary>
    /// Pattern to match FROM address (e.g., "*@time.com.my", "noreply@*")
    /// Supports wildcards: * (any chars), ? (single char)
    /// </summary>
    public string? PartnerPattern { get; set; }

    /// <summary>
    /// Pattern to match subject keywords (e.g., "FTTH", "HSBB", "Modification")
    /// </summary>
    public string? SubjectPattern { get; set; }

    /// <summary>
    /// Order type ID this parser creates (maps to OrderType entity)
    /// </summary>
    public Guid? OrderTypeId { get; set; }

    /// <summary>
    /// Order type code (e.g., "ACTIVATION", "MODIFICATION_INDOOR") - fallback if OrderTypeId not set
    /// </summary>
    public string? OrderTypeCode { get; set; }

    /// <summary>
    /// If true, parser will automatically create orders without human review (when all required fields are valid)
    /// If false, parsed orders go to Parser Review Queue for human approval
    /// </summary>
    public bool AutoApprove { get; set; } = false;

    /// <summary>
    /// Priority for evaluation order (higher = evaluated first)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether this template is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Description of what this parser handles
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Partner ID this template is associated with (optional)
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// Department to assign parsed orders to
    /// </summary>
    public Guid? DefaultDepartmentId { get; set; }

    /// <summary>
    /// Expected attachment types (e.g., ".xls,.xlsx,.pdf")
    /// </summary>
    public string? ExpectedAttachmentTypes { get; set; }

    /// <summary>
    /// User who created this template
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// User who last updated this template
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }
}

