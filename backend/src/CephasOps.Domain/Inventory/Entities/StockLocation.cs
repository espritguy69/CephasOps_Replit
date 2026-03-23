using CephasOps.Domain.Common;

namespace CephasOps.Domain.Inventory.Entities;

/// <summary>
/// Stock location entity
/// </summary>
public class StockLocation : CompanyScopedEntity
{
    /// <summary>
    /// Location name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Location type ID (FK to LocationType)
    /// </summary>
    public Guid? LocationTypeId { get; set; }

    /// <summary>
    /// Location type (legacy string field - kept for backward compatibility)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Warehouse ID (if this location belongs to a warehouse)
    /// </summary>
    public Guid? WarehouseId { get; set; }

    /// <summary>
    /// Linked service installer ID (if Type is SI)
    /// </summary>
    public Guid? LinkedServiceInstallerId { get; set; }

    /// <summary>
    /// Linked building ID (if Type is CustomerSite)
    /// </summary>
    public Guid? LinkedBuildingId { get; set; }

    // Navigation properties
    public virtual LocationType? LocationType { get; set; }

    /// <summary>
    /// Whether this location is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

