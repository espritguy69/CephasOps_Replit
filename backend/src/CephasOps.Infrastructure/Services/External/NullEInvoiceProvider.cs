using CephasOps.Domain.Billing;
using Microsoft.Extensions.Logging;

namespace CephasOps.Infrastructure.Services.External;

/// <summary>
/// Null implementation of IEInvoiceProvider (fail-proof fallback)
/// Returns success but doesn't actually submit to any portal
/// Useful for development/testing or when MyInvois is not configured
/// </summary>
public class NullEInvoiceProvider : IEInvoiceProvider
{
    private readonly ILogger<NullEInvoiceProvider> _logger;

    public NullEInvoiceProvider(ILogger<NullEInvoiceProvider> logger)
    {
        _logger = logger;
    }

    public Task<EInvoiceSubmissionResult> SubmitInvoiceAsync(
        EInvoiceInvoiceDto invoice,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("NullEInvoiceProvider: Invoice submission requested but no provider configured. Invoice: {InvoiceNumber}", 
            invoice.InvoiceNumber);

        return Task.FromResult(new EInvoiceSubmissionResult
        {
            Success = true,
            SubmissionId = $"NULL-{Guid.NewGuid()}",
            Message = "Null provider: Invoice not actually submitted (no e-invoice provider configured)",
            SubmittedAt = DateTime.UtcNow
        });
    }

    public Task<EInvoiceStatusResult> GetInvoiceStatusAsync(
        string submissionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("NullEInvoiceProvider: Status check requested for {SubmissionId} but no provider configured", submissionId);

        return Task.FromResult(new EInvoiceStatusResult
        {
            Success = true,
            SubmissionId = submissionId,
            Status = "Submitted",
            Message = "Null provider: Status check not available (no e-invoice provider configured)"
        });
    }

    public Task<EInvoiceSubmissionResult> SubmitCreditNoteAsync(
        EInvoiceCreditNoteDto creditNote,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("NullEInvoiceProvider: Credit note submission requested but no provider configured. CreditNote: {CreditNoteNumber}", 
            creditNote.CreditNoteNumber);

        return Task.FromResult(new EInvoiceSubmissionResult
        {
            Success = true,
            SubmissionId = $"NULL-CN-{Guid.NewGuid()}",
            Message = "Null provider: Credit note not actually submitted (no e-invoice provider configured)",
            SubmittedAt = DateTime.UtcNow
        });
    }

    public Task<EInvoiceStatusResult> GetCreditNoteStatusAsync(
        string submissionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("NullEInvoiceProvider: Credit note status check requested for {SubmissionId} but no provider configured", submissionId);

        return Task.FromResult(new EInvoiceStatusResult
        {
            Success = true,
            SubmissionId = submissionId,
            Status = "Submitted",
            Message = "Null provider: Status check not available (no e-invoice provider configured)"
        });
    }
}

