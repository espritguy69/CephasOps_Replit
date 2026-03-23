using CephasOps.Application.Billing.DTOs;
using CephasOps.Application.Billing.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Domain.Billing.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Payment management endpoints
/// </summary>
[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get payments list
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<PaymentDto>>>> GetPayments(
        [FromQuery] PaymentType? paymentType = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] bool? isReconciled = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var payments = await _paymentService.GetPaymentsAsync(companyId, paymentType, fromDate, toDate, isReconciled, cancellationToken);
            return this.Success(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments");
            return this.InternalServerError<List<PaymentDto>>($"Failed to get payments: {ex.Message}");
        }
    }

    /// <summary>
    /// Get payment summary
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<PaymentSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaymentSummaryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PaymentSummaryDto>>> GetPaymentSummary(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var summary = await _paymentService.GetPaymentSummaryAsync(companyId, fromDate, toDate, cancellationToken);
            return this.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment summary");
            return this.InternalServerError<PaymentSummaryDto>($"Failed to get payment summary: {ex.Message}");
        }
    }

    /// <summary>
    /// Get accounting dashboard
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<AccountingDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AccountingDashboardDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AccountingDashboardDto>>> GetAccountingDashboard(CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var dashboard = await _paymentService.GetAccountingDashboardAsync(companyId, cancellationToken);
            return this.Success(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounting dashboard");
            return this.InternalServerError<AccountingDashboardDto>($"Failed to get accounting dashboard: {ex.Message}");
        }
    }

    /// <summary>
    /// Get payment by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> GetPayment(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var payment = await _paymentService.GetPaymentByIdAsync(id, companyId, cancellationToken);
            if (payment == null)
            {
                return this.NotFound<PaymentDto>($"Payment with ID {id} not found");
            }
            return this.Success(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment {PaymentId}", id);
            return this.InternalServerError<PaymentDto>($"Failed to get payment: {ex.Message}");
        }
    }

    /// <summary>
    /// Create new payment
    /// </summary>
    /// <param name="dto">Payment data (optional <c>idempotencyKey</c> for duplicate-safe create; replay returns existing payment)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created payment (201); on replay with same idempotency key returns same payment</returns>
    /// <remarks>
    /// For retry-safe or duplicate-safe creation, set <c>idempotencyKey</c> in the request body (or send <c>X-Idempotency-Key</c> header).
    /// Repeated requests with the same key (same company) return the existing payment instead of creating a duplicate.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> CreatePayment(
        [FromBody] CreatePaymentDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId ?? Guid.Empty;
        if (dto == null)
            return this.BadRequest<PaymentDto>("Request body is required.");

        if (string.IsNullOrWhiteSpace(dto.IdempotencyKey) && Request.Headers.TryGetValue("X-Idempotency-Key", out var headerKey) && !string.IsNullOrWhiteSpace(headerKey.ToString()))
            dto.IdempotencyKey = headerKey.ToString().Trim();

        try
        {
            var payment = await _paymentService.CreatePaymentAsync(dto, companyId, userId, cancellationToken);
            return this.CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment, "Payment created successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<PaymentDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment");
            return this.InternalServerError<PaymentDto>($"Failed to create payment: {ex.Message}");
        }
    }

    /// <summary>
    /// Update payment
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> UpdatePayment(
        Guid id,
        [FromBody] UpdatePaymentDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var payment = await _paymentService.UpdatePaymentAsync(id, dto, companyId, cancellationToken);
            return this.Success(payment, "Payment updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<PaymentDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<PaymentDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment {PaymentId}", id);
            return this.InternalServerError<PaymentDto>($"Failed to update payment: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete payment
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeletePayment(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            await _paymentService.DeletePaymentAsync(id, companyId, cancellationToken);
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
            _logger.LogError(ex, "Error deleting payment {PaymentId}", id);
            return this.InternalServerError($"Failed to delete payment: {ex.Message}");
        }
    }

    /// <summary>
    /// Void payment
    /// </summary>
    [HttpPost("{id}/void")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> VoidPayment(
        Guid id,
        [FromBody] VoidPaymentDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var payment = await _paymentService.VoidPaymentAsync(id, dto, companyId, cancellationToken);
            return this.Success(payment, "Payment voided successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<PaymentDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voiding payment {PaymentId}", id);
            return this.InternalServerError<PaymentDto>($"Failed to void payment: {ex.Message}");
        }
    }

    /// <summary>
    /// Reconcile payment
    /// </summary>
    [HttpPost("{id}/reconcile")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> ReconcilePayment(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var payment = await _paymentService.ReconcilePaymentAsync(id, companyId, cancellationToken);
            return this.Success(payment, "Payment reconciled successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<PaymentDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reconciling payment {PaymentId}", id);
            return this.InternalServerError<PaymentDto>($"Failed to reconcile payment: {ex.Message}");
        }
    }
}
