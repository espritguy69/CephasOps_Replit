using CephasOps.Domain.Common;

namespace CephasOps.Domain.Buildings.Entities;

/// <summary>
/// BuildingType entity - represents building classifications
/// (Condominium, Office Tower, Terrace House, etc.)
/// This represents WHAT the building is, not how installations are performed.
/// </summary>
public class BuildingType : CompanyScopedEntity
{
    public Guid? DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty; // Condominium, Office Tower, Terrace House, etc.
    public string Code { get; set; } = string.Empty; // CONDO, OFFICE_TOWER, TERRACE, etc.
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

