using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// SLA Profile entity - defines Service Level Agreement rules for response and resolution times
/// </summary>
public class SlaProfile : CompanyScopedEntity
{
    /// <summary>
    /// Profile name (e.g. "TIME Activation SLA", "VIP Orders SLA")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Partner ID (nullable) - if set, applies only to this partner
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// Order type (Activation, Assurance, Modification, etc.)
    /// </summary>
    public string OrderType { get; set; } = string.Empty;

    /// <summary>
    /// Department ID (nullable) - if set, applies only to this department
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Whether this applies to VIP orders only
    /// </summary>
    public bool IsVipOnly { get; set; }

    // ============================================
    // Response SLA (Time to first action)
    // ============================================

    /// <summary>
    /// Response SLA in minutes (e.g., from Pending → Assigned)
    /// Null means no response SLA requirement
    /// </summary>
    public int? ResponseSlaMinutes { get; set; }

    /// <summary>
    /// Response SLA status transition (e.g., "Pending" → "Assigned")
    /// </summary>
    public string? ResponseSlaFromStatus { get; set; }

    /// <summary>
    /// Response SLA target status (e.g., "Assigned")
    /// </summary>
    public string? ResponseSlaToStatus { get; set; }

    // ============================================
    // Resolution SLA (Time to completion)
    // ============================================

    /// <summary>
    /// Resolution SLA in minutes (e.g., from Assigned → OrderCompleted)
    /// Null means no resolution SLA requirement
    /// </summary>
    public int? ResolutionSlaMinutes { get; set; }

    /// <summary>
    /// Resolution SLA status transition (e.g., "Assigned" → "OrderCompleted")
    /// </summary>
    public string? ResolutionSlaFromStatus { get; set; }

    /// <summary>
    /// Resolution SLA target status (e.g., "OrderCompleted")
    /// </summary>
    public string? ResolutionSlaToStatus { get; set; }

    // ============================================
    // Escalation Rules
    // ============================================

    /// <summary>
    /// Escalate at X% of SLA (e.g., 80 = escalate when 80% of SLA time has passed)
    /// </summary>
    public int? EscalationThresholdPercent { get; set; }

    /// <summary>
    /// Role to escalate to (e.g., "Manager", "HOD")
    /// </summary>
    public string? EscalationRole { get; set; }

    /// <summary>
    /// User ID to escalate to (nullable, overrides EscalationRole if set)
    /// </summary>
    public Guid? EscalationUserId { get; set; }

    /// <summary>
    /// Whether to send notification on escalation
    /// </summary>
    public bool NotifyOnEscalation { get; set; } = true;

    /// <summary>
    /// Whether to send notification on SLA breach
    /// </summary>
    public bool NotifyOnBreach { get; set; } = true;

    // ============================================
    // Business Hours
    // ============================================

    /// <summary>
    /// Whether SLA calculation should exclude non-business hours
    /// </summary>
    public bool ExcludeNonBusinessHours { get; set; } = true;

    /// <summary>
    /// Whether SLA calculation should exclude weekends
    /// </summary>
    public bool ExcludeWeekends { get; set; } = true;

    /// <summary>
    /// Whether SLA calculation should exclude public holidays
    /// </summary>
    public bool ExcludePublicHolidays { get; set; } = true;

    // ============================================
    // Status & Metadata
    // ============================================

    /// <summary>
    /// Whether this is the default profile for (CompanyId, OrderType)
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Whether this profile is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Effective from date
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// Effective to date (nullable for ongoing profiles)
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// User ID who created this profile
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// User ID who last updated this profile
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }
}

