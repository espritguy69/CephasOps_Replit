using CephasOps.Domain.Common;

namespace CephasOps.Domain.Inventory.Entities;

/// <summary>
/// Location type entity (settings-driven)
/// Defines configurable stock location types (Warehouse, SI, RMA, CustomerSite, etc.)
/// </summary>
public class LocationType : CompanyScopedEntity
{
    /// <summary>
    /// Location type code (unique within company)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Location type name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this location type requires linked service installer ID
    /// </summary>
    public bool RequiresServiceInstallerId { get; set; }

    /// <summary>
    /// Whether this location type requires linked building ID
    /// </summary>
    public bool RequiresBuildingId { get; set; }

    /// <summary>
    /// Whether this location type requires warehouse ID
    /// </summary>
    public bool RequiresWarehouseId { get; set; }

    /// <summary>
    /// Whether locations of this type should be auto-created
    /// </summary>
    public bool AutoCreate { get; set; }

    /// <summary>
    /// Auto-create trigger: ServiceInstallerCreated, BuildingCreated, WarehouseCreated, Manual
    /// </summary>
    public string? AutoCreateTrigger { get; set; }

    /// <summary>
    /// Whether this location type is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sort order for display
    /// </summary>
    public int SortOrder { get; set; } = 0;
}

