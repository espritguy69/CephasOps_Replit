namespace CephasOps.Application.Departments.DTOs;

/// <summary>
/// Department DTO
/// </summary>
public class DepartmentDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public Guid? CostCentreId { get; set; }
    public string? CostCentreName { get; set; }
    public bool IsActive { get; set; }
    public List<MaterialAllocationDto> MaterialAllocations { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Material allocation DTO
/// </summary>
public class MaterialAllocationDto
{
    public Guid Id { get; set; }
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Create department request DTO
/// </summary>
public class CreateDepartmentDto
{
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public Guid? CostCentreId { get; set; }
}

/// <summary>
/// Update department request DTO
/// </summary>
public class UpdateDepartmentDto
{
    public Guid? CompanyId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public Guid? CostCentreId { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Create material allocation request DTO
/// </summary>
public class CreateMaterialAllocationDto
{
    public Guid MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
}

