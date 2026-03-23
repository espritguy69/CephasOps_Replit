using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Time slot service implementation
/// </summary>
public class TimeSlotService : ITimeSlotService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TimeSlotService> _logger;

    public TimeSlotService(ApplicationDbContext context, ILogger<TimeSlotService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<TimeSlotDto>> GetTimeSlotsAsync(Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all time slots, companyId: {CompanyId}", companyId);

        var query = _context.Set<TimeSlot>().AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(t => t.CompanyId == companyId.Value);
        }

        var timeSlots = await query
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Time)
            .ToListAsync(cancellationToken);

        return timeSlots.Select(t => new TimeSlotDto
        {
            Id = t.Id,
            Time = t.Time,
            SortOrder = t.SortOrder,
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        }).ToList();
    }

    public async Task<TimeSlotDto> CreateTimeSlotAsync(CreateTimeSlotDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating time slot: {Time}, companyId: {CompanyId}", dto.Time, companyId);

        // Validate time format (basic check)
        if (string.IsNullOrWhiteSpace(dto.Time))
        {
            throw new ArgumentException("Time cannot be empty", nameof(dto));
        }

        // Check for duplicates
        var existingQuery = _context.Set<TimeSlot>().AsQueryable();
        if (companyId.HasValue)
        {
            existingQuery = existingQuery.Where(t => t.CompanyId == companyId.Value);
        }

        var trimmedTime = dto.Time.Trim();
        var duplicate = await existingQuery
            .FirstOrDefaultAsync(t => t.Time.Trim().ToLower() == trimmedTime.ToLower(), cancellationToken);

        if (duplicate != null)
        {
            throw new InvalidOperationException($"Time slot '{dto.Time}' already exists");
        }

        var timeSlot = new TimeSlot
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Time = dto.Time.Trim(),
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<TimeSlot>().Add(timeSlot);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Time slot created successfully: {Id}", timeSlot.Id);

        return new TimeSlotDto
        {
            Id = timeSlot.Id,
            Time = timeSlot.Time,
            SortOrder = timeSlot.SortOrder,
            IsActive = timeSlot.IsActive,
            CreatedAt = timeSlot.CreatedAt,
            UpdatedAt = timeSlot.UpdatedAt
        };
    }

    public async Task<TimeSlotDto> UpdateTimeSlotAsync(Guid id, UpdateTimeSlotDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating time slot: {Id}, companyId: {CompanyId}", id, companyId);

        var query = _context.Set<TimeSlot>().AsQueryable();
        if (companyId.HasValue)
        {
            query = query.Where(t => t.CompanyId == companyId.Value);
        }

        var timeSlot = await query.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (timeSlot == null)
        {
            throw new KeyNotFoundException($"Time slot with ID {id} not found");
        }

        // Check for duplicate time if time is being updated
        if (!string.IsNullOrWhiteSpace(dto.Time) && dto.Time.Trim() != timeSlot.Time)
        {
            var existingQuery = _context.Set<TimeSlot>().AsQueryable();
            if (companyId.HasValue)
            {
                existingQuery = existingQuery.Where(t => t.CompanyId == companyId.Value);
            }

            var trimmedTime = dto.Time.Trim();
            var duplicate = await existingQuery
                .FirstOrDefaultAsync(t => t.Id != id && t.Time.Trim().ToLower() == trimmedTime.ToLower(), cancellationToken);

            if (duplicate != null)
            {
                throw new InvalidOperationException($"Time slot '{dto.Time}' already exists");
            }

            timeSlot.Time = dto.Time.Trim();
        }

        if (dto.SortOrder.HasValue)
        {
            timeSlot.SortOrder = dto.SortOrder.Value;
        }

        if (dto.IsActive.HasValue)
        {
            timeSlot.IsActive = dto.IsActive.Value;
        }

        timeSlot.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Time slot updated successfully: {Id}", id);

        return new TimeSlotDto
        {
            Id = timeSlot.Id,
            Time = timeSlot.Time,
            SortOrder = timeSlot.SortOrder,
            IsActive = timeSlot.IsActive,
            CreatedAt = timeSlot.CreatedAt,
            UpdatedAt = timeSlot.UpdatedAt
        };
    }

    public async Task DeleteTimeSlotAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting time slot: {Id}, companyId: {CompanyId}", id, companyId);

        var query = _context.Set<TimeSlot>().AsQueryable();
        if (companyId.HasValue)
        {
            query = query.Where(t => t.CompanyId == companyId.Value);
        }

        var timeSlot = await query.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (timeSlot == null)
        {
            throw new KeyNotFoundException($"Time slot with ID {id} not found");
        }

        _context.Set<TimeSlot>().Remove(timeSlot);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Time slot deleted successfully: {Id}", id);
    }

    public async Task ReorderTimeSlotsAsync(ReorderTimeSlotsDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reordering time slots, companyId: {CompanyId}, count: {Count}", companyId, dto.TimeSlotIds.Count);

        var query = _context.Set<TimeSlot>().AsQueryable();
        if (companyId.HasValue)
        {
            query = query.Where(t => t.CompanyId == companyId.Value);
        }

        var timeSlots = await query
            .Where(t => dto.TimeSlotIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (timeSlots.Count != dto.TimeSlotIds.Count)
        {
            throw new InvalidOperationException("Some time slot IDs were not found");
        }

        // Update sort order based on the order of IDs in the list
        for (int i = 0; i < dto.TimeSlotIds.Count; i++)
        {
            var timeSlot = timeSlots.FirstOrDefault(t => t.Id == dto.TimeSlotIds[i]);
            if (timeSlot != null)
            {
                timeSlot.SortOrder = i;
                timeSlot.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Time slots reordered successfully");
    }

    public async Task<int> SeedDefaultTimeSlotsAsync(Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding default time slots, companyId: {CompanyId}", companyId);

        var query = _context.Set<TimeSlot>().AsQueryable();
        if (companyId.HasValue)
        {
            query = query.Where(t => t.CompanyId == companyId.Value);
        }

        var existingCount = await query.CountAsync(cancellationToken);

        if (existingCount > 0)
        {
            throw new InvalidOperationException("Time slots already exist. Delete existing slots first.");
        }

        // Default time slots: 8:00 AM - 5:30 PM, 30-minute intervals (20 slots)
        var defaultTimes = new[]
        {
            "8:00 AM", "8:30 AM", "9:00 AM", "9:30 AM",
            "10:00 AM", "10:30 AM", "11:00 AM", "11:30 AM",
            "12:00 PM", "12:30 PM", "1:00 PM", "1:30 PM",
            "2:00 PM", "2:30 PM", "3:00 PM", "3:30 PM",
            "4:00 PM", "4:30 PM", "5:00 PM", "5:30 PM"
        };

        var timeSlots = defaultTimes.Select((time, index) => new TimeSlot
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Time = time,
            SortOrder = index,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        _context.Set<TimeSlot>().AddRange(timeSlots);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} default time slots", timeSlots.Count);

        return timeSlots.Count;
    }
}

