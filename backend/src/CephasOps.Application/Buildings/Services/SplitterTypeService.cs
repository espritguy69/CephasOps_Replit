using CephasOps.Application.Buildings.DTOs;
using CephasOps.Domain.Buildings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Buildings.Services;

/// <summary>
/// SplitterType service implementation
/// </summary>
public class SplitterTypeService : ISplitterTypeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SplitterTypeService> _logger;

    public SplitterTypeService(ApplicationDbContext context, ILogger<SplitterTypeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SplitterTypeDto>> GetSplitterTypesAsync(Guid? companyId, Guid? departmentId = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<SplitterTypeDto>();

        var query = _context.SplitterTypes.Where(st => st.CompanyId == effectiveCompanyId.Value);

        if (departmentId.HasValue)
        {
            query = query.Where(st => st.DepartmentId == departmentId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(st => st.IsActive == isActive.Value);
        }

        var splitterTypes = await query
            .OrderBy(st => st.DisplayOrder)
            .ThenBy(st => st.Name)
            .ToListAsync(cancellationToken);

        return splitterTypes.Select(MapToDto).ToList();
    }

    public async Task<SplitterTypeDto?> GetSplitterTypeByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return null;

        var splitterType = await _context.SplitterTypes
            .Where(st => st.Id == id && st.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return splitterType != null ? MapToDto(splitterType) : null;
    }

    public async Task<SplitterTypeDto> CreateSplitterTypeAsync(CreateSplitterTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to create a splitter type.");

        // Check for duplicate name (case-insensitive)
        var duplicateName = await _context.SplitterTypes
            .Where(st => st.CompanyId == effectiveCompanyId.Value && EF.Functions.ILike(st.Name, dto.Name.Trim()))
            .FirstOrDefaultAsync(cancellationToken);
        if (duplicateName != null)
        {
            throw new InvalidOperationException($"A splitter type with the name '{dto.Name}' already exists.");
        }

        // Check for duplicate code (case-insensitive)
        if (!string.IsNullOrWhiteSpace(dto.Code))
        {
            var duplicateCode = await _context.SplitterTypes
                .Where(st => st.CompanyId == effectiveCompanyId.Value && EF.Functions.ILike(st.Code, dto.Code.Trim()))
                .FirstOrDefaultAsync(cancellationToken);
            if (duplicateCode != null)
            {
                throw new InvalidOperationException($"A splitter type with the code '{dto.Code}' already exists.");
            }
        }

        var splitterType = new SplitterType
        {
            Id = Guid.NewGuid(),
            CompanyId = effectiveCompanyId.Value,
            DepartmentId = dto.DepartmentId,
            Name = dto.Name.Trim(),
            Code = dto.Code?.Trim() ?? string.Empty,
            TotalPorts = dto.TotalPorts,
            StandbyPortNumber = dto.StandbyPortNumber,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SplitterTypes.Add(splitterType);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SplitterType created: {SplitterTypeId}, Name: {Name}", splitterType.Id, splitterType.Name);

        return MapToDto(splitterType);
    }

    public async Task<SplitterTypeDto> UpdateSplitterTypeAsync(Guid id, UpdateSplitterTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to update a splitter type.");

        var splitterType = await _context.SplitterTypes
            .Where(st => st.Id == id && st.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (splitterType == null)
        {
            throw new KeyNotFoundException($"SplitterType with ID {id} not found");
        }

        // Check for duplicate name (case-insensitive) - exclude current record
        if (!string.IsNullOrEmpty(dto.Name) && dto.Name.Trim() != splitterType.Name)
        {
            var duplicateName = await _context.SplitterTypes
                .Where(st => st.Id != id && st.CompanyId == effectiveCompanyId.Value && EF.Functions.ILike(st.Name, dto.Name.Trim()))
                .FirstOrDefaultAsync(cancellationToken);
            if (duplicateName != null)
            {
                throw new InvalidOperationException($"A splitter type with the name '{dto.Name}' already exists.");
            }
        }

        // Check for duplicate code (case-insensitive) - exclude current record
        if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code.Trim() != splitterType.Code)
        {
            var duplicateCode = await _context.SplitterTypes
                .Where(st => st.Id != id && st.CompanyId == effectiveCompanyId.Value && EF.Functions.ILike(st.Code, dto.Code.Trim()))
                .FirstOrDefaultAsync(cancellationToken);
            if (duplicateCode != null)
            {
                throw new InvalidOperationException($"A splitter type with the code '{dto.Code}' already exists.");
            }
        }

        if (dto.DepartmentId.HasValue) splitterType.DepartmentId = dto.DepartmentId;
        if (!string.IsNullOrEmpty(dto.Name)) splitterType.Name = dto.Name.Trim();
        if (!string.IsNullOrEmpty(dto.Code)) splitterType.Code = dto.Code.Trim();
        if (dto.TotalPorts.HasValue) splitterType.TotalPorts = dto.TotalPorts.Value;
        if (dto.StandbyPortNumber.HasValue) splitterType.StandbyPortNumber = dto.StandbyPortNumber;
        if (dto.Description != null) splitterType.Description = dto.Description;
        if (dto.IsActive.HasValue) splitterType.IsActive = dto.IsActive.Value;
        if (dto.DisplayOrder.HasValue) splitterType.DisplayOrder = dto.DisplayOrder.Value;
        splitterType.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SplitterType updated: {SplitterTypeId}", id);

        return MapToDto(splitterType);
    }

    public async Task DeleteSplitterTypeAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to delete a splitter type.");

        var splitterType = await _context.SplitterTypes
            .Where(st => st.Id == id && st.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (splitterType == null)
        {
            throw new KeyNotFoundException($"SplitterType with ID {id} not found");
        }

        // Check if any splitters are using this splitter type
        var hasSplitters = await _context.Splitters.AnyAsync(s => s.SplitterTypeId == id, cancellationToken);
        if (hasSplitters)
        {
            throw new InvalidOperationException($"Cannot delete SplitterType {id} because it is being used by splitters");
        }

        _context.SplitterTypes.Remove(splitterType);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SplitterType deleted: {SplitterTypeId}", id);
    }

    private static SplitterTypeDto MapToDto(SplitterType splitterType)
    {
        return new SplitterTypeDto
        {
            Id = splitterType.Id,
            CompanyId = splitterType.CompanyId,
            DepartmentId = splitterType.DepartmentId,
            Name = splitterType.Name,
            Code = splitterType.Code,
            TotalPorts = splitterType.TotalPorts,
            StandbyPortNumber = splitterType.StandbyPortNumber,
            Description = splitterType.Description,
            IsActive = splitterType.IsActive,
            DisplayOrder = splitterType.DisplayOrder,
            CreatedAt = splitterType.CreatedAt,
            UpdatedAt = splitterType.UpdatedAt
        };
    }
}

