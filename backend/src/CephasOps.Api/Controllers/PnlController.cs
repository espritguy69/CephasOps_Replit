using CephasOps.Api.Common;
using CephasOps.Application.Pnl.DTOs;
using CephasOps.Application.Pnl.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// P&amp;L management endpoints
/// </summary>
[ApiController]
[Route("api/pnl")]
[Authorize(Policy = "Reports")]
public class PnlController : ControllerBase
{
    private readonly IPnlService _pnlService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<PnlController> _logger;

    public PnlController(
        IPnlService pnlService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<PnlController> logger)
    {
        _pnlService = pnlService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    /// <summary>
    /// Get P&amp;L summary
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<PnlSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PnlSummaryDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<PnlSummaryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PnlSummaryDto>>> GetPnlSummary(
        [FromQuery] Guid? periodId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? departmentId = null, // Validated for RBAC; not yet used in PnlFact (CostCentreId)
        CancellationToken cancellationToken = default)
    {
        if (departmentId.HasValue || _departmentRequestContext.DepartmentId.HasValue)
        {
            try
            {
                await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                return this.Error<PnlSummaryDto>("You do not have access to this department", 403);
            }
        }

        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var summary = await _pnlService.GetPnlSummaryAsync(companyId, periodId, startDate, endDate, cancellationToken);
            return this.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting P&L summary");
            return this.Error<PnlSummaryDto>("Failed to get P&L summary", 500);
        }
    }

    /// <summary>
    /// Get P&amp;L order details
    /// </summary>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(ApiResponse<List<PnlOrderDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<PnlOrderDetailDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<PnlOrderDetailDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<PnlOrderDetailDto>>>> GetPnlOrderDetails(
        [FromQuery] Guid? orderId = null,
        [FromQuery] Guid? periodId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var details = await _pnlService.GetPnlOrderDetailsAsync(companyId, orderId, periodId, startDate, endDate, cancellationToken);
            return this.Success(details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting P&L order details");
            return this.Error<List<PnlOrderDetailDto>>("Failed to get P&L order details", 500);
        }
    }

    /// <summary>
    /// Get P&amp;L detail per order with enhanced filtering
    /// </summary>
    [HttpGet("orders/detail")]
    [ProducesResponseType(typeof(ApiResponse<List<PnlDetailPerOrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<PnlDetailPerOrderDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<PnlDetailPerOrderDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<PnlDetailPerOrderDto>>>> GetPnlDetailPerOrder(
        [FromQuery] Guid? orderId = null,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? serviceInstallerId = null,
        [FromQuery] string? orderType = null,
        [FromQuery] string? kpiResult = null,
        [FromQuery] string? period = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        Guid? departmentScope;
        try
        {
            departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return this.Error<List<PnlDetailPerOrderDto>>("You do not have access to this department", 403);
        }

        try
        {
            var details = await _pnlService.GetPnlDetailPerOrderAsync(
                companyId, 
                orderId, 
                partnerId, 
                departmentScope, 
                serviceInstallerId, 
                orderType, 
                kpiResult, 
                period, 
                cancellationToken);
            return this.Success(details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting P&L detail per order");
            return this.Error<List<PnlDetailPerOrderDto>>("Failed to get P&L detail per order", 500);
        }
    }

    /// <summary>
    /// Rebuild P&amp;L for a period
    /// </summary>
    [HttpPost("rebuild")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> RebuildPnl(
        [FromBody] RebuildPnlDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            await _pnlService.RebuildPnlAsync(companyId, dto.Period, cancellationToken);
            return this.Success("P&L rebuild initiated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding P&L");
            return StatusCode(500, ApiResponse.ErrorResponse("Failed to rebuild P&L"));
        }
    }

    /// <summary>
    /// Get P&amp;L periods
    /// </summary>
    [HttpGet("periods")]
    [ProducesResponseType(typeof(ApiResponse<List<PnlPeriodDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<PnlPeriodDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<PnlPeriodDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<PnlPeriodDto>>>> GetPnlPeriods(
        [FromQuery] string? year = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var periods = await _pnlService.GetPnlPeriodsAsync(companyId, year, cancellationToken);
            return this.Success(periods);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting P&L periods");
            return this.Error<List<PnlPeriodDto>>("Failed to get P&L periods", 500);
        }
    }

    /// <summary>
    /// Get overhead entries
    /// </summary>
    [HttpGet("overheads")]
    [ProducesResponseType(typeof(ApiResponse<List<OverheadEntryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<OverheadEntryDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<OverheadEntryDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<OverheadEntryDto>>>> GetOverheadEntries(
        [FromQuery] Guid? costCentreId = null,
        [FromQuery] string? period = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var entries = await _pnlService.GetOverheadEntriesAsync(companyId, costCentreId, period, cancellationToken);
            return this.Success(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overhead entries");
            return this.Error<List<OverheadEntryDto>>("Failed to get overhead entries", 500);
        }
    }

    /// <summary>
    /// Create overhead entry
    /// </summary>
    [HttpPost("overheads")]
    [ProducesResponseType(typeof(ApiResponse<OverheadEntryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<OverheadEntryDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<OverheadEntryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<OverheadEntryDto>>> CreateOverheadEntry(
        [FromBody] CreateOverheadEntryDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Error<OverheadEntryDto>("User context required", 401);
        }

        try
        {
            var entry = await _pnlService.CreateOverheadEntryAsync(dto, companyId, userId.Value, cancellationToken);
            return CreatedAtAction(nameof(GetOverheadEntries), new { }, ApiResponse<OverheadEntryDto>.SuccessResponse(entry, "Overhead entry created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating overhead entry");
            return this.Error<OverheadEntryDto>("Failed to create overhead entry", 500);
        }
    }

    /// <summary>
    /// Delete overhead entry
    /// </summary>
    [HttpDelete("overheads/{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteOverheadEntry(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            await _pnlService.DeleteOverheadEntryAsync(id, companyId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"Overhead entry with ID {id} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting overhead entry: {EntryId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse("Failed to delete overhead entry"));
        }
    }
}

/// <summary>
/// Rebuild P&amp;L request DTO
/// </summary>
public class RebuildPnlDto
{
    public string Period { get; set; } = string.Empty;
}

