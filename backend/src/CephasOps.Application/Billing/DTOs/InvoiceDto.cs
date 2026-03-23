namespace CephasOps.Application.Billing.DTOs;

/// <summary>
/// Invoice DTO
/// </summary>
public class InvoiceDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string? PartnerAddress { get; set; }
    public string? PartnerContactName { get; set; }
    public string? PartnerContactEmail { get; set; }
    public string? PartnerContactPhone { get; set; }
    /// <summary>Subject line for Bill To (e.g. order type from first line item)</summary>
    public string? BillToSubject { get; set; }
    public DateTime InvoiceDate { get; set; }
    public int TermsInDays { get; set; } = 45;
    public DateTime? DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal SubTotal { get; set; }
    public string? DoRefNo { get; set; }
    public string? PurchaseOrderNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SubmissionId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public List<InvoiceLineItemDto> LineItems { get; set; } = new();
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Company letterhead for invoice document (name, address, phone, email, registration)
    /// </summary>
    public CompanyLetterheadDto? CompanyLetterhead { get; set; }
}

/// <summary>
/// Company letterhead for invoice document rendering
/// </summary>
public class CompanyLetterheadDto
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? RegistrationNo { get; set; }
}

/// <summary>
/// Invoice line item DTO
/// </summary>
public class InvoiceLineItemDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public Guid? OrderId { get; set; }
    /// <summary>Order customer name - for Description block (CUSTOMER NAME:)</summary>
    public string? CustomerName { get; set; }
    /// <summary>Order service ID - for Description block (SERVICE ID:)</summary>
    public string? ServiceId { get; set; }
    /// <summary>Order type name - for Description block (ORDER TYPE:)</summary>
    public string? OrderType { get; set; }
    /// <summary>Order docket number - for Description block (DOCKET NO:)</summary>
    public string? DocketNo { get; set; }
}

/// <summary>
/// Create invoice request DTO
/// </summary>
public class CreateInvoiceDto
{
    /// <summary>
    /// Optional idempotency key. When provided, repeated requests with the same key (same company) return the existing invoice instead of creating a duplicate.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    public Guid PartnerId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public int? TermsInDays { get; set; }
    public DateTime? DueDate { get; set; }
    public List<CreateInvoiceLineItemDto> LineItems { get; set; } = new();
}

/// <summary>
/// Create invoice line item DTO
/// </summary>
public class CreateInvoiceLineItemDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Guid? OrderId { get; set; }
}

/// <summary>
/// Update invoice request DTO
/// </summary>
public class UpdateInvoiceDto
{
    public Guid? PartnerId { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public int? TermsInDays { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Status { get; set; }
    public string? SubmissionId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    /// <summary>When provided, replaces all line items. Omit to leave unchanged.</summary>
    public List<UpdateInvoiceLineItemDto>? LineItems { get; set; }
}

/// <summary>
/// Update invoice line item - Id required for existing, empty for new
/// </summary>
public class UpdateInvoiceLineItemDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Guid? OrderId { get; set; }
}

/// <summary>
/// Result of resolving a single invoice line from an order using BillingRatecard.
/// Used by BuildInvoiceLinesFromOrdersAsync.
/// </summary>
public class ResolvedInvoiceLineDto
{
    public Guid OrderId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public Guid? BillingRatecardId { get; set; }
}

/// <summary>
/// Result of building suggested invoice line items from orders using BillingRatecard resolution.
/// </summary>
public class BuildInvoiceLinesResult
{
    public List<CreateInvoiceLineItemDto> LineItems { get; set; } = new();
    /// <summary>Order IDs that could not be resolved to a rate (e.g. missing OrderCategoryId, no matching BillingRatecard).</summary>
    public List<Guid> UnresolvedOrderIds { get; set; } = new();
    /// <summary>Per-order error or warning messages.</summary>
    public List<string> Messages { get; set; } = new();
}

/// <summary>
/// Request to build suggested invoice line items from orders using BillingRatecard.
/// </summary>
public class BuildInvoiceLinesRequest
{
    public List<Guid> OrderIds { get; set; } = new();
    /// <summary>Optional reference date for BillingRatecard EffectiveFrom/EffectiveTo. Defaults to UTC now.</summary>
    public DateTime? ReferenceDate { get; set; }
}

