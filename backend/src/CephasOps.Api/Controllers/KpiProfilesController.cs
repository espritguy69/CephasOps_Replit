using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// KPI profiles management endpoints
/// </summary>
[ApiController]
[Route("api/kpi-profiles")]
[Authorize]
public class KpiProfilesController : ControllerBase
{
    private readonly IKpiProfileService _kpiProfileService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<KpiProfilesController> _logger;

    public KpiProfilesController(
        IKpiProfileService kpiProfileService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<KpiProfilesController> logger)
    {
        _kpiProfileService = kpiProfileService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get KPI profiles
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<KpiProfileDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<KpiProfileDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<KpiProfileDto>>>> GetKpiProfiles(
        [FromQuery] string? orderType = null,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] Guid? installationMethodId = null,
        [FromQuery] Guid? buildingTypeId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var profiles = await _kpiProfileService.GetProfilesAsync(
                companyId, orderType, partnerId, installationMethodId, buildingTypeId, isActive, cancellationToken);
            return this.Success(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting KPI profiles");
            return this.InternalServerError<List<KpiProfileDto>>($"Failed to get KPI profiles: {ex.Message}");
        }
    }

    /// <summary>
    /// Get KPI profile by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<KpiProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<KpiProfileDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<KpiProfileDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<KpiProfileDto>>> GetKpiProfile(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var profile = await _kpiProfileService.GetProfileByIdAsync(id, companyId, cancellationToken);
            if (profile == null)
            {
                return this.NotFound<KpiProfileDto>($"KPI profile with ID {id} not found");
            }

            return this.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting KPI profile: {ProfileId}", id);
            return this.InternalServerError<KpiProfileDto>($"Failed to get KPI profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Get effective KPI profile for order context
    /// </summary>
    [HttpGet("effective")]
    [ProducesResponseType(typeof(ApiResponse<KpiProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<KpiProfileDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<KpiProfileDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<KpiProfileDto>>> GetEffectiveProfile(
        [FromQuery] string orderType,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] Guid? installationMethodId = null,
        [FromQuery] Guid? buildingTypeId = null,
        [FromQuery] DateTime? jobDate = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        if (string.IsNullOrWhiteSpace(orderType))
        {
            return ValidationError<KpiProfileDto>("OrderType is required.");
        }

        try
        {
            var profile = await _kpiProfileService.GetEffectiveProfileAsync(
                companyId, partnerId, orderType, installationMethodId, buildingTypeId, jobDate, cancellationToken);
            
            if (profile == null)
            {
                return this.NotFound<KpiProfileDto>("No effective profile found for the given context");
            }

            return this.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effective KPI profile");
            return this.InternalServerError<KpiProfileDto>($"Failed to get effective profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Create KPI profile
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<KpiProfileDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<KpiProfileDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<KpiProfileDto>>> CreateKpiProfile(
        [FromBody] CreateKpiProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<KpiProfileDto>("User context required");
        }

        var validationErrors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            validationErrors.Add("Name is required.");
        }
        if (string.IsNullOrWhiteSpace(dto.OrderType))
        {
            validationErrors.Add("OrderType is required.");
        }
        if (validationErrors.Count > 0)
        {
            return ValidationError<KpiProfileDto>(validationErrors.ToArray());
        }

        try
        {
            var profile = await _kpiProfileService.CreateProfileAsync(
                dto, companyId, userId.Value, cancellationToken);
            return this.CreatedAtAction(nameof(GetKpiProfile), new { id = profile.Id }, profile, "KPI profile created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating KPI profile");
            return this.InternalServerError<KpiProfileDto>($"Failed to create KPI profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Update KPI profile
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<KpiProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<KpiProfileDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<KpiProfileDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<KpiProfileDto>>> UpdateKpiProfile(
        Guid id,
        [FromBody] UpdateKpiProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<KpiProfileDto>("User context required");
        }

        var hasUpdates = dto.Name != null
            || dto.InstallationMethodId.HasValue
            || dto.MaxJobDurationMinutes.HasValue
            || dto.DocketKpiMinutes.HasValue
            || dto.MaxReschedulesAllowed.HasValue
            || dto.IsDefault.HasValue
            || dto.EffectiveFrom.HasValue
            || dto.EffectiveTo.HasValue;

        if (!hasUpdates)
        {
            return ValidationError<KpiProfileDto>("At least one field must be provided for update.");
        }

        try
        {
            var profile = await _kpiProfileService.UpdateProfileAsync(
                id, dto, companyId, userId.Value, cancellationToken);
            return this.Success(profile, "KPI profile updated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<KpiProfileDto>($"KPI profile with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating KPI profile: {ProfileId}", id);
            return this.InternalServerError<KpiProfileDto>($"Failed to update KPI profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete KPI profile
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteKpiProfile(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            await _kpiProfileService.DeleteProfileAsync(id, companyId, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound($"KPI profile with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting KPI profile: {ProfileId}", id);
            return this.InternalServerError($"Failed to delete KPI profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Set KPI profile as default
    /// </summary>
    [HttpPost("{id}/set-default")]
    [ProducesResponseType(typeof(ApiResponse<KpiProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<KpiProfileDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<KpiProfileDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<KpiProfileDto>>> SetAsDefault(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<KpiProfileDto>("User context required");
        }

        try
        {
            var profile = await _kpiProfileService.SetAsDefaultAsync(
                id, companyId, userId.Value, cancellationToken);
            return this.Success(profile, "KPI profile set as default successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<KpiProfileDto>($"KPI profile with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting KPI profile as default: {ProfileId}", id);
            return this.InternalServerError<KpiProfileDto>($"Failed to set profile as default: {ex.Message}");
        }
    }

    /// <summary>
    /// Evaluate KPI for an order
    /// </summary>
    [HttpGet("evaluate-order/{orderId}")]
    [ProducesResponseType(typeof(ApiResponse<KpiEvaluationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<KpiEvaluationResultDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<KpiEvaluationResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<KpiEvaluationResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<KpiEvaluationResultDto>>> EvaluateOrder(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        if (orderId == Guid.Empty)
        {
            return ValidationError<KpiEvaluationResultDto>("OrderId is required.");
        }

        try
        {
            var result = await _kpiProfileService.EvaluateOrderAsync(orderId, companyId, cancellationToken);
            return this.Success(result);
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<KpiEvaluationResultDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<KpiEvaluationResultDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating KPI for order: {OrderId}", orderId);
            return this.InternalServerError<KpiEvaluationResultDto>($"Failed to evaluate KPI: {ex.Message}");
        }
    }

    private ActionResult<ApiResponse<T>> ValidationError<T>(params string[] errors)
    {
        var errorList = new List<string> { "VALIDATION_ERROR" };
        errorList.AddRange(errors);
        return this.Error<T>(errorList, "Validation failed", 400);
    }
}

