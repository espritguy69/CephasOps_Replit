using CephasOps.Application.Billing.Services;
using CephasOps.Domain.Workflow.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Workflow.JobOrchestration.Executors;

/// <summary>
/// Polls MyInvois for invoice submission status and updates submission history (Phase 9). Payload: submissionHistoryId (guid string).
/// Replaces legacy myinvoisstatuspoll BackgroundJob execution.
/// </summary>
public sealed class MyInvoisStatusPollJobExecutor : IJobExecutor
{
    public string JobType => "myinvoisstatuspoll";

    private readonly EInvoiceProviderFactory _eInvoiceProviderFactory;
    private readonly IInvoiceSubmissionService _invoiceSubmissionService;
    private readonly ILogger<MyInvoisStatusPollJobExecutor> _logger;

    public MyInvoisStatusPollJobExecutor(
        EInvoiceProviderFactory eInvoiceProviderFactory,
        IInvoiceSubmissionService invoiceSubmissionService,
        ILogger<MyInvoisStatusPollJobExecutor> logger)
    {
        _eInvoiceProviderFactory = eInvoiceProviderFactory;
        _invoiceSubmissionService = invoiceSubmissionService;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(JobExecution job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing MyInvois status poll job {JobId}", job.Id);

        if (string.IsNullOrEmpty(job.PayloadJson))
            throw new ArgumentException("MyInvois status poll job requires payload with submissionHistoryId");

        Guid submissionHistoryId;
        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(job.PayloadJson);
            if (payload == null || !payload.TryGetValue("submissionHistoryId", out var el))
                throw new ArgumentException("MyInvois status poll payload must contain submissionHistoryId");
            var idStr = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
            if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out submissionHistoryId))
                throw new ArgumentException("Invalid submissionHistoryId in payload");
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid MyInvois status poll payload JSON", ex);
        }

        var submission = await _invoiceSubmissionService.GetSubmissionByHistoryIdAsync(submissionHistoryId, cancellationToken);
        if (submission == null || string.IsNullOrEmpty(submission.SubmissionId))
            throw new KeyNotFoundException($"Submission history {submissionHistoryId} not found or has no SubmissionId");

        var provider = await _eInvoiceProviderFactory.GetProviderAsync(cancellationToken);
        var statusResult = await provider.GetInvoiceStatusAsync(submission.SubmissionId, cancellationToken);

        if (!statusResult.Success)
        {
            _logger.LogWarning("Failed to get invoice status from MyInvois for submission {SubmissionHistoryId}: {Error}",
                submissionHistoryId, statusResult.ErrorMessage);
            throw new InvalidOperationException(statusResult.ErrorMessage ?? "MyInvois status check failed");
        }

        await _invoiceSubmissionService.UpdateSubmissionStatusAsync(
            submissionHistoryId,
            statusResult.Status,
            statusResult.RejectionReason,
            null,
            null,
            cancellationToken);

        _logger.LogInformation("MyInvois status poll job {JobId} completed: SubmissionHistoryId={SubmissionHistoryId}, Status={Status}",
            job.Id, submissionHistoryId, statusResult.Status);
        return true;
    }
}
