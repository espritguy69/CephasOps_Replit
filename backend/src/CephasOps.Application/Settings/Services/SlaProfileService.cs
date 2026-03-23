using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// SLA profile service implementation
/// </summary>
public class SlaProfileService : ISlaProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SlaProfileService> _logger;

    public SlaProfileService(ApplicationDbContext context, ILogger<SlaProfileService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SlaProfileDto>> GetProfilesAsync(Guid companyId, string? orderType = null, Guid? partnerId = null, Guid? departmentId = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting SLA profiles for company {CompanyId}", companyId);

        var query = _context.SlaProfiles
            .Where(p => p.CompanyId == companyId);

        if (!string.IsNullOrEmpty(orderType))
        {
            query = query.Where(p => p.OrderType == orderType);
        }

        if (partnerId.HasValue)
        {
            query = query.Where(p => p.PartnerId == partnerId);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(p => p.DepartmentId == departmentId);
        }

        if (isActive.HasValue)
        {
            var now = DateTime.UtcNow;
            if (isActive.Value)
            {
                query = query.Where(p => p.IsActive
                    && (!p.EffectiveFrom.HasValue || p.EffectiveFrom <= now)
                    && (!p.EffectiveTo.HasValue || p.EffectiveTo >= now));
            }
            else
            {
                query = query.Where(p => !p.IsActive
                    || (p.EffectiveTo.HasValue && p.EffectiveTo < now)
                    || (p.EffectiveFrom.HasValue && p.EffectiveFrom > now));
            }
        }

        var profiles = await query.OrderBy(p => p.OrderType).ThenBy(p => p.Name).ToListAsync(cancellationToken);

        return profiles.Select(MapToDto).ToList();
    }

    public async Task<SlaProfileDto?> GetProfileByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting SLA profile {ProfileId} for company {CompanyId}", id, companyId);

        var profile = await _context.SlaProfiles
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken);

        if (profile == null) return null;

        return MapToDto(profile);
    }

    public async Task<SlaProfileDto?> GetEffectiveProfileAsync(Guid companyId, Guid? partnerId, string orderType, Guid? departmentId, bool isVip = false, DateTime? effectiveDate = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting effective SLA profile for company {CompanyId}, partner {PartnerId}, orderType {OrderType}, department {DepartmentId}, isVip {IsVip}", 
            companyId, partnerId, orderType, departmentId, isVip);

        var effective = effectiveDate ?? DateTime.UtcNow;

        // Try most specific match first: CompanyId + PartnerId + OrderType + DepartmentId + IsVip
        var profile = await _context.SlaProfiles
            .Where(p => p.CompanyId == companyId 
                && p.OrderType == orderType
                && p.IsActive
                && (effective >= p.EffectiveFrom || !p.EffectiveFrom.HasValue)
                && (effective <= p.EffectiveTo || !p.EffectiveTo.HasValue)
                && (partnerId == null || p.PartnerId == partnerId)
                && (departmentId == null || p.DepartmentId == departmentId)
                && (!isVip || p.IsVipOnly))
            .OrderByDescending(p => p.IsVipOnly ? 1 : 0)
            .ThenByDescending(p => p.PartnerId != null ? 1 : 0)
            .ThenByDescending(p => p.DepartmentId != null ? 1 : 0)
            .FirstOrDefaultAsync(cancellationToken);

        // Fallback to default profile
        if (profile == null)
        {
            profile = await _context.SlaProfiles
                .Where(p => p.CompanyId == companyId 
                    && p.OrderType == orderType 
                    && p.IsDefault
                    && p.IsActive
                    && (effective >= p.EffectiveFrom || !p.EffectiveFrom.HasValue)
                    && (effective <= p.EffectiveTo || !p.EffectiveTo.HasValue))
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (profile == null) return null;

        return MapToDto(profile);
    }

    public async Task<SlaProfileDto> CreateProfileAsync(CreateSlaProfileDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating SLA profile for company {CompanyId}", companyId);

        var profile = new SlaProfile
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = dto.Name,
            Description = dto.Description,
            PartnerId = dto.PartnerId,
            OrderType = dto.OrderType,
            DepartmentId = dto.DepartmentId,
            IsVipOnly = dto.IsVipOnly,
            ResponseSlaMinutes = dto.ResponseSlaMinutes,
            ResponseSlaFromStatus = dto.ResponseSlaFromStatus,
            ResponseSlaToStatus = dto.ResponseSlaToStatus,
            ResolutionSlaMinutes = dto.ResolutionSlaMinutes,
            ResolutionSlaFromStatus = dto.ResolutionSlaFromStatus,
            ResolutionSlaToStatus = dto.ResolutionSlaToStatus,
            EscalationThresholdPercent = dto.EscalationThresholdPercent,
            EscalationRole = dto.EscalationRole,
            EscalationUserId = dto.EscalationUserId,
            NotifyOnEscalation = dto.NotifyOnEscalation,
            NotifyOnBreach = dto.NotifyOnBreach,
            ExcludeNonBusinessHours = dto.ExcludeNonBusinessHours,
            ExcludeWeekends = dto.ExcludeWeekends,
            ExcludePublicHolidays = dto.ExcludePublicHolidays,
            IsDefault = dto.IsDefault,
            IsActive = dto.IsActive,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = userId
        };

        // If this is set as default, unset other defaults for the same context
        if (dto.IsDefault)
        {
            var existingDefaults = await _context.SlaProfiles
                .Where(p => p.CompanyId == companyId 
                    && p.OrderType == dto.OrderType 
                    && p.DepartmentId == dto.DepartmentId
                    && p.PartnerId == dto.PartnerId
                    && p.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = userId;
            }
        }

        _context.SlaProfiles.Add(profile);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created SLA profile {ProfileId}", profile.Id);

        return MapToDto(profile);
    }

    public async Task<SlaProfileDto> UpdateProfileAsync(Guid id, UpdateSlaProfileDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating SLA profile {ProfileId} for company {CompanyId}", id, companyId);

        var profile = await _context.SlaProfiles
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken);

        if (profile == null)
        {
            throw new KeyNotFoundException($"SLA profile with ID {id} not found");
        }

        if (!string.IsNullOrEmpty(dto.Name))
        {
            profile.Name = dto.Name;
        }

        if (dto.Description != null)
        {
            profile.Description = dto.Description;
        }

        if (dto.ResponseSlaMinutes.HasValue)
        {
            profile.ResponseSlaMinutes = dto.ResponseSlaMinutes;
        }

        if (dto.ResponseSlaFromStatus != null)
        {
            profile.ResponseSlaFromStatus = dto.ResponseSlaFromStatus;
        }

        if (dto.ResponseSlaToStatus != null)
        {
            profile.ResponseSlaToStatus = dto.ResponseSlaToStatus;
        }

        if (dto.ResolutionSlaMinutes.HasValue)
        {
            profile.ResolutionSlaMinutes = dto.ResolutionSlaMinutes;
        }

        if (dto.ResolutionSlaFromStatus != null)
        {
            profile.ResolutionSlaFromStatus = dto.ResolutionSlaFromStatus;
        }

        if (dto.ResolutionSlaToStatus != null)
        {
            profile.ResolutionSlaToStatus = dto.ResolutionSlaToStatus;
        }

        if (dto.EscalationThresholdPercent.HasValue)
        {
            profile.EscalationThresholdPercent = dto.EscalationThresholdPercent;
        }

        if (dto.EscalationRole != null)
        {
            profile.EscalationRole = dto.EscalationRole;
        }

        if (dto.EscalationUserId.HasValue)
        {
            profile.EscalationUserId = dto.EscalationUserId;
        }

        if (dto.NotifyOnEscalation.HasValue)
        {
            profile.NotifyOnEscalation = dto.NotifyOnEscalation.Value;
        }

        if (dto.NotifyOnBreach.HasValue)
        {
            profile.NotifyOnBreach = dto.NotifyOnBreach.Value;
        }

        if (dto.ExcludeNonBusinessHours.HasValue)
        {
            profile.ExcludeNonBusinessHours = dto.ExcludeNonBusinessHours.Value;
        }

        if (dto.ExcludeWeekends.HasValue)
        {
            profile.ExcludeWeekends = dto.ExcludeWeekends.Value;
        }

        if (dto.ExcludePublicHolidays.HasValue)
        {
            profile.ExcludePublicHolidays = dto.ExcludePublicHolidays.Value;
        }

        if (dto.IsDefault.HasValue && dto.IsDefault.Value)
        {
            // Unset other defaults
            var existingDefaults = await _context.SlaProfiles
                .Where(p => p.CompanyId == companyId 
                    && p.OrderType == profile.OrderType 
                    && p.DepartmentId == profile.DepartmentId
                    && p.PartnerId == profile.PartnerId
                    && p.Id != id
                    && p.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = userId;
            }

            profile.IsDefault = true;
        }
        else if (dto.IsDefault.HasValue)
        {
            profile.IsDefault = false;
        }

        if (dto.IsActive.HasValue)
        {
            profile.IsActive = dto.IsActive.Value;
        }

        if (dto.EffectiveFrom.HasValue)
        {
            profile.EffectiveFrom = dto.EffectiveFrom;
        }

        if (dto.EffectiveTo.HasValue)
        {
            profile.EffectiveTo = dto.EffectiveTo;
        }

        profile.UpdatedAt = DateTime.UtcNow;
        profile.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated SLA profile {ProfileId}", id);

        return MapToDto(profile);
    }

    public async Task DeleteProfileAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting SLA profile {ProfileId} for company {CompanyId}", id, companyId);

        var profile = await _context.SlaProfiles
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken);

        if (profile == null)
        {
            throw new KeyNotFoundException($"SLA profile with ID {id} not found");
        }

        _context.SlaProfiles.Remove(profile);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted SLA profile {ProfileId}", id);
    }

    public async Task<SlaProfileDto> SetAsDefaultAsync(Guid id, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting SLA profile {ProfileId} as default for company {CompanyId}", id, companyId);

        var profile = await _context.SlaProfiles
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken);

        if (profile == null)
        {
            throw new KeyNotFoundException($"SLA profile with ID {id} not found");
        }

        // Unset other defaults for the same context
        var existingDefaults = await _context.SlaProfiles
            .Where(p => p.CompanyId == companyId 
                && p.OrderType == profile.OrderType 
                && p.DepartmentId == profile.DepartmentId
                && p.PartnerId == profile.PartnerId
                && p.Id != id
                && p.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingDefaults)
        {
            existing.IsDefault = false;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = userId;
        }

        profile.IsDefault = true;
        profile.UpdatedAt = DateTime.UtcNow;
        profile.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Set SLA profile {ProfileId} as default", id);

        return MapToDto(profile);
    }

    private static SlaProfileDto MapToDto(SlaProfile profile)
    {
        return new SlaProfileDto
        {
            Id = profile.Id,
            CompanyId = profile.CompanyId,
            Name = profile.Name,
            Description = profile.Description,
            PartnerId = profile.PartnerId,
            OrderType = profile.OrderType,
            DepartmentId = profile.DepartmentId,
            IsVipOnly = profile.IsVipOnly,
            ResponseSlaMinutes = profile.ResponseSlaMinutes,
            ResponseSlaFromStatus = profile.ResponseSlaFromStatus,
            ResponseSlaToStatus = profile.ResponseSlaToStatus,
            ResolutionSlaMinutes = profile.ResolutionSlaMinutes,
            ResolutionSlaFromStatus = profile.ResolutionSlaFromStatus,
            ResolutionSlaToStatus = profile.ResolutionSlaToStatus,
            EscalationThresholdPercent = profile.EscalationThresholdPercent,
            EscalationRole = profile.EscalationRole,
            EscalationUserId = profile.EscalationUserId,
            NotifyOnEscalation = profile.NotifyOnEscalation,
            NotifyOnBreach = profile.NotifyOnBreach,
            ExcludeNonBusinessHours = profile.ExcludeNonBusinessHours,
            ExcludeWeekends = profile.ExcludeWeekends,
            ExcludePublicHolidays = profile.ExcludePublicHolidays,
            IsDefault = profile.IsDefault,
            IsActive = profile.IsActive,
            EffectiveFrom = profile.EffectiveFrom,
            EffectiveTo = profile.EffectiveTo,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }
}

