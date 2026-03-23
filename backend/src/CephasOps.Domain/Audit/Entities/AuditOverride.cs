using CephasOps.Domain.Common;

namespace CephasOps.Domain.Audit.Entities;

/// <summary>
/// AuditOverride entity - tracks HOD/SuperAdmin override actions for audit trail.
/// Per ORDER_LIFECYCLE.md: All override actions must be logged with evidence.
/// </summary>
public class AuditOverride : CompanyScopedEntity
{
    /// <summary>
    /// Entity type being overridden (e.g., "Order", "OrderBlocker", "SplitterPort", "Payroll")
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Entity ID being overridden
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Type of override action (e.g., "StatusChange", "BlockerOverride", "StandbyPortApproval", "RescheduleApproval")
    /// </summary>
    public string OverrideType { get; set; } = string.Empty;

    /// <summary>
    /// Original value before override (JSON or string)
    /// </summary>
    public string? OriginalValue { get; set; }

    /// <summary>
    /// New value after override (JSON or string)
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Reason for the override (required)
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Evidence attachment ID (required for most overrides)
    /// </summary>
    public Guid? EvidenceAttachmentId { get; set; }

    /// <summary>
    /// Additional evidence notes
    /// </summary>
    public string? EvidenceNotes { get; set; }

    /// <summary>
    /// User who performed the override
    /// </summary>
    public Guid OverriddenByUserId { get; set; }

    /// <summary>
    /// User's role at time of override (e.g., "HOD", "SuperAdmin", "Manager")
    /// </summary>
    public string OverriddenByRole { get; set; } = string.Empty;

    /// <summary>
    /// When the override was performed
    /// </summary>
    public DateTime OverriddenAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the override was approved by a secondary approver (for high-risk actions)
    /// </summary>
    public bool RequiredSecondaryApproval { get; set; }

    /// <summary>
    /// Secondary approver user ID (if required)
    /// </summary>
    public Guid? SecondaryApproverUserId { get; set; }

    /// <summary>
    /// When secondary approval was given
    /// </summary>
    public DateTime? SecondaryApprovedAt { get; set; }
}

