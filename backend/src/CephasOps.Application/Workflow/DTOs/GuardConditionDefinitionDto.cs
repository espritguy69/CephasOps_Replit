namespace CephasOps.Application.Workflow.DTOs;

/// <summary>
/// DTO for GuardConditionDefinition
/// </summary>
public class GuardConditionDefinitionDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string ValidatorType { get; set; } = string.Empty;
    public string? ValidatorConfigJson { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating a GuardConditionDefinition
/// </summary>
public class CreateGuardConditionDefinitionDto
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string ValidatorType { get; set; } = string.Empty;
    public string? ValidatorConfigJson { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// DTO for updating a GuardConditionDefinition
/// </summary>
public class UpdateGuardConditionDefinitionDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ValidatorType { get; set; }
    public string? ValidatorConfigJson { get; set; }
    public bool? IsActive { get; set; }
    public int? DisplayOrder { get; set; }
}

