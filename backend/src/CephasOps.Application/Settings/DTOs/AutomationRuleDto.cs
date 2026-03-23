namespace CephasOps.Application.Settings.DTOs;

/// <summary>
/// DTO for Automation Rule
/// </summary>
public class AutomationRuleDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RuleType { get; set; } = string.Empty;
    public string EntityType { get; set; } = "Order";
    public Guid? PartnerId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? OrderType { get; set; }

    // Trigger
    public string TriggerType { get; set; } = string.Empty;
    public string? TriggerConditionsJson { get; set; }
    public string? TriggerStatus { get; set; }
    public int? TriggerDelayMinutes { get; set; }

    // Action
    public string ActionType { get; set; } = string.Empty;
    public string? ActionConfigJson { get; set; }
    public Guid? TargetUserId { get; set; }
    public string? TargetRole { get; set; }
    public Guid? TargetTeamId { get; set; }
    public string? TargetStatus { get; set; }
    public Guid? NotificationTemplateId { get; set; }

    // Conditions
    public string? ConditionsJson { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; } = true;
    public bool StopOnMatch { get; set; } = false;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating Automation Rule
/// </summary>
public class CreateAutomationRuleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RuleType { get; set; } = string.Empty;
    public string EntityType { get; set; } = "Order";
    public Guid? PartnerId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? OrderType { get; set; }

    // Trigger
    public string TriggerType { get; set; } = string.Empty;
    public string? TriggerConditionsJson { get; set; }
    public string? TriggerStatus { get; set; }
    public int? TriggerDelayMinutes { get; set; }

    // Action
    public string ActionType { get; set; } = string.Empty;
    public string? ActionConfigJson { get; set; }
    public Guid? TargetUserId { get; set; }
    public string? TargetRole { get; set; }
    public Guid? TargetTeamId { get; set; }
    public string? TargetStatus { get; set; }
    public Guid? NotificationTemplateId { get; set; }

    // Conditions
    public string? ConditionsJson { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; } = true;
    public bool StopOnMatch { get; set; } = false;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

/// <summary>
/// DTO for updating Automation Rule
/// </summary>
public class UpdateAutomationRuleDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? TriggerType { get; set; }
    public string? TriggerConditionsJson { get; set; }
    public string? TriggerStatus { get; set; }
    public int? TriggerDelayMinutes { get; set; }
    public string? ActionType { get; set; }
    public string? ActionConfigJson { get; set; }
    public Guid? TargetUserId { get; set; }
    public string? TargetRole { get; set; }
    public Guid? TargetTeamId { get; set; }
    public string? TargetStatus { get; set; }
    public Guid? NotificationTemplateId { get; set; }
    public string? ConditionsJson { get; set; }
    public int? Priority { get; set; }
    public bool? IsActive { get; set; }
    public bool? StopOnMatch { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

