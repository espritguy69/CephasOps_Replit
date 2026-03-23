using CephasOps.Domain.Common;
using CephasOps.Domain.Billing.Enums;

namespace CephasOps.Domain.Billing.Entities;

/// <summary>
/// Supplier invoice entity (invoices received from vendors/suppliers)
/// </summary>
public class SupplierInvoice : CompanyScopedEntity
{
    /// <summary>
    /// Supplier's invoice number
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Internal reference number
    /// </summary>
    public string? InternalReference { get; set; }

    /// <summary>
    /// Supplier/vendor name
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Supplier registration/tax number
    /// </summary>
    public string? SupplierTaxNumber { get; set; }

    /// <summary>
    /// Supplier address
    /// </summary>
    public string? SupplierAddress { get; set; }

    /// <summary>
    /// Supplier email
    /// </summary>
    public string? SupplierEmail { get; set; }

    /// <summary>
    /// Invoice date (from supplier)
    /// </summary>
    public DateTime InvoiceDate { get; set; }

    /// <summary>
    /// Date invoice was received
    /// </summary>
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Due date for payment
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Subtotal (before tax)
    /// </summary>
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Tax amount (SST/GST)
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Total amount (including tax)
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Amount paid so far
    /// </summary>
    public decimal AmountPaid { get; set; }

    /// <summary>
    /// Outstanding amount (TotalAmount - AmountPaid)
    /// </summary>
    public decimal OutstandingAmount { get; set; }

    /// <summary>
    /// Currency code (default MYR)
    /// </summary>
    public string Currency { get; set; } = "MYR";

    /// <summary>
    /// Invoice status
    /// </summary>
    public SupplierInvoiceStatus Status { get; set; } = SupplierInvoiceStatus.Draft;

    /// <summary>
    /// Cost centre ID for default allocation
    /// </summary>
    public Guid? CostCentreId { get; set; }

    /// <summary>
    /// Default P&amp;L Type ID (can be overridden per line item)
    /// </summary>
    public Guid? DefaultPnlTypeId { get; set; }

    /// <summary>
    /// Description/notes
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Notes for internal use
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// File attachment path (scanned invoice, PDF)
    /// </summary>
    public string? AttachmentPath { get; set; }

    /// <summary>
    /// User ID who created this invoice
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// User ID who approved this invoice
    /// </summary>
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>
    /// Approval date
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Date when fully paid
    /// </summary>
    public DateTime? PaidAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Line items for this invoice
    /// </summary>
    public ICollection<SupplierInvoiceLineItem> LineItems { get; set; } = new List<SupplierInvoiceLineItem>();

    /// <summary>
    /// Payments made against this invoice
    /// </summary>
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

