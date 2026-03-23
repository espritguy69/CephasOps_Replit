using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// Escalation Rule entity - defines auto-escalation based on time, status, or conditions
/// </summary>
public class EscalationRule : CompanyScopedEntity
{
    /// <summary>
    /// Rule name (e.g. "Escalate Blocked Orders", "Escalate VIP Orders")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Entity type this rule applies to (e.g., "Order", "Task", "Invoice")
    /// </summary>
    public string EntityType { get; set; } = "Order";

    /// <summary>
    /// Partner ID (nullable) - if set, applies only to this partner
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// Department ID (nullable) - if set, applies only to this department
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Order type filter (nullable)
    /// </summary>
    public string? OrderType { get; set; }

    // ============================================
    // Trigger Conditions
    // ============================================

    /// <summary>
    /// Trigger type: TimeBased, StatusBased, ConditionBased, EventBased
    /// </summary>
    public string TriggerType { get; set; } = string.Empty;

    /// <summary>
    /// Status to trigger on (for StatusBased trigger)
    /// </summary>
    public string? TriggerStatus { get; set; }

    /// <summary>
    /// Time-based trigger delay in minutes (for TimeBased trigger)
    /// </summary>
    public int? TriggerDelayMinutes { get; set; }

    /// <summary>
    /// Additional trigger conditions as JSON (e.g., {"isVip": true, "priority": "High"})
    /// </summary>
    public string? TriggerConditionsJson { get; set; }

    // ============================================
    // Escalation Action
    // ============================================

    /// <summary>
    /// Escalation type: NotifyUser, NotifyRole, AssignToUser, AssignToRole, ChangeStatus, CreateTask
    /// </summary>
    public string EscalationType { get; set; } = string.Empty;

    /// <summary>
    /// Target user ID (for NotifyUser, AssignToUser)
    /// </summary>
    public Guid? TargetUserId { get; set; }

    /// <summary>
    /// Target role (for NotifyRole, AssignToRole)
    /// </summary>
    public string? TargetRole { get; set; }

    /// <summary>
    /// Target team ID (for AssignToTeam)
    /// </summary>
    public Guid? TargetTeamId { get; set; }

    /// <summary>
    /// Target status (for ChangeStatus)
    /// </summary>
    public string? TargetStatus { get; set; }

    /// <summary>
    /// Notification template ID (for NotifyUser, NotifyRole)
    /// </summary>
    public Guid? NotificationTemplateId { get; set; }

    /// <summary>
    /// Escalation message/notes
    /// </summary>
    public string? EscalationMessage { get; set; }

    // ============================================
    // Escalation Chain
    // ============================================

    /// <summary>
    /// Whether to continue escalating if no response (escalation chain)
    /// </summary>
    public bool ContinueEscalation { get; set; } = false;

    /// <summary>
    /// Next escalation rule ID (for escalation chain)
    /// </summary>
    public Guid? NextEscalationRuleId { get; set; }

    /// <summary>
    /// Delay before next escalation in minutes
    /// </summary>
    public int? NextEscalationDelayMinutes { get; set; }

    // ============================================
    // Status & Metadata
    // ============================================

    /// <summary>
    /// Priority for rule evaluation (higher priority rules are evaluated first)
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Whether this rule is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether to stop evaluating other rules after this one matches
    /// </summary>
    public bool StopOnMatch { get; set; } = false;

    /// <summary>
    /// Effective from date
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// Effective to date (nullable for ongoing rules)
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// User ID who created this rule
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// User ID who last updated this rule
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }
}

