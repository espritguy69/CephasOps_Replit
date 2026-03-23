using CephasOps.Application.Scheduler.DTOs;

namespace CephasOps.Application.Scheduler.Services;

/// <summary>
/// Scheduler service interface
/// </summary>
public interface ISchedulerService
{
    /// <summary>
    /// Get calendar view for date range. When departmentId is set, only slots/orders/SIs in that department are returned.
    /// </summary>
    Task<List<CalendarDto>> GetCalendarAsync(Guid? companyId, DateTime fromDate, DateTime toDate, Guid? departmentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get schedule slots. When departmentId is set, only slots for orders/SIs in that department are returned.
    /// </summary>
    Task<List<ScheduleSlotDto>> GetScheduleSlotsAsync(Guid? companyId, Guid? siId = null, DateTime? date = null, Guid? orderId = null, Guid? departmentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get SI availability. When departmentId is set, SI must belong to that department.
    /// </summary>
    Task<List<SiAvailabilityDto>> GetSiAvailabilityAsync(Guid? companyId, Guid siId, DateTime? fromDate = null, DateTime? toDate = null, Guid? departmentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a schedule slot. When departmentId is set, order and SI must belong to that department.
    /// </summary>
    Task<ScheduleSlotDto> CreateScheduleSlotAsync(CreateScheduleSlotDto dto, Guid? companyId, Guid userId, Guid? departmentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create SI availability. When departmentId is set, SI must belong to that department.
    /// </summary>
    Task<SiAvailabilityDto> CreateSiAvailabilityAsync(CreateSiAvailabilityDto dto, Guid? companyId, Guid? departmentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unassigned orders (pending orders not yet scheduled). When departmentId is set, only orders in that department are returned.
    /// </summary>
    Task<List<UnassignedOrderDto>> GetUnassignedOrdersAsync(Guid? companyId, Guid? partnerId = null, DateTime? fromDate = null, DateTime? toDate = null, Guid? departmentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update schedule slot (for rescheduling). When departmentId is set, slot's order/SI must belong to that department.
    /// </summary>
    Task<ScheduleSlotDto> UpdateScheduleSlotAsync(Guid slotId, UpdateScheduleSlotDto dto, Guid? companyId, Guid? departmentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Block an order (create blocker and update status). When departmentId is set, order must belong to that department.
    /// </summary>
    Task BlockOrderAsync(Guid orderId, BlockOrderDto dto, Guid? companyId, Guid userId, Guid? departmentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirm schedule (changes ScheduledSlot status from Draft to Confirmed). When departmentId is set, slot's order/SI must belong to that department.
    /// </summary>
    Task<ScheduleSlotDto> ConfirmScheduleAsync(Guid slotId, Guid? companyId, Guid userId, Guid? departmentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Post schedule to SI (changes ScheduledSlot status from Confirmed to Posted and triggers order status change via workflow). When departmentId is set, slot's order/SI must belong to that department.
    /// </summary>
    Task<ScheduleSlotDto> PostScheduleToSIAsync(Guid slotId, Guid? companyId, Guid userId, Guid? departmentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Return schedule to Draft (reverts Confirmed back to Draft). When departmentId is set, slot's order/SI must belong to that department.
    /// </summary>
    Task<ScheduleSlotDto> ReturnScheduleToDraftAsync(Guid slotId, Guid? companyId, Guid userId, Guid? departmentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// SI requests reschedule (different day) - updates ScheduledSlot and transitions order to ReschedulePendingApproval via workflow. When departmentId is set, slot's order/SI must belong to that department.
    /// </summary>
    Task<ScheduleSlotDto> RequestRescheduleAsync(
        Guid slotId,
        DateTime newDate,
        TimeSpan newWindowFrom,
        TimeSpan newWindowTo,
        string reason,
        string? notes,
        Guid? companyId,
        Guid siId,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin approves reschedule - updates ScheduledSlot and transitions order back to Assigned via workflow. When departmentId is set, slot's order/SI must belong to that department.
    /// </summary>
    Task<ScheduleSlotDto> ApproveRescheduleAsync(
        Guid slotId,
        Guid? companyId,
        Guid userId,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin rejects reschedule - updates ScheduledSlot and transitions order back to Assigned via workflow. When departmentId is set, slot's order/SI must belong to that department.
    /// </summary>
    Task<ScheduleSlotDto> RejectRescheduleAsync(
        Guid slotId,
        string rejectionReason,
        Guid? companyId,
        Guid userId,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect scheduling conflicts for a given order or slot. When departmentId is set, scope is limited to that department.
    /// </summary>
    Task<List<ScheduleConflictDto>> DetectSchedulingConflictsAsync(
        Guid? orderId,
        Guid? slotId,
        Guid? siId,
        DateTime? date,
        Guid? companyId,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);
}

