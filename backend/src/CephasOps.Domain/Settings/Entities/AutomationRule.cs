using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// Automation Rule entity - defines automated actions based on triggers and conditions
/// </summary>
public class AutomationRule : CompanyScopedEntity
{
    /// <summary>
    /// Rule name (e.g. "Auto-Assign TIME Activation", "Escalate Blocked Orders")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Rule type: AutoAssignment, AutoEscalation, AutoNotification, AutoStatusChange
    /// </summary>
    public string RuleType { get; set; } = string.Empty;

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
    /// Order type filter (nullable) - if set, applies only to this order type
    /// </summary>
    public string? OrderType { get; set; }

    // ============================================
    // Trigger Conditions (JSON)
    // ============================================

    /// <summary>
    /// Trigger type: StatusChange, TimeBased, ConditionBased, EventBased
    /// </summary>
    public string TriggerType { get; set; } = string.Empty;

    /// <summary>
    /// Trigger conditions as JSON (e.g., {"status": "Blocker", "durationMinutes": 1440})
    /// </summary>
    public string? TriggerConditionsJson { get; set; }

    /// <summary>
    /// Status to trigger on (for StatusChange trigger)
    /// </summary>
    public string? TriggerStatus { get; set; }

    /// <summary>
    /// Time-based trigger delay in minutes (for TimeBased trigger)
    /// </summary>
    public int? TriggerDelayMinutes { get; set; }

    // ============================================
    // Action Configuration (JSON)
    // ============================================

    /// <summary>
    /// Action type: AssignToUser, AssignToRole, AssignToTeam, Escalate, Notify, ChangeStatus, CreateTask
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Action configuration as JSON (e.g., {"targetUserId": "...", "notificationTemplate": "..."})
    /// </summary>
    public string? ActionConfigJson { get; set; }

    /// <summary>
    /// Target user ID (for AssignToUser action)
    /// </summary>
    public Guid? TargetUserId { get; set; }

    /// <summary>
    /// Target role (for AssignToRole action)
    /// </summary>
    public string? TargetRole { get; set; }

    /// <summary>
    /// Target team ID (for AssignToTeam action)
    /// </summary>
    public Guid? TargetTeamId { get; set; }

    /// <summary>
    /// Target status (for ChangeStatus action)
    /// </summary>
    public string? TargetStatus { get; set; }

    /// <summary>
    /// Notification template ID (for Notify action)
    /// </summary>
    public Guid? NotificationTemplateId { get; set; }

    // ============================================
    // Additional Conditions
    // ============================================

    /// <summary>
    /// Additional conditions as JSON (e.g., {"isVip": true, "priority": "High"})
    /// </summary>
    public string? ConditionsJson { get; set; }

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

