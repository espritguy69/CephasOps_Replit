using CephasOps.Application.Settings.DTOs;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Time slot service interface
/// </summary>
public interface ITimeSlotService
{
    /// <summary>
    /// Get all time slots
    /// </summary>
    Task<List<TimeSlotDto>> GetTimeSlotsAsync(Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new time slot
    /// </summary>
    Task<TimeSlotDto> CreateTimeSlotAsync(CreateTimeSlotDto dto, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing time slot
    /// </summary>
    Task<TimeSlotDto> UpdateTimeSlotAsync(Guid id, UpdateTimeSlotDto dto, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a time slot
    /// </summary>
    Task DeleteTimeSlotAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorder time slots based on provided IDs
    /// </summary>
    Task ReorderTimeSlotsAsync(ReorderTimeSlotsDto dto, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seed default time slots (8:00 AM - 5:30 PM, 30-minute intervals)
    /// </summary>
    Task<int> SeedDefaultTimeSlotsAsync(Guid? companyId, CancellationToken cancellationToken = default);
}

