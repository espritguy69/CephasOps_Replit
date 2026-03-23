using CephasOps.Domain.Common;

namespace CephasOps.Domain.Billing.Entities;

/// <summary>
/// Tracks all invoice submission attempts to portal (MyInvois/e-Invoice)
/// Each submission gets a unique SubmissionId that can be referenced for payment status
/// </summary>
public class InvoiceSubmissionHistory : CompanyScopedEntity
{
    /// <summary>
    /// Invoice ID
    /// </summary>
    public Guid InvoiceId { get; set; }

    /// <summary>
    /// Submission ID from portal (MyInvois/e-Invoice)
    /// </summary>
    public string SubmissionId { get; set; } = string.Empty;

    /// <summary>
    /// Submission timestamp
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Submission status (Submitted, Accepted, Rejected, Pending)
    /// </summary>
    public string Status { get; set; } = "Submitted";

    /// <summary>
    /// Response from portal (if any)
    /// </summary>
    public string? ResponseMessage { get; set; }

    /// <summary>
    /// Response code from portal
    /// </summary>
    public string? ResponseCode { get; set; }

    /// <summary>
    /// Rejection reason (if rejected)
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Portal type (MyInvois, e-Invoice, TIME Portal, etc.)
    /// </summary>
    public string PortalType { get; set; } = "MyInvois";

    /// <summary>
    /// User who submitted this invoice
    /// </summary>
    public Guid SubmittedByUserId { get; set; }

    /// <summary>
    /// Whether this is the current active submission
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Payment status for this submission (if tracked by portal)
    /// </summary>
    public string? PaymentStatus { get; set; }

    /// <summary>
    /// Payment reference (if available from portal)
    /// </summary>
    public string? PaymentReference { get; set; }

    /// <summary>
    /// Notes/remarks
    /// </summary>
    public string? Notes { get; set; }
}

