using CephasOps.Application.Billing.DTOs;
using CephasOps.Application.Billing.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Domain.Billing.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Supplier Invoice management endpoints
/// </summary>
[ApiController]
[Route("api/supplier-invoices")]
[Authorize]
public class SupplierInvoicesController : ControllerBase
{
    private readonly ISupplierInvoiceService _supplierInvoiceService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SupplierInvoicesController> _logger;

    public SupplierInvoicesController(
        ISupplierInvoiceService supplierInvoiceService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<SupplierInvoicesController> logger)
    {
        _supplierInvoiceService = supplierInvoiceService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get supplier invoices list
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SupplierInvoiceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<SupplierInvoiceDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<SupplierInvoiceDto>>>> GetSupplierInvoices(
        [FromQuery] SupplierInvoiceStatus? status = null,
        [FromQuery] string? supplierName = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var invoices = await _supplierInvoiceService.GetSupplierInvoicesAsync(companyId, status, supplierName, fromDate, toDate, cancellationToken);
            return this.Success(invoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier invoices");
            return this.InternalServerError<List<SupplierInvoiceDto>>($"Failed to get supplier invoices: {ex.Message}");
        }
    }

    /// <summary>
    /// Get supplier invoice summary
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceSummaryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SupplierInvoiceSummaryDto>>> GetSupplierInvoiceSummary(CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var summary = await _supplierInvoiceService.GetSupplierInvoiceSummaryAsync(companyId, cancellationToken);
            return this.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier invoice summary");
            return this.InternalServerError<SupplierInvoiceSummaryDto>($"Failed to get supplier invoice summary: {ex.Message}");
        }
    }

    /// <summary>
    /// Get overdue invoices
    /// </summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(ApiResponse<List<SupplierInvoiceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<SupplierInvoiceDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<SupplierInvoiceDto>>>> GetOverdueInvoices(CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var invoices = await _supplierInvoiceService.GetOverdueInvoicesAsync(companyId, cancellationToken);
            return this.Success(invoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overdue invoices");
            return this.InternalServerError<List<SupplierInvoiceDto>>($"Failed to get overdue invoices: {ex.Message}");
        }
    }

    /// <summary>
    /// Get supplier invoice by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SupplierInvoiceDto>>> GetSupplierInvoice(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var invoice = await _supplierInvoiceService.GetSupplierInvoiceByIdAsync(id, companyId, cancellationToken);
            if (invoice == null)
            {
                return this.NotFound<SupplierInvoiceDto>($"Supplier Invoice with ID {id} not found");
            }
            return this.Success(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier invoice {InvoiceId}", id);
            return this.InternalServerError<SupplierInvoiceDto>($"Failed to get supplier invoice: {ex.Message}");
        }
    }

    /// <summary>
    /// Create new supplier invoice
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SupplierInvoiceDto>>> CreateSupplierInvoice(
        [FromBody] CreateSupplierInvoiceDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId ?? Guid.Empty;

        try
        {
            var invoice = await _supplierInvoiceService.CreateSupplierInvoiceAsync(dto, companyId, userId, cancellationToken);
            return this.CreatedAtAction(nameof(GetSupplierInvoice), new { id = invoice.Id }, invoice, "Supplier invoice created successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<SupplierInvoiceDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier invoice");
            return this.InternalServerError<SupplierInvoiceDto>($"Failed to create supplier invoice: {ex.Message}");
        }
    }

    /// <summary>
    /// Update supplier invoice
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SupplierInvoiceDto>>> UpdateSupplierInvoice(
        Guid id,
        [FromBody] UpdateSupplierInvoiceDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var invoice = await _supplierInvoiceService.UpdateSupplierInvoiceAsync(id, dto, companyId, cancellationToken);
            return this.Success(invoice, "Supplier invoice updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<SupplierInvoiceDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier invoice {InvoiceId}", id);
            return this.InternalServerError<SupplierInvoiceDto>($"Failed to update supplier invoice: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete supplier invoice
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteSupplierInvoice(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            await _supplierInvoiceService.DeleteSupplierInvoiceAsync(id, companyId, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting supplier invoice {InvoiceId}", id);
            return this.InternalServerError($"Failed to delete supplier invoice: {ex.Message}");
        }
    }

    /// <summary>
    /// Approve supplier invoice
    /// </summary>
    [HttpPost("{id}/approve")]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SupplierInvoiceDto>>> ApproveSupplierInvoice(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId ?? Guid.Empty;

        try
        {
            var invoice = await _supplierInvoiceService.ApproveSupplierInvoiceAsync(id, companyId, userId, cancellationToken);
            return this.Success(invoice, "Supplier invoice approved successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<SupplierInvoiceDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving supplier invoice {InvoiceId}", id);
            return this.InternalServerError<SupplierInvoiceDto>($"Failed to approve supplier invoice: {ex.Message}");
        }
    }
}
