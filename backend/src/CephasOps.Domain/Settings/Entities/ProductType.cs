using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

public class ProductType : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool RequiresInstallation { get; set; } = true;
    public int PlanCount { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

