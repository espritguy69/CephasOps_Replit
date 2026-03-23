namespace CephasOps.Application.Settings.DTOs;

/// <summary>
/// DTO for Escalation Rule
/// </summary>
public class EscalationRuleDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EntityType { get; set; } = "Order";
    public Guid? PartnerId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? OrderType { get; set; }

    // Trigger
    public string TriggerType { get; set; } = string.Empty;
    public string? TriggerStatus { get; set; }
    public int? TriggerDelayMinutes { get; set; }
    public string? TriggerConditionsJson { get; set; }

    // Escalation
    public string EscalationType { get; set; } = string.Empty;
    public Guid? TargetUserId { get; set; }
    public string? TargetRole { get; set; }
    public Guid? TargetTeamId { get; set; }
    public string? TargetStatus { get; set; }
    public Guid? NotificationTemplateId { get; set; }
    public string? EscalationMessage { get; set; }

    // Escalation Chain
    public bool ContinueEscalation { get; set; } = false;
    public Guid? NextEscalationRuleId { get; set; }
    public int? NextEscalationDelayMinutes { get; set; }

    // Status
    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; } = true;
    public bool StopOnMatch { get; set; } = false;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating Escalation Rule
/// </summary>
public class CreateEscalationRuleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EntityType { get; set; } = "Order";
    public Guid? PartnerId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? OrderType { get; set; }
    public string TriggerType { get; set; } = string.Empty;
    public string? TriggerStatus { get; set; }
    public int? TriggerDelayMinutes { get; set; }
    public string? TriggerConditionsJson { get; set; }
    public string EscalationType { get; set; } = string.Empty;
    public Guid? TargetUserId { get; set; }
    public string? TargetRole { get; set; }
    public Guid? TargetTeamId { get; set; }
    public string? TargetStatus { get; set; }
    public Guid? NotificationTemplateId { get; set; }
    public string? EscalationMessage { get; set; }
    public bool ContinueEscalation { get; set; } = false;
    public Guid? NextEscalationRuleId { get; set; }
    public int? NextEscalationDelayMinutes { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; } = true;
    public bool StopOnMatch { get; set; } = false;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

/// <summary>
/// DTO for updating Escalation Rule
/// </summary>
public class UpdateEscalationRuleDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? TriggerType { get; set; }
    public string? TriggerStatus { get; set; }
    public int? TriggerDelayMinutes { get; set; }
    public string? TriggerConditionsJson { get; set; }
    public string? EscalationType { get; set; }
    public Guid? TargetUserId { get; set; }
    public string? TargetRole { get; set; }
    public Guid? TargetTeamId { get; set; }
    public string? TargetStatus { get; set; }
    public Guid? NotificationTemplateId { get; set; }
    public string? EscalationMessage { get; set; }
    public bool? ContinueEscalation { get; set; }
    public Guid? NextEscalationRuleId { get; set; }
    public int? NextEscalationDelayMinutes { get; set; }
    public int? Priority { get; set; }
    public bool? IsActive { get; set; }
    public bool? StopOnMatch { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

