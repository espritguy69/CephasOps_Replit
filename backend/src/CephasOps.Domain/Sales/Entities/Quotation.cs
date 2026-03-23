using CephasOps.Domain.Common;

namespace CephasOps.Domain.Sales.Entities;

/// <summary>
/// Quotation entity for sales quotes to customers
/// </summary>
public class Quotation : CompanyScopedEntity
{
    /// <summary>
    /// Quotation number (auto-generated or manual)
    /// </summary>
    public string QuotationNumber { get; set; } = string.Empty;

    /// <summary>
    /// Partner ID (if quoting to a partner)
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// Department ID
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Project ID (if project-based quotation)
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Customer name (for non-partner customers)
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Customer phone
    /// </summary>
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// Customer email
    /// </summary>
    public string? CustomerEmail { get; set; }

    /// <summary>
    /// Customer address
    /// </summary>
    public string? CustomerAddress { get; set; }

    /// <summary>
    /// Quotation date
    /// </summary>
    public DateTime QuotationDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Valid until date
    /// </summary>
    public DateTime? ValidUntil { get; set; }

    /// <summary>
    /// Status: Draft, Sent, Accepted, Rejected, Expired, Converted
    /// </summary>
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// Subject / title
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Introduction / cover letter
    /// </summary>
    public string? Introduction { get; set; }

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
    /// Payment terms
    /// </summary>
    public string? PaymentTerms { get; set; }

    /// <summary>
    /// Delivery terms
    /// </summary>
    public string? DeliveryTerms { get; set; }

    /// <summary>
    /// Terms and conditions
    /// </summary>
    public string? TermsAndConditions { get; set; }

    /// <summary>
    /// Notes / remarks
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Internal notes
    /// </summary>
    public string? InternalNotes { get; set; }

    /// <summary>
    /// User ID who created this quotation
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// User ID who approved this quotation
    /// </summary>
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>
    /// Approval date
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Date when sent to customer
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Date when customer accepted
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    /// Date when customer rejected
    /// </summary>
    public DateTime? RejectedAt { get; set; }

    /// <summary>
    /// Rejection reason
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Converted to Order ID (if accepted and converted)
    /// </summary>
    public Guid? ConvertedToOrderId { get; set; }

    /// <summary>
    /// Converted to PO ID (if procurement quotation)
    /// </summary>
    public Guid? ConvertedToPurchaseOrderId { get; set; }

    // Navigation properties
    public virtual ICollection<QuotationItem> Items { get; set; } = new List<QuotationItem>();
}

/// <summary>
/// Quotation line item
/// </summary>
public class QuotationItem : CompanyScopedEntity
{
    /// <summary>
    /// Parent quotation ID
    /// </summary>
    public Guid QuotationId { get; set; }

    /// <summary>
    /// Material ID (if quoting inventory item)
    /// </summary>
    public Guid? MaterialId { get; set; }

    /// <summary>
    /// Line number for ordering
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Item type: Material, Labor, Service, Other
    /// </summary>
    public string ItemType { get; set; } = "Material";

    /// <summary>
    /// Item description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// SKU / Part number
    /// </summary>
    public string? Sku { get; set; }

    /// <summary>
    /// Unit of measure
    /// </summary>
    public string Unit { get; set; } = "pcs";

    /// <summary>
    /// Quantity
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
    /// Line total
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    // Navigation
    public virtual Quotation? Quotation { get; set; }
}

