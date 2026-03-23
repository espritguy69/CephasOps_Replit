using CephasOps.Application.Buildings.DTOs;
using CephasOps.Domain.Buildings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Buildings.Services;

/// <summary>
/// Service implementation for building default materials
/// </summary>
public class BuildingDefaultMaterialService : IBuildingDefaultMaterialService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BuildingDefaultMaterialService> _logger;

    public BuildingDefaultMaterialService(
        ApplicationDbContext context,
        ILogger<BuildingDefaultMaterialService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<BuildingDefaultMaterialDto>> GetBuildingDefaultMaterialsAsync(
        Guid buildingId,
        Guid? orderTypeId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting default materials for building {BuildingId}", buildingId);

        var query = _context.BuildingDefaultMaterials
            .Where(m => m.BuildingId == buildingId);

        if (orderTypeId.HasValue)
        {
            query = query.Where(m => m.OrderTypeId == orderTypeId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(m => m.IsActive == isActive.Value);
        }

        var materials = await query.ToListAsync(cancellationToken);

        // Get related data
        var materialIds = materials.Select(m => m.MaterialId).Distinct().ToList();
        var orderTypeIds = materials.Select(m => m.OrderTypeId).Distinct().ToList();

        var materialsDict = await _context.Materials
            .Where(m => materialIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);

        var orderTypesDict = await _context.OrderTypes
            .Where(ot => orderTypeIds.Contains(ot.Id))
            .ToDictionaryAsync(ot => ot.Id, cancellationToken);

        return materials.Select(m => MapToDto(m, materialsDict, orderTypesDict)).ToList();
    }

    public async Task<BuildingDefaultMaterialDto?> GetBuildingDefaultMaterialByIdAsync(
        Guid buildingId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var material = await _context.BuildingDefaultMaterials
            .FirstOrDefaultAsync(m => m.Id == id && m.BuildingId == buildingId, cancellationToken);

        if (material == null) return null;

        var tenantId = CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        var mat = (tenantId.HasValue && tenantId.Value != Guid.Empty)
            ? await _context.Materials.FirstOrDefaultAsync(m => m.Id == material.MaterialId && m.CompanyId == tenantId.Value, cancellationToken)
            : await _context.Materials.FirstOrDefaultAsync(m => m.Id == material.MaterialId, cancellationToken);
        var orderType = (tenantId.HasValue && tenantId.Value != Guid.Empty)
            ? await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == material.OrderTypeId && ot.CompanyId == tenantId.Value, cancellationToken)
            : await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == material.OrderTypeId, cancellationToken);

        var materialsDict = mat != null ? new Dictionary<Guid, Domain.Inventory.Entities.Material> { { mat.Id, mat } } : new();
        var orderTypesDict = orderType != null ? new Dictionary<Guid, Domain.Orders.Entities.OrderType> { { orderType.Id, orderType } } : new();

        return MapToDto(material, materialsDict, orderTypesDict);
    }

    public async Task<BuildingDefaultMaterialDto> CreateBuildingDefaultMaterialAsync(
        Guid buildingId,
        CreateBuildingDefaultMaterialDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating default material for building {BuildingId}", buildingId);

        // Validate building exists and get company for tenant-safe lookups
        var building = await _context.Buildings.FirstOrDefaultAsync(b => b.Id == buildingId, cancellationToken);
        if (building == null)
        {
            throw new KeyNotFoundException($"Building {buildingId} not found");
        }
        var cid = building.CompanyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!cid.HasValue || cid.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to create building default material.");

        // Validate material exists and is non-serialized (tenant-safe: scope by company)
        var material = await _context.Materials
            .FirstOrDefaultAsync(m => m.Id == dto.MaterialId && m.CompanyId == cid.Value, cancellationToken);
        if (material == null)
        {
            throw new KeyNotFoundException($"Material {dto.MaterialId} not found");
        }
        if (material.IsSerialised)
        {
            throw new InvalidOperationException("Only non-serialized materials can be set as default");
        }

        // Validate order type exists (tenant-safe: scope by company)
        var orderType = await _context.OrderTypes
            .FirstOrDefaultAsync(ot => ot.Id == dto.OrderTypeId && ot.CompanyId == cid.Value, cancellationToken);
        if (orderType == null)
        {
            throw new KeyNotFoundException($"Order type {dto.OrderTypeId} not found");
        }

        // Check for duplicate
        var exists = await _context.BuildingDefaultMaterials
            .AnyAsync(m => m.BuildingId == buildingId 
                && m.OrderTypeId == dto.OrderTypeId 
                && m.MaterialId == dto.MaterialId, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("This material is already configured for this building and job type");
        }

        var defaultMaterial = new BuildingDefaultMaterial
        {
            Id = Guid.NewGuid(),
            BuildingId = buildingId,
            OrderTypeId = dto.OrderTypeId,
            MaterialId = dto.MaterialId,
            DefaultQuantity = dto.DefaultQuantity,
            Notes = dto.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.BuildingDefaultMaterials.Add(defaultMaterial);
        await _context.SaveChangesAsync(cancellationToken);

        var materialsDict = new Dictionary<Guid, Domain.Inventory.Entities.Material> { { material.Id, material } };
        var orderTypesDict = new Dictionary<Guid, Domain.Orders.Entities.OrderType> { { orderType.Id, orderType } };

        return MapToDto(defaultMaterial, materialsDict, orderTypesDict);
    }

    public async Task<BuildingDefaultMaterialDto> UpdateBuildingDefaultMaterialAsync(
        Guid buildingId,
        Guid id,
        UpdateBuildingDefaultMaterialDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating default material {Id} for building {BuildingId}", id, buildingId);

        var material = await _context.BuildingDefaultMaterials
            .FirstOrDefaultAsync(m => m.Id == id && m.BuildingId == buildingId, cancellationToken);

        if (material == null)
        {
            throw new KeyNotFoundException($"Default material {id} not found for building {buildingId}");
        }

        if (dto.DefaultQuantity.HasValue)
        {
            material.DefaultQuantity = dto.DefaultQuantity.Value;
        }

        if (dto.Notes != null)
        {
            material.Notes = dto.Notes;
        }

        if (dto.IsActive.HasValue)
        {
            material.IsActive = dto.IsActive.Value;
        }

        material.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetBuildingDefaultMaterialByIdAsync(buildingId, id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated material");
    }

    public async Task DeleteBuildingDefaultMaterialAsync(
        Guid buildingId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting default material {Id} for building {BuildingId}", id, buildingId);

        var material = await _context.BuildingDefaultMaterials
            .FirstOrDefaultAsync(m => m.Id == id && m.BuildingId == buildingId, cancellationToken);

        if (material == null)
        {
            throw new KeyNotFoundException($"Default material {id} not found for building {buildingId}");
        }

        _context.BuildingDefaultMaterials.Remove(material);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<BuildingDefaultMaterialsSummaryDto> GetDefaultMaterialsSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting default materials summary for dashboard");

        var allDefaultMaterials = await _context.BuildingDefaultMaterials
            .Where(m => m.IsActive)
            .ToListAsync(cancellationToken);

        var buildingsWithMaterials = allDefaultMaterials.Select(m => m.BuildingId).Distinct().Count();
        var jobTypesConfigured = allDefaultMaterials.Select(m => m.OrderTypeId).Distinct().Count();
        var totalItems = allDefaultMaterials.Count;
        var avgItemsPerBuilding = buildingsWithMaterials > 0 
            ? Math.Round((decimal)totalItems / buildingsWithMaterials, 1) 
            : 0;

        // Most used materials
        var materialUsage = allDefaultMaterials
            .GroupBy(m => m.MaterialId)
            .Select(g => new { MaterialId = g.Key, BuildingCount = g.Select(m => m.BuildingId).Distinct().Count() })
            .OrderByDescending(x => x.BuildingCount)
            .Take(5)
            .ToList();

        var materialIds = materialUsage.Select(x => x.MaterialId).ToList();
        var materials = await _context.Materials
            .Where(m => materialIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);

        var mostUsedMaterials = materialUsage.Select(x => new MaterialUsageSummaryDto
        {
            MaterialId = x.MaterialId,
            MaterialCode = materials.TryGetValue(x.MaterialId, out var mat) ? mat.ItemCode : "",
            MaterialDescription = materials.TryGetValue(x.MaterialId, out var mat2) ? mat2.Description : "",
            BuildingCount = x.BuildingCount
        }).ToList();

        // Stock impact (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var recentMovements = await _context.StockMovements
            .Where(sm => sm.CreatedAt >= thirtyDaysAgo 
                && sm.OrderId != null 
                && (sm.MovementType == "ConsumeForOrder" || sm.MovementType == "UseForOrder"))
            .ToListAsync(cancellationToken);

        var ordersCompleted = recentMovements.Select(m => m.OrderId).Distinct().Count();
        var materialsConsumed = recentMovements.Sum(m => (int)m.Quantity);

        // Get material costs
        var movementMaterialIds = recentMovements.Select(m => m.MaterialId).Distinct().ToList();
        var movementMaterials = await _context.Materials
            .Where(m => movementMaterialIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);

        var totalValue = recentMovements.Sum(m => 
            movementMaterials.TryGetValue(m.MaterialId, out var mat) 
                ? m.Quantity * (mat.DefaultCost ?? 0) 
                : 0);

        return new BuildingDefaultMaterialsSummaryDto
        {
            BuildingsWithMaterials = buildingsWithMaterials,
            JobTypesConfigured = jobTypesConfigured,
            TotalMaterialItems = totalItems,
            AvgItemsPerBuilding = avgItemsPerBuilding,
            MostUsedMaterials = mostUsedMaterials,
            StockImpact = new StockImpactSummaryDto
            {
                OrdersCompleted = ordersCompleted,
                MaterialsConsumed = materialsConsumed,
                TotalValue = totalValue
            }
        };
    }

    public async Task<List<BuildingDefaultMaterialDto>> GetMaterialsForOrderAsync(
        Guid buildingId,
        Guid orderTypeId,
        CancellationToken cancellationToken = default)
    {
        return await GetBuildingDefaultMaterialsAsync(buildingId, orderTypeId, true, cancellationToken);
    }

    private static BuildingDefaultMaterialDto MapToDto(
        BuildingDefaultMaterial material,
        Dictionary<Guid, Domain.Inventory.Entities.Material> materialsDict,
        Dictionary<Guid, Domain.Orders.Entities.OrderType> orderTypesDict)
    {
        var mat = materialsDict.TryGetValue(material.MaterialId, out var m) ? m : null;
        var orderType = orderTypesDict.TryGetValue(material.OrderTypeId, out var ot) ? ot : null;

        return new BuildingDefaultMaterialDto
        {
            Id = material.Id,
            BuildingId = material.BuildingId,
            OrderTypeId = material.OrderTypeId,
            OrderTypeName = orderType?.Name,
            OrderTypeCode = orderType?.Code,
            MaterialId = material.MaterialId,
            MaterialCode = mat?.ItemCode,
            MaterialDescription = mat?.Description,
            MaterialUnitOfMeasure = mat?.UnitOfMeasure,
            DefaultQuantity = material.DefaultQuantity,
            Notes = material.Notes,
            IsActive = material.IsActive,
            CreatedAt = material.CreatedAt,
            UpdatedAt = material.UpdatedAt
        };
    }
}

