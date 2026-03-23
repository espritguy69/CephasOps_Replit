using CephasOps.Application.Buildings.DTOs;
using CephasOps.Domain.Buildings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Buildings.Services;

/// <summary>
/// BuildingType service implementation
/// </summary>
public class BuildingTypeService : IBuildingTypeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BuildingTypeService> _logger;

    public BuildingTypeService(ApplicationDbContext context, ILogger<BuildingTypeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<BuildingTypeDto>> GetBuildingTypesAsync(Guid? companyId, Guid? departmentId = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<BuildingTypeDto>();

        var query = _context.BuildingTypes.Where(bt => bt.CompanyId == effectiveCompanyId.Value);

        // Include building types with null departmentId when filtering by department
        // (null means department-agnostic types that apply to all departments)
        if (departmentId.HasValue)
        {
            query = query.Where(bt => bt.DepartmentId == departmentId.Value || bt.DepartmentId == null);
        }

        if (isActive.HasValue)
        {
            query = query.Where(bt => bt.IsActive == isActive.Value);
        }

        var buildingTypes = await query
            .OrderBy(bt => bt.DisplayOrder)
            .ThenBy(bt => bt.Name)
            .ToListAsync(cancellationToken);

        return buildingTypes.Select(MapToDto).ToList();
    }

    public async Task<BuildingTypeDto?> GetBuildingTypeByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return null;

        var buildingType = await _context.BuildingTypes
            .Where(bt => bt.Id == id && bt.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return buildingType != null ? MapToDto(buildingType) : null;
    }

    public async Task<BuildingTypeDto> CreateBuildingTypeAsync(CreateBuildingTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to create a building type.");

        // Check for duplicate name (case-insensitive)
        var duplicateName = await _context.BuildingTypes
            .Where(bt => bt.CompanyId == effectiveCompanyId.Value && EF.Functions.ILike(bt.Name, dto.Name.Trim()))
            .FirstOrDefaultAsync(cancellationToken);
        if (duplicateName != null)
        {
            throw new InvalidOperationException($"A building type with the name '{dto.Name}' already exists.");
        }

        // Check for duplicate code (case-insensitive)
        if (!string.IsNullOrWhiteSpace(dto.Code))
        {
            var duplicateCode = await _context.BuildingTypes
                .Where(bt => bt.CompanyId == effectiveCompanyId.Value && EF.Functions.ILike(bt.Code, dto.Code.Trim()))
                .FirstOrDefaultAsync(cancellationToken);
            if (duplicateCode != null)
            {
                throw new InvalidOperationException($"A building type with the code '{dto.Code}' already exists.");
            }
        }

        var buildingType = new BuildingType
        {
            Id = Guid.NewGuid(),
            CompanyId = effectiveCompanyId.Value,
            DepartmentId = dto.DepartmentId,
            Name = dto.Name.Trim(),
            Code = dto.Code?.Trim() ?? string.Empty,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.BuildingTypes.Add(buildingType);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("BuildingType created: {BuildingTypeId}, Name: {Name}", buildingType.Id, buildingType.Name);

        return MapToDto(buildingType);
    }

    public async Task<BuildingTypeDto> UpdateBuildingTypeAsync(Guid id, UpdateBuildingTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to update a building type.");

        var buildingType = await _context.BuildingTypes
            .Where(bt => bt.Id == id && bt.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (buildingType == null)
        {
            throw new KeyNotFoundException($"BuildingType with ID {id} not found");
        }

        // Check for duplicate name (case-insensitive) - exclude current record
        if (!string.IsNullOrEmpty(dto.Name) && dto.Name.Trim() != buildingType.Name)
        {
            var duplicateName = await _context.BuildingTypes
                .Where(bt => bt.Id != id && bt.CompanyId == effectiveCompanyId.Value && EF.Functions.ILike(bt.Name, dto.Name.Trim()))
                .FirstOrDefaultAsync(cancellationToken);
            if (duplicateName != null)
            {
                throw new InvalidOperationException($"A building type with the name '{dto.Name}' already exists.");
            }
        }

        // Check for duplicate code (case-insensitive) - exclude current record
        if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code.Trim() != buildingType.Code)
        {
            var duplicateCode = await _context.BuildingTypes
                .Where(bt => bt.Id != id && bt.CompanyId == effectiveCompanyId.Value && EF.Functions.ILike(bt.Code, dto.Code.Trim()))
                .FirstOrDefaultAsync(cancellationToken);
            if (duplicateCode != null)
            {
                throw new InvalidOperationException($"A building type with the code '{dto.Code}' already exists.");
            }
        }

        if (dto.DepartmentId.HasValue) buildingType.DepartmentId = dto.DepartmentId;
        if (!string.IsNullOrEmpty(dto.Name)) buildingType.Name = dto.Name.Trim();
        if (!string.IsNullOrEmpty(dto.Code)) buildingType.Code = dto.Code.Trim();
        if (dto.Description != null) buildingType.Description = dto.Description;
        if (dto.IsActive.HasValue) buildingType.IsActive = dto.IsActive.Value;
        if (dto.DisplayOrder.HasValue) buildingType.DisplayOrder = dto.DisplayOrder.Value;
        buildingType.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("BuildingType updated: {BuildingTypeId}", id);

        return MapToDto(buildingType);
    }

    public async Task DeleteBuildingTypeAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to delete a building type.");

        var buildingType = await _context.BuildingTypes
            .Where(bt => bt.Id == id && bt.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (buildingType == null)
        {
            throw new KeyNotFoundException($"BuildingType with ID {id} not found");
        }

        // Check if any buildings are using this building type
        // BuildingTypeId is obsolete on Building entity - no longer checked
        var hasBuildings = false; // Always false since BuildingTypeId was removed from Building
        if (hasBuildings)
        {
            throw new InvalidOperationException($"Cannot delete BuildingType {id} because it is being used by buildings");
        }

        _context.BuildingTypes.Remove(buildingType);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("BuildingType deleted: {BuildingTypeId}", id);
    }

    private static BuildingTypeDto MapToDto(BuildingType buildingType)
    {
        return new BuildingTypeDto
        {
            Id = buildingType.Id,
            CompanyId = buildingType.CompanyId,
            DepartmentId = buildingType.DepartmentId,
            Name = buildingType.Name,
            Code = buildingType.Code,
            Description = buildingType.Description,
            IsActive = buildingType.IsActive,
            DisplayOrder = buildingType.DisplayOrder,
            CreatedAt = buildingType.CreatedAt,
            UpdatedAt = buildingType.UpdatedAt
        };
    }
}

