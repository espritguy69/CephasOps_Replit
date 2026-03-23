using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// GPON Order Category → Service Profile mappings. One category can map to at most one profile per company.
/// </summary>
[ApiController]
[Route("api/settings/gpon/service-profile-mappings")]
[Authorize]
public class ServiceProfileMappingsController : ControllerBase
{
    private readonly IOrderCategoryServiceProfileService _service;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ServiceProfileMappingsController> _logger;

    public ServiceProfileMappingsController(
        IOrderCategoryServiceProfileService service,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<ServiceProfileMappingsController> logger)
    {
        _service = service;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<OrderCategoryServiceProfileDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<OrderCategoryServiceProfileDto>>>> List(
        [FromQuery] Guid? serviceProfileId = null,
        [FromQuery] Guid? orderCategoryId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var filter = new OrderCategoryServiceProfileListFilter
        {
            ServiceProfileId = serviceProfileId,
            OrderCategoryId = orderCategoryId
        };
        try
        {
            var list = await _service.ListAsync(companyId, filter, cancellationToken);
            return this.Success(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing service profile mappings");
            return this.InternalServerError<List<OrderCategoryServiceProfileDto>>(ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryServiceProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OrderCategoryServiceProfileDto>>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var dto = await _service.GetByIdAsync(id, companyId, cancellationToken);
        if (dto == null)
            return this.NotFound<OrderCategoryServiceProfileDto>("Mapping not found.");
        return this.Success(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryServiceProfileDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<OrderCategoryServiceProfileDto>>> Create(
        [FromBody] CreateOrderCategoryServiceProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        try
        {
            var created = await _service.CreateAsync(dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(Get), new { id = created.Id }, created, "Mapping created.");
        }
        catch (ArgumentException ex)
        {
            return this.BadRequest<OrderCategoryServiceProfileDto>(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return this.BadRequest<OrderCategoryServiceProfileDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<OrderCategoryServiceProfileDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating service profile mapping");
            return this.InternalServerError<OrderCategoryServiceProfileDto>(ex.Message);
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
            return this.NoContent("Mapping deleted.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound("Mapping not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting service profile mapping");
            return this.InternalServerError(ex.Message);
        }
    }
}
