namespace CephasOps.Domain.Buildings.Entities;

/// <summary>
/// BuildingDefaultMaterial entity - defines default materials for a building per job type
/// When an order is created for this building with the specified job type,
/// these materials are auto-applied to the order
/// </summary>
public class BuildingDefaultMaterial
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
    /// FK to OrderType (Job Type) - e.g., Activation, Modification Outdoor
    /// </summary>
    public Guid OrderTypeId { get; set; }
    
    /// <summary>
    /// FK to Material (must be non-serialized)
    /// </summary>
    public Guid MaterialId { get; set; }
    
    /// <summary>
    /// Default quantity to apply to orders
    /// </summary>
    public decimal DefaultQuantity { get; set; } = 1;
    
    /// <summary>
    /// Optional notes about this default material
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Whether this default material is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public Building? Building { get; set; }
}
