namespace CephasOps.Application.Settings.DTOs;

/// <summary>
/// DTO for SLA profile
/// </summary>
public class SlaProfileDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? PartnerId { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public bool IsVipOnly { get; set; }

    // Response SLA
    public int? ResponseSlaMinutes { get; set; }
    public string? ResponseSlaFromStatus { get; set; }
    public string? ResponseSlaToStatus { get; set; }

    // Resolution SLA
    public int? ResolutionSlaMinutes { get; set; }
    public string? ResolutionSlaFromStatus { get; set; }
    public string? ResolutionSlaToStatus { get; set; }

    // Escalation
    public int? EscalationThresholdPercent { get; set; }
    public string? EscalationRole { get; set; }
    public Guid? EscalationUserId { get; set; }
    public bool NotifyOnEscalation { get; set; } = true;
    public bool NotifyOnBreach { get; set; } = true;

    // Business Hours
    public bool ExcludeNonBusinessHours { get; set; } = true;
    public bool ExcludeWeekends { get; set; } = true;
    public bool ExcludePublicHolidays { get; set; } = true;

    // Status
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating SLA profile
/// </summary>
public class CreateSlaProfileDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? PartnerId { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public bool IsVipOnly { get; set; }

    // Response SLA
    public int? ResponseSlaMinutes { get; set; }
    public string? ResponseSlaFromStatus { get; set; }
    public string? ResponseSlaToStatus { get; set; }

    // Resolution SLA
    public int? ResolutionSlaMinutes { get; set; }
    public string? ResolutionSlaFromStatus { get; set; }
    public string? ResolutionSlaToStatus { get; set; }

    // Escalation
    public int? EscalationThresholdPercent { get; set; }
    public string? EscalationRole { get; set; }
    public Guid? EscalationUserId { get; set; }
    public bool NotifyOnEscalation { get; set; } = true;
    public bool NotifyOnBreach { get; set; } = true;

    // Business Hours
    public bool ExcludeNonBusinessHours { get; set; } = true;
    public bool ExcludeWeekends { get; set; } = true;
    public bool ExcludePublicHolidays { get; set; } = true;

    // Status
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

/// <summary>
/// DTO for updating SLA profile
/// </summary>
public class UpdateSlaProfileDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? ResponseSlaMinutes { get; set; }
    public string? ResponseSlaFromStatus { get; set; }
    public string? ResponseSlaToStatus { get; set; }
    public int? ResolutionSlaMinutes { get; set; }
    public string? ResolutionSlaFromStatus { get; set; }
    public string? ResolutionSlaToStatus { get; set; }
    public int? EscalationThresholdPercent { get; set; }
    public string? EscalationRole { get; set; }
    public Guid? EscalationUserId { get; set; }
    public bool? NotifyOnEscalation { get; set; }
    public bool? NotifyOnBreach { get; set; }
    public bool? ExcludeNonBusinessHours { get; set; }
    public bool? ExcludeWeekends { get; set; }
    public bool? ExcludePublicHolidays { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

