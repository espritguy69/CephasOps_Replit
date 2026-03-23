using CephasOps.Domain.Billing.Enums;

namespace CephasOps.Application.Billing.DTOs;

/// <summary>
/// Supplier Invoice DTO
/// </summary>
public class SupplierInvoiceDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? InternalReference { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierTaxNumber { get; set; }
    public string? SupplierAddress { get; set; }
    public string? SupplierEmail { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime ReceivedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal OutstandingAmount { get; set; }
    public string Currency { get; set; } = "MYR";
    public SupplierInvoiceStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public Guid? CostCentreId { get; set; }
    public Guid? DefaultPnlTypeId { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? AttachmentPath { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<SupplierInvoiceLineItemDto> LineItems { get; set; } = new();
}

/// <summary>
/// Supplier Invoice Line Item DTO
/// </summary>
public class SupplierInvoiceLineItemDto
{
    public Guid Id { get; set; }
    public Guid SupplierInvoiceId { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalWithTax { get; set; }
    public Guid? PnlTypeId { get; set; }
    public string? PnlTypeName { get; set; }
    public Guid? CostCentreId { get; set; }
    public Guid? AssetId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Create Supplier Invoice request DTO
/// </summary>
public class CreateSupplierInvoiceDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? InternalReference { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierTaxNumber { get; set; }
    public string? SupplierAddress { get; set; }
    public string? SupplierEmail { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string Currency { get; set; } = "MYR";
    public Guid? CostCentreId { get; set; }
    public Guid? DefaultPnlTypeId { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? AttachmentPath { get; set; }
    public List<CreateSupplierInvoiceLineItemDto> LineItems { get; set; } = new();
}

/// <summary>
/// Create Supplier Invoice Line Item request DTO
/// </summary>
public class CreateSupplierInvoiceLineItemDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public string? UnitOfMeasure { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public Guid? PnlTypeId { get; set; }
    public Guid? CostCentreId { get; set; }
    public Guid? AssetId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Update Supplier Invoice request DTO
/// </summary>
public class UpdateSupplierInvoiceDto
{
    public string? InvoiceNumber { get; set; }
    public string? InternalReference { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierTaxNumber { get; set; }
    public string? SupplierAddress { get; set; }
    public string? SupplierEmail { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public SupplierInvoiceStatus? Status { get; set; }
    public Guid? CostCentreId { get; set; }
    public Guid? DefaultPnlTypeId { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? AttachmentPath { get; set; }
}

/// <summary>
/// Supplier Invoice summary for dashboard
/// </summary>
public class SupplierInvoiceSummaryDto
{
    public int TotalInvoices { get; set; }
    public int PendingApproval { get; set; }
    public int OverdueInvoices { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalThisMonth { get; set; }
    public List<SupplierInvoicesByStatusDto> ByStatus { get; set; } = new();
}

/// <summary>
/// Invoices grouped by status
/// </summary>
public class SupplierInvoicesByStatusDto
{
    public SupplierInvoiceStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}

