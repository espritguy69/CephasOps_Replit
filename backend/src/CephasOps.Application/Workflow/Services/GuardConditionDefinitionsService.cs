using CephasOps.Application.Workflow.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Services;

/// <summary>
/// Service for managing guard condition definitions
/// </summary>
public class GuardConditionDefinitionsService : IGuardConditionDefinitionsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GuardConditionDefinitionsService> _logger;

    public GuardConditionDefinitionsService(
        ApplicationDbContext context,
        ILogger<GuardConditionDefinitionsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<GuardConditionDefinitionDto>> GetGuardConditionDefinitionsAsync(
        Guid companyId,
        string? entityType = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting guard condition definitions for company {CompanyId}, entityType: {EntityType}, isActive: {IsActive}",
            companyId, entityType, isActive);

        var query = _context.Set<GuardConditionDefinition>()
            .Where(gcd => gcd.CompanyId == companyId && !gcd.IsDeleted);

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(gcd => gcd.EntityType == entityType);
        }

        if (isActive.HasValue)
        {
            query = query.Where(gcd => gcd.IsActive == isActive.Value);
        }

        var definitions = await query
            .OrderBy(gcd => gcd.DisplayOrder)
            .ThenBy(gcd => gcd.Name)
            .ToListAsync(cancellationToken);

        return definitions.Select(MapToDto).ToList();
    }

    public async Task<GuardConditionDefinitionDto?> GetGuardConditionDefinitionAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting guard condition definition {Id} for company {CompanyId}", id, companyId);

        var definition = await _context.Set<GuardConditionDefinition>()
            .FirstOrDefaultAsync(gcd => gcd.Id == id 
                && gcd.CompanyId == companyId 
                && !gcd.IsDeleted, 
                cancellationToken);

        return definition != null ? MapToDto(definition) : null;
    }

    public async Task<GuardConditionDefinitionDto> CreateGuardConditionDefinitionAsync(
        Guid companyId,
        CreateGuardConditionDefinitionDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating guard condition definition {Key} for company {CompanyId}", dto.Key, companyId);

        // Check if key already exists
        var existing = await _context.Set<GuardConditionDefinition>()
            .FirstOrDefaultAsync(gcd => gcd.CompanyId == companyId 
                && gcd.Key == dto.Key 
                && gcd.EntityType == dto.EntityType
                && !gcd.IsDeleted, 
                cancellationToken);

        if (existing != null)
        {
            throw new InvalidOperationException(
                $"Guard condition definition with key '{dto.Key}' already exists for entity type '{dto.EntityType}'.");
        }

        var definition = new GuardConditionDefinition
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Key = dto.Key,
            Name = dto.Name,
            Description = dto.Description,
            EntityType = dto.EntityType,
            ValidatorType = dto.ValidatorType,
            ValidatorConfigJson = dto.ValidatorConfigJson,
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<GuardConditionDefinition>().Add(definition);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created guard condition definition {Id} ({Key})", definition.Id, definition.Key);

        return MapToDto(definition);
    }

    public async Task<GuardConditionDefinitionDto> UpdateGuardConditionDefinitionAsync(
        Guid companyId,
        Guid id,
        UpdateGuardConditionDefinitionDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating guard condition definition {Id} for company {CompanyId}", id, companyId);

        var definition = await _context.Set<GuardConditionDefinition>()
            .FirstOrDefaultAsync(gcd => gcd.Id == id 
                && gcd.CompanyId == companyId 
                && !gcd.IsDeleted, 
                cancellationToken);

        if (definition == null)
        {
            throw new KeyNotFoundException($"Guard condition definition with ID '{id}' not found.");
        }

        if (dto.Name != null) definition.Name = dto.Name;
        if (dto.Description != null) definition.Description = dto.Description;
        if (dto.ValidatorType != null) definition.ValidatorType = dto.ValidatorType;
        if (dto.ValidatorConfigJson != null) definition.ValidatorConfigJson = dto.ValidatorConfigJson;
        if (dto.IsActive.HasValue) definition.IsActive = dto.IsActive.Value;
        if (dto.DisplayOrder.HasValue) definition.DisplayOrder = dto.DisplayOrder.Value;
        definition.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated guard condition definition {Id} ({Key})", definition.Id, definition.Key);

        return MapToDto(definition);
    }

    public async Task DeleteGuardConditionDefinitionAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting guard condition definition {Id} for company {CompanyId}", id, companyId);

        var definition = await _context.Set<GuardConditionDefinition>()
            .FirstOrDefaultAsync(gcd => gcd.Id == id 
                && gcd.CompanyId == companyId 
                && !gcd.IsDeleted, 
                cancellationToken);

        if (definition == null)
        {
            throw new KeyNotFoundException($"Guard condition definition with ID '{id}' not found.");
        }

        // Soft delete
        definition.IsDeleted = true;
        definition.DeletedAt = DateTime.UtcNow;
        definition.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted guard condition definition {Id} ({Key})", definition.Id, definition.Key);
    }

    private static GuardConditionDefinitionDto MapToDto(GuardConditionDefinition definition)
    {
        return new GuardConditionDefinitionDto
        {
            Id = definition.Id,
            CompanyId = definition.CompanyId,
            Key = definition.Key,
            Name = definition.Name,
            Description = definition.Description,
            EntityType = definition.EntityType,
            ValidatorType = definition.ValidatorType,
            ValidatorConfigJson = definition.ValidatorConfigJson,
            IsActive = definition.IsActive,
            DisplayOrder = definition.DisplayOrder,
            CreatedAt = definition.CreatedAt,
            UpdatedAt = definition.UpdatedAt
        };
    }
}

