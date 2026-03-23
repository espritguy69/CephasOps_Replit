using CephasOps.Application.Billing.DTOs;

namespace CephasOps.Application.Billing.Services;

/// <summary>
/// Service for tracking invoice submissions to portals
/// </summary>
public interface IInvoiceSubmissionService
{
    /// <summary>
    /// Record a new invoice submission to portal
    /// </summary>
    Task<InvoiceSubmissionHistoryDto> RecordSubmissionAsync(
        Guid invoiceId,
        string submissionId,
        string portalType,
        Guid submittedByUserId,
        string? responseMessage = null,
        string? responseCode = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update submission status (e.g., when payment is rejected)
    /// </summary>
    Task<InvoiceSubmissionHistoryDto> UpdateSubmissionStatusAsync(
        Guid submissionHistoryId,
        string status,
        string? rejectionReason = null,
        string? paymentStatus = null,
        string? paymentReference = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all submission history for an invoice
    /// </summary>
    Task<List<InvoiceSubmissionHistoryDto>> GetSubmissionHistoryAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active submission for an invoice
    /// </summary>
    Task<InvoiceSubmissionHistoryDto?> GetActiveSubmissionAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get submission by history record id (for status poll job).
    /// </summary>
    Task<InvoiceSubmissionHistoryDto?> GetSubmissionByHistoryIdAsync(
        Guid submissionHistoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit invoice to e-invoice portal (MyInvois)
    /// This is the two-step submission flow: submit → get submission ID → record submission
    /// </summary>
    Task<InvoiceSubmissionHistoryDto> SubmitInvoiceToPortalAsync(
        Guid invoiceId,
        Guid submittedByUserId,
        CancellationToken cancellationToken = default);
}

