using CephasOps.Domain.Common;

namespace CephasOps.Domain.Inventory.Entities;

/// <summary>
/// Delivery Order entity for tracking material deliveries
/// </summary>
public class DeliveryOrder : CompanyScopedEntity
{
    /// <summary>
    /// DO number (auto-generated or manual)
    /// </summary>
    public string DoNumber { get; set; } = string.Empty;

    /// <summary>
    /// DO date
    /// </summary>
    public DateTime DoDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Type: Outbound (to customer/site), Inbound (from supplier), Transfer (between locations)
    /// </summary>
    public string DoType { get; set; } = "Outbound";

    /// <summary>
    /// Status: Draft, Pending, InTransit, Delivered, Cancelled
    /// </summary>
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// Source location ID (warehouse, stock location)
    /// </summary>
    public Guid? SourceLocationId { get; set; }

    /// <summary>
    /// Destination location ID (for transfers)
    /// </summary>
    public Guid? DestinationLocationId { get; set; }

    /// <summary>
    /// Related Order ID (if delivery for an order)
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// Related Purchase Order ID (if receiving from supplier)
    /// </summary>
    public Guid? PurchaseOrderId { get; set; }

    /// <summary>
    /// Related Project ID
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Recipient name
    /// </summary>
    public string RecipientName { get; set; } = string.Empty;

    /// <summary>
    /// Recipient phone
    /// </summary>
    public string? RecipientPhone { get; set; }

    /// <summary>
    /// Recipient email
    /// </summary>
    public string? RecipientEmail { get; set; }

    /// <summary>
    /// Delivery address
    /// </summary>
    public string DeliveryAddress { get; set; } = string.Empty;

    /// <summary>
    /// City
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// State
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Postcode
    /// </summary>
    public string? Postcode { get; set; }

    /// <summary>
    /// Expected delivery date
    /// </summary>
    public DateTime? ExpectedDeliveryDate { get; set; }

    /// <summary>
    /// Actual delivery date
    /// </summary>
    public DateTime? ActualDeliveryDate { get; set; }

    /// <summary>
    /// Delivery person / driver name
    /// </summary>
    public string? DeliveryPerson { get; set; }

    /// <summary>
    /// Vehicle number
    /// </summary>
    public string? VehicleNumber { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Internal notes
    /// </summary>
    public string? InternalNotes { get; set; }

    /// <summary>
    /// Signature captured (base64 or file ID)
    /// </summary>
    public string? RecipientSignature { get; set; }

    /// <summary>
    /// Received by name
    /// </summary>
    public string? ReceivedByName { get; set; }

    /// <summary>
    /// Received date/time
    /// </summary>
    public DateTime? ReceivedAt { get; set; }

    /// <summary>
    /// User ID who created this DO
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    // Navigation properties
    public virtual ICollection<DeliveryOrderItem> Items { get; set; } = new List<DeliveryOrderItem>();
}

/// <summary>
/// Delivery Order line item
/// </summary>
public class DeliveryOrderItem : CompanyScopedEntity
{
    /// <summary>
    /// Parent DO ID
    /// </summary>
    public Guid DeliveryOrderId { get; set; }

    /// <summary>
    /// Material ID
    /// </summary>
    public Guid MaterialId { get; set; }

    /// <summary>
    /// Line number
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Item description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// SKU
    /// </summary>
    public string? Sku { get; set; }

    /// <summary>
    /// Unit of measure
    /// </summary>
    public string Unit { get; set; } = "pcs";

    /// <summary>
    /// Quantity to deliver
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Quantity actually delivered
    /// </summary>
    public decimal QuantityDelivered { get; set; }

    /// <summary>
    /// Serial numbers (comma-separated or JSON array)
    /// </summary>
    public string? SerialNumbers { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    // Navigation
    public virtual DeliveryOrder? DeliveryOrder { get; set; }
    public virtual Material? Material { get; set; }
}

