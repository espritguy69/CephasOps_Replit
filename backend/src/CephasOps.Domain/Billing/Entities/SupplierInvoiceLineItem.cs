using CephasOps.Domain.Common;

namespace CephasOps.Domain.Billing.Entities;

/// <summary>
/// Supplier invoice line item entity
/// </summary>
public class SupplierInvoiceLineItem : CompanyScopedEntity
{
    /// <summary>
    /// Parent supplier invoice ID
    /// </summary>
    public Guid SupplierInvoiceId { get; set; }

    /// <summary>
    /// Line number
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Description of the item/service
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Quantity
    /// </summary>
    public decimal Quantity { get; set; } = 1;

    /// <summary>
    /// Unit of measure
    /// </summary>
    public string? UnitOfMeasure { get; set; }

    /// <summary>
    /// Unit price
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Line total before tax (Quantity * UnitPrice)
    /// </summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Tax rate percentage
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Tax amount for this line
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Total including tax
    /// </summary>
    public decimal TotalWithTax { get; set; }

    /// <summary>
    /// P&amp;L Type ID for this line item (expense categorization)
    /// </summary>
    public Guid? PnlTypeId { get; set; }

    /// <summary>
    /// Cost centre ID for this line item
    /// </summary>
    public Guid? CostCentreId { get; set; }

    /// <summary>
    /// Asset ID if this is an asset purchase
    /// </summary>
    public Guid? AssetId { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties

    /// <summary>
    /// Parent supplier invoice
    /// </summary>
    public SupplierInvoice? SupplierInvoice { get; set; }
}

