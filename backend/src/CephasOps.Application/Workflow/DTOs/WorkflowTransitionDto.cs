namespace CephasOps.Application.Workflow.DTOs;

/// <summary>
/// DTO for WorkflowTransition
/// </summary>
public class WorkflowTransitionDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public Guid WorkflowDefinitionId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public List<string> AllowedRoles { get; set; } = new();
    public Dictionary<string, object>? GuardConditions { get; set; }
    public Dictionary<string, object>? SideEffectsConfig { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}

/// <summary>
/// DTO for creating a new WorkflowTransition
/// </summary>
public class CreateWorkflowTransitionDto
{
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public List<string> AllowedRoles { get; set; } = new();
    public Dictionary<string, object>? GuardConditions { get; set; }
    public Dictionary<string, object>? SideEffectsConfig { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for updating an existing WorkflowTransition
/// </summary>
public class UpdateWorkflowTransitionDto
{
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
    public List<string>? AllowedRoles { get; set; }
    public Dictionary<string, object>? GuardConditions { get; set; }
    public Dictionary<string, object>? SideEffectsConfig { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsActive { get; set; }
}

