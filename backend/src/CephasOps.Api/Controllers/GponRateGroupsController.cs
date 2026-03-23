using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// GPON Rate Groups (Phase 1 — additive; does not affect payout resolution).
/// </summary>
[ApiController]
[Route("api/settings/gpon/rate-groups")]
[Authorize]
public class GponRateGroupsController : ControllerBase
{
    private readonly IRateGroupService _rateGroupService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<GponRateGroupsController> _logger;

    public GponRateGroupsController(
        IRateGroupService rateGroupService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<GponRateGroupsController> logger)
    {
        _rateGroupService = rateGroupService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>List rate groups.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<RateGroupDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<RateGroupDto>>>> List(
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        try
        {
            var list = await _rateGroupService.ListRateGroupsAsync(companyId, isActive, cancellationToken);
            return this.Success(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing rate groups");
            return this.InternalServerError<List<RateGroupDto>>(ex.Message);
        }
    }

    /// <summary>Get rate group by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RateGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RateGroupDto>>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var dto = await _rateGroupService.GetRateGroupByIdAsync(id, companyId, cancellationToken);
        if (dto == null)
            return this.NotFound<RateGroupDto>("Rate group not found.");
        return this.Success(dto);
    }

    /// <summary>Create rate group.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RateGroupDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RateGroupDto>>> Create(
        [FromBody] CreateRateGroupDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        try
        {
            var created = await _rateGroupService.CreateRateGroupAsync(dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(Get), new { id = created.Id }, created, "Rate group created.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rate group");
            return this.BadRequest<RateGroupDto>(ex.Message);
        }
    }

    /// <summary>Update rate group.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RateGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RateGroupDto>>> Update(
        Guid id,
        [FromBody] UpdateRateGroupDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        try
        {
            var updated = await _rateGroupService.UpdateRateGroupAsync(id, dto, companyId, cancellationToken);
            return this.Success(updated, "Rate group updated.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<RateGroupDto>("Rate group not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rate group");
            return this.BadRequest<RateGroupDto>(ex.Message);
        }
    }

    /// <summary>Delete rate group (fails if mappings exist).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        try
        {
            await _rateGroupService.DeleteRateGroupAsync(id, companyId, cancellationToken);
            return this.NoContent("Rate group deleted.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound("Rate group not found.");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rate group");
            return this.InternalServerError(ex.Message);
        }
    }
}
