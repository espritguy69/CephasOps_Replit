using CephasOps.Domain.Common;

namespace CephasOps.Domain.Departments.Entities;

/// <summary>
/// Department entity
/// </summary>
public class Department : CompanyScopedEntity
{
    /// <summary>
    /// Department name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Department code
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Cost centre ID
    /// </summary>
    public Guid? CostCentreId { get; set; }

    /// <summary>
    /// Whether this department is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<MaterialAllocation> MaterialAllocations { get; set; } = new List<MaterialAllocation>();
    public ICollection<DepartmentMembership> Memberships { get; set; } = new List<DepartmentMembership>();
}

