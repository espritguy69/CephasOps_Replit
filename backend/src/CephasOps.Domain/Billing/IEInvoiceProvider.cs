namespace CephasOps.Domain.Billing;

/// <summary>
/// Interface for e-invoice providers (MyInvois, etc.)
/// Moved to Domain to avoid circular dependency (Infrastructure -> Application)
/// </summary>
public interface IEInvoiceProvider
{
    /// <summary>
    /// Submit an invoice to the e-invoice portal
    /// Returns submission ID and status
    /// </summary>
    Task<EInvoiceSubmissionResult> SubmitInvoiceAsync(
        EInvoiceInvoiceDto invoice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check the status of a submitted invoice
    /// </summary>
    Task<EInvoiceStatusResult> GetInvoiceStatusAsync(
        string submissionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit a credit note
    /// </summary>
    Task<EInvoiceSubmissionResult> SubmitCreditNoteAsync(
        EInvoiceCreditNoteDto creditNote,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check the status of a submitted credit note
    /// </summary>
    Task<EInvoiceStatusResult> GetCreditNoteStatusAsync(
        string submissionId,
        CancellationToken cancellationToken = default);
}

