using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// SLA profiles management endpoints
/// </summary>
[ApiController]
[Route("api/sla-profiles")]
[Authorize]
public class SlaProfilesController : ControllerBase
{
    private readonly ISlaProfileService _slaProfileService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<SlaProfilesController> _logger;

    public SlaProfilesController(
        ISlaProfileService slaProfileService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<SlaProfilesController> logger)
    {
        _slaProfileService = slaProfileService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    /// <summary>
    /// Get SLA profiles
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SlaProfileDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<SlaProfileDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<SlaProfileDto>>>> GetSlaProfiles(
        [FromQuery] string? orderType = null,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool? isActive = null,
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
            return this.Error<List<SlaProfileDto>>("You do not have access to this department", 403);
        }

        try
        {
            var profiles = await _slaProfileService.GetProfilesAsync(
                companyId, orderType, partnerId, departmentScope, isActive, cancellationToken);
            return this.Success(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SLA profiles");
            return this.InternalServerError<List<SlaProfileDto>>($"Failed to get SLA profiles: {ex.Message}");
        }
    }

    /// <summary>
    /// Get SLA profile by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SlaProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SlaProfileDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SlaProfileDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SlaProfileDto>>> GetSlaProfile(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var profile = await _slaProfileService.GetProfileByIdAsync(id, companyId, cancellationToken);
            if (profile == null)
            {
                return this.NotFound<SlaProfileDto>($"SLA profile with ID {id} not found");
            }

            return this.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SLA profile: {ProfileId}", id);
            return this.InternalServerError<SlaProfileDto>($"Failed to get SLA profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Get effective SLA profile for order context
    /// </summary>
    [HttpGet("effective")]
    [ProducesResponseType(typeof(ApiResponse<SlaProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SlaProfileDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SlaProfileDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SlaProfileDto>>> GetEffectiveProfile(
        [FromQuery] Guid? partnerId = null,
        [FromQuery] string orderType = "",
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool isVip = false,
        [FromQuery] DateTime? effectiveDate = null,
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
            return this.Error<SlaProfileDto>("You do not have access to this department", 403);
        }

        try
        {
            var profile = await _slaProfileService.GetEffectiveProfileAsync(
                companyId, partnerId, orderType, departmentScope, isVip, effectiveDate, cancellationToken);
            
            if (profile == null)
            {
                return this.NotFound<SlaProfileDto>("No effective SLA profile found for the given context");
            }

            return this.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effective SLA profile");
            return this.InternalServerError<SlaProfileDto>($"Failed to get effective SLA profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Create SLA profile
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<SlaProfileDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SlaProfileDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SlaProfileDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SlaProfileDto>>> CreateSlaProfile(
        [FromBody] CreateSlaProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        
        if (userId == null)
        {
            return this.Unauthorized<SlaProfileDto>("User context required");
        }

        try
        {
            var profile = await _slaProfileService.CreateProfileAsync(dto, companyId, userId.Value, cancellationToken);
            return this.CreatedAtAction(nameof(GetSlaProfile), new { id = profile.Id }, profile, "SLA profile created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SLA profile");
            return this.InternalServerError<SlaProfileDto>($"Failed to create SLA profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Update SLA profile
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<SlaProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SlaProfileDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SlaProfileDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SlaProfileDto>>> UpdateSlaProfile(
        Guid id,
        [FromBody] UpdateSlaProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        
        if (userId == null)
        {
            return this.Unauthorized<SlaProfileDto>("User context required");
        }

        try
        {
            var profile = await _slaProfileService.UpdateProfileAsync(id, dto, companyId, userId.Value, cancellationToken);
            return this.Success(profile, "SLA profile updated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<SlaProfileDto>($"SLA profile with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating SLA profile: {ProfileId}", id);
            return this.InternalServerError<SlaProfileDto>($"Failed to update SLA profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete SLA profile
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteSlaProfile(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            await _slaProfileService.DeleteProfileAsync(id, companyId, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound($"SLA profile with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting SLA profile: {ProfileId}", id);
            return this.InternalServerError($"Failed to delete SLA profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Set SLA profile as default
    /// </summary>
    [HttpPost("{id}/set-default")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<SlaProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SlaProfileDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SlaProfileDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SlaProfileDto>>> SetAsDefault(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        
        if (userId == null)
        {
            return this.Unauthorized<SlaProfileDto>("User context required");
        }

        try
        {
            var profile = await _slaProfileService.SetAsDefaultAsync(id, companyId, userId.Value, cancellationToken);
            return this.Success(profile, "SLA profile set as default successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<SlaProfileDto>($"SLA profile with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting SLA profile as default: {ProfileId}", id);
            return this.InternalServerError<SlaProfileDto>($"Failed to set SLA profile as default: {ex.Message}");
        }
    }
}

