using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Pnl.DTOs;
using CephasOps.Application.Pnl.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Persisted financial alerts (dashboard / list). For per-order computed alerts use GET api/orders/{id}/financial-alerts.
/// </summary>
[ApiController]
[Route("api/financial-alerts")]
[Authorize(Policy = "Orders")]
public class FinancialAlertsController : ControllerBase
{
    private readonly IOrderProfitAlertService _orderProfitAlertService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<FinancialAlertsController> _logger;

    public FinancialAlertsController(
        IOrderProfitAlertService orderProfitAlertService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<FinancialAlertsController> logger)
    {
        _orderProfitAlertService = orderProfitAlertService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// List persisted financial alerts (optionally filtered by order, severity, date range).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PersistedOrderFinancialAlertDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<PersistedOrderFinancialAlertDto>>>> GetFinancialAlerts(
        [FromQuery] Guid? orderId = null,
        [FromQuery] string? severity = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }
        if (companyId == null)
        {
            return this.Error<List<PersistedOrderFinancialAlertDto>>("Company context required for financial alerts", 401);
        }

        try
        {
            var query = new ListOrderFinancialAlertsQuery
            {
                CompanyId = companyId.Value,
                OrderId = orderId,
                Severity = severity,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                ActiveOnly = activeOnly
            };
            var list = await _orderProfitAlertService.GetPersistedAlertsAsync(query, cancellationToken);
            return this.Success(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing financial alerts");
            return this.Error<List<PersistedOrderFinancialAlertDto>>($"Failed to list financial alerts: {ex.Message}", 500);
        }
    }
}
