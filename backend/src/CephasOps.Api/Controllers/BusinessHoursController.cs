using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Business Hours and Public Holidays management endpoints
/// </summary>
[ApiController]
[Route("api/business-hours")]
[Authorize]
public class BusinessHoursController : ControllerBase
{
    private readonly IBusinessHoursService _businessHoursService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<BusinessHoursController> _logger;

    public BusinessHoursController(
        IBusinessHoursService businessHoursService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<BusinessHoursController> logger)
    {
        _businessHoursService = businessHoursService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    /// <summary>
    /// Get business hours configurations
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<BusinessHoursDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<BusinessHoursDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<BusinessHoursDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<BusinessHoursDto>>>> GetBusinessHours(
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
            return this.Error<List<BusinessHoursDto>>("You do not have access to this department", 403);
        }

        try
        {
            var businessHours = await _businessHoursService.GetBusinessHoursAsync(
                companyId, departmentScope, isActive, cancellationToken);
            return this.Success(businessHours);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business hours");
            return this.Error<List<BusinessHoursDto>>($"Failed to get business hours: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get business hours by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BusinessHoursDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BusinessHoursDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<BusinessHoursDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BusinessHoursDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BusinessHoursDto>>> GetBusinessHoursById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        if (companyId == Guid.Empty)
        {
            return this.Unauthorized<BusinessHoursDto>("Company context required");
        }

        try
        {
            var businessHours = await _businessHoursService.GetBusinessHoursByIdAsync(id, companyId, cancellationToken);
            if (businessHours == null)
            {
                return this.NotFound<BusinessHoursDto>($"Business hours with ID {id} not found");
            }

            return this.Success(businessHours);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business hours: {BusinessHoursId}", id);
            return this.Error<BusinessHoursDto>($"Failed to get business hours: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Check if a date/time is within business hours
    /// </summary>
    [HttpGet("check")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> IsBusinessHours(
        [FromQuery] DateTime dateTime,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        if (companyId == Guid.Empty)
        {
            return this.Unauthorized<bool>("Company context required");
        }

        Guid? departmentScope;
        try
        {
            departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return this.Error<bool>("You do not have access to this department", 403);
        }

        try
        {
            var isBusinessHours = await _businessHoursService.IsBusinessHoursAsync(
                companyId, dateTime, departmentScope, cancellationToken);
            return this.Success(isBusinessHours);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking business hours");
            return this.Error<bool>($"Failed to check business hours: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create template business hours (8am-6pm Monday-Friday)
    /// </summary>
    [HttpPost("template")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<BusinessHoursDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<BusinessHoursDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<BusinessHoursDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BusinessHoursDto>>> CreateTemplateBusinessHours(
        [FromQuery] string? name = null,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        
        if (companyId == Guid.Empty || userId == null)
        {
            return this.Unauthorized<BusinessHoursDto>("Company and user context required");
        }

        Guid? departmentScope;
        try
        {
            departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return this.Error<BusinessHoursDto>("You do not have access to this department", 403);
        }

        try
        {
            var businessHours = await _businessHoursService.CreateTemplateBusinessHoursAsync(
                name ?? "Standard Business Hours (8am-6pm)",
                departmentScope,
                companyId,
                userId.Value,
                cancellationToken);
            return this.StatusCode(201, ApiResponse<BusinessHoursDto>.SuccessResponse(businessHours, "Template business hours created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template business hours");
            return this.Error<BusinessHoursDto>($"Failed to create template business hours: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create business hours configuration
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<BusinessHoursDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<BusinessHoursDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<BusinessHoursDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BusinessHoursDto>>> CreateBusinessHours(
        [FromBody] CreateBusinessHoursDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        
        if (companyId == Guid.Empty || userId == null)
        {
            return this.Unauthorized<BusinessHoursDto>("Company and user context required");
        }

        try
        {
            var businessHours = await _businessHoursService.CreateBusinessHoursAsync(dto, companyId, userId.Value, cancellationToken);
            return this.StatusCode(201, ApiResponse<BusinessHoursDto>.SuccessResponse(businessHours, "Business hours created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating business hours");
            return this.Error<BusinessHoursDto>($"Failed to create business hours: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update business hours configuration
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<BusinessHoursDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BusinessHoursDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<BusinessHoursDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BusinessHoursDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BusinessHoursDto>>> UpdateBusinessHours(
        Guid id,
        [FromBody] UpdateBusinessHoursDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        
        if (companyId == Guid.Empty || userId == null)
        {
            return this.Unauthorized<BusinessHoursDto>("Company and user context required");
        }

        try
        {
            var businessHours = await _businessHoursService.UpdateBusinessHoursAsync(id, dto, companyId, userId.Value, cancellationToken);
            return this.Success(businessHours, "Business hours updated successfully");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<BusinessHoursDto>($"Business hours with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating business hours: {BusinessHoursId}", id);
            return this.Error<BusinessHoursDto>($"Failed to update business hours: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete business hours configuration
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteBusinessHours(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        if (companyId == Guid.Empty)
        {
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        }

        try
        {
            await _businessHoursService.DeleteBusinessHoursAsync(id, companyId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Business hours deleted successfully"));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"Business hours with ID {id} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting business hours: {BusinessHoursId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete business hours: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get public holidays
    /// </summary>
    [HttpGet("holidays")]
    [ProducesResponseType(typeof(ApiResponse<List<PublicHolidayDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<PublicHolidayDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<PublicHolidayDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<PublicHolidayDto>>>> GetPublicHolidays(
        [FromQuery] int? year = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        if (companyId == Guid.Empty)
        {
            return this.Unauthorized<List<PublicHolidayDto>>("Company context required");
        }

        try
        {
            var holidays = await _businessHoursService.GetPublicHolidaysAsync(
                companyId, year, isActive, cancellationToken);
            return this.Success(holidays);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public holidays");
            return this.Error<List<PublicHolidayDto>>($"Failed to get public holidays: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Check if a date is a public holiday
    /// </summary>
    [HttpGet("holidays/check")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> IsPublicHoliday(
        [FromQuery] DateTime date,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        if (companyId == Guid.Empty)
        {
            return this.Unauthorized<bool>("Company context required");
        }

        try
        {
            var isHoliday = await _businessHoursService.IsPublicHolidayAsync(companyId, date, cancellationToken);
            return this.Success(isHoliday);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking public holiday");
            return this.Error<bool>($"Failed to check public holiday: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create public holiday
    /// </summary>
    [HttpPost("holidays")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<PublicHolidayDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<PublicHolidayDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<PublicHolidayDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PublicHolidayDto>>> CreatePublicHoliday(
        [FromBody] CreatePublicHolidayDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        
        if (companyId == Guid.Empty || userId == null)
        {
            return this.Unauthorized<PublicHolidayDto>("Company and user context required");
        }

        try
        {
            var holiday = await _businessHoursService.CreatePublicHolidayAsync(dto, companyId, userId.Value, cancellationToken);
            return this.StatusCode(201, ApiResponse<PublicHolidayDto>.SuccessResponse(holiday, "Public holiday created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating public holiday");
            return this.Error<PublicHolidayDto>($"Failed to create public holiday: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update public holiday
    /// </summary>
    [HttpPut("holidays/{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<PublicHolidayDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PublicHolidayDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<PublicHolidayDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PublicHolidayDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PublicHolidayDto>>> UpdatePublicHoliday(
        Guid id,
        [FromBody] UpdatePublicHolidayDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        
        if (companyId == Guid.Empty || userId == null)
        {
            return this.Unauthorized<PublicHolidayDto>("Company and user context required");
        }

        try
        {
            var holiday = await _businessHoursService.UpdatePublicHolidayAsync(id, dto, companyId, userId.Value, cancellationToken);
            return this.Success(holiday, "Public holiday updated successfully");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<PublicHolidayDto>($"Public holiday with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating public holiday: {HolidayId}", id);
            return this.Error<PublicHolidayDto>($"Failed to update public holiday: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete public holiday
    /// </summary>
    [HttpDelete("holidays/{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeletePublicHoliday(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        if (companyId == Guid.Empty)
        {
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        }

        try
        {
            await _businessHoursService.DeletePublicHolidayAsync(id, companyId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Public holiday deleted successfully"));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"Public holiday with ID {id} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting public holiday: {HolidayId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete public holiday: {ex.Message}"));
        }
    }
}

