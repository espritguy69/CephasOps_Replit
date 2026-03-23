using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// KPI profile service implementation
/// </summary>
public class KpiProfileService : IKpiProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<KpiProfileService> _logger;

    public KpiProfileService(ApplicationDbContext context, ILogger<KpiProfileService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<KpiProfileDto>> GetProfilesAsync(Guid companyId, string? orderType = null, Guid? partnerId = null, Guid? installationMethodId = null, Guid? buildingTypeId = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting KPI profiles for company {CompanyId}", companyId);

        if (!installationMethodId.HasValue && buildingTypeId.HasValue)
        {
            _logger.LogWarning("BuildingTypeId is deprecated. Use InstallationMethodId instead when querying KPI profiles.");
        }

        var query = _context.KpiProfiles
            .Where(p => p.CompanyId == companyId);

        if (!string.IsNullOrEmpty(orderType))
        {
            query = query.Where(p => p.OrderType == orderType);
        }

        if (partnerId.HasValue)
        {
            query = query.Where(p => p.PartnerId == partnerId);
        }

        if (installationMethodId.HasValue)
        {
            query = query.Where(p => p.InstallationMethodId == installationMethodId);
        }
        else if (buildingTypeId.HasValue)
        {
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            query = query.Where(p => p.BuildingTypeId == buildingTypeId);
#pragma warning restore CS0618
        }

        if (isActive.HasValue)
        {
            var now = DateTime.UtcNow;
            if (isActive.Value)
            {
                query = query.Where(p => (!p.EffectiveFrom.HasValue || p.EffectiveFrom <= now)
                    && (!p.EffectiveTo.HasValue || p.EffectiveTo >= now));
            }
            else
            {
                query = query.Where(p => (p.EffectiveTo.HasValue && p.EffectiveTo < now)
                    || (p.EffectiveFrom.HasValue && p.EffectiveFrom > now));
            }
        }

        var profiles = await query.OrderBy(p => p.OrderType).ThenBy(p => p.Name).ToListAsync(cancellationToken);

        return profiles.Select(MapToDto).ToList();
    }

    public async Task<KpiProfileDto?> GetProfileByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting KPI profile {ProfileId} for company {CompanyId}", id, companyId);

        var profile = await _context.KpiProfiles
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken);

        if (profile == null) return null;

        return MapToDto(profile);
    }

    public async Task<KpiProfileDto?> GetEffectiveProfileAsync(Guid companyId, Guid? partnerId, string orderType, Guid? installationMethodId, Guid? buildingTypeId, DateTime? jobDate = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderType))
        {
            throw new ArgumentException("OrderType cannot be null or empty", nameof(orderType));
        }

        if (!installationMethodId.HasValue && buildingTypeId.HasValue)
        {
            _logger.LogWarning("BuildingTypeId is deprecated. Use InstallationMethodId instead when resolving KPI profiles.");
        }

        _logger.LogInformation("Getting effective KPI profile for company {CompanyId}, partner {PartnerId}, orderType {OrderType}, installationMethod {InstallationMethodId}, buildingType {BuildingTypeId}", 
            companyId, partnerId, orderType, installationMethodId, buildingTypeId);

        var effectiveDate = jobDate ?? DateTime.UtcNow;

        // Try most specific match first: CompanyId + PartnerId + OrderType + InstallationMethodId/BuildingTypeId
        var profileQuery = _context.KpiProfiles
            .Where(p => p.CompanyId == companyId
                && p.OrderType == orderType
                && (effectiveDate >= p.EffectiveFrom || !p.EffectiveFrom.HasValue)
                && (effectiveDate <= p.EffectiveTo || !p.EffectiveTo.HasValue)
                && (partnerId == null || p.PartnerId == partnerId));

        if (installationMethodId.HasValue)
        {
            profileQuery = profileQuery.Where(p => p.InstallationMethodId == installationMethodId);
        }
        else if (buildingTypeId.HasValue)
        {
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            profileQuery = profileQuery.Where(p => p.InstallationMethodId == null && p.BuildingTypeId == buildingTypeId);
#pragma warning restore CS0618
        }
        else
        {
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            profileQuery = profileQuery.Where(p => p.InstallationMethodId == null && p.BuildingTypeId == null);
#pragma warning restore CS0618
        }

        var profile = await profileQuery
            .OrderByDescending(p => p.PartnerId != null ? 1 : 0)
            .ThenByDescending(p => p.InstallationMethodId != null ? 1 : 0)
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            .ThenByDescending(p => p.BuildingTypeId != null ? 1 : 0)
#pragma warning restore CS0618
            .FirstOrDefaultAsync(cancellationToken);

        // Fallback to default profile
        if (profile == null)
        {
            var defaultQuery = _context.KpiProfiles
                .Where(p => p.CompanyId == companyId 
                    && p.OrderType == orderType 
                    && p.IsDefault
                    && (effectiveDate >= p.EffectiveFrom || !p.EffectiveFrom.HasValue)
                    && (effectiveDate <= p.EffectiveTo || !p.EffectiveTo.HasValue));

            if (installationMethodId.HasValue)
            {
                defaultQuery = defaultQuery.Where(p => p.InstallationMethodId == installationMethodId);
            }
            else if (buildingTypeId.HasValue)
            {
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
                defaultQuery = defaultQuery.Where(p => p.InstallationMethodId == null && p.BuildingTypeId == buildingTypeId);
#pragma warning restore CS0618
            }
            else
            {
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
                defaultQuery = defaultQuery.Where(p => p.InstallationMethodId == null && p.BuildingTypeId == null);
#pragma warning restore CS0618
            }

            profile = await defaultQuery.FirstOrDefaultAsync(cancellationToken);
        }

        if (profile == null) return null;

        return MapToDto(profile);
    }

    public async Task<KpiProfileDto> CreateProfileAsync(CreateKpiProfileDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating KPI profile for company {CompanyId}", companyId);

        if (!dto.InstallationMethodId.HasValue && dto.BuildingTypeId.HasValue)
        {
            _logger.LogWarning("BuildingTypeId is deprecated. Use InstallationMethodId instead when creating KPI profiles.");
        }

        // Validate effective date range
        if (dto.EffectiveFrom.HasValue && dto.EffectiveTo.HasValue && dto.EffectiveFrom >= dto.EffectiveTo)
        {
            throw new ArgumentException(
                $"EffectiveFrom ({dto.EffectiveFrom}) must be before EffectiveTo ({dto.EffectiveTo})",
                nameof(dto));
        }

        // Check for overlapping date ranges with same context
        if (dto.EffectiveFrom.HasValue || dto.EffectiveTo.HasValue)
        {
            var overlappingQuery = _context.KpiProfiles
                .Where(p => p.CompanyId == companyId
                    && p.OrderType == dto.OrderType
                    && p.PartnerId == dto.PartnerId);

            if (dto.InstallationMethodId.HasValue)
            {
                overlappingQuery = overlappingQuery.Where(p => p.InstallationMethodId == dto.InstallationMethodId);
            }
            else
            {
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
                overlappingQuery = overlappingQuery.Where(p => p.BuildingTypeId == dto.BuildingTypeId && p.InstallationMethodId == null);
#pragma warning restore CS0618
            }

            var overlapping = await overlappingQuery
                .Where(p => 
                    // New profile starts before existing ends AND new profile ends after existing starts
                    (!dto.EffectiveTo.HasValue || !p.EffectiveFrom.HasValue || dto.EffectiveTo >= p.EffectiveFrom)
                    && (!dto.EffectiveFrom.HasValue || !p.EffectiveTo.HasValue || dto.EffectiveFrom <= p.EffectiveTo))
                .ToListAsync(cancellationToken);

            if (overlapping.Any() && !dto.IsDefault)
            {
                var existing = overlapping.First();
                throw new InvalidOperationException(
                    $"Overlapping effective date range found with existing profile '{existing.Name}' (ID: {existing.Id}). " +
                    $"Existing: {existing.EffectiveFrom} to {existing.EffectiveTo}, " +
                    $"New: {dto.EffectiveFrom} to {dto.EffectiveTo}. " +
                    "Only one active profile can exist for the same context (CompanyId, OrderType, PartnerId, BuildingTypeId/InstallationMethodId) at a time.");
            }
        }

        var profile = new KpiProfile
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = dto.Name,
            PartnerId = dto.PartnerId,
            OrderType = dto.OrderType,
            InstallationMethodId = dto.InstallationMethodId,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            BuildingTypeId = dto.BuildingTypeId,
#pragma warning restore CS0618
            MaxJobDurationMinutes = dto.MaxJobDurationMinutes,
            DocketKpiMinutes = dto.DocketKpiMinutes,
            MaxReschedulesAllowed = dto.MaxReschedulesAllowed,
            IsDefault = dto.IsDefault,
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
            var existingDefaults = await _context.KpiProfiles
                .Where(p => p.CompanyId == companyId 
                    && p.OrderType == dto.OrderType
                    && p.InstallationMethodId == dto.InstallationMethodId
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
                    && p.BuildingTypeId == dto.BuildingTypeId
#pragma warning restore CS0618
                    && p.PartnerId == dto.PartnerId
                    && p.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
            }
        }

        _context.KpiProfiles.Add(profile);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(profile);
    }

    public async Task<KpiProfileDto> UpdateProfileAsync(Guid id, UpdateKpiProfileDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating KPI profile {ProfileId} for company {CompanyId}", id, companyId);

        var profile = await _context.KpiProfiles
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken);

        if (profile == null)
        {
            throw new KeyNotFoundException($"KPI profile entity with ID {id} not found for company {companyId}");
        }

        if (!dto.InstallationMethodId.HasValue && profile.InstallationMethodId == null)
        {
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            if (profile.BuildingTypeId.HasValue)
#pragma warning restore CS0618
            {
                _logger.LogWarning("BuildingTypeId is deprecated. Use InstallationMethodId instead when updating KPI profiles.");
            }
        }

        // Validate effective date range if both are being updated
        if (dto.EffectiveFrom.HasValue && dto.EffectiveTo.HasValue && dto.EffectiveFrom >= dto.EffectiveTo)
        {
            throw new ArgumentException(
                $"EffectiveFrom ({dto.EffectiveFrom}) must be before EffectiveTo ({dto.EffectiveTo})",
                nameof(dto));
        }

        // Check for overlapping date ranges if dates are being changed
        if ((dto.EffectiveFrom.HasValue || dto.EffectiveTo.HasValue) && 
            (dto.EffectiveFrom != profile.EffectiveFrom || dto.EffectiveTo != profile.EffectiveTo))
        {
            var effectiveFrom = dto.EffectiveFrom ?? profile.EffectiveFrom;
            var effectiveTo = dto.EffectiveTo ?? profile.EffectiveTo;

            var overlappingQuery = _context.KpiProfiles
                .Where(p => p.CompanyId == companyId
                    && p.OrderType == profile.OrderType
                    && p.PartnerId == profile.PartnerId
                    && p.Id != id);

            var effectiveInstallationMethodId = dto.InstallationMethodId ?? profile.InstallationMethodId;
            if (effectiveInstallationMethodId.HasValue)
            {
                overlappingQuery = overlappingQuery.Where(p => p.InstallationMethodId == effectiveInstallationMethodId);
            }
            else
            {
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
                overlappingQuery = overlappingQuery.Where(p => p.BuildingTypeId == profile.BuildingTypeId && p.InstallationMethodId == null);
#pragma warning restore CS0618
            }

            var overlapping = await overlappingQuery
                .Where(p => 
                    // New profile starts before existing ends AND new profile ends after existing starts
                    (!effectiveTo.HasValue || !p.EffectiveFrom.HasValue || effectiveTo >= p.EffectiveFrom)
                    && (!effectiveFrom.HasValue || !p.EffectiveTo.HasValue || effectiveFrom <= p.EffectiveTo))
                .ToListAsync(cancellationToken);

            if (overlapping.Any() && !(dto.IsDefault ?? profile.IsDefault))
            {
                var existing = overlapping.First();
                throw new InvalidOperationException(
                    $"Overlapping effective date range found with existing profile '{existing.Name}' (ID: {existing.Id}). " +
                    $"Existing: {existing.EffectiveFrom} to {existing.EffectiveTo}, " +
                    $"Updated: {effectiveFrom} to {effectiveTo}. " +
                    "Only one active profile can exist for the same context (CompanyId, OrderType, PartnerId, BuildingTypeId/InstallationMethodId) at a time.");
            }
        }

        if (!string.IsNullOrEmpty(dto.Name))
        {
            profile.Name = dto.Name;
        }

        if (dto.MaxJobDurationMinutes.HasValue)
        {
            profile.MaxJobDurationMinutes = dto.MaxJobDurationMinutes.Value;
        }

        if (dto.DocketKpiMinutes.HasValue)
        {
            profile.DocketKpiMinutes = dto.DocketKpiMinutes.Value;
        }

        if (dto.MaxReschedulesAllowed.HasValue)
        {
            profile.MaxReschedulesAllowed = dto.MaxReschedulesAllowed;
        }

        if (dto.IsDefault.HasValue)
        {
            profile.IsDefault = dto.IsDefault.Value;
            
            // If setting as default, unset other defaults
            if (dto.IsDefault.Value)
            {
                var existingDefaults = await _context.KpiProfiles
                    .Where(p => p.CompanyId == companyId 
                        && p.OrderType == profile.OrderType
                        && p.InstallationMethodId == (dto.InstallationMethodId ?? profile.InstallationMethodId)
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
                && p.BuildingTypeId == profile.BuildingTypeId
#pragma warning restore CS0618
                        && p.PartnerId == profile.PartnerId
                        && p.Id != id
                        && p.IsDefault)
                    .ToListAsync(cancellationToken);

                foreach (var existing in existingDefaults)
                {
                    existing.IsDefault = false;
                }
            }
        }

        if (dto.InstallationMethodId.HasValue)
        {
            profile.InstallationMethodId = dto.InstallationMethodId;
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

        return MapToDto(profile);
    }

    public async Task DeleteProfileAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting KPI profile {ProfileId} for company {CompanyId}", id, companyId);

        var profile = await _context.KpiProfiles
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken);

        if (profile == null)
        {
            throw new KeyNotFoundException($"KPI profile entity with ID {id} not found for company {companyId}");
        }

        _context.KpiProfiles.Remove(profile);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<KpiProfileDto> SetAsDefaultAsync(Guid id, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting KPI profile {ProfileId} as default for company {CompanyId}", id, companyId);

        var profile = await _context.KpiProfiles
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken);

        if (profile == null)
        {
            throw new KeyNotFoundException($"KPI profile entity with ID {id} not found for company {companyId}");
        }

        // Unset other defaults for the same context
        var existingDefaults = await _context.KpiProfiles
            .Where(p => p.CompanyId == companyId 
                && p.OrderType == profile.OrderType
                && p.InstallationMethodId == profile.InstallationMethodId
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
                && p.BuildingTypeId == profile.BuildingTypeId
#pragma warning restore CS0618
                && p.PartnerId == profile.PartnerId
                && p.Id != id
                && p.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingDefaults)
        {
            existing.IsDefault = false;
        }

        profile.IsDefault = true;
        profile.UpdatedAt = DateTime.UtcNow;
        profile.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetProfileByIdAsync(id, companyId, cancellationToken) 
            ?? throw new InvalidOperationException("Failed to retrieve updated profile");
    }

    public async Task<KpiEvaluationResultDto> EvaluateOrderAsync(Guid orderId, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Evaluating KPI for order {OrderId}", orderId);

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CompanyId == companyId, cancellationToken);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order entity with ID {orderId} not found for company {companyId}");
        }

        // Get order type name/code from OrderTypes table
        var orderType = await _context.OrderTypes
            .FirstOrDefaultAsync(ot => ot.Id == order.OrderTypeId, cancellationToken);
        
        if (orderType == null)
        {
            throw new InvalidOperationException(
                $"OrderType entity with ID {order.OrderTypeId} not found for order {orderId}. " +
                "Cannot evaluate KPI without order type information.");
        }

        // Use order type name (or code if name is not available)
        var orderTypeName = !string.IsNullOrWhiteSpace(orderType.Name) ? orderType.Name : orderType.Code;

        // Get effective KPI profile
        var profile = await GetEffectiveProfileAsync(
            companyId, order.PartnerId, orderTypeName, order.InstallationMethodId, null, order.CreatedAt, cancellationToken);

        if (profile == null)
        {
            throw new InvalidOperationException(
                $"No KPI profile found for order {orderId} (CompanyId: {companyId}, OrderType: {orderTypeName}, PartnerId: {order.PartnerId}). " +
                "Please create a KPI profile matching this order's context.");
        }

        // Calculate actual job duration
        var statusLogs = await _context.OrderStatusLogs
            .Where(sl => sl.OrderId == order.Id)
            .OrderBy(sl => sl.CreatedAt)
            .ToListAsync(cancellationToken);
        
        var assignedLog = statusLogs.FirstOrDefault(sl => sl.ToStatus == "Assigned");
        var completedLog = statusLogs.FirstOrDefault(sl => sl.ToStatus == "OrderCompleted");

        int actualJobMinutes = 0;
        if (assignedLog != null && completedLog != null)
        {
            actualJobMinutes = (int)(completedLog.CreatedAt - assignedLog.CreatedAt).TotalMinutes;
        }

        // Calculate docket KPI
        var docketLog = statusLogs.FirstOrDefault(sl => sl.ToStatus == "DocketsReceived");
        int? actualDocketMinutes = null;
        if (completedLog != null && docketLog != null)
        {
            actualDocketMinutes = (int)(docketLog.CreatedAt - completedLog.CreatedAt).TotalMinutes;
        }

        // Determine KPI result
        string kpiResult = "OnTime";
        if (actualJobMinutes > profile.MaxJobDurationMinutes)
        {
            kpiResult = "ExceededSla";
        }
        else if (actualJobMinutes > profile.MaxJobDurationMinutes * 0.8) // 80% threshold
        {
            kpiResult = "Late";
        }

        return new KpiEvaluationResultDto
        {
            OrderId = orderId,
            KpiProfileId = profile.Id,
            KpiResult = kpiResult,
            ActualJobMinutes = actualJobMinutes,
            TargetJobMinutes = profile.MaxJobDurationMinutes,
            ActualDocketMinutes = actualDocketMinutes,
            TargetDocketMinutes = profile.DocketKpiMinutes,
            DeltaMinutes = actualJobMinutes - profile.MaxJobDurationMinutes
        };
    }

    private KpiProfileDto MapToDto(KpiProfile profile)
    {
        return new KpiProfileDto
        {
            Id = profile.Id,
            CompanyId = profile.CompanyId,
            Name = profile.Name,
            PartnerId = profile.PartnerId,
            OrderType = profile.OrderType,
            InstallationMethodId = profile.InstallationMethodId,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            BuildingTypeId = profile.BuildingTypeId,
#pragma warning restore CS0618
            MaxJobDurationMinutes = profile.MaxJobDurationMinutes,
            DocketKpiMinutes = profile.DocketKpiMinutes,
            MaxReschedulesAllowed = profile.MaxReschedulesAllowed,
            IsDefault = profile.IsDefault,
            EffectiveFrom = profile.EffectiveFrom,
            EffectiveTo = profile.EffectiveTo,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }
}

