using CephasOps.Application.Scheduler.DTOs;
using CephasOps.Application.Scheduler.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Scheduler endpoints. Department scope enforced via DepartmentAccessService; users see only installers/orders in their permitted department.
/// </summary>
[ApiController]
[Route("api/scheduler")]
[Authorize]
public class SchedulerController : ControllerBase
{
    private readonly ISchedulerService _schedulerService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<SchedulerController> _logger;

    public SchedulerController(
        ISchedulerService schedulerService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<SchedulerController> logger)
    {
        _schedulerService = schedulerService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    private async Task<Guid?> ResolveDepartmentScopeAsync(Guid? requestedDepartmentId, CancellationToken cancellationToken)
    {
        var departmentFromRequest = requestedDepartmentId ?? _departmentRequestContext.DepartmentId;
        return await _departmentAccessService.ResolveDepartmentScopeAsync(departmentFromRequest, cancellationToken);
    }

    /// <summary>
    /// Get calendar view for date range
    /// </summary>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <param name="departmentId">Optional department scope; when set, results are limited to that department.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Calendar view</returns>
    [HttpGet("calendar")]
    [ProducesResponseType(typeof(ApiResponse<List<CalendarDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<CalendarDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<CalendarDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<CalendarDto>>>> GetCalendar(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var calendar = await _schedulerService.GetCalendarAsync(companyId, fromDate, toDate, departmentScope, cancellationToken);
            return this.Success(calendar);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Department access denied for calendar");
            return this.Error<List<CalendarDto>>(ex.Message, 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calendar");
            return this.Error<List<CalendarDto>>($"Failed to get calendar: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get scheduler utilization: flattened schedule slots for a date range (for reports, dashboards, or export).
    /// Same data as Reports Hub "scheduler-utilization" report via a dedicated GET endpoint.
    /// </summary>
    /// <param name="fromDate">Start date (required)</param>
    /// <param name="toDate">End date (required)</param>
    /// <param name="departmentId">Optional department scope; when set, results are limited to that department.</param>
    /// <param name="siId">Optional filter by service installer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Flattened list of schedule slots in the date range</returns>
    [HttpGet("utilization")]
    [ProducesResponseType(typeof(ApiResponse<List<ScheduleSlotDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ScheduleSlotDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<List<ScheduleSlotDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<ScheduleSlotDto>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<List<ScheduleSlotDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<ScheduleSlotDto>>>> GetUtilization(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? siId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var calendar = await _schedulerService.GetCalendarAsync(companyId, fromDate, toDate, departmentScope, cancellationToken);
            var slots = calendar.SelectMany(c => c.Slots).ToList();
            if (siId.HasValue)
                slots = slots.Where(s => s.ServiceInstallerId == siId.Value).ToList();
            return this.Success(slots);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Department access denied for utilization");
            return this.Error<List<ScheduleSlotDto>>(ex.Message, 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting utilization");
            return this.Error<List<ScheduleSlotDto>>($"Failed to get utilization: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get schedule slots
    /// </summary>
    /// <param name="siId">Filter by SI ID</param>
    /// <param name="date">Filter by date</param>
    /// <param name="orderId">Filter by order ID</param>
    /// <param name="departmentId">Optional department scope; when set, results are limited to that department.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of schedule slots</returns>
    [HttpGet("slots")]
    [ProducesResponseType(typeof(ApiResponse<List<ScheduleSlotDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ScheduleSlotDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<ScheduleSlotDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<ScheduleSlotDto>>>> GetScheduleSlots(
        [FromQuery] Guid? siId = null,
        [FromQuery] DateTime? date = null,
        [FromQuery] Guid? orderId = null,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var slots = await _schedulerService.GetScheduleSlotsAsync(companyId, siId, date, orderId, departmentScope, cancellationToken);
            return this.Success(slots);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Department access denied for schedule slots");
            return this.Error<List<ScheduleSlotDto>>(ex.Message, 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schedule slots");
            return this.Error<List<ScheduleSlotDto>>($"Failed to get schedule slots: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get SI availability
    /// </summary>
    /// <param name="siId">SI ID</param>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <param name="departmentId">Optional department scope; when set, SI must belong to that department.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of SI availabilities</returns>
    [HttpGet("si-availability/{siId}")]
    [ProducesResponseType(typeof(ApiResponse<List<SiAvailabilityDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<SiAvailabilityDto>>>> GetSiAvailability(
        Guid siId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var availabilities = await _schedulerService.GetSiAvailabilityAsync(companyId, siId, fromDate, toDate, departmentScope, cancellationToken);
            return this.Success<List<SiAvailabilityDto>>(availabilities);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Department access denied for SI availability");
            return this.Error<List<SiAvailabilityDto>>(ex.Message, 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SI availability");
            return StatusCode(500, new { error = "Failed to get SI availability", message = ex.Message });
        }
    }

    /// <summary>
    /// Create a schedule slot
    /// </summary>
    /// <param name="dto">Schedule slot data</param>
    /// <param name="departmentId">Optional department scope; when set, order and SI must belong to that department.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created schedule slot</returns>
    [HttpPost("slots")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleSlotDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ScheduleSlotDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ScheduleSlotDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ScheduleSlotDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ScheduleSlotDto>>> CreateScheduleSlot(
        [FromBody] CreateScheduleSlotDto dto,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if ((companyId == null && !_currentUserService.IsSuperAdmin) || userId == null)
        {
            return this.Error<ScheduleSlotDto>("Company and user context required", 401);
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var slot = await _schedulerService.CreateScheduleSlotAsync(dto, companyId, userId.Value, departmentScope, cancellationToken);
            return this.StatusCode(201, ApiResponse<ScheduleSlotDto>.SuccessResponse(slot, "Schedule slot created successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Department access denied for create schedule slot");
            return this.Error<ScheduleSlotDto>(ex.Message, 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating schedule slot");
            return this.Error<ScheduleSlotDto>($"Failed to create schedule slot: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create SI availability
    /// </summary>
    /// <param name="dto">SI availability data</param>
    /// <param name="departmentId">Optional department scope; when set, SI must belong to that department.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created SI availability</returns>
    [HttpPost("availability")]
    [ProducesResponseType(typeof(SiAvailabilityDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SiAvailabilityDto>> CreateSiAvailability(
        [FromBody] CreateSiAvailabilityDto dto,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var availability = await _schedulerService.CreateSiAvailabilityAsync(dto, companyId, departmentScope, cancellationToken);
            return CreatedAtAction(nameof(GetSiAvailability), new { siId = dto.ServiceInstallerId }, availability);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Department access denied for create SI availability");
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SI availability");
            return StatusCode(500, new { error = "Failed to create SI availability", message = ex.Message });
        }
    }

    /// <summary>
    /// Get unassigned orders (pending orders not yet scheduled)
    /// </summary>
    /// <param name="partnerId">Filter by partner ID</param>
    /// <param name="fromDate">Filter from date</param>
    /// <param name="toDate">Filter to date</param>
    /// <param name="departmentId">Optional department scope; when set, only orders in that department are returned.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unassigned orders</returns>
    [HttpGet("unassigned-orders")]
    [ProducesResponseType(typeof(ApiResponse<List<UnassignedOrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<UnassignedOrderDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<UnassignedOrderDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<UnassignedOrderDto>>>> GetUnassignedOrders(
        [FromQuery] Guid? partnerId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var orders = await _schedulerService.GetUnassignedOrdersAsync(companyId, partnerId, fromDate, toDate, departmentScope, cancellationToken);
            return this.Success(orders);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Department access denied for unassigned orders");
            return this.Error<List<UnassignedOrderDto>>(ex.Message, 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unassigned orders");
            return this.Error<List<UnassignedOrderDto>>($"Failed to get unassigned orders: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update schedule slot (for rescheduling)
    /// </summary>
    /// <param name="slotId">Slot ID</param>
    /// <param name="dto">Update data</param>
    /// <param name="departmentId">Optional department scope; when set, slot's order/SI must belong to that department.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated schedule slot</returns>
    [HttpPut("slots/{slotId}")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ScheduleSlotDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ScheduleSlotDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ScheduleSlotDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ScheduleSlotDto>>> UpdateScheduleSlot(
        Guid slotId,
        [FromBody] UpdateScheduleSlotDto dto,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var slot = await _schedulerService.UpdateScheduleSlotAsync(slotId, dto, companyId, departmentScope, cancellationToken);
            return this.Success(slot, "Schedule slot updated successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Department access denied for update schedule slot");
            return this.Error<ScheduleSlotDto>(ex.Message, 403);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Schedule slot not found: {SlotId}", slotId);
            return this.NotFound<ScheduleSlotDto>($"Schedule slot not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating schedule slot");
            return this.Error<ScheduleSlotDto>($"Failed to update schedule slot: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Block an order (create blocker and update status)
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <param name="dto">Block order data</param>
    /// <param name="departmentId">Optional department scope; when set, order must belong to that department.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("orders/{orderId}/block")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> BlockOrder(
        Guid orderId,
        [FromBody] BlockOrderDto dto,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if ((companyId == null && !_currentUserService.IsSuperAdmin) || userId == null)
        {
            return StatusCode(401, ApiResponse.ErrorResponse("Company and user context required"));
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            await _schedulerService.BlockOrderAsync(orderId, dto, companyId, userId.Value, departmentScope, cancellationToken);
            return this.Success("Order blocked successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Department access denied for block order");
            return StatusCode(403, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Order not found: {OrderId}", orderId);
            return StatusCode(404, ApiResponse.ErrorResponse($"Order not found: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking order: {OrderId}", orderId);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to block order: {ex.Message}"));
        }
    }

    /// <summary>
    /// Confirm schedule (changes ScheduledSlot status from Draft to Confirmed)
    /// </summary>
    /// <param name="slotId">Slot ID</param>
    /// <param name="departmentId">Optional department scope; when set, slot's order/SI must belong to that department.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmed schedule slot</returns>
    [HttpPost("slots/{slotId}/confirm")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ScheduleSlotDto>>> ConfirmSchedule(
        Guid slotId,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if ((companyId == null && !_currentUserService.IsSuperAdmin) || userId == null)
        {
            return Unauthorized("Company and user context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var slot = await _schedulerService.ConfirmScheduleAsync(slotId, companyId, userId.Value, departmentScope, cancellationToken);
            return this.Success<ScheduleSlotDto>(slot, "Schedule confirmed successfully.");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Department access denied for confirm schedule");
            return this.Error<ScheduleSlotDto>(ex.Message, 403);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot confirm schedule slot: {SlotId}", slotId);
            return this.BadRequest<ScheduleSlotDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming schedule slot: {SlotId}", slotId);
            return StatusCode(500, new { error = "Failed to confirm schedule", message = ex.Message });
        }
    }

    /// <summary>
    /// Post schedule to SI (changes ScheduledSlot status from Confirmed to Posted and triggers order status change via workflow)
    /// </summary>
    /// <param name="slotId">Slot ID</param>
    /// <param name="departmentId">Optional department scope; when set, slot's order/SI must belong to that department.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Posted schedule slot</returns>
    [HttpPost("slots/{slotId}/post")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ScheduleSlotDto>>> PostScheduleToSI(
        Guid slotId,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if ((companyId == null && !_currentUserService.IsSuperAdmin) || userId == null)
        {
            return Unauthorized("Company and user context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var slot = await _schedulerService.PostScheduleToSIAsync(slotId, companyId, userId.Value, departmentScope, cancellationToken);
            return this.Success<ScheduleSlotDto>(slot, "Schedule posted to SI successfully.");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Department access denied for post schedule");
            return this.Error<ScheduleSlotDto>(ex.Message, 403);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot post schedule slot: {SlotId}", slotId);
            return this.BadRequest<ScheduleSlotDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting schedule slot: {SlotId}", slotId);
            return StatusCode(500, new { error = "Failed to post schedule", message = ex.Message });
        }
    }

    /// <summary>
    /// Return schedule to Draft (reverts Confirmed back to Draft)
    /// </summary>
    /// <param name="slotId">Slot ID</param>
    /// <param name="departmentId">Optional department scope; when set, slot's order/SI must belong to that department.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Schedule slot returned to Draft</returns>
    [HttpPost("slots/{slotId}/return-to-draft")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ScheduleSlotDto>>> ReturnScheduleToDraft(
        Guid slotId,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if ((companyId == null && !_currentUserService.IsSuperAdmin) || userId == null)
        {
            return Unauthorized("Company and user context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var slot = await _schedulerService.ReturnScheduleToDraftAsync(slotId, companyId, userId.Value, departmentScope, cancellationToken);
            return this.Success<ScheduleSlotDto>(slot, "Schedule returned to draft successfully.");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Department access denied for return to draft");
            return this.Error<ScheduleSlotDto>(ex.Message, 403);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot return schedule slot to draft: {SlotId}", slotId);
            return this.BadRequest<ScheduleSlotDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error returning schedule slot to draft: {SlotId}", slotId);
            return StatusCode(500, new { error = "Failed to return schedule to draft", message = ex.Message });
        }
    }

    /// <summary>
    /// SI requests reschedule (different day) - updates ScheduledSlot and transitions order to ReschedulePendingApproval via workflow
    /// </summary>
    /// <param name="slotId">Slot ID</param>
    /// <param name="dto">Reschedule request data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated schedule slot</returns>
    [HttpPost("slots/{slotId}/reschedule-request")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ScheduleSlotDto>>> RequestReschedule(
        Guid slotId,
        [FromBody] RequestRescheduleDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var siId = _currentUserService.ServiceInstallerId;
        if ((companyId == null && !_currentUserService.IsSuperAdmin) || siId == null)
        {
            return Unauthorized("Company and SI context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(null, cancellationToken);
            var slot = await _schedulerService.RequestRescheduleAsync(
                slotId,
                dto.NewDate,
                dto.NewWindowFrom,
                dto.NewWindowTo,
                dto.Reason,
                dto.Notes,
                companyId,
                siId.Value,
                departmentScope,
                cancellationToken);
            return this.Success<ScheduleSlotDto>(slot, "Reschedule requested successfully.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot request reschedule for slot: {SlotId}", slotId);
            return this.BadRequest<ScheduleSlotDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting reschedule for slot: {SlotId}", slotId);
            return StatusCode(500, new { error = "Failed to request reschedule", message = ex.Message });
        }
    }

    /// <summary>
    /// Admin approves reschedule - updates ScheduledSlot and transitions order back to Assigned via workflow
    /// </summary>
    /// <param name="slotId">Slot ID</param>
    /// <param name="departmentId">Optional department scope; when set, slot's order/SI must belong to that department.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated schedule slot</returns>
    [HttpPost("slots/{slotId}/reschedule-approve")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ScheduleSlotDto>>> ApproveReschedule(
        Guid slotId,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if ((companyId == null && !_currentUserService.IsSuperAdmin) || userId == null)
        {
            return Unauthorized("Company and user context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var slot = await _schedulerService.ApproveRescheduleAsync(slotId, companyId, userId.Value, departmentScope, cancellationToken);
            return this.Success<ScheduleSlotDto>(slot, "Reschedule approved successfully.");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Department access denied for approve reschedule");
            return this.Error<ScheduleSlotDto>(ex.Message, 403);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot approve reschedule for slot: {SlotId}", slotId);
            return this.BadRequest<ScheduleSlotDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving reschedule for slot: {SlotId}", slotId);
            return StatusCode(500, new { error = "Failed to approve reschedule", message = ex.Message });
        }
    }

    /// <summary>
    /// Admin rejects reschedule - updates ScheduledSlot and transitions order back to Assigned via workflow
    /// </summary>
    /// <param name="slotId">Slot ID</param>
    /// <param name="dto">Rejection reason</param>
    /// <param name="departmentId">Optional department scope; when set, slot's order/SI must belong to that department.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated schedule slot</returns>
    [HttpPost("slots/{slotId}/reschedule-reject")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ScheduleSlotDto>>> RejectReschedule(
        Guid slotId,
        [FromBody] RejectRescheduleDto dto,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if ((companyId == null && !_currentUserService.IsSuperAdmin) || userId == null)
        {
            return Unauthorized("Company and user context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var slot = await _schedulerService.RejectRescheduleAsync(slotId, dto.RejectionReason, companyId, userId.Value, departmentScope, cancellationToken);
            return this.Success<ScheduleSlotDto>(slot, "Reschedule rejected successfully.");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Department access denied for reject reschedule");
            return this.Error<ScheduleSlotDto>(ex.Message, 403);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot reject reschedule for slot: {SlotId}", slotId);
            return this.BadRequest<ScheduleSlotDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting reschedule for slot: {SlotId}", slotId);
            return StatusCode(500, new { error = "Failed to reject reschedule", message = ex.Message });
        }
    }

    /// <summary>
    /// Get scheduling conflicts for a given order or slot
    /// </summary>
    /// <param name="orderId">Order ID (optional)</param>
    /// <param name="slotId">Slot ID (optional)</param>
    /// <param name="siId">SI ID (optional, required if orderId/slotId not provided)</param>
    /// <param name="date">Date (optional, required if orderId/slotId not provided)</param>
    /// <param name="departmentId">Optional department scope; when set, scope is limited to that department.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of conflicts</returns>
    [HttpGet("conflicts")]
    [ProducesResponseType(typeof(ApiResponse<List<ScheduleConflictDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ScheduleConflictDto>>>> GetConflicts(
        [FromQuery] Guid? orderId = null,
        [FromQuery] Guid? slotId = null,
        [FromQuery] Guid? siId = null,
        [FromQuery] DateTime? date = null,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var conflicts = await _schedulerService.DetectSchedulingConflictsAsync(
                orderId, slotId, siId, date, companyId, departmentScope, cancellationToken);
            return this.Success<List<ScheduleConflictDto>>(conflicts);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Department access denied for conflicts");
            return this.Error<List<ScheduleConflictDto>>(ex.Message, 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting scheduling conflicts");
            return this.InternalServerError<List<ScheduleConflictDto>>($"Failed to detect conflicts: {ex.Message}");
        }
    }
}

