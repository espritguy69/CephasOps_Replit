using CephasOps.Domain.Common;

namespace CephasOps.Domain.Billing.Entities;

/// <summary>
/// Invoice line item entity
/// </summary>
public class InvoiceLineItem : CompanyScopedEntity
{
    /// <summary>
    /// Invoice ID
    /// </summary>
    public Guid InvoiceId { get; set; }

    /// <summary>
    /// Line item description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Quantity
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit price
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Total (Quantity * UnitPrice)
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Related order ID (if applicable)
    /// </summary>
    public Guid? OrderId { get; set; }

    // Navigation properties
    public Invoice? Invoice { get; set; }
}

