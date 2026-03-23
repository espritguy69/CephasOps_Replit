using CephasOps.Api.Common;
using CephasOps.Application.Authorization;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// GPON Base Work Rates (Phase 2 — additive; does not affect payout resolution).
/// </summary>
[ApiController]
[Route("api/settings/gpon/base-work-rates")]
[Authorize]
public class GponBaseWorkRatesController : ControllerBase
{
    private readonly IBaseWorkRateService _baseWorkRateService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IFieldLevelSecurityFilter _fieldLevelSecurity;
    private readonly ILogger<GponBaseWorkRatesController> _logger;

    public GponBaseWorkRatesController(
        IBaseWorkRateService baseWorkRateService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IFieldLevelSecurityFilter fieldLevelSecurity,
        ILogger<GponBaseWorkRatesController> logger)
    {
        _baseWorkRateService = baseWorkRateService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _fieldLevelSecurity = fieldLevelSecurity;
        _logger = logger;
    }

    /// <summary>List base work rates with optional filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<BaseWorkRateDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<BaseWorkRateDto>>>> List(
        [FromQuery] Guid? rateGroupId = null,
        [FromQuery] Guid? orderCategoryId = null,
        [FromQuery] Guid? serviceProfileId = null,
        [FromQuery] Guid? installationMethodId = null,
        [FromQuery] Guid? orderSubtypeId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var filter = new BaseWorkRateListFilter
        {
            RateGroupId = rateGroupId,
            OrderCategoryId = orderCategoryId,
            ServiceProfileId = serviceProfileId,
            InstallationMethodId = installationMethodId,
            OrderSubtypeId = orderSubtypeId,
            IsActive = isActive
        };
        try
        {
            var list = await _baseWorkRateService.ListAsync(companyId, filter, cancellationToken);
            await _fieldLevelSecurity.ApplyBaseWorkRateDtosAsync(list, cancellationToken);
            return this.Success(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing base work rates");
            return this.InternalServerError<List<BaseWorkRateDto>>(ex.Message);
        }
    }

    /// <summary>Get base work rate by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BaseWorkRateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BaseWorkRateDto>>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var dto = await _baseWorkRateService.GetByIdAsync(id, companyId, cancellationToken);
        if (dto == null)
            return this.NotFound<BaseWorkRateDto>("Base work rate not found.");
        await _fieldLevelSecurity.ApplyBaseWorkRateDtoAsync(dto, cancellationToken);
        return this.Success(dto);
    }

    /// <summary>Create base work rate.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BaseWorkRateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<BaseWorkRateDto>>> Create(
        [FromBody] CreateBaseWorkRateDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        try
        {
            var created = await _baseWorkRateService.CreateAsync(dto, companyId, cancellationToken);
            await _fieldLevelSecurity.ApplyBaseWorkRateDtoAsync(created, cancellationToken);
            return this.CreatedAtAction(nameof(Get), new { id = created.Id }, created, "Base work rate created.");
        }
        catch (ArgumentException ex)
        {
            return this.BadRequest<BaseWorkRateDto>(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return this.BadRequest<BaseWorkRateDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<BaseWorkRateDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating base work rate");
            return this.InternalServerError<BaseWorkRateDto>(ex.Message);
        }
    }

    /// <summary>Update base work rate. Use clear flags to remove optional dimensions.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BaseWorkRateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BaseWorkRateDto>>> Update(
        Guid id,
        [FromBody] UpdateBaseWorkRateDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        try
        {
            var updated = await _baseWorkRateService.UpdateAsync(id, dto, companyId, cancellationToken);
            await _fieldLevelSecurity.ApplyBaseWorkRateDtoAsync(updated, cancellationToken);
            return this.Success(updated, "Base work rate updated.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<BaseWorkRateDto>("Base work rate not found.");
        }
        catch (ArgumentException ex)
        {
            return this.BadRequest<BaseWorkRateDto>(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return this.BadRequest<BaseWorkRateDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<BaseWorkRateDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating base work rate");
            return this.InternalServerError<BaseWorkRateDto>(ex.Message);
        }
    }

    /// <summary>Delete (soft-deactivate) base work rate.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        try
        {
            await _baseWorkRateService.DeleteAsync(id, companyId, cancellationToken);
            return this.NoContent("Base work rate deleted.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound("Base work rate not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting base work rate");
            return this.InternalServerError(ex.Message);
        }
    }
}
