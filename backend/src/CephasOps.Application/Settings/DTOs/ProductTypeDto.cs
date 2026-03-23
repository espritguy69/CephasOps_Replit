namespace CephasOps.Application.Settings.DTOs;

public class ProductTypeDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool RequiresInstallation { get; set; }
    public int PlanCount { get; set; }
    public bool IsActive { get; set; }
}

