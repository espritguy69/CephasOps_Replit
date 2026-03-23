namespace CephasOps.Domain.Billing.Enums;

/// <summary>
/// Status of a supplier invoice
/// </summary>
public enum SupplierInvoiceStatus
{
    /// <summary>
    /// Invoice is in draft state
    /// </summary>
    Draft = 1,

    /// <summary>
    /// Invoice has been received and pending approval
    /// </summary>
    PendingApproval = 2,

    /// <summary>
    /// Invoice has been approved for payment
    /// </summary>
    Approved = 3,

    /// <summary>
    /// Invoice has been partially paid
    /// </summary>
    PartiallyPaid = 4,

    /// <summary>
    /// Invoice has been fully paid
    /// </summary>
    Paid = 5,

    /// <summary>
    /// Invoice is overdue
    /// </summary>
    Overdue = 6,

    /// <summary>
    /// Invoice has been cancelled/voided
    /// </summary>
    Cancelled = 7,

    /// <summary>
    /// Invoice is disputed
    /// </summary>
    Disputed = 8
}

