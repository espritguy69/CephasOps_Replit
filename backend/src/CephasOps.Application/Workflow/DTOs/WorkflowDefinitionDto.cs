namespace CephasOps.Application.Workflow.DTOs;

/// <summary>
/// DTO for WorkflowDefinition
/// </summary>
public class WorkflowDefinitionDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Name { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? OrderTypeCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public List<WorkflowTransitionDto> Transitions { get; set; } = new();
}

/// <summary>
/// DTO for creating a new WorkflowDefinition
/// </summary>
public class CreateWorkflowDefinitionDto
{
    public string Name { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? PartnerId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? OrderTypeCode { get; set; }
}

/// <summary>
/// DTO for updating an existing WorkflowDefinition
/// </summary>
public class UpdateWorkflowDefinitionDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? OrderTypeCode { get; set; }
}

