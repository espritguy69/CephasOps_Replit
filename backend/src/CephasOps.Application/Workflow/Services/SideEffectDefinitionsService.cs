using CephasOps.Application.Workflow.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Services;

/// <summary>
/// Service for managing side effect definitions
/// </summary>
public class SideEffectDefinitionsService : ISideEffectDefinitionsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SideEffectDefinitionsService> _logger;

    public SideEffectDefinitionsService(
        ApplicationDbContext context,
        ILogger<SideEffectDefinitionsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SideEffectDefinitionDto>> GetSideEffectDefinitionsAsync(
        Guid companyId,
        string? entityType = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting side effect definitions for company {CompanyId}, entityType: {EntityType}, isActive: {IsActive}",
            companyId, entityType, isActive);

        var query = _context.Set<SideEffectDefinition>()
            .Where(sed => sed.CompanyId == companyId && !sed.IsDeleted);

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(sed => sed.EntityType == entityType);
        }

        if (isActive.HasValue)
        {
            query = query.Where(sed => sed.IsActive == isActive.Value);
        }

        var definitions = await query
            .OrderBy(sed => sed.DisplayOrder)
            .ThenBy(sed => sed.Name)
            .ToListAsync(cancellationToken);

        return definitions.Select(MapToDto).ToList();
    }

    public async Task<SideEffectDefinitionDto?> GetSideEffectDefinitionAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting side effect definition {Id} for company {CompanyId}", id, companyId);

        var definition = await _context.Set<SideEffectDefinition>()
            .FirstOrDefaultAsync(sed => sed.Id == id 
                && sed.CompanyId == companyId 
                && !sed.IsDeleted, 
                cancellationToken);

        return definition != null ? MapToDto(definition) : null;
    }

    public async Task<SideEffectDefinitionDto> CreateSideEffectDefinitionAsync(
        Guid companyId,
        CreateSideEffectDefinitionDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating side effect definition {Key} for company {CompanyId}", dto.Key, companyId);

        // Check if key already exists
        var existing = await _context.Set<SideEffectDefinition>()
            .FirstOrDefaultAsync(sed => sed.CompanyId == companyId 
                && sed.Key == dto.Key 
                && sed.EntityType == dto.EntityType
                && !sed.IsDeleted, 
                cancellationToken);

        if (existing != null)
        {
            throw new InvalidOperationException(
                $"Side effect definition with key '{dto.Key}' already exists for entity type '{dto.EntityType}'.");
        }

        var definition = new SideEffectDefinition
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Key = dto.Key,
            Name = dto.Name,
            Description = dto.Description,
            EntityType = dto.EntityType,
            ExecutorType = dto.ExecutorType,
            ExecutorConfigJson = dto.ExecutorConfigJson,
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<SideEffectDefinition>().Add(definition);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created side effect definition {Id} ({Key})", definition.Id, definition.Key);

        return MapToDto(definition);
    }

    public async Task<SideEffectDefinitionDto> UpdateSideEffectDefinitionAsync(
        Guid companyId,
        Guid id,
        UpdateSideEffectDefinitionDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating side effect definition {Id} for company {CompanyId}", id, companyId);

        var definition = await _context.Set<SideEffectDefinition>()
            .FirstOrDefaultAsync(sed => sed.Id == id 
                && sed.CompanyId == companyId 
                && !sed.IsDeleted, 
                cancellationToken);

        if (definition == null)
        {
            throw new KeyNotFoundException($"Side effect definition with ID '{id}' not found.");
        }

        if (dto.Name != null) definition.Name = dto.Name;
        if (dto.Description != null) definition.Description = dto.Description;
        if (dto.ExecutorType != null) definition.ExecutorType = dto.ExecutorType;
        if (dto.ExecutorConfigJson != null) definition.ExecutorConfigJson = dto.ExecutorConfigJson;
        if (dto.IsActive.HasValue) definition.IsActive = dto.IsActive.Value;
        if (dto.DisplayOrder.HasValue) definition.DisplayOrder = dto.DisplayOrder.Value;
        definition.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated side effect definition {Id} ({Key})", definition.Id, definition.Key);

        return MapToDto(definition);
    }

    public async Task DeleteSideEffectDefinitionAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting side effect definition {Id} for company {CompanyId}", id, companyId);

        var definition = await _context.Set<SideEffectDefinition>()
            .FirstOrDefaultAsync(sed => sed.Id == id 
                && sed.CompanyId == companyId 
                && !sed.IsDeleted, 
                cancellationToken);

        if (definition == null)
        {
            throw new KeyNotFoundException($"Side effect definition with ID '{id}' not found.");
        }

        // Soft delete
        definition.IsDeleted = true;
        definition.DeletedAt = DateTime.UtcNow;
        definition.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted side effect definition {Id} ({Key})", definition.Id, definition.Key);
    }

    private static SideEffectDefinitionDto MapToDto(SideEffectDefinition definition)
    {
        return new SideEffectDefinitionDto
        {
            Id = definition.Id,
            CompanyId = definition.CompanyId,
            Key = definition.Key,
            Name = definition.Name,
            Description = definition.Description,
            EntityType = definition.EntityType,
            ExecutorType = definition.ExecutorType,
            ExecutorConfigJson = definition.ExecutorConfigJson,
            IsActive = definition.IsActive,
            DisplayOrder = definition.DisplayOrder,
            CreatedAt = definition.CreatedAt,
            UpdatedAt = definition.UpdatedAt
        };
    }
}

