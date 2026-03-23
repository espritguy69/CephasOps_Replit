using CephasOps.Domain.Common;

namespace CephasOps.Domain.Inventory.Entities;

/// <summary>
/// Serialised item entity (for tracking individual serial numbers)
/// </summary>
public class SerialisedItem : CompanyScopedEntity
{
    /// <summary>
    /// Material ID
    /// </summary>
    public Guid MaterialId { get; set; }

    /// <summary>
    /// Serial number (unique within company)
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Current location ID
    /// </summary>
    public Guid? CurrentLocationId { get; set; }

    /// <summary>
    /// Status (InWarehouse, WithSI, InstalledAtCustomer, FaultyInWarehouse, InTransitToPartner, RMAClosed, Scrapped)
    /// </summary>
    public string Status { get; set; } = "InWarehouse";

    /// <summary>
    /// Last order ID where this item was used
    /// </summary>
    public Guid? LastOrderId { get; set; }

    /// <summary>
    /// Last service ID (if applicable)
    /// </summary>
    public Guid? LastServiceId { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public Material? Material { get; set; }
    public StockLocation? CurrentLocation { get; set; }
}

