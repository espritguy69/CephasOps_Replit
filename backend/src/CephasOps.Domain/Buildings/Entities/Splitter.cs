using CephasOps.Domain.Common;

namespace CephasOps.Domain.Buildings.Entities;

/// <summary>
/// Splitter entity - represents splitters in buildings
/// </summary>
public class Splitter : CompanyScopedEntity
{
    public Guid BuildingId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public Guid? SplitterTypeId { get; set; } // FK to SplitterType entity
    public string? Location { get; set; } // MDF, riser, etc.
    public string? Block { get; set; } // Block A, Block B, etc.
    public string? Floor { get; set; } // Floor 1, Ground Floor, etc.
    public Guid? DepartmentId { get; set; } // Optional: department assignment
    public bool IsActive { get; set; } = true;
}

