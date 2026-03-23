using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// WhatsApp template entity - WhatsApp Business API approved templates
/// </summary>
public class WhatsAppTemplate : CompanyScopedEntity
{
    /// <summary>
    /// Template code (e.g., "WA_APPT", "WA_OTW", "WA_COMPLETE")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Template display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category for grouping (Appointments, Jobs, Finance, etc.)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// WhatsApp Business API template ID (from WhatsApp approval)
    /// </summary>
    public string? TemplateId { get; set; }

    /// <summary>
    /// Approval status: Approved, Pending, Rejected
    /// </summary>
    public string ApprovalStatus { get; set; } = "Pending";

    /// <summary>
    /// Template message body (for reference, actual message comes from WhatsApp)
    /// </summary>
    public string? MessageBody { get; set; }

    /// <summary>
    /// Template language code (e.g., "en", "ms")
    /// </summary>
    public string? Language { get; set; } = "en";

    /// <summary>
    /// Whether this template is active and can be used
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User ID who created this template
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// User ID who last updated this template
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }

    /// <summary>
    /// Notes or usage instructions
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Date when template was submitted for approval
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// Date when template was approved by WhatsApp
    /// </summary>
    public DateTime? ApprovedAt { get; set; }
}

