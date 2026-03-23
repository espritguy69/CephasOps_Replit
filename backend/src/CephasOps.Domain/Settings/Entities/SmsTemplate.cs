using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// SMS template entity - reusable SMS message templates with placeholders
/// </summary>
public class SmsTemplate : CompanyScopedEntity
{
    /// <summary>
    /// Template code (e.g., "APPT_CONFIRM", "OTW", "COMPLETED")
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
    /// Message text with placeholders (e.g., "Hi {customerName}, your appointment is on {appointmentDate}")
    /// </summary>
    public string MessageText { get; set; } = string.Empty;

    /// <summary>
    /// Character count (calculated from MessageText)
    /// </summary>
    public int CharCount { get; set; }

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
}

