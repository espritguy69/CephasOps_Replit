using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Utilities;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Business Hours service implementation
/// </summary>
public class BusinessHoursService : IBusinessHoursService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BusinessHoursService> _logger;

    public BusinessHoursService(ApplicationDbContext context, ILogger<BusinessHoursService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<BusinessHoursDto>> GetBusinessHoursAsync(Guid companyId, Guid? departmentId = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting business hours for company {CompanyId}", companyId);

        var query = _context.BusinessHours
            .Where(b => b.CompanyId == companyId);

        if (departmentId.HasValue)
        {
            query = query.Where(b => b.DepartmentId == departmentId);
        }

        if (isActive.HasValue)
        {
            var now = DateTime.UtcNow;
            if (isActive.Value)
            {
                query = query.Where(b => b.IsActive
                    && (!b.EffectiveFrom.HasValue || b.EffectiveFrom <= now)
                    && (!b.EffectiveTo.HasValue || b.EffectiveTo >= now));
            }
        }

        var businessHours = await query.OrderBy(b => b.Name).ToListAsync(cancellationToken);

        return businessHours.Select(MapToDto).ToList();
    }

    public async Task<BusinessHoursDto?> GetBusinessHoursByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        var businessHours = await _context.BusinessHours
            .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId, cancellationToken);

        if (businessHours == null) return null;

        return MapToDto(businessHours);
    }

    public async Task<BusinessHoursDto?> GetEffectiveBusinessHoursAsync(Guid companyId, Guid? departmentId = null, DateTime? effectiveDate = null, CancellationToken cancellationToken = default)
    {
        var effective = effectiveDate ?? DateTime.UtcNow;

        var query = _context.BusinessHours
            .Where(b => b.CompanyId == companyId
                && b.IsActive
                && (!b.EffectiveFrom.HasValue || b.EffectiveFrom <= effective)
                && (!b.EffectiveTo.HasValue || b.EffectiveTo >= effective));

        if (departmentId.HasValue)
        {
            query = query.Where(b => b.DepartmentId == null || b.DepartmentId == departmentId);
        }

        var businessHours = await query
            .OrderByDescending(b => b.DepartmentId != null ? 1 : 0)
            .ThenByDescending(b => b.IsDefault ? 1 : 0)
            .FirstOrDefaultAsync(cancellationToken);

        if (businessHours == null) return null;

        return MapToDto(businessHours);
    }

    public async Task<bool> IsBusinessHoursAsync(Guid companyId, DateTime dateTime, Guid? departmentId = null, CancellationToken cancellationToken = default)
    {
        var businessHours = await GetEffectiveBusinessHoursAsync(companyId, departmentId, dateTime, cancellationToken);
        
        if (businessHours == null) return true; // Default to always open if no config

        var dayOfWeek = dateTime.DayOfWeek;
        var time = dateTime.ToString("HH:mm");

        return dayOfWeek switch
        {
            DayOfWeek.Monday => IsTimeInRange(time, businessHours.MondayStart, businessHours.MondayEnd),
            DayOfWeek.Tuesday => IsTimeInRange(time, businessHours.TuesdayStart, businessHours.TuesdayEnd),
            DayOfWeek.Wednesday => IsTimeInRange(time, businessHours.WednesdayStart, businessHours.WednesdayEnd),
            DayOfWeek.Thursday => IsTimeInRange(time, businessHours.ThursdayStart, businessHours.ThursdayEnd),
            DayOfWeek.Friday => IsTimeInRange(time, businessHours.FridayStart, businessHours.FridayEnd),
            DayOfWeek.Saturday => IsTimeInRange(time, businessHours.SaturdayStart, businessHours.SaturdayEnd),
            DayOfWeek.Sunday => IsTimeInRange(time, businessHours.SundayStart, businessHours.SundayEnd),
            _ => false
        };
    }

    private static bool IsTimeInRange(string time, string? start, string? end)
    {
        if (string.IsNullOrEmpty(start) || string.IsNullOrEmpty(end)) return true; // No restriction = always open
        
        return string.Compare(time, start, StringComparison.Ordinal) >= 0 
            && string.Compare(time, end, StringComparison.Ordinal) <= 0;
    }

    public async Task<BusinessHoursDto> CreateBusinessHoursAsync(CreateBusinessHoursDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating business hours for company {CompanyId}", companyId);

        // Convert all time fields from 12-hour to 24-hour format if needed
        var businessHours = new BusinessHours
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = dto.Name,
            Description = dto.Description,
            DepartmentId = dto.DepartmentId,
            Timezone = dto.Timezone,
            MondayStart = TimeFormatConverter.NormalizeTime(dto.MondayStart),
            MondayEnd = TimeFormatConverter.NormalizeTime(dto.MondayEnd),
            TuesdayStart = TimeFormatConverter.NormalizeTime(dto.TuesdayStart),
            TuesdayEnd = TimeFormatConverter.NormalizeTime(dto.TuesdayEnd),
            WednesdayStart = TimeFormatConverter.NormalizeTime(dto.WednesdayStart),
            WednesdayEnd = TimeFormatConverter.NormalizeTime(dto.WednesdayEnd),
            ThursdayStart = TimeFormatConverter.NormalizeTime(dto.ThursdayStart),
            ThursdayEnd = TimeFormatConverter.NormalizeTime(dto.ThursdayEnd),
            FridayStart = TimeFormatConverter.NormalizeTime(dto.FridayStart),
            FridayEnd = TimeFormatConverter.NormalizeTime(dto.FridayEnd),
            SaturdayStart = TimeFormatConverter.NormalizeTime(dto.SaturdayStart),
            SaturdayEnd = TimeFormatConverter.NormalizeTime(dto.SaturdayEnd),
            SundayStart = TimeFormatConverter.NormalizeTime(dto.SundayStart),
            SundayEnd = TimeFormatConverter.NormalizeTime(dto.SundayEnd),
            IsDefault = dto.IsDefault,
            IsActive = dto.IsActive,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = userId
        };

        if (dto.IsDefault)
        {
            var existingDefaults = await _context.BusinessHours
                .Where(b => b.CompanyId == companyId && b.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = userId;
            }
        }

        _context.BusinessHours.Add(businessHours);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created business hours {BusinessHoursId}", businessHours.Id);

        return MapToDto(businessHours);
    }

    public async Task<BusinessHoursDto> UpdateBusinessHoursAsync(Guid id, UpdateBusinessHoursDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating business hours {BusinessHoursId} for company {CompanyId}", id, companyId);

        var businessHours = await _context.BusinessHours
            .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId, cancellationToken);

        if (businessHours == null)
        {
            throw new KeyNotFoundException($"Business hours with ID {id} not found");
        }

        if (!string.IsNullOrEmpty(dto.Name))
        {
            businessHours.Name = dto.Name;
        }

        if (dto.Description != null)
        {
            businessHours.Description = dto.Description;
        }

        if (!string.IsNullOrEmpty(dto.Timezone))
        {
            businessHours.Timezone = dto.Timezone;
        }

        // Update day-specific hours (convert from 12-hour to 24-hour format if needed)
        if (dto.MondayStart != null) businessHours.MondayStart = TimeFormatConverter.NormalizeTime(dto.MondayStart);
        if (dto.MondayEnd != null) businessHours.MondayEnd = TimeFormatConverter.NormalizeTime(dto.MondayEnd);
        if (dto.TuesdayStart != null) businessHours.TuesdayStart = TimeFormatConverter.NormalizeTime(dto.TuesdayStart);
        if (dto.TuesdayEnd != null) businessHours.TuesdayEnd = TimeFormatConverter.NormalizeTime(dto.TuesdayEnd);
        if (dto.WednesdayStart != null) businessHours.WednesdayStart = TimeFormatConverter.NormalizeTime(dto.WednesdayStart);
        if (dto.WednesdayEnd != null) businessHours.WednesdayEnd = TimeFormatConverter.NormalizeTime(dto.WednesdayEnd);
        if (dto.ThursdayStart != null) businessHours.ThursdayStart = TimeFormatConverter.NormalizeTime(dto.ThursdayStart);
        if (dto.ThursdayEnd != null) businessHours.ThursdayEnd = TimeFormatConverter.NormalizeTime(dto.ThursdayEnd);
        if (dto.FridayStart != null) businessHours.FridayStart = TimeFormatConverter.NormalizeTime(dto.FridayStart);
        if (dto.FridayEnd != null) businessHours.FridayEnd = TimeFormatConverter.NormalizeTime(dto.FridayEnd);
        if (dto.SaturdayStart != null) businessHours.SaturdayStart = TimeFormatConverter.NormalizeTime(dto.SaturdayStart);
        if (dto.SaturdayEnd != null) businessHours.SaturdayEnd = TimeFormatConverter.NormalizeTime(dto.SaturdayEnd);
        if (dto.SundayStart != null) businessHours.SundayStart = TimeFormatConverter.NormalizeTime(dto.SundayStart);
        if (dto.SundayEnd != null) businessHours.SundayEnd = TimeFormatConverter.NormalizeTime(dto.SundayEnd);

        if (dto.IsDefault.HasValue && dto.IsDefault.Value)
        {
            var existingDefaults = await _context.BusinessHours
                .Where(b => b.CompanyId == companyId && b.Id != id && b.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = userId;
            }

            businessHours.IsDefault = true;
        }
        else if (dto.IsDefault.HasValue)
        {
            businessHours.IsDefault = false;
        }

        if (dto.IsActive.HasValue)
        {
            businessHours.IsActive = dto.IsActive.Value;
        }

        if (dto.EffectiveFrom.HasValue)
        {
            businessHours.EffectiveFrom = dto.EffectiveFrom;
        }

        if (dto.EffectiveTo.HasValue)
        {
            businessHours.EffectiveTo = dto.EffectiveTo;
        }

        businessHours.UpdatedAt = DateTime.UtcNow;
        businessHours.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated business hours {BusinessHoursId}", id);

        return MapToDto(businessHours);
    }

    public async Task DeleteBusinessHoursAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting business hours {BusinessHoursId} for company {CompanyId}", id, companyId);

        var businessHours = await _context.BusinessHours
            .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId, cancellationToken);

        if (businessHours == null)
        {
            throw new KeyNotFoundException($"Business hours with ID {id} not found");
        }

        _context.BusinessHours.Remove(businessHours);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted business hours {BusinessHoursId}", id);
    }

    public async Task<List<PublicHolidayDto>> GetPublicHolidaysAsync(Guid companyId, int? year = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting public holidays for company {CompanyId}, year {Year}", companyId, year);

        var query = _context.PublicHolidays
            .Where(h => h.CompanyId == companyId);

        if (year.HasValue)
        {
            var startDate = new DateTime(year.Value, 1, 1);
            var endDate = new DateTime(year.Value, 12, 31);
            query = query.Where(h => h.HolidayDate >= startDate && h.HolidayDate <= endDate);
        }

        if (isActive.HasValue)
        {
            query = query.Where(h => h.IsActive == isActive.Value);
        }

        var holidays = await query.OrderBy(h => h.HolidayDate).ToListAsync(cancellationToken);

        return holidays.Select(MapHolidayToDto).ToList();
    }

    public async Task<PublicHolidayDto> CreatePublicHolidayAsync(CreatePublicHolidayDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating public holiday for company {CompanyId}", companyId);

        var holiday = new PublicHoliday
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = dto.Name,
            HolidayDate = dto.HolidayDate,
            HolidayType = dto.HolidayType,
            State = dto.State,
            IsRecurring = dto.IsRecurring,
            IsActive = dto.IsActive,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = userId
        };

        _context.PublicHolidays.Add(holiday);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created public holiday {HolidayId}", holiday.Id);

        return MapHolidayToDto(holiday);
    }

    public async Task<PublicHolidayDto> UpdatePublicHolidayAsync(Guid id, UpdatePublicHolidayDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating public holiday {HolidayId} for company {CompanyId}", id, companyId);

        var holiday = await _context.PublicHolidays
            .FirstOrDefaultAsync(h => h.Id == id && h.CompanyId == companyId, cancellationToken);

        if (holiday == null)
        {
            throw new KeyNotFoundException($"Public holiday with ID {id} not found");
        }

        if (!string.IsNullOrEmpty(dto.Name))
        {
            holiday.Name = dto.Name;
        }

        if (dto.HolidayDate.HasValue)
        {
            holiday.HolidayDate = dto.HolidayDate.Value;
        }

        if (!string.IsNullOrEmpty(dto.HolidayType))
        {
            holiday.HolidayType = dto.HolidayType;
        }

        if (dto.State != null)
        {
            holiday.State = dto.State;
        }

        if (dto.IsRecurring.HasValue)
        {
            holiday.IsRecurring = dto.IsRecurring.Value;
        }

        if (dto.IsActive.HasValue)
        {
            holiday.IsActive = dto.IsActive.Value;
        }

        if (dto.Description != null)
        {
            holiday.Description = dto.Description;
        }

        holiday.UpdatedAt = DateTime.UtcNow;
        holiday.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated public holiday {HolidayId}", id);

        return MapHolidayToDto(holiday);
    }

    public async Task DeletePublicHolidayAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting public holiday {HolidayId} for company {CompanyId}", id, companyId);

        var holiday = await _context.PublicHolidays
            .FirstOrDefaultAsync(h => h.Id == id && h.CompanyId == companyId, cancellationToken);

        if (holiday == null)
        {
            throw new KeyNotFoundException($"Public holiday with ID {id} not found");
        }

        _context.PublicHolidays.Remove(holiday);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted public holiday {HolidayId}", id);
    }

    public async Task<bool> IsPublicHolidayAsync(Guid companyId, DateTime date, CancellationToken cancellationToken = default)
    {
        var dateOnly = date.Date;

        var isHoliday = await _context.PublicHolidays
            .AnyAsync(h => h.CompanyId == companyId
                && h.IsActive
                && (h.HolidayDate.Date == dateOnly
                    || (h.IsRecurring && h.HolidayDate.Day == dateOnly.Day && h.HolidayDate.Month == dateOnly.Month)),
                cancellationToken);

        return isHoliday;
    }

    private static BusinessHoursDto MapToDto(BusinessHours businessHours)
    {
        return new BusinessHoursDto
        {
            Id = businessHours.Id,
            CompanyId = businessHours.CompanyId,
            Name = businessHours.Name,
            Description = businessHours.Description,
            DepartmentId = businessHours.DepartmentId,
            Timezone = businessHours.Timezone,
            MondayStart = businessHours.MondayStart,
            MondayEnd = businessHours.MondayEnd,
            TuesdayStart = businessHours.TuesdayStart,
            TuesdayEnd = businessHours.TuesdayEnd,
            WednesdayStart = businessHours.WednesdayStart,
            WednesdayEnd = businessHours.WednesdayEnd,
            ThursdayStart = businessHours.ThursdayStart,
            ThursdayEnd = businessHours.ThursdayEnd,
            FridayStart = businessHours.FridayStart,
            FridayEnd = businessHours.FridayEnd,
            SaturdayStart = businessHours.SaturdayStart,
            SaturdayEnd = businessHours.SaturdayEnd,
            SundayStart = businessHours.SundayStart,
            SundayEnd = businessHours.SundayEnd,
            IsDefault = businessHours.IsDefault,
            IsActive = businessHours.IsActive,
            EffectiveFrom = businessHours.EffectiveFrom,
            EffectiveTo = businessHours.EffectiveTo,
            CreatedAt = businessHours.CreatedAt,
            UpdatedAt = businessHours.UpdatedAt
        };
    }

    private static PublicHolidayDto MapHolidayToDto(PublicHoliday holiday)
    {
        return new PublicHolidayDto
        {
            Id = holiday.Id,
            CompanyId = holiday.CompanyId,
            Name = holiday.Name,
            HolidayDate = holiday.HolidayDate,
            HolidayType = holiday.HolidayType,
            State = holiday.State,
            IsRecurring = holiday.IsRecurring,
            IsActive = holiday.IsActive,
            Description = holiday.Description,
            CreatedAt = holiday.CreatedAt,
            UpdatedAt = holiday.UpdatedAt
        };
    }

    /// <summary>
    /// Creates a template business hours configuration (8am-6pm Monday-Friday)
    /// </summary>
    public async Task<BusinessHoursDto> CreateTemplateBusinessHoursAsync(
        string name = "Standard Business Hours (8am-6pm)",
        Guid? departmentId = null,
        Guid? companyId = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (companyId == null || userId == null)
        {
            throw new ArgumentException("CompanyId and UserId are required");
        }

        _logger.LogInformation("Creating template business hours (8am-6pm Mon-Fri) for company {CompanyId}", companyId);

        var templateDto = new CreateBusinessHoursDto
        {
            Name = name,
            Description = "Standard business hours template: Monday to Friday, 8:00 AM to 6:00 PM",
            DepartmentId = departmentId,
            Timezone = "Asia/Kuala_Lumpur",
            // Monday to Friday: 8am - 6pm
            MondayStart = "8:00 AM",
            MondayEnd = "6:00 PM",
            TuesdayStart = "8:00 AM",
            TuesdayEnd = "6:00 PM",
            WednesdayStart = "8:00 AM",
            WednesdayEnd = "6:00 PM",
            ThursdayStart = "8:00 AM",
            ThursdayEnd = "6:00 PM",
            FridayStart = "8:00 AM",
            FridayEnd = "6:00 PM",
            // Saturday and Sunday: closed (null)
            SaturdayStart = null,
            SaturdayEnd = null,
            SundayStart = null,
            SundayEnd = null,
            IsDefault = false,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.Date,
            EffectiveTo = null
        };

        return await CreateBusinessHoursAsync(templateDto, companyId.Value, userId.Value, cancellationToken);
    }
}

