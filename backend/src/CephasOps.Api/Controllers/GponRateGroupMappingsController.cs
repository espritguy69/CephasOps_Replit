using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// GPON Rate Group mappings: order type/subtype → rate group (Phase 1 — does not affect payout resolution).
/// </summary>
[ApiController]
[Route("api/settings/gpon/rate-group-mappings")]
[Authorize]
public class GponRateGroupMappingsController : ControllerBase
{
    private readonly IRateGroupService _rateGroupService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<GponRateGroupMappingsController> _logger;

    public GponRateGroupMappingsController(
        IRateGroupService rateGroupService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<GponRateGroupMappingsController> logger)
    {
        _rateGroupService = rateGroupService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>List mappings, optionally filtered by rateGroupId or orderTypeId.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<OrderTypeSubtypeRateGroupMappingDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<OrderTypeSubtypeRateGroupMappingDto>>>> List(
        [FromQuery] Guid? rateGroupId = null,
        [FromQuery] Guid? orderTypeId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        try
        {
            var list = await _rateGroupService.ListMappingsAsync(companyId, rateGroupId, orderTypeId, cancellationToken);
            return this.Success(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing rate group mappings");
            return this.InternalServerError<List<OrderTypeSubtypeRateGroupMappingDto>>(ex.Message);
        }
    }

    /// <summary>Assign a rate group to an order type (and optional subtype). Creates or updates mapping.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderTypeSubtypeRateGroupMappingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<OrderTypeSubtypeRateGroupMappingDto>>> Assign(
        [FromBody] AssignRateGroupToOrderTypeSubtypeDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        try
        {
            var mapping = await _rateGroupService.AssignRateGroupToOrderTypeSubtypeAsync(dto, companyId, cancellationToken);
            return this.Success(mapping, "Mapping saved.");
        }
        catch (ArgumentException ex)
        {
            return this.BadRequest<OrderTypeSubtypeRateGroupMappingDto>(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return this.Forbidden<OrderTypeSubtypeRateGroupMappingDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning rate group mapping");
            return this.InternalServerError<OrderTypeSubtypeRateGroupMappingDto>(ex.Message);
        }
    }

    /// <summary>Remove a mapping.</summary>
    [HttpDelete("{mappingId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Unassign(Guid mappingId, CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        try
        {
            await _rateGroupService.UnassignRateGroupMappingAsync(mappingId, companyId, cancellationToken);
            return this.NoContent("Mapping removed.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound("Mapping not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing rate group mapping");
            return this.InternalServerError(ex.Message);
        }
    }
}
