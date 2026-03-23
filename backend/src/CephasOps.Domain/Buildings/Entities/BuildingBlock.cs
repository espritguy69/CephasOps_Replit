namespace CephasOps.Domain.Buildings.Entities;

/// <summary>
/// BuildingBlock entity - represents blocks/towers within an MDU building
/// </summary>
public class BuildingBlock
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// FK to Building
    /// </summary>
    public Guid BuildingId { get; set; }

    /// <summary>
    /// Block name (e.g., "Block A", "Tower 1", "West Wing")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Block code/identifier
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Number of floors in this block
    /// </summary>
    public int Floors { get; set; }

    /// <summary>
    /// Number of units per floor (approximate)
    /// </summary>
    public int? UnitsPerFloor { get; set; }

    /// <summary>
    /// Total units in block
    /// </summary>
    public int? TotalUnits { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this block is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Updated timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public Building? Building { get; set; }
}

