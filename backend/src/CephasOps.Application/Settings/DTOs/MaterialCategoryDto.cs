namespace CephasOps.Application.Settings.DTOs;

public class MaterialCategoryDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string? ParentCategoryName { get; set; }
    public int MaterialCount { get; set; }
    public bool IsSerialised { get; set; }
    public bool IsActive { get; set; }
}

