namespace CephasOps.Domain.Buildings.Entities;

/// <summary>
/// Street entity - represents a street in a landed/SDU area
/// Used for street-based infrastructure management
/// </summary>
public class Street
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// FK to Building (parent building/area)
    /// </summary>
    public Guid BuildingId { get; set; }

    /// <summary>
    /// Street name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Street code (e.g., "JLN-01", "SS2-15")
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Section or phase (e.g., "Section 14", "Phase 2")
    /// </summary>
    public string? Section { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this street is active
    /// </summary>
    public bool IsActive { get; set; } = true;

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

