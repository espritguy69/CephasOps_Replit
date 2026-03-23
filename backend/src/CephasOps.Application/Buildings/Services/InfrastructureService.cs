using CephasOps.Application.Buildings.DTOs;
using CephasOps.Domain.Buildings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Buildings.Services;

/// <summary>
/// Building infrastructure service implementation
/// </summary>
public class InfrastructureService : IInfrastructureService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InfrastructureService> _logger;

    public InfrastructureService(ApplicationDbContext context, ILogger<InfrastructureService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Infrastructure Overview

    public async Task<BuildingInfrastructureDto> GetBuildingInfrastructureAsync(Guid buildingId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var building = await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var blocks = await GetBuildingBlocksAsync(buildingId, companyId, cancellationToken);
        var splitters = await GetBuildingSplittersAsync(buildingId, companyId, null, cancellationToken);
        var streets = await GetStreetsAsync(buildingId, companyId, cancellationToken);
        var hubBoxes = await GetHubBoxesAsync(buildingId, companyId, cancellationToken);
        var poles = await GetPolesAsync(buildingId, companyId, cancellationToken);

        return new BuildingInfrastructureDto
        {
            BuildingId = buildingId,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            PropertyType = building.PropertyType,
#pragma warning restore CS0618
            Blocks = blocks,
            Splitters = splitters,
            Streets = streets,
            HubBoxes = hubBoxes,
            Poles = poles,
            TotalBlocks = blocks.Count,
            TotalSplitters = splitters.Count,
            TotalSplitterPorts = splitters.Sum(s => s.PortsTotal),
            UsedSplitterPorts = splitters.Sum(s => s.PortsUsed),
            TotalStreets = streets.Count,
            TotalHubBoxes = hubBoxes.Count,
            TotalHubBoxPorts = hubBoxes.Sum(h => h.PortsTotal),
            UsedHubBoxPorts = hubBoxes.Sum(h => h.PortsUsed),
            TotalPoles = poles.Count
        };
    }

    #endregion

    #region Building Blocks

    public async Task<List<BuildingBlockDto>> GetBuildingBlocksAsync(Guid buildingId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var blocks = await _context.BuildingBlocks
            .Where(b => b.BuildingId == buildingId)
            .OrderBy(b => b.DisplayOrder)
            .ThenBy(b => b.Name)
            .ToListAsync(cancellationToken);

        var blockIds = blocks.Select(b => b.Id).ToList();
        var splitterCounts = await _context.BuildingSplitters
            .Where(s => s.BlockId.HasValue && blockIds.Contains(s.BlockId.Value))
            .GroupBy(s => s.BlockId)
            .Select(g => new { BlockId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BlockId!.Value, x => x.Count, cancellationToken);

        return blocks.Select(b => new BuildingBlockDto
        {
            Id = b.Id,
            BuildingId = b.BuildingId,
            Name = b.Name,
            Code = b.Code,
            Floors = b.Floors,
            UnitsPerFloor = b.UnitsPerFloor,
            TotalUnits = b.TotalUnits,
            DisplayOrder = b.DisplayOrder,
            IsActive = b.IsActive,
            Notes = b.Notes,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt,
            SplittersCount = splitterCounts.GetValueOrDefault(b.Id, 0)
        }).ToList();
    }

    public async Task<BuildingBlockDto?> GetBuildingBlockByIdAsync(Guid buildingId, Guid blockId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var block = await _context.BuildingBlocks
            .FirstOrDefaultAsync(b => b.Id == blockId && b.BuildingId == buildingId, cancellationToken);

        if (block == null) return null;

        var splittersCount = await _context.BuildingSplitters
            .CountAsync(s => s.BlockId == blockId, cancellationToken);

        return new BuildingBlockDto
        {
            Id = block.Id,
            BuildingId = block.BuildingId,
            Name = block.Name,
            Code = block.Code,
            Floors = block.Floors,
            UnitsPerFloor = block.UnitsPerFloor,
            TotalUnits = block.TotalUnits,
            DisplayOrder = block.DisplayOrder,
            IsActive = block.IsActive,
            Notes = block.Notes,
            CreatedAt = block.CreatedAt,
            UpdatedAt = block.UpdatedAt,
            SplittersCount = splittersCount
        };
    }

    public async Task<BuildingBlockDto> CreateBuildingBlockAsync(Guid buildingId, SaveBuildingBlockDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var block = new BuildingBlock
        {
            Id = Guid.NewGuid(),
            BuildingId = buildingId,
            Name = dto.Name,
            Code = dto.Code,
            Floors = dto.Floors,
            UnitsPerFloor = dto.UnitsPerFloor,
            TotalUnits = dto.TotalUnits,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.BuildingBlocks.Add(block);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("BuildingBlock created: {BlockId} for Building: {BuildingId}", block.Id, buildingId);

        return new BuildingBlockDto
        {
            Id = block.Id,
            BuildingId = block.BuildingId,
            Name = block.Name,
            Code = block.Code,
            Floors = block.Floors,
            UnitsPerFloor = block.UnitsPerFloor,
            TotalUnits = block.TotalUnits,
            DisplayOrder = block.DisplayOrder,
            IsActive = block.IsActive,
            Notes = block.Notes,
            CreatedAt = block.CreatedAt,
            SplittersCount = 0
        };
    }

    public async Task<BuildingBlockDto> UpdateBuildingBlockAsync(Guid buildingId, Guid blockId, SaveBuildingBlockDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var block = await _context.BuildingBlocks
            .FirstOrDefaultAsync(b => b.Id == blockId && b.BuildingId == buildingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Block with ID {blockId} not found");

        block.Name = dto.Name;
        block.Code = dto.Code;
        block.Floors = dto.Floors;
        block.UnitsPerFloor = dto.UnitsPerFloor;
        block.TotalUnits = dto.TotalUnits;
        block.DisplayOrder = dto.DisplayOrder;
        block.IsActive = dto.IsActive;
        block.Notes = dto.Notes;
        block.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("BuildingBlock updated: {BlockId}", blockId);

        var splittersCount = await _context.BuildingSplitters.CountAsync(s => s.BlockId == blockId, cancellationToken);

        return new BuildingBlockDto
        {
            Id = block.Id,
            BuildingId = block.BuildingId,
            Name = block.Name,
            Code = block.Code,
            Floors = block.Floors,
            UnitsPerFloor = block.UnitsPerFloor,
            TotalUnits = block.TotalUnits,
            DisplayOrder = block.DisplayOrder,
            IsActive = block.IsActive,
            Notes = block.Notes,
            CreatedAt = block.CreatedAt,
            UpdatedAt = block.UpdatedAt,
            SplittersCount = splittersCount
        };
    }

    public async Task DeleteBuildingBlockAsync(Guid buildingId, Guid blockId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var block = await _context.BuildingBlocks
            .FirstOrDefaultAsync(b => b.Id == blockId && b.BuildingId == buildingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Block with ID {blockId} not found");

        // Check if block has splitters
        var hasSplitters = await _context.BuildingSplitters.AnyAsync(s => s.BlockId == blockId, cancellationToken);
        if (hasSplitters)
        {
            throw new InvalidOperationException("Cannot delete block with existing splitters. Remove splitters first or reassign them.");
        }

        _context.BuildingBlocks.Remove(block);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("BuildingBlock deleted: {BlockId}", blockId);
    }

    #endregion

    #region Building Splitters

    public async Task<List<BuildingSplitterDto>> GetBuildingSplittersAsync(Guid buildingId, Guid? companyId, SplitterFilterDto? filter = null, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var query = _context.BuildingSplitters.Where(s => s.BuildingId == buildingId);
        query = ApplySplitterFilters(query, filter);

        var splitters = await query
            .OrderBy(s => s.Block != null ? s.Block.DisplayOrder : 0)
            .ThenBy(s => s.Floor)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return await MapSplittersToDto(splitters, cancellationToken);
    }

    public async Task<List<BuildingSplitterDto>> GetAllSplittersAsync(Guid? companyId, SplitterFilterDto? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.BuildingSplitters.AsQueryable();

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(s => _context.Buildings.Any(b => b.Id == s.BuildingId && b.CompanyId == companyId.Value));
        }

        query = ApplySplitterFilters(query, filter);

        var splitters = await query
            .OrderBy(s => s.Building != null ? s.Building.Name : "")
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return await MapSplittersToDto(splitters, cancellationToken);
    }

    private IQueryable<BuildingSplitter> ApplySplitterFilters(IQueryable<BuildingSplitter> query, SplitterFilterDto? filter)
    {
        if (filter == null) return query;

        if (filter.BuildingId.HasValue)
            query = query.Where(s => s.BuildingId == filter.BuildingId.Value);
        if (filter.BlockId.HasValue)
            query = query.Where(s => s.BlockId == filter.BlockId.Value);
        if (filter.SplitterTypeId.HasValue)
            query = query.Where(s => s.SplitterTypeId == filter.SplitterTypeId.Value);
        if (filter.Status.HasValue)
            query = query.Where(s => s.Status == filter.Status.Value);
        if (filter.NeedsAttention.HasValue)
            query = query.Where(s => s.NeedsAttention == filter.NeedsAttention.Value || s.Status == SplitterStatus.Faulty || s.Status == SplitterStatus.MaintenanceRequired || s.PortsUsed >= s.PortsTotal);
        if (filter.IsFull.HasValue && filter.IsFull.Value)
            query = query.Where(s => s.PortsUsed >= s.PortsTotal);
        if (filter.OnlyWithAvailability)
            query = query.Where(s => s.PortsUsed < s.PortsTotal && s.Status == SplitterStatus.Active);

        return query;
    }

    private async Task<List<BuildingSplitterDto>> MapSplittersToDto(List<BuildingSplitter> splitters, CancellationToken cancellationToken)
    {
        var buildingIds = splitters.Select(s => s.BuildingId).Distinct().ToList();
        var blockIds = splitters.Where(s => s.BlockId.HasValue).Select(s => s.BlockId!.Value).Distinct().ToList();
        var typeIds = splitters.Select(s => s.SplitterTypeId).Distinct().ToList();

        var buildings = await _context.Buildings
            .Where(b => buildingIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.Name, cancellationToken);

        var blocks = await _context.BuildingBlocks
            .Where(b => blockIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.Name, cancellationToken);

        var types = await _context.SplitterTypes
            .Where(t => typeIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        return splitters.Select(s => new BuildingSplitterDto
        {
            Id = s.Id,
            BuildingId = s.BuildingId,
            BuildingName = buildings.GetValueOrDefault(s.BuildingId),
            BlockId = s.BlockId,
            BlockName = s.BlockId.HasValue ? blocks.GetValueOrDefault(s.BlockId.Value) : null,
            SplitterTypeId = s.SplitterTypeId,
            SplitterTypeName = types.GetValueOrDefault(s.SplitterTypeId),
            Name = s.Name,
            Floor = s.Floor,
            LocationDescription = s.LocationDescription,
            PortsTotal = s.PortsTotal,
            PortsUsed = s.PortsUsed,
            PortsAvailable = s.PortsAvailable,
            UtilizationPercent = s.UtilizationPercent,
            IsFull = s.IsFull,
            Status = s.Status,
            SerialNumber = s.SerialNumber,
            InstalledAt = s.InstalledAt,
            LastMaintenanceAt = s.LastMaintenanceAt,
            Remarks = s.Remarks,
            NeedsAttention = s.NeedsAttention || s.IsFull || s.Status == SplitterStatus.Faulty || s.Status == SplitterStatus.MaintenanceRequired,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        }).ToList();
    }

    public async Task<BuildingSplitterDto?> GetBuildingSplitterByIdAsync(Guid buildingId, Guid splitterId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var splitter = await _context.BuildingSplitters
            .FirstOrDefaultAsync(s => s.Id == splitterId && s.BuildingId == buildingId, cancellationToken);

        if (splitter == null) return null;

        var result = await MapSplittersToDto(new List<BuildingSplitter> { splitter }, cancellationToken);
        return result.FirstOrDefault();
    }

    public async Task<BuildingSplitterDto> CreateBuildingSplitterAsync(Guid buildingId, SaveBuildingSplitterDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var splitter = new BuildingSplitter
        {
            Id = Guid.NewGuid(),
            BuildingId = buildingId,
            BlockId = dto.BlockId,
            SplitterTypeId = dto.SplitterTypeId,
            Name = dto.Name,
            Floor = dto.Floor,
            LocationDescription = dto.LocationDescription,
            PortsTotal = dto.PortsTotal,
            PortsUsed = dto.PortsUsed,
            Status = dto.Status,
            SerialNumber = dto.SerialNumber,
            InstalledAt = dto.InstalledAt,
            LastMaintenanceAt = dto.LastMaintenanceAt,
            Remarks = dto.Remarks,
            NeedsAttention = dto.NeedsAttention,
            CreatedAt = DateTime.UtcNow
        };

        _context.BuildingSplitters.Add(splitter);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("BuildingSplitter created: {SplitterId} for Building: {BuildingId}", splitter.Id, buildingId);

        var result = await MapSplittersToDto(new List<BuildingSplitter> { splitter }, cancellationToken);
        return result.First();
    }

    public async Task<BuildingSplitterDto> UpdateBuildingSplitterAsync(Guid buildingId, Guid splitterId, SaveBuildingSplitterDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var splitter = await _context.BuildingSplitters
            .FirstOrDefaultAsync(s => s.Id == splitterId && s.BuildingId == buildingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Splitter with ID {splitterId} not found");

        splitter.BlockId = dto.BlockId;
        splitter.SplitterTypeId = dto.SplitterTypeId;
        splitter.Name = dto.Name;
        splitter.Floor = dto.Floor;
        splitter.LocationDescription = dto.LocationDescription;
        splitter.PortsTotal = dto.PortsTotal;
        splitter.PortsUsed = dto.PortsUsed;
        splitter.Status = dto.Status;
        splitter.SerialNumber = dto.SerialNumber;
        splitter.InstalledAt = dto.InstalledAt;
        splitter.LastMaintenanceAt = dto.LastMaintenanceAt;
        splitter.Remarks = dto.Remarks;
        splitter.NeedsAttention = dto.NeedsAttention;
        splitter.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("BuildingSplitter updated: {SplitterId}", splitterId);

        var result = await MapSplittersToDto(new List<BuildingSplitter> { splitter }, cancellationToken);
        return result.First();
    }

    public async Task DeleteBuildingSplitterAsync(Guid buildingId, Guid splitterId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var splitter = await _context.BuildingSplitters
            .FirstOrDefaultAsync(s => s.Id == splitterId && s.BuildingId == buildingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Splitter with ID {splitterId} not found");

        if (splitter.PortsUsed > 0)
        {
            throw new InvalidOperationException("Cannot delete splitter with ports in use. Reassign connections first.");
        }

        _context.BuildingSplitters.Remove(splitter);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("BuildingSplitter deleted: {SplitterId}", splitterId);
    }

    public async Task<BuildingSplitterDto> UpdateSplitterPortUsageAsync(Guid buildingId, Guid splitterId, int portsUsed, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var splitter = await _context.BuildingSplitters
            .FirstOrDefaultAsync(s => s.Id == splitterId && s.BuildingId == buildingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Splitter with ID {splitterId} not found");

        if (portsUsed < 0 || portsUsed > splitter.PortsTotal)
        {
            throw new InvalidOperationException($"Ports used must be between 0 and {splitter.PortsTotal}");
        }

        splitter.PortsUsed = portsUsed;
        splitter.UpdatedAt = DateTime.UtcNow;

        // Auto-update status if full
        if (splitter.IsFull && splitter.Status == SplitterStatus.Active)
        {
            splitter.Status = SplitterStatus.Full;
        }
        else if (!splitter.IsFull && splitter.Status == SplitterStatus.Full)
        {
            splitter.Status = SplitterStatus.Active;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var result = await MapSplittersToDto(new List<BuildingSplitter> { splitter }, cancellationToken);
        return result.First();
    }

    #endregion

    #region Streets

    public async Task<List<StreetDto>> GetStreetsAsync(Guid buildingId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var streets = await _context.Streets
            .Where(s => s.BuildingId == buildingId)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

        var streetIds = streets.Select(s => s.Id).ToList();

        var hubBoxCounts = await _context.HubBoxes
            .Where(h => h.StreetId.HasValue && streetIds.Contains(h.StreetId.Value))
            .GroupBy(h => h.StreetId)
            .Select(g => new { StreetId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StreetId!.Value, x => x.Count, cancellationToken);

        var poleCounts = await _context.Poles
            .Where(p => p.StreetId.HasValue && streetIds.Contains(p.StreetId.Value))
            .GroupBy(p => p.StreetId)
            .Select(g => new { StreetId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StreetId!.Value, x => x.Count, cancellationToken);

        return streets.Select(s => new StreetDto
        {
            Id = s.Id,
            BuildingId = s.BuildingId,
            Name = s.Name,
            Code = s.Code,
            Section = s.Section,
            DisplayOrder = s.DisplayOrder,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
            HubBoxesCount = hubBoxCounts.GetValueOrDefault(s.Id, 0),
            PolesCount = poleCounts.GetValueOrDefault(s.Id, 0)
        }).ToList();
    }

    public async Task<StreetDto?> GetStreetByIdAsync(Guid buildingId, Guid streetId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var street = await _context.Streets
            .FirstOrDefaultAsync(s => s.Id == streetId && s.BuildingId == buildingId, cancellationToken);

        if (street == null) return null;

        var hubBoxCount = await _context.HubBoxes.CountAsync(h => h.StreetId == streetId, cancellationToken);
        var poleCount = await _context.Poles.CountAsync(p => p.StreetId == streetId, cancellationToken);

        return new StreetDto
        {
            Id = street.Id,
            BuildingId = street.BuildingId,
            Name = street.Name,
            Code = street.Code,
            Section = street.Section,
            DisplayOrder = street.DisplayOrder,
            IsActive = street.IsActive,
            CreatedAt = street.CreatedAt,
            UpdatedAt = street.UpdatedAt,
            HubBoxesCount = hubBoxCount,
            PolesCount = poleCount
        };
    }

    public async Task<StreetDto> CreateStreetAsync(Guid buildingId, SaveStreetDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var street = new Street
        {
            Id = Guid.NewGuid(),
            BuildingId = buildingId,
            Name = dto.Name,
            Code = dto.Code,
            Section = dto.Section,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Streets.Add(street);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Street created: {StreetId} for Building: {BuildingId}", street.Id, buildingId);

        return new StreetDto
        {
            Id = street.Id,
            BuildingId = street.BuildingId,
            Name = street.Name,
            Code = street.Code,
            Section = street.Section,
            DisplayOrder = street.DisplayOrder,
            IsActive = street.IsActive,
            CreatedAt = street.CreatedAt
        };
    }

    public async Task<StreetDto> UpdateStreetAsync(Guid buildingId, Guid streetId, SaveStreetDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var street = await _context.Streets
            .FirstOrDefaultAsync(s => s.Id == streetId && s.BuildingId == buildingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Street with ID {streetId} not found");

        street.Name = dto.Name;
        street.Code = dto.Code;
        street.Section = dto.Section;
        street.DisplayOrder = dto.DisplayOrder;
        street.IsActive = dto.IsActive;
        street.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Street updated: {StreetId}", streetId);

        var hubBoxCount = await _context.HubBoxes.CountAsync(h => h.StreetId == streetId, cancellationToken);
        var poleCount = await _context.Poles.CountAsync(p => p.StreetId == streetId, cancellationToken);

        return new StreetDto
        {
            Id = street.Id,
            BuildingId = street.BuildingId,
            Name = street.Name,
            Code = street.Code,
            Section = street.Section,
            DisplayOrder = street.DisplayOrder,
            IsActive = street.IsActive,
            CreatedAt = street.CreatedAt,
            UpdatedAt = street.UpdatedAt,
            HubBoxesCount = hubBoxCount,
            PolesCount = poleCount
        };
    }

    public async Task DeleteStreetAsync(Guid buildingId, Guid streetId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var street = await _context.Streets
            .FirstOrDefaultAsync(s => s.Id == streetId && s.BuildingId == buildingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Street with ID {streetId} not found");

        // Check for dependencies
        var hasHubBoxes = await _context.HubBoxes.AnyAsync(h => h.StreetId == streetId, cancellationToken);
        var hasPoles = await _context.Poles.AnyAsync(p => p.StreetId == streetId, cancellationToken);

        if (hasHubBoxes || hasPoles)
        {
            throw new InvalidOperationException("Cannot delete street with existing hub boxes or poles. Remove them first.");
        }

        _context.Streets.Remove(street);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Street deleted: {StreetId}", streetId);
    }

    #endregion

    #region Hub Boxes

    public async Task<List<HubBoxDto>> GetHubBoxesAsync(Guid buildingId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var hubBoxes = await _context.HubBoxes
            .Where(h => h.BuildingId == buildingId)
            .OrderBy(h => h.Name)
            .ToListAsync(cancellationToken);

        var streetIds = hubBoxes.Where(h => h.StreetId.HasValue).Select(h => h.StreetId!.Value).Distinct().ToList();
        var streets = await _context.Streets
            .Where(s => streetIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        return hubBoxes.Select(h => new HubBoxDto
        {
            Id = h.Id,
            BuildingId = h.BuildingId,
            StreetId = h.StreetId,
            StreetName = h.StreetId.HasValue ? streets.GetValueOrDefault(h.StreetId.Value) : null,
            Name = h.Name,
            Code = h.Code,
            LocationDescription = h.LocationDescription,
            Latitude = h.Latitude,
            Longitude = h.Longitude,
            PortsTotal = h.PortsTotal,
            PortsUsed = h.PortsUsed,
            PortsAvailable = h.PortsAvailable,
            UtilizationPercent = h.UtilizationPercent,
            IsFull = h.IsFull,
            Status = h.Status,
            InstalledAt = h.InstalledAt,
            Remarks = h.Remarks,
            IsActive = h.IsActive,
            CreatedAt = h.CreatedAt,
            UpdatedAt = h.UpdatedAt
        }).ToList();
    }

    public async Task<HubBoxDto?> GetHubBoxByIdAsync(Guid buildingId, Guid hubBoxId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var hubBox = await _context.HubBoxes
            .FirstOrDefaultAsync(h => h.Id == hubBoxId && h.BuildingId == buildingId, cancellationToken);

        if (hubBox == null) return null;

        string? streetName = null;
        if (hubBox.StreetId.HasValue)
        {
            streetName = await _context.Streets
                .Where(s => s.Id == hubBox.StreetId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new HubBoxDto
        {
            Id = hubBox.Id,
            BuildingId = hubBox.BuildingId,
            StreetId = hubBox.StreetId,
            StreetName = streetName,
            Name = hubBox.Name,
            Code = hubBox.Code,
            LocationDescription = hubBox.LocationDescription,
            Latitude = hubBox.Latitude,
            Longitude = hubBox.Longitude,
            PortsTotal = hubBox.PortsTotal,
            PortsUsed = hubBox.PortsUsed,
            PortsAvailable = hubBox.PortsAvailable,
            UtilizationPercent = hubBox.UtilizationPercent,
            IsFull = hubBox.IsFull,
            Status = hubBox.Status,
            InstalledAt = hubBox.InstalledAt,
            Remarks = hubBox.Remarks,
            IsActive = hubBox.IsActive,
            CreatedAt = hubBox.CreatedAt,
            UpdatedAt = hubBox.UpdatedAt
        };
    }

    public async Task<HubBoxDto> CreateHubBoxAsync(Guid buildingId, SaveHubBoxDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var hubBox = new HubBox
        {
            Id = Guid.NewGuid(),
            BuildingId = buildingId,
            StreetId = dto.StreetId,
            Name = dto.Name,
            Code = dto.Code,
            LocationDescription = dto.LocationDescription,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            PortsTotal = dto.PortsTotal,
            PortsUsed = dto.PortsUsed,
            Status = dto.Status,
            InstalledAt = dto.InstalledAt,
            Remarks = dto.Remarks,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.HubBoxes.Add(hubBox);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("HubBox created: {HubBoxId} for Building: {BuildingId}", hubBox.Id, buildingId);

        return new HubBoxDto
        {
            Id = hubBox.Id,
            BuildingId = hubBox.BuildingId,
            StreetId = hubBox.StreetId,
            Name = hubBox.Name,
            Code = hubBox.Code,
            LocationDescription = hubBox.LocationDescription,
            Latitude = hubBox.Latitude,
            Longitude = hubBox.Longitude,
            PortsTotal = hubBox.PortsTotal,
            PortsUsed = hubBox.PortsUsed,
            PortsAvailable = hubBox.PortsAvailable,
            UtilizationPercent = hubBox.UtilizationPercent,
            IsFull = hubBox.IsFull,
            Status = hubBox.Status,
            InstalledAt = hubBox.InstalledAt,
            Remarks = hubBox.Remarks,
            IsActive = hubBox.IsActive,
            CreatedAt = hubBox.CreatedAt
        };
    }

    public async Task<HubBoxDto> UpdateHubBoxAsync(Guid buildingId, Guid hubBoxId, SaveHubBoxDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var hubBox = await _context.HubBoxes
            .FirstOrDefaultAsync(h => h.Id == hubBoxId && h.BuildingId == buildingId, cancellationToken)
            ?? throw new KeyNotFoundException($"HubBox with ID {hubBoxId} not found");

        hubBox.StreetId = dto.StreetId;
        hubBox.Name = dto.Name;
        hubBox.Code = dto.Code;
        hubBox.LocationDescription = dto.LocationDescription;
        hubBox.Latitude = dto.Latitude;
        hubBox.Longitude = dto.Longitude;
        hubBox.PortsTotal = dto.PortsTotal;
        hubBox.PortsUsed = dto.PortsUsed;
        hubBox.Status = dto.Status;
        hubBox.InstalledAt = dto.InstalledAt;
        hubBox.Remarks = dto.Remarks;
        hubBox.IsActive = dto.IsActive;
        hubBox.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("HubBox updated: {HubBoxId}", hubBoxId);

        string? streetName = null;
        if (hubBox.StreetId.HasValue)
        {
            streetName = await _context.Streets
                .Where(s => s.Id == hubBox.StreetId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new HubBoxDto
        {
            Id = hubBox.Id,
            BuildingId = hubBox.BuildingId,
            StreetId = hubBox.StreetId,
            StreetName = streetName,
            Name = hubBox.Name,
            Code = hubBox.Code,
            LocationDescription = hubBox.LocationDescription,
            Latitude = hubBox.Latitude,
            Longitude = hubBox.Longitude,
            PortsTotal = hubBox.PortsTotal,
            PortsUsed = hubBox.PortsUsed,
            PortsAvailable = hubBox.PortsAvailable,
            UtilizationPercent = hubBox.UtilizationPercent,
            IsFull = hubBox.IsFull,
            Status = hubBox.Status,
            InstalledAt = hubBox.InstalledAt,
            Remarks = hubBox.Remarks,
            IsActive = hubBox.IsActive,
            CreatedAt = hubBox.CreatedAt,
            UpdatedAt = hubBox.UpdatedAt
        };
    }

    public async Task DeleteHubBoxAsync(Guid buildingId, Guid hubBoxId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var hubBox = await _context.HubBoxes
            .FirstOrDefaultAsync(h => h.Id == hubBoxId && h.BuildingId == buildingId, cancellationToken)
            ?? throw new KeyNotFoundException($"HubBox with ID {hubBoxId} not found");

        if (hubBox.PortsUsed > 0)
        {
            throw new InvalidOperationException("Cannot delete hub box with ports in use.");
        }

        _context.HubBoxes.Remove(hubBox);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("HubBox deleted: {HubBoxId}", hubBoxId);
    }

    public async Task<HubBoxDto> UpdateHubBoxPortUsageAsync(Guid buildingId, Guid hubBoxId, int portsUsed, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var hubBox = await _context.HubBoxes
            .FirstOrDefaultAsync(h => h.Id == hubBoxId && h.BuildingId == buildingId, cancellationToken)
            ?? throw new KeyNotFoundException($"HubBox with ID {hubBoxId} not found");

        if (portsUsed < 0 || portsUsed > hubBox.PortsTotal)
        {
            throw new InvalidOperationException($"Ports used must be between 0 and {hubBox.PortsTotal}");
        }

        hubBox.PortsUsed = portsUsed;
        hubBox.UpdatedAt = DateTime.UtcNow;

        if (hubBox.IsFull && hubBox.Status == HubBoxStatus.Active)
        {
            hubBox.Status = HubBoxStatus.Full;
        }
        else if (!hubBox.IsFull && hubBox.Status == HubBoxStatus.Full)
        {
            hubBox.Status = HubBoxStatus.Active;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return (await GetHubBoxByIdAsync(buildingId, hubBoxId, companyId, cancellationToken))!;
    }

    #endregion

    #region Poles

    public async Task<List<PoleDto>> GetPolesAsync(Guid buildingId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var poles = await _context.Poles
            .Where(p => p.BuildingId == buildingId)
            .OrderBy(p => p.PoleNumber)
            .ToListAsync(cancellationToken);

        var streetIds = poles.Where(p => p.StreetId.HasValue).Select(p => p.StreetId!.Value).Distinct().ToList();
        var streets = await _context.Streets
            .Where(s => streetIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        return poles.Select(p => new PoleDto
        {
            Id = p.Id,
            BuildingId = p.BuildingId,
            StreetId = p.StreetId,
            StreetName = p.StreetId.HasValue ? streets.GetValueOrDefault(p.StreetId.Value) : null,
            PoleNumber = p.PoleNumber,
            PoleType = p.PoleType,
            LocationDescription = p.LocationDescription,
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            HeightMeters = p.HeightMeters,
            HasExistingFibre = p.HasExistingFibre,
            DropsCount = p.DropsCount,
            Status = p.Status,
            Remarks = p.Remarks,
            IsActive = p.IsActive,
            LastInspectedAt = p.LastInspectedAt,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();
    }

    public async Task<PoleDto?> GetPoleByIdAsync(Guid buildingId, Guid poleId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var pole = await _context.Poles
            .FirstOrDefaultAsync(p => p.Id == poleId && p.BuildingId == buildingId, cancellationToken);

        if (pole == null) return null;

        string? streetName = null;
        if (pole.StreetId.HasValue)
        {
            streetName = await _context.Streets
                .Where(s => s.Id == pole.StreetId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new PoleDto
        {
            Id = pole.Id,
            BuildingId = pole.BuildingId,
            StreetId = pole.StreetId,
            StreetName = streetName,
            PoleNumber = pole.PoleNumber,
            PoleType = pole.PoleType,
            LocationDescription = pole.LocationDescription,
            Latitude = pole.Latitude,
            Longitude = pole.Longitude,
            HeightMeters = pole.HeightMeters,
            HasExistingFibre = pole.HasExistingFibre,
            DropsCount = pole.DropsCount,
            Status = pole.Status,
            Remarks = pole.Remarks,
            IsActive = pole.IsActive,
            LastInspectedAt = pole.LastInspectedAt,
            CreatedAt = pole.CreatedAt,
            UpdatedAt = pole.UpdatedAt
        };
    }

    public async Task<PoleDto> CreatePoleAsync(Guid buildingId, SavePoleDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var pole = new Pole
        {
            Id = Guid.NewGuid(),
            BuildingId = buildingId,
            StreetId = dto.StreetId,
            PoleNumber = dto.PoleNumber,
            PoleType = dto.PoleType,
            LocationDescription = dto.LocationDescription,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            HeightMeters = dto.HeightMeters,
            HasExistingFibre = dto.HasExistingFibre,
            DropsCount = dto.DropsCount,
            Status = dto.Status,
            Remarks = dto.Remarks,
            IsActive = dto.IsActive,
            LastInspectedAt = dto.LastInspectedAt,
            CreatedAt = DateTime.UtcNow
        };

        _context.Poles.Add(pole);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Pole created: {PoleId} for Building: {BuildingId}", pole.Id, buildingId);

        return new PoleDto
        {
            Id = pole.Id,
            BuildingId = pole.BuildingId,
            StreetId = pole.StreetId,
            PoleNumber = pole.PoleNumber,
            PoleType = pole.PoleType,
            LocationDescription = pole.LocationDescription,
            Latitude = pole.Latitude,
            Longitude = pole.Longitude,
            HeightMeters = pole.HeightMeters,
            HasExistingFibre = pole.HasExistingFibre,
            DropsCount = pole.DropsCount,
            Status = pole.Status,
            Remarks = pole.Remarks,
            IsActive = pole.IsActive,
            LastInspectedAt = pole.LastInspectedAt,
            CreatedAt = pole.CreatedAt
        };
    }

    public async Task<PoleDto> UpdatePoleAsync(Guid buildingId, Guid poleId, SavePoleDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var pole = await _context.Poles
            .FirstOrDefaultAsync(p => p.Id == poleId && p.BuildingId == buildingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Pole with ID {poleId} not found");

        pole.StreetId = dto.StreetId;
        pole.PoleNumber = dto.PoleNumber;
        pole.PoleType = dto.PoleType;
        pole.LocationDescription = dto.LocationDescription;
        pole.Latitude = dto.Latitude;
        pole.Longitude = dto.Longitude;
        pole.HeightMeters = dto.HeightMeters;
        pole.HasExistingFibre = dto.HasExistingFibre;
        pole.DropsCount = dto.DropsCount;
        pole.Status = dto.Status;
        pole.Remarks = dto.Remarks;
        pole.IsActive = dto.IsActive;
        pole.LastInspectedAt = dto.LastInspectedAt;
        pole.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Pole updated: {PoleId}", poleId);

        return (await GetPoleByIdAsync(buildingId, poleId, companyId, cancellationToken))!;
    }

    public async Task DeletePoleAsync(Guid buildingId, Guid poleId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await GetBuildingOrThrowAsync(buildingId, companyId, cancellationToken);

        var pole = await _context.Poles
            .FirstOrDefaultAsync(p => p.Id == poleId && p.BuildingId == buildingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Pole with ID {poleId} not found");

        if (pole.DropsCount > 0)
        {
            throw new InvalidOperationException("Cannot delete pole with active drops.");
        }

        _context.Poles.Remove(pole);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Pole deleted: {PoleId}", poleId);
    }

    #endregion

    #region Helpers

    private async Task<Building> GetBuildingOrThrowAsync(Guid buildingId, Guid? companyId, CancellationToken cancellationToken)
    {
        var query = _context.Buildings.Where(b => b.Id == buildingId);
        
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(b => b.CompanyId == companyId.Value);
        }

        var building = await query.FirstOrDefaultAsync(cancellationToken);
        
        if (building == null)
        {
            throw new KeyNotFoundException($"Building with ID {buildingId} not found");
        }

        return building;
    }

    #endregion
}

