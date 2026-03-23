using CephasOps.Application.Billing.DTOs;
using CephasOps.Application.Billing.Services;
using CephasOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Invoice submission history management endpoints
/// </summary>
[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoiceSubmissionsController : ControllerBase
{
    private readonly IInvoiceSubmissionService _invoiceSubmissionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<InvoiceSubmissionsController> _logger;

    public InvoiceSubmissionsController(
        IInvoiceSubmissionService invoiceSubmissionService,
        ICurrentUserService currentUserService,
        ILogger<InvoiceSubmissionsController> logger)
    {
        _invoiceSubmissionService = invoiceSubmissionService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get all submission history for an invoice
    /// </summary>
    [HttpGet("{invoiceId}/submission-history")]
    [ProducesResponseType(typeof(ApiResponse<List<InvoiceSubmissionHistoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<InvoiceSubmissionHistoryDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<InvoiceSubmissionHistoryDto>>>> GetSubmissionHistory(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await _invoiceSubmissionService.GetSubmissionHistoryAsync(
                invoiceId, cancellationToken);
            return this.Success(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submission history for invoice {InvoiceId}", invoiceId);
            return this.InternalServerError<List<InvoiceSubmissionHistoryDto>>($"Failed to get submission history: {ex.Message}");
        }
    }

    /// <summary>
    /// Get active submission for an invoice
    /// </summary>
    [HttpGet("{invoiceId}/submission-history/active")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceSubmissionHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceSubmissionHistoryDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceSubmissionHistoryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<InvoiceSubmissionHistoryDto>>> GetActiveSubmission(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var submission = await _invoiceSubmissionService.GetActiveSubmissionAsync(
                invoiceId, cancellationToken);
            
            if (submission == null)
            {
                return this.NotFound<InvoiceSubmissionHistoryDto>("No active submission found for this invoice");
            }

            return this.Success(submission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active submission for invoice {InvoiceId}", invoiceId);
            return this.InternalServerError<InvoiceSubmissionHistoryDto>($"Failed to get active submission: {ex.Message}");
        }
    }

    /// <summary>
    /// Record a new invoice submission to portal
    /// </summary>
    [HttpPost("{invoiceId}/submission-history")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceSubmissionHistoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceSubmissionHistoryDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceSubmissionHistoryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<InvoiceSubmissionHistoryDto>>> RecordSubmission(
        Guid invoiceId,
        [FromBody] RecordSubmissionDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<InvoiceSubmissionHistoryDto>("User context required");
        }

        try
        {
            var submission = await _invoiceSubmissionService.RecordSubmissionAsync(
                invoiceId: invoiceId,
                submissionId: dto.SubmissionId,
                portalType: dto.PortalType ?? "MyInvois",
                submittedByUserId: userId.Value,
                responseMessage: dto.ResponseMessage,
                responseCode: dto.ResponseCode,
                cancellationToken: cancellationToken);

            return this.CreatedAtAction(
                nameof(GetActiveSubmission),
                new { invoiceId },
                submission,
                "Invoice submission recorded successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<InvoiceSubmissionHistoryDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording submission for invoice {InvoiceId}", invoiceId);
            return this.InternalServerError<InvoiceSubmissionHistoryDto>($"Failed to record submission: {ex.Message}");
        }
    }

    /// <summary>
    /// Update submission status (e.g., when payment is rejected)
    /// </summary>
    [HttpPut("submission-history/{submissionHistoryId}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceSubmissionHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceSubmissionHistoryDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceSubmissionHistoryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<InvoiceSubmissionHistoryDto>>> UpdateSubmissionStatus(
        Guid submissionHistoryId,
        [FromBody] UpdateSubmissionStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var submission = await _invoiceSubmissionService.UpdateSubmissionStatusAsync(
                submissionHistoryId: submissionHistoryId,
                status: dto.Status,
                rejectionReason: dto.RejectionReason,
                paymentStatus: dto.PaymentStatus,
                paymentReference: dto.PaymentReference,
                cancellationToken: cancellationToken);

            return this.Success(submission, "Submission status updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<InvoiceSubmissionHistoryDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating submission status {SubmissionHistoryId}", submissionHistoryId);
            return this.InternalServerError<InvoiceSubmissionHistoryDto>($"Failed to update submission status: {ex.Message}");
        }
    }

    /// <summary>
    /// Submit invoice to e-invoice portal (MyInvois)
    /// POST /api/invoice-submissions/{invoiceId}/submit
    /// </summary>
    [HttpPost("{invoiceId}/submit")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceSubmissionHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceSubmissionHistoryDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceSubmissionHistoryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<InvoiceSubmissionHistoryDto>>> SubmitInvoiceToPortal(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                return this.Unauthorized<InvoiceSubmissionHistoryDto>("User context required");
            }

            var submission = await _invoiceSubmissionService.SubmitInvoiceToPortalAsync(
                invoiceId,
                userId.Value,
                cancellationToken);

            return this.Success(submission, "Invoice submitted to e-invoice portal successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<InvoiceSubmissionHistoryDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit invoice {InvoiceId} to portal", invoiceId);
            return this.Error<InvoiceSubmissionHistoryDto>($"Failed to submit invoice: {ex.Message}", 500);
        }
    }
}

/// <summary>
/// DTO for recording invoice submission
/// </summary>
public class RecordSubmissionDto
{
    public string SubmissionId { get; set; } = string.Empty;
    public string? PortalType { get; set; }
    public string? ResponseMessage { get; set; }
    public string? ResponseCode { get; set; }
}

/// <summary>
/// DTO for updating submission status
/// </summary>
public class UpdateSubmissionStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public string? PaymentStatus { get; set; }
    public string? PaymentReference { get; set; }
}

