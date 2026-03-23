namespace CephasOps.Application.Buildings.DTOs;

/// <summary>
/// SplitterType DTO
/// </summary>
public class SplitterTypeDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public Guid? DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int TotalPorts { get; set; }
    public int? StandbyPortNumber { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Create SplitterType request DTO
/// </summary>
public class CreateSplitterTypeDto
{
    public Guid? DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int TotalPorts { get; set; }
    public int? StandbyPortNumber { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Update SplitterType request DTO
/// </summary>
public class UpdateSplitterTypeDto
{
    public Guid? DepartmentId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public int? TotalPorts { get; set; }
    public int? StandbyPortNumber { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public int? DisplayOrder { get; set; }
}

