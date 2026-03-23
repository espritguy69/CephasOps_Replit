using CephasOps.Domain.Common;

namespace CephasOps.Domain.Billing.Entities;

/// <summary>
/// Invoice entity
/// </summary>
public class Invoice : CompanyScopedEntity
{
    /// <summary>
    /// Invoice number (unique within company)
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Partner ID
    /// </summary>
    public Guid PartnerId { get; set; }

    /// <summary>
    /// Invoice date
    /// </summary>
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Payment terms in days (default 45 = Net 45 days)
    /// </summary>
    public int TermsInDays { get; set; } = 45;

    /// <summary>
    /// Due date (calculated: InvoiceDate + TermsInDays, locked when Sent)
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Total amount (including tax)
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Tax amount (SST/GST)
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Subtotal (before tax)
    /// </summary>
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Status (Draft, Sent, Paid, Overdue, Cancelled)
    /// </summary>
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// MyInvois submission ID (if submitted)
    /// </summary>
    public string? SubmissionId { get; set; }

    /// <summary>
    /// Submission timestamp
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// Payment timestamp
    /// </summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// User ID who created this invoice
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    // Navigation properties
    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
}

