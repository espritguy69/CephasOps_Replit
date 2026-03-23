using CephasOps.Domain.Common;

namespace CephasOps.Domain.Departments.Entities;

/// <summary>
/// Material allocation entity (materials allocated to departments)
/// </summary>
public class MaterialAllocation : CompanyScopedEntity
{
    /// <summary>
    /// Department ID
    /// </summary>
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// Material ID
    /// </summary>
    public Guid MaterialId { get; set; }

    /// <summary>
    /// Allocated quantity
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public Department? Department { get; set; }
}

