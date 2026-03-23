using CephasOps.Domain.Common;

namespace CephasOps.Domain.Buildings.Entities;

/// <summary>
/// SplitterType entity - represents splitter types (1:8, 1:12, 1:32)
/// </summary>
public class SplitterType : CompanyScopedEntity
{
    public Guid? DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty; // 1:8, 1:12, 1:32
    public string Code { get; set; } = string.Empty; // 1_8, 1_12, 1_32
    public int TotalPorts { get; set; } // 8, 12, 32
    public int? StandbyPortNumber { get; set; } // 32 for 1:32 splitter
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

