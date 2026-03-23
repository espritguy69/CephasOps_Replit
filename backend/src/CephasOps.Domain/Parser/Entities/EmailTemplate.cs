using CephasOps.Domain.Common;

namespace CephasOps.Domain.Parser.Entities;

/// <summary>
/// Email template entity - defines email templates for sending communications (reschedules, approvals, etc.)
/// Similar structure to ParserTemplate for consistency
/// </summary>
public class EmailTemplate : CompanyScopedEntity
{
    /// <summary>
    /// Template name (e.g., "Same Day Time Change Request", "Date Change Approval Request")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique code for the template (e.g., "RESCHEDULE_TIME_ONLY", "RESCHEDULE_DATE_TIME", "ASSURANCE_CABLE_REPULL")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Email account/mailbox to send from (optional).
    /// If null, uses default email account with SMTP configured.
    /// </summary>
    public Guid? EmailAccountId { get; set; }

    /// <summary>
    /// Email subject template with placeholders (e.g., "Reschedule Request - Order {OrderNumber}")
    /// </summary>
    public string SubjectTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Email body template with placeholders (HTML supported)
    /// Placeholders: {CustomerName}, {OrderNumber}, {OldDate}, {NewDate}, {OldTime}, {NewTime}, {Reason}, {Address}, etc.
    /// </summary>
    public string BodyTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Department this template belongs to (for routing and access control)
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Entity type this template is used for (e.g., "Order", "Reschedule", "Assurance")
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// Priority for template selection (higher = selected first if multiple templates match)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether this template is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// If true, automatically process replies matching this template (e.g., auto-approve if reply contains "Approved")
    /// If false, requires manual review of replies
    /// </summary>
    public bool AutoProcessReplies { get; set; } = false;

    /// <summary>
    /// Pattern to match in reply subject/body for auto-processing (e.g., "Approved", "Confirmed", "Rejected")
    /// </summary>
    public string? ReplyPattern { get; set; }

    /// <summary>
    /// Description of what this template is used for
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Direction of email template: "Incoming" (for parsing received emails) or "Outgoing" (for sending emails)
    /// Defaults to "Outgoing" since EmailTemplate is primarily for sending
    /// </summary>
    public string Direction { get; set; } = "Outgoing";

    /// <summary>
    /// User who created this template
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// User who last updated this template
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }
}

