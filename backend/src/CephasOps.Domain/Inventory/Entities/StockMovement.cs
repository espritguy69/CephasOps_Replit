using CephasOps.Domain.Common;

namespace CephasOps.Domain.Inventory.Entities;

/// <summary>
/// Stock movement entity (transaction record)
/// </summary>
public class StockMovement : CompanyScopedEntity
{
    /// <summary>
    /// Source location ID (null for GRN)
    /// </summary>
    public Guid? FromLocationId { get; set; }

    /// <summary>
    /// Destination location ID
    /// </summary>
    public Guid? ToLocationId { get; set; }

    /// <summary>
    /// Material ID
    /// </summary>
    public Guid MaterialId { get; set; }

    /// <summary>
    /// Quantity moved
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Movement type ID (FK to MovementType)
    /// </summary>
    public Guid? MovementTypeId { get; set; }

    /// <summary>
    /// Movement type (legacy string field - kept for backward compatibility)
    /// </summary>
    public string MovementType { get; set; } = string.Empty;

    // Navigation properties
    public virtual MovementType? MovementTypeNavigation { get; set; }

    /// <summary>
    /// Related order ID (if applicable)
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// Service installer ID (if applicable)
    /// </summary>
    public Guid? ServiceInstallerId { get; set; }

    /// <summary>
    /// Partner ID (if applicable)
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// Remarks/notes
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// User ID who created this movement
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    // Navigation properties
    public Material? Material { get; set; }
    public StockLocation? FromLocation { get; set; }
    public StockLocation? ToLocation { get; set; }
}

