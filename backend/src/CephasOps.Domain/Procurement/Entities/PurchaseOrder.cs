using CephasOps.Domain.Common;

namespace CephasOps.Domain.Procurement.Entities;

/// <summary>
/// Purchase Order entity for procurement from suppliers
/// </summary>
public class PurchaseOrder : CompanyScopedEntity
{
    /// <summary>
    /// PO number (auto-generated or manual)
    /// </summary>
    public string PoNumber { get; set; } = string.Empty;

    /// <summary>
    /// Supplier ID
    /// </summary>
    public Guid SupplierId { get; set; }

    /// <summary>
    /// Department ID (optional)
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Cost Centre ID
    /// </summary>
    public Guid? CostCentreId { get; set; }

    /// <summary>
    /// PO date
    /// </summary>
    public DateTime PoDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Expected delivery date
    /// </summary>
    public DateTime? ExpectedDeliveryDate { get; set; }

    /// <summary>
    /// Delivery address
    /// </summary>
    public string? DeliveryAddress { get; set; }

    /// <summary>
    /// Status: Draft, Submitted, Approved, PartiallyReceived, FullyReceived, Cancelled
    /// </summary>
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// Sub-total before tax
    /// </summary>
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Tax amount
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Discount amount
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Total amount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "MYR";

    /// <summary>
    /// Payment terms (e.g., "Net 30", "COD")
    /// </summary>
    public string? PaymentTerms { get; set; }

    /// <summary>
    /// Terms and conditions
    /// </summary>
    public string? TermsAndConditions { get; set; }

    /// <summary>
    /// Notes / remarks
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Internal notes (not shown on printed PO)
    /// </summary>
    public string? InternalNotes { get; set; }

    /// <summary>
    /// User ID who created this PO
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// User ID who approved this PO
    /// </summary>
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>
    /// Approval date
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Reference to related quotation (if created from quotation)
    /// </summary>
    public Guid? QuotationId { get; set; }

    /// <summary>
    /// Reference to related project (if project-based)
    /// </summary>
    public Guid? ProjectId { get; set; }

    // Navigation properties
    public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}

/// <summary>
/// Purchase Order line item
/// </summary>
public class PurchaseOrderItem : CompanyScopedEntity
{
    /// <summary>
    /// Parent PO ID
    /// </summary>
    public Guid PurchaseOrderId { get; set; }

    /// <summary>
    /// Material ID (if ordering inventory item)
    /// </summary>
    public Guid? MaterialId { get; set; }

    /// <summary>
    /// Line number for ordering
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Item description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// SKU / Part number
    /// </summary>
    public string? Sku { get; set; }

    /// <summary>
    /// Unit of measure (pcs, box, meter, etc.)
    /// </summary>
    public string Unit { get; set; } = "pcs";

    /// <summary>
    /// Quantity ordered
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit price
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Discount percentage
    /// </summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>
    /// Tax percentage
    /// </summary>
    public decimal TaxPercent { get; set; }

    /// <summary>
    /// Line total (after discount, before tax)
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Quantity received so far
    /// </summary>
    public decimal QuantityReceived { get; set; }

    /// <summary>
    /// Notes for this line item
    /// </summary>
    public string? Notes { get; set; }

    // Navigation
    public virtual PurchaseOrder? PurchaseOrder { get; set; }
}

