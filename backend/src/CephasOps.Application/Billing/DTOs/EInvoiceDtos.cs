namespace CephasOps.Application.Billing.DTOs;

/// <summary>
/// DTO for e-invoice submission request
/// </summary>
public class EInvoiceInvoiceDto
{
    /// <summary>
    /// Invoice number (unique identifier)
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Invoice date
    /// </summary>
    public DateTime InvoiceDate { get; set; }

    /// <summary>
    /// Due date
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Supplier (seller) information
    /// </summary>
    public EInvoicePartyDto Supplier { get; set; } = new();

    /// <summary>
    /// Customer (buyer) information
    /// </summary>
    public EInvoicePartyDto Customer { get; set; } = new();

    /// <summary>
    /// Invoice line items
    /// </summary>
    public List<EInvoiceLineItemDto> LineItems { get; set; } = new();

    /// <summary>
    /// Subtotal (before tax)
    /// </summary>
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Tax amount
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Total amount (including tax)
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Currency code (e.g., "MYR")
    /// </summary>
    public string CurrencyCode { get; set; } = "MYR";

    /// <summary>
    /// Exchange rate (if currency is not MYR)
    /// </summary>
    public decimal? ExchangeRate { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for party information (supplier/customer)
/// </summary>
public class EInvoicePartyDto
{
    /// <summary>
    /// Company name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Registration number (SSM, etc.)
    /// </summary>
    public string? RegistrationNumber { get; set; }

    /// <summary>
    /// Tax identification number (GST/SST number)
    /// </summary>
    public string? TaxId { get; set; }

    /// <summary>
    /// Address line 1
    /// </summary>
    public string? AddressLine1 { get; set; }

    /// <summary>
    /// Address line 2
    /// </summary>
    public string? AddressLine2 { get; set; }

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
    /// Country code (e.g., "MY")
    /// </summary>
    public string CountryCode { get; set; } = "MY";

    /// <summary>
    /// Contact email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Contact phone
    /// </summary>
    public string? Phone { get; set; }
}

/// <summary>
/// DTO for invoice line item
/// </summary>
public class EInvoiceLineItemDto
{
    /// <summary>
    /// Line item number (1, 2, 3, etc.)
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Item description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Item code/SKU
    /// </summary>
    public string? ItemCode { get; set; }

    /// <summary>
    /// Quantity
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit of measure (e.g., "UNIT", "HOUR", "KM")
    /// </summary>
    public string UnitOfMeasure { get; set; } = "UNIT";

    /// <summary>
    /// Unit price
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Line total (before tax)
    /// </summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Tax rate (e.g., 0.06 for 6% SST)
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Tax amount
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Tax code (e.g., "SST", "GST", "ZRL")
    /// </summary>
    public string? TaxCode { get; set; }

    /// <summary>
    /// Discount amount (if any)
    /// </summary>
    public decimal DiscountAmount { get; set; }
}

/// <summary>
/// DTO for credit note submission
/// </summary>
public class EInvoiceCreditNoteDto
{
    /// <summary>
    /// Credit note number
    /// </summary>
    public string CreditNoteNumber { get; set; } = string.Empty;

    /// <summary>
    /// Original invoice number (if applicable)
    /// </summary>
    public string? OriginalInvoiceNumber { get; set; }

    /// <summary>
    /// Credit note date
    /// </summary>
    public DateTime CreditNoteDate { get; set; }

    /// <summary>
    /// Supplier (seller) information
    /// </summary>
    public EInvoicePartyDto Supplier { get; set; } = new();

    /// <summary>
    /// Customer (buyer) information
    /// </summary>
    public EInvoicePartyDto Customer { get; set; } = new();

    /// <summary>
    /// Credit note line items
    /// </summary>
    public List<EInvoiceLineItemDto> LineItems { get; set; } = new();

    /// <summary>
    /// Subtotal (before tax)
    /// </summary>
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Tax amount
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Total amount (including tax)
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Reason for credit note
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Currency code
    /// </summary>
    public string CurrencyCode { get; set; } = "MYR";

    /// <summary>
    /// Exchange rate (if currency is not MYR)
    /// </summary>
    public decimal? ExchangeRate { get; set; }
}

/// <summary>
/// Result of e-invoice submission
/// </summary>
public class EInvoiceSubmissionResult
{
    /// <summary>
    /// Whether submission was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Submission ID from portal (UUID or reference number)
    /// </summary>
    public string? SubmissionId { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Error message (if failed)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Response code from portal
    /// </summary>
    public string? ResponseCode { get; set; }

    /// <summary>
    /// Timestamp of submission
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of e-invoice status check
/// </summary>
public class EInvoiceStatusResult
{
    /// <summary>
    /// Whether status check was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Submission ID
    /// </summary>
    public string SubmissionId { get; set; } = string.Empty;

    /// <summary>
    /// Current status (e.g., "Submitted", "Approved", "Rejected", "Pending")
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Status message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Rejection reason (if rejected)
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime? LastUpdatedAt { get; set; }

    /// <summary>
    /// Error message (if status check failed)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

