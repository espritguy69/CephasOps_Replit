using CephasOps.Domain.Common;
using CephasOps.Domain.Inventory.Entities;

namespace CephasOps.Domain.Orders.Entities;

/// <summary>
/// Order material usage entity - tracks materials used per order
/// </summary>
public class OrderMaterialUsage : CompanyScopedEntity
{
    public Guid OrderId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid? SerialisedItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? TotalCost { get; set; }
    public Guid? SourceLocationId { get; set; }
    public Guid? StockMovementId { get; set; }
    public Guid? RecordedBySiId { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public DateTime RecordedAt { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Material? Material { get; set; }
    public virtual SerialisedItem? SerialisedItem { get; set; }
}

