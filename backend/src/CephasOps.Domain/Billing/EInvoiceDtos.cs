namespace CephasOps.Domain.Billing;

/// <summary>
/// DTO for e-invoice submission request
/// Moved to Domain to avoid circular dependency (Infrastructure -> Application)
/// </summary>
public class EInvoiceInvoiceDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public EInvoicePartyDto Supplier { get; set; } = new();
    public EInvoicePartyDto Customer { get; set; } = new();
    public List<EInvoiceLineItemDto> LineItems { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "MYR";
    public decimal? ExchangeRate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for party information (supplier/customer)
/// </summary>
public class EInvoicePartyDto
{
    public string Name { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public string? TaxId { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Postcode { get; set; }
    public string CountryCode { get; set; } = "MY";
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

/// <summary>
/// DTO for invoice line item
/// </summary>
public class EInvoiceLineItemDto
{
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ItemCode { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = "UNIT";
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public string? TaxCode { get; set; }
    public decimal DiscountAmount { get; set; }
}

/// <summary>
/// DTO for credit note submission
/// </summary>
public class EInvoiceCreditNoteDto
{
    public string CreditNoteNumber { get; set; } = string.Empty;
    public string? OriginalInvoiceNumber { get; set; }
    public DateTime CreditNoteDate { get; set; }
    public EInvoicePartyDto Supplier { get; set; } = new();
    public EInvoicePartyDto Customer { get; set; } = new();
    public List<EInvoiceLineItemDto> LineItems { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "MYR";
    public decimal? ExchangeRate { get; set; }
}

/// <summary>
/// Result of e-invoice submission
/// </summary>
public class EInvoiceSubmissionResult
{
    public bool Success { get; set; }
    public string? SubmissionId { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ResponseCode { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of e-invoice status check
/// </summary>
public class EInvoiceStatusResult
{
    public bool Success { get; set; }
    public string SubmissionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

