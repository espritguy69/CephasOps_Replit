namespace CephasOps.Application.Settings.DTOs;

/// <summary>
/// DTO for material template
/// </summary>
public class MaterialTemplateDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public Guid? InstallationMethodId { get; set; }
    public string? InstallationMethodName { get; set; }
    public Guid? BuildingTypeId { get; set; } // Legacy
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public List<MaterialTemplateItemDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for material template item
/// </summary>
public class MaterialTemplateItemDto
{
    public Guid Id { get; set; }
    public Guid MaterialTemplateId { get; set; }
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public bool IsSerialised { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for creating material template
/// </summary>
public class CreateMaterialTemplateDto
{
    public Guid? DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public Guid? InstallationMethodId { get; set; }
    public Guid? BuildingTypeId { get; set; } // Legacy
    public Guid? PartnerId { get; set; }
    public bool IsDefault { get; set; }
    public List<CreateMaterialTemplateItemDto> Items { get; set; } = new();
}

/// <summary>
/// DTO for creating material template item
/// </summary>
public class CreateMaterialTemplateItemDto
{
    public Guid MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for updating material template
/// </summary>
public class UpdateMaterialTemplateDto
{
    public Guid? DepartmentId { get; set; }
    public string? Name { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsActive { get; set; }
    public List<CreateMaterialTemplateItemDto>? Items { get; set; }
}

/// <summary>
/// DTO for cloning material templates
/// </summary>
public class CloneMaterialTemplateDto
{
    public string NewName { get; set; } = string.Empty;
}
