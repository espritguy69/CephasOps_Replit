using CephasOps.Application.Billing.DTOs;
using CephasOps.Application.Billing.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Files.Services;
using CephasOps.Application.Settings.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Note: Invoice submission history endpoints are in InvoiceSubmissionsController

namespace CephasOps.Api.Controllers;

/// <summary>
/// Billing and invoice management endpoints
/// </summary>
[ApiController]
[Route("api/billing")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly IFileService _fileService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        IBillingService billingService,
        IDocumentGenerationService documentGenerationService,
        IFileService fileService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<BillingController> logger)
    {
        _billingService = billingService;
        _documentGenerationService = documentGenerationService;
        _fileService = fileService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get invoices with filtering
    /// </summary>
    /// <param name="status">Filter by status</param>
    /// <param name="partnerId">Filter by partner</param>
    /// <param name="fromDate">Filter from date</param>
    /// <param name="toDate">Filter to date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of invoices</returns>
    [HttpGet("invoices")]
    [ProducesResponseType(typeof(ApiResponse<List<InvoiceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<InvoiceDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<InvoiceDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<InvoiceDto>>>> GetInvoices(
        [FromQuery] string? status = null,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return this.Error<List<InvoiceDto>>("Company context required", 401);
        }

        try
        {
            var invoices = await _billingService.GetInvoicesAsync(companyId, status, partnerId, fromDate, toDate, cancellationToken);
            return this.Success(invoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices");
            return this.Error<List<InvoiceDto>>($"Failed to get invoices: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get invoice by ID
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Invoice details</returns>
    [HttpGet("invoices/{id}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> GetInvoice(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return this.Error<InvoiceDto>("Company context required", 401);
        }

        try
        {
            var invoice = await _billingService.GetInvoiceByIdAsync(id, companyId, cancellationToken);
            if (invoice == null)
            {
                return this.NotFound<InvoiceDto>($"Invoice with ID {id} not found");
            }

            return this.Success(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice: {InvoiceId}", id);
            return this.Error<InvoiceDto>($"Failed to get invoice: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create a new invoice
    /// </summary>
    /// <param name="dto">Invoice data (optional <c>idempotencyKey</c> for duplicate-safe create; replay returns existing invoice)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created invoice (201); on replay with same idempotency key returns same invoice</returns>
    /// <remarks>
    /// For retry-safe or duplicate-safe creation, set <c>idempotencyKey</c> in the request body (or send <c>X-Idempotency-Key</c> header).
    /// Repeated requests with the same key (same company) return the existing invoice instead of creating a duplicate.
    /// </remarks>
    [HttpPost("invoices")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> CreateInvoice(
        [FromBody] CreateInvoiceDto dto,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if ((companyId == null && !_currentUserService.IsSuperAdmin) || userId == null)
        {
            return this.Error<InvoiceDto>("Company and user context required", 401);
        }
        if (dto == null)
            return this.BadRequest<InvoiceDto>("Request body is required.");

        if (string.IsNullOrWhiteSpace(dto.IdempotencyKey) && Request.Headers.TryGetValue("X-Idempotency-Key", out var headerKey) && !string.IsNullOrWhiteSpace(headerKey.ToString()))
            dto.IdempotencyKey = headerKey.ToString().Trim();

        try
        {
            var invoice = await _billingService.CreateInvoiceAsync(dto, companyId, userId.Value, cancellationToken);
            return this.StatusCode(201, ApiResponse<InvoiceDto>.SuccessResponse(invoice, "Invoice created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice");
            return this.Error<InvoiceDto>($"Failed to create invoice: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Build suggested invoice line items from orders using BillingRatecard resolution.
    /// Does not create an invoice; returns line items that can be used in CreateInvoice or UpdateInvoice.
    /// </summary>
    /// <param name="request">Order IDs and optional reference date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("invoices/build-lines")]
    [ProducesResponseType(typeof(ApiResponse<BuildInvoiceLinesResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BuildInvoiceLinesResult>>> BuildInvoiceLinesFromOrders(
        [FromBody] BuildInvoiceLinesRequest request,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return this.Error<BuildInvoiceLinesResult>("Company context required", 401);
        }

        if (companyId == null)
        {
            return this.Error<BuildInvoiceLinesResult>("Company context required for building invoice lines", 401);
        }

        if (request?.OrderIds == null || request.OrderIds.Count == 0)
        {
            return this.Error<BuildInvoiceLinesResult>("OrderIds are required", 400);
        }

        try
        {
            var result = await _billingService.BuildInvoiceLinesFromOrdersAsync(
                request.OrderIds,
                companyId.Value,
                request.ReferenceDate,
                cancellationToken);
            return this.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building invoice lines from orders");
            return this.Error<BuildInvoiceLinesResult>($"Failed to build invoice lines: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update an existing invoice
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="dto">Updated invoice data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated invoice</returns>
    [HttpPut("invoices/{id}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> UpdateInvoice(
        Guid id,
        [FromBody] UpdateInvoiceDto dto,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return this.Error<InvoiceDto>("Company context required", 401);
        }

        try
        {
            var invoice = await _billingService.UpdateInvoiceAsync(id, dto, companyId, cancellationToken);
            return this.Success(invoice, "Invoice updated successfully");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<InvoiceDto>($"Invoice with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice: {InvoiceId}", id);
            return this.Error<InvoiceDto>($"Failed to update invoice: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete an invoice
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("invoices/{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteInvoice(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        }

        try
        {
            await _billingService.DeleteInvoiceAsync(id, companyId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Invoice deleted successfully"));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"Invoice with ID {id} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice: {InvoiceId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete invoice: {ex.Message}"));
        }
    }

    /// <summary>
    /// Generate invoice PDF (uses Document Templates)
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="templateId">Optional template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF file</returns>
    [HttpGet("invoices/{id}/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateInvoicePdf(
        Guid id,
        [FromQuery] Guid? templateId = null,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        }

        try
        {
            // For SuperAdmin (companyId null), derive company from invoice
            if (!companyId.HasValue)
            {
                companyId = await _billingService.GetInvoiceCompanyIdAsync(id, cancellationToken);
                if (!companyId.HasValue)
                {
                    return StatusCode(404, ApiResponse.ErrorResponse($"Invoice with ID {id} not found"));
                }
            }

            var generated = await _documentGenerationService.GenerateInvoiceDocumentAsync(id, companyId.Value, templateId, cancellationToken);
            var pdfBytes = await _fileService.GetFileContentAsync(generated.FileId, companyId, cancellationToken);
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                return StatusCode(404, ApiResponse.ErrorResponse($"Invoice PDF for {id} not found or empty"));
            }

            var invoice = await _billingService.GetInvoiceByIdAsync(id, companyId, cancellationToken);
            var fileName = invoice != null ? $"invoice-{invoice.InvoiceNumber}.pdf" : $"invoice-{id}.pdf";

            // File downloads don't use ApiResponse envelope - they return file content directly
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"Invoice with ID {id} not found"));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(400, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice PDF: {InvoiceId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to generate invoice PDF: {ex.Message}"));
        }
    }

    /// <summary>
    /// Render invoice as HTML for print preview
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="templateId">Optional template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTML content</returns>
    [HttpGet("invoices/{id}/preview-html")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("text/html")]
    public async Task<IActionResult> GetInvoicePreviewHtml(
        Guid id,
        [FromQuery] Guid? templateId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        }

        try
        {
            if (!companyId.HasValue)
            {
                companyId = await _billingService.GetInvoiceCompanyIdAsync(id, cancellationToken);
                if (!companyId.HasValue)
                {
                    return StatusCode(404, ApiResponse.ErrorResponse($"Invoice with ID {id} not found"));
                }
            }

            var html = await _documentGenerationService.RenderInvoiceHtmlAsync(id, companyId.Value, templateId, cancellationToken);
            return Content(html, "text/html");
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"Invoice with ID {id} not found"));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(400, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering invoice HTML: {InvoiceId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to render invoice preview: {ex.Message}"));
        }
    }
}

