namespace CephasOps.Application.Workflow.DTOs;

/// <summary>
/// DTO for SideEffectDefinition
/// </summary>
public class SideEffectDefinitionDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string ExecutorType { get; set; } = string.Empty;
    public string? ExecutorConfigJson { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating a SideEffectDefinition
/// </summary>
public class CreateSideEffectDefinitionDto
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string ExecutorType { get; set; } = string.Empty;
    public string? ExecutorConfigJson { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// DTO for updating a SideEffectDefinition
/// </summary>
public class UpdateSideEffectDefinitionDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ExecutorType { get; set; }
    public string? ExecutorConfigJson { get; set; }
    public bool? IsActive { get; set; }
    public int? DisplayOrder { get; set; }
}

