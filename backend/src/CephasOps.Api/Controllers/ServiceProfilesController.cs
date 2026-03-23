using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// GPON Service Profiles — group order categories into service families for pricing (e.g. RESIDENTIAL_FIBER, BUSINESS_FIBER).
/// Foundation only; engine integration is a later phase.
/// </summary>
[ApiController]
[Route("api/settings/gpon/service-profiles")]
[Authorize]
public class ServiceProfilesController : ControllerBase
{
    private readonly IServiceProfileService _service;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ServiceProfilesController> _logger;

    public ServiceProfilesController(
        IServiceProfileService service,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<ServiceProfilesController> logger)
    {
        _service = service;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ServiceProfileDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ServiceProfileDto>>>> List(
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var filter = new ServiceProfileListFilter { IsActive = isActive, Search = search };
        try
        {
            var list = await _service.ListAsync(companyId, filter, cancellationToken);
            return this.Success(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing service profiles");
            return this.InternalServerError<List<ServiceProfileDto>>(ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ServiceProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ServiceProfileDto>>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var dto = await _service.GetByIdAsync(id, companyId, cancellationToken);
        if (dto == null)
            return this.NotFound<ServiceProfileDto>("Service profile not found.");
        return this.Success(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ServiceProfileDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ServiceProfileDto>>> Create(
        [FromBody] CreateServiceProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        try
        {
            var created = await _service.CreateAsync(dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(Get), new { id = created.Id }, created, "Service profile created.");
        }
        catch (ArgumentException ex)
        {
            return this.BadRequest<ServiceProfileDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<ServiceProfileDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating service profile");
            return this.InternalServerError<ServiceProfileDto>(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ServiceProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ServiceProfileDto>>> Update(
        Guid id,
        [FromBody] UpdateServiceProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        try
        {
            var updated = await _service.UpdateAsync(id, dto, companyId, cancellationToken);
            return this.Success(updated, "Service profile updated.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<ServiceProfileDto>("Service profile not found.");
        }
        catch (ArgumentException ex)
        {
            return this.BadRequest<ServiceProfileDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<ServiceProfileDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating service profile");
            return this.InternalServerError<ServiceProfileDto>(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        try
        {
            await _service.DeleteAsync(id, companyId, cancellationToken);
            return this.NoContent("Service profile deleted.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound("Service profile not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting service profile");
            return this.InternalServerError(ex.Message);
        }
    }
}
