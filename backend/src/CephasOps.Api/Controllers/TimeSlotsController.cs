using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Time slots controller - manages appointment time slots (e.g., "9:00 AM", "2:30 PM").
/// Tenant scope from ITenantProvider; optional companyId query only for SuperAdmin.
/// </summary>
[ApiController]
[Route("api/time-slots")]
[Authorize]
public class TimeSlotsController : ControllerBase
{
    private readonly ITimeSlotService _timeSlotService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TimeSlotsController> _logger;

    public TimeSlotsController(ITimeSlotService timeSlotService, ITenantProvider tenantProvider, ILogger<TimeSlotsController> logger)
    {
        _timeSlotService = timeSlotService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid? EffectiveCompanyId(Guid? queryCompanyId)
    {
        var current = _tenantProvider.CurrentTenantId;
        if (!queryCompanyId.HasValue || queryCompanyId.Value == Guid.Empty)
            return current;
        if (current == queryCompanyId)
            return current;
        if (User.IsInRole("SuperAdmin"))
            return queryCompanyId;
        return current;
    }

    /// <summary>
    /// Get all time slots
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TimeSlotDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<TimeSlotDto>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<List<TimeSlotDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<TimeSlotDto>>>> GetTimeSlots([FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var effectiveCompanyId = EffectiveCompanyId(companyId);
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return this.Forbidden<List<TimeSlotDto>>("Company context is required for this operation.");
        try
        {
            var timeSlots = await _timeSlotService.GetTimeSlotsAsync(effectiveCompanyId, cancellationToken);
            return this.Success(timeSlots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time slots");
            return this.InternalServerError<List<TimeSlotDto>>($"An error occurred while retrieving time slots: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a new time slot
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TimeSlotDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TimeSlotDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TimeSlotDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<TimeSlotDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TimeSlotDto>>> CreateTimeSlot([FromBody] CreateTimeSlotDto dto, [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var effectiveCompanyId = EffectiveCompanyId(companyId);
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return this.Forbidden<TimeSlotDto>("Company context is required for this operation.");
        try
        {
            var timeSlot = await _timeSlotService.CreateTimeSlotAsync(dto, effectiveCompanyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetTimeSlots), new { id = timeSlot.Id }, timeSlot, "Time slot created successfully.");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating time slot");
            return this.BadRequest<TimeSlotDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating time slot");
            return this.Error<TimeSlotDto>(ex.Message, 409);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating time slot");
            return this.InternalServerError<TimeSlotDto>($"An error occurred while creating time slot: {ex.Message}");
        }
    }

    /// <summary>
    /// Update an existing time slot
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TimeSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TimeSlotDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<TimeSlotDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<TimeSlotDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TimeSlotDto>>> UpdateTimeSlot(Guid id, [FromBody] UpdateTimeSlotDto dto, [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var effectiveCompanyId = EffectiveCompanyId(companyId);
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return this.Forbidden<TimeSlotDto>("Company context is required for this operation.");
        try
        {
            var timeSlot = await _timeSlotService.UpdateTimeSlotAsync(id, dto, effectiveCompanyId, cancellationToken);
            return this.Success(timeSlot, "Time slot updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Time slot not found: {Id}", id);
            return this.NotFound<TimeSlotDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when updating time slot");
            return this.Error<TimeSlotDto>(ex.Message, 409);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating time slot: {Id}", id);
            return this.InternalServerError<TimeSlotDto>($"An error occurred while updating time slot: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a time slot
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteTimeSlot(Guid id, [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var effectiveCompanyId = EffectiveCompanyId(companyId);
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return this.StatusCode(403, ApiResponse.ErrorResponse("Company context is required for this operation."));
        try
        {
            await _timeSlotService.DeleteTimeSlotAsync(id, effectiveCompanyId, cancellationToken);
            return this.Success("Time slot deleted successfully");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Time slot not found: {Id}", id);
            return this.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting time slot: {Id}", id);
            return this.InternalServerError($"An error occurred while deleting time slot: {ex.Message}");
        }
    }

    /// <summary>
    /// Reorder time slots
    /// </summary>
    [HttpPost("reorder")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> ReorderTimeSlots([FromBody] ReorderTimeSlotsDto dto, [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var effectiveCompanyId = EffectiveCompanyId(companyId);
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return this.StatusCode(403, ApiResponse.ErrorResponse("Company context is required for this operation."));
        try
        {
            await _timeSlotService.ReorderTimeSlotsAsync(dto, effectiveCompanyId, cancellationToken);
            return this.Success("Time slots reordered successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when reordering time slots");
            var response = ApiResponse.ErrorResponse(ex.Message);
            return this.StatusCode(400, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering time slots");
            return this.InternalServerError($"An error occurred while reordering time slots: {ex.Message}");
        }
    }

    /// <summary>
    /// Seed default time slots (8:00 AM - 5:30 PM, 30-minute intervals)
    /// </summary>
    [HttpPost("seed-defaults")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<int>>> SeedDefaultTimeSlots([FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var effectiveCompanyId = EffectiveCompanyId(companyId);
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return this.Forbidden<int>("Company context is required for this operation.");
        try
        {
            var count = await _timeSlotService.SeedDefaultTimeSlotsAsync(effectiveCompanyId, cancellationToken);
            return this.Success(count, $"Created {count} default time slots");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when seeding default time slots");
            return this.Error<int>(ex.Message, 409);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding default time slots");
            return this.InternalServerError<int>($"An error occurred while seeding default time slots: {ex.Message}");
        }
    }
}

