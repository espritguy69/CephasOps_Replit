using CephasOps.Domain.Common;
using CephasOps.Domain.Billing.Enums;

namespace CephasOps.Domain.Billing.Entities;

/// <summary>
/// Payment entity for tracking both incoming (receipts) and outgoing (disbursements) payments
/// </summary>
public class Payment : CompanyScopedEntity
{
    /// <summary>
    /// Payment reference number
    /// </summary>
    public string PaymentNumber { get; set; } = string.Empty;

    /// <summary>
    /// Type of payment (Income/Expense)
    /// </summary>
    public PaymentType PaymentType { get; set; }

    /// <summary>
    /// Payment method
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Payment date
    /// </summary>
    public DateTime PaymentDate { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (default MYR)
    /// </summary>
    public string Currency { get; set; } = "MYR";

    /// <summary>
    /// Payer/payee name
    /// </summary>
    public string PayerPayeeName { get; set; } = string.Empty;

    /// <summary>
    /// Bank account used for this payment (if applicable)
    /// </summary>
    public string? BankAccount { get; set; }

    /// <summary>
    /// Bank reference / transaction ID
    /// </summary>
    public string? BankReference { get; set; }

    /// <summary>
    /// Cheque number (if payment by cheque)
    /// </summary>
    public string? ChequeNumber { get; set; }

    /// <summary>
    /// Linked customer invoice ID (for income payments)
    /// </summary>
    public Guid? InvoiceId { get; set; }

    /// <summary>
    /// Linked supplier invoice ID (for expense payments)
    /// </summary>
    public Guid? SupplierInvoiceId { get; set; }

    /// <summary>
    /// P&amp;L Type ID (for direct expenses/income not linked to invoices)
    /// </summary>
    public Guid? PnlTypeId { get; set; }

    /// <summary>
    /// Cost centre ID
    /// </summary>
    public Guid? CostCentreId { get; set; }

    /// <summary>
    /// Description/notes
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Internal notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Attachment path (receipt, bank slip, etc.)
    /// </summary>
    public string? AttachmentPath { get; set; }

    /// <summary>
    /// Whether this payment has been reconciled
    /// </summary>
    public bool IsReconciled { get; set; }

    /// <summary>
    /// Reconciliation date
    /// </summary>
    public DateTime? ReconciledAt { get; set; }

    /// <summary>
    /// User ID who created this payment
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Whether this payment is voided/cancelled
    /// </summary>
    public bool IsVoided { get; set; }

    /// <summary>
    /// Void reason
    /// </summary>
    public string? VoidReason { get; set; }

    /// <summary>
    /// Void date
    /// </summary>
    public DateTime? VoidedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Linked customer invoice (for income)
    /// </summary>
    public Invoice? Invoice { get; set; }

    /// <summary>
    /// Linked supplier invoice (for expense)
    /// </summary>
    public SupplierInvoice? SupplierInvoice { get; set; }
}

