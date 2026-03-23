namespace CephasOps.Domain.Buildings.Entities;

/// <summary>
/// BuildingRules entity - stores building-related rules and notes
/// One-to-one relationship with Building
/// </summary>
public class BuildingRules
{
    /// <summary>
    /// Unique identifier (same as BuildingId for 1:1 relationship)
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// FK to Building
    /// </summary>
    public Guid BuildingId { get; set; }

    /// <summary>
    /// Access rules and requirements
    /// </summary>
    public string? AccessRules { get; set; }

    /// <summary>
    /// Installation rules and restrictions
    /// </summary>
    public string? InstallationRules { get; set; }

    /// <summary>
    /// Other notes and information
    /// </summary>
    public string? OtherNotes { get; set; }

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

