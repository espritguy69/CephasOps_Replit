using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Material template service implementation
/// </summary>
public class MaterialTemplateService : IMaterialTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MaterialTemplateService> _logger;

    public MaterialTemplateService(ApplicationDbContext context, ILogger<MaterialTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<MaterialTemplateDto>> GetTemplatesAsync(Guid companyId, string? orderType = null, Guid? installationMethodId = null, Guid? buildingTypeId = null, Guid? partnerId = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting material templates for company {CompanyId}", companyId);

        if (!installationMethodId.HasValue && buildingTypeId.HasValue)
        {
            _logger.LogWarning("BuildingTypeId is deprecated. Use InstallationMethodId instead when querying material templates.");
        }

        var query = _context.MaterialTemplates
            .Include(t => t.Items)
            .Where(t => t.CompanyId == companyId);

        if (!string.IsNullOrEmpty(orderType))
        {
            query = query.Where(t => t.OrderType == orderType);
        }

        if (installationMethodId.HasValue)
        {
            query = query.Where(t => t.InstallationMethodId == installationMethodId);
        }
        else if (buildingTypeId.HasValue)
        {
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            query = query.Where(t => t.BuildingTypeId == buildingTypeId);
#pragma warning restore CS0618
        }

        if (partnerId.HasValue)
        {
            query = query.Where(t => t.PartnerId == partnerId);
        }

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        var templates = await query.OrderBy(t => t.OrderType).ThenBy(t => t.Name).ToListAsync(cancellationToken);

        var templatesList = templates.ToList();
        var dtos = new List<MaterialTemplateDto>();
        foreach (var t in templatesList)
        {
            dtos.Add(await MapToDtoAsync(t, cancellationToken));
        }
        return dtos;
    }

    public async Task<MaterialTemplateDto?> GetTemplateByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting material template {TemplateId} for company {CompanyId}", id, companyId);

        var template = await _context.MaterialTemplates
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId, cancellationToken);

        if (template == null) return null;

        return await MapToDtoAsync(template, cancellationToken);
    }

    public async Task<MaterialTemplateDto?> GetEffectiveTemplateAsync(Guid companyId, Guid? partnerId, string orderType, Guid? installationMethodId, Guid? buildingTypeId, CancellationToken cancellationToken = default)
    {
        if (!installationMethodId.HasValue && buildingTypeId.HasValue)
        {
            _logger.LogWarning("BuildingTypeId is deprecated. Use InstallationMethodId instead when resolving material templates.");
        }

        _logger.LogInformation("Getting effective material template for company {CompanyId}, partner {PartnerId}, orderType {OrderType}, installationMethod {InstallationMethodId}, buildingType {BuildingTypeId}", 
            companyId, partnerId, orderType, installationMethodId, buildingTypeId);

        // Try most specific match first: CompanyId + PartnerId + OrderType + InstallationMethodId/BuildingTypeId
        // Prefer InstallationMethodId over BuildingTypeId for matching
        var templateQuery = _context.MaterialTemplates
            .Include(t => t.Items)
            .Where(t => t.CompanyId == companyId 
                && t.OrderType == orderType 
                && t.IsActive
                && (partnerId == null || t.PartnerId == partnerId));

        if (installationMethodId.HasValue)
        {
            templateQuery = templateQuery.Where(t => t.InstallationMethodId == installationMethodId);
        }
        else if (buildingTypeId.HasValue)
        {
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            templateQuery = templateQuery.Where(t => t.InstallationMethodId == null && t.BuildingTypeId == buildingTypeId);
#pragma warning restore CS0618
        }
        else
        {
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            templateQuery = templateQuery.Where(t => t.InstallationMethodId == null && t.BuildingTypeId == null);
#pragma warning restore CS0618
        }

        var template = await templateQuery
            .OrderByDescending(t => t.PartnerId != null ? 1 : 0)
            .ThenByDescending(t => t.InstallationMethodId != null ? 1 : 0)
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            .ThenByDescending(t => t.BuildingTypeId != null ? 1 : 0)
#pragma warning restore CS0618
            .FirstOrDefaultAsync(cancellationToken);

        // Fallback to default template
        if (template == null)
        {
            template = await _context.MaterialTemplates
                .Include(t => t.Items)
                .Where(t => t.CompanyId == companyId 
                    && t.OrderType == orderType 
                    && t.IsDefault 
                    && t.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (template == null) return null;

        return await MapToDtoAsync(template, cancellationToken);
    }

    public async Task<MaterialTemplateDto> CreateTemplateAsync(CreateMaterialTemplateDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating material template for company {CompanyId}", companyId);

        if (!dto.InstallationMethodId.HasValue && dto.BuildingTypeId.HasValue)
        {
            _logger.LogWarning("BuildingTypeId is deprecated. Use InstallationMethodId instead when creating material templates.");
        }

        // Check for duplicate template (same name + orderType + partner + buildingType/installationMethod)
        var duplicateQuery = _context.MaterialTemplates
            .Where(t => t.CompanyId == companyId
                && t.Name == dto.Name.Trim()
                && t.OrderType == dto.OrderType
                && t.PartnerId == dto.PartnerId);
        
        if (dto.InstallationMethodId.HasValue)
        {
            duplicateQuery = duplicateQuery.Where(t => t.InstallationMethodId == dto.InstallationMethodId);
        }
        else
        {
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            duplicateQuery = duplicateQuery.Where(t => t.InstallationMethodId == null && t.BuildingTypeId == dto.BuildingTypeId);
#pragma warning restore CS0618
        }

        var duplicate = await duplicateQuery.FirstOrDefaultAsync(cancellationToken);
        if (duplicate != null)
        {
            throw new InvalidOperationException(
                $"A material template with the name '{dto.Name}' already exists for the same context " +
                $"(OrderType: {dto.OrderType}, PartnerId: {dto.PartnerId}, " +
                $"InstallationMethodId: {dto.InstallationMethodId}, BuildingTypeId: {dto.BuildingTypeId}). " +
                $"Existing template ID: {duplicate.Id}");
        }

        var template = new MaterialTemplate
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = dto.Name.Trim(),
            OrderType = dto.OrderType,
            InstallationMethodId = dto.InstallationMethodId,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            BuildingTypeId = dto.BuildingTypeId,
#pragma warning restore CS0618
            PartnerId = dto.PartnerId,
            IsDefault = dto.IsDefault,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = userId
        };

        // If this is set as default, unset other defaults for the same context
        if (dto.IsDefault)
        {
            var existingDefaults = await _context.MaterialTemplates
                .Where(t => t.CompanyId == companyId 
                    && t.OrderType == dto.OrderType 
                    && t.InstallationMethodId == dto.InstallationMethodId
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
                    && t.BuildingTypeId == dto.BuildingTypeId
#pragma warning restore CS0618
                    && t.PartnerId == dto.PartnerId
                    && t.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
            }
        }

        // Add items (tenant-safe: scope material by companyId)
        foreach (var itemDto in dto.Items)
        {
            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Id == itemDto.MaterialId && m.CompanyId == companyId, cancellationToken);
            if (material == null || material.CompanyId != companyId)
            {
                throw new InvalidOperationException($"Material {itemDto.MaterialId} not found or doesn't belong to company");
            }

            template.Items.Add(new MaterialTemplateItem
            {
                Id = Guid.NewGuid(),
                MaterialTemplateId = template.Id,
                MaterialId = itemDto.MaterialId,
                Quantity = itemDto.Quantity,
                UnitOfMeasure = material.UnitOfMeasure,
                IsSerialised = material.IsSerialised,
                Notes = itemDto.Notes,
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.MaterialTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(template, cancellationToken);
    }

    public async Task<MaterialTemplateDto> UpdateTemplateAsync(Guid id, UpdateMaterialTemplateDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating material template {TemplateId} for company {CompanyId}", id, companyId);

        var template = await _context.MaterialTemplates
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId, cancellationToken);

        if (template == null)
        {
            throw new KeyNotFoundException($"Material template {id} not found");
        }

        if (!dto.InstallationMethodId.HasValue && template.InstallationMethodId == null)
        {
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            if (template.BuildingTypeId.HasValue)
#pragma warning restore CS0618
            {
                _logger.LogWarning("BuildingTypeId is deprecated. Use InstallationMethodId instead when updating material templates.");
            }
        }

        if (!string.IsNullOrEmpty(dto.Name))
        {
            template.Name = dto.Name;
        }

        if (dto.InstallationMethodId.HasValue)
        {
            template.InstallationMethodId = dto.InstallationMethodId;
        }

        if (dto.IsDefault.HasValue)
        {
            template.IsDefault = dto.IsDefault.Value;
            
            // If setting as default, unset other defaults
            if (dto.IsDefault.Value)
            {
                var existingDefaults = await _context.MaterialTemplates
                    .Where(t => t.CompanyId == companyId 
                        && t.OrderType == template.OrderType 
                        && t.InstallationMethodId == (dto.InstallationMethodId ?? template.InstallationMethodId)
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
                && t.BuildingTypeId == template.BuildingTypeId
#pragma warning restore CS0618
                        && t.PartnerId == template.PartnerId
                        && t.Id != id
                        && t.IsDefault)
                    .ToListAsync(cancellationToken);

                foreach (var existing in existingDefaults)
                {
                    existing.IsDefault = false;
                }
            }
        }

        if (dto.IsActive.HasValue)
        {
            template.IsActive = dto.IsActive.Value;
        }

        // Update items if provided
        if (dto.Items != null)
        {
            // Items are already loaded via Include() above
            // Create a copy of the list to avoid modification during iteration
            var itemsToRemove = template.Items.ToList();
            
            // Remove each item from the DbSet
            // Items from Include() should be tracked, but we'll remove them explicitly
            foreach (var item in itemsToRemove)
            {
                // Check if item is tracked, if not, attach it first
                var entry = _context.Entry(item);
                if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
                {
                    // Try to find the item in the database first (tenant-safe: item belongs to template which is company-scoped)
                    var existingItem = await _context.MaterialTemplateItems
                        .FirstOrDefaultAsync(i => i.Id == item.Id && i.MaterialTemplateId == template.Id, cancellationToken);
                    if (existingItem != null)
                    {
                        _context.MaterialTemplateItems.Remove(existingItem);
                    }
                }
                else
                {
                    _context.MaterialTemplateItems.Remove(item);
                }
            }
            
            // Clear the navigation property
            template.Items.Clear();

            // Add new items (tenant-safe: scope material by companyId)
            foreach (var itemDto in dto.Items)
            {
                var material = await _context.Materials
                    .FirstOrDefaultAsync(m => m.Id == itemDto.MaterialId && m.CompanyId == companyId, cancellationToken);
                if (material == null || material.CompanyId != companyId)
                {
                    throw new InvalidOperationException($"Material {itemDto.MaterialId} not found or doesn't belong to company");
                }

                template.Items.Add(new MaterialTemplateItem
                {
                    Id = Guid.NewGuid(),
                    MaterialTemplateId = template.Id,
                    MaterialId = itemDto.MaterialId,
                    Quantity = itemDto.Quantity,
                    UnitOfMeasure = material.UnitOfMeasure,
                    IsSerialised = material.IsSerialised,
                    Notes = itemDto.Notes,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(template, cancellationToken);
    }

    public async Task DeleteTemplateAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting material template {TemplateId} for company {CompanyId}", id, companyId);

        var template = await _context.MaterialTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId, cancellationToken);

        if (template == null)
        {
            throw new KeyNotFoundException($"Material template {id} not found");
        }

        _context.MaterialTemplates.Remove(template);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<MaterialTemplateDto> SetAsDefaultAsync(Guid id, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting material template {TemplateId} as default for company {CompanyId}", id, companyId);

        var template = await _context.MaterialTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId, cancellationToken);

        if (template == null)
        {
            throw new KeyNotFoundException($"Material template {id} not found");
        }

        // Unset other defaults for the same context
        var existingDefaults = await _context.MaterialTemplates
            .Where(t => t.CompanyId == companyId 
                && t.OrderType == template.OrderType 
                && t.InstallationMethodId == template.InstallationMethodId
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
                && t.BuildingTypeId == template.BuildingTypeId
#pragma warning restore CS0618
                && t.PartnerId == template.PartnerId
                && t.Id != id
                && t.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingDefaults)
        {
            existing.IsDefault = false;
        }

        template.IsDefault = true;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetTemplateByIdAsync(id, companyId, cancellationToken) 
            ?? throw new InvalidOperationException("Failed to retrieve updated template");
    }

    public async Task<MaterialTemplateDto> CloneTemplateAsync(Guid sourceId, string newName, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cloning material template {SourceId} to '{NewName}' for company {CompanyId}", sourceId, newName, companyId);

        var sourceTemplate = await _context.MaterialTemplates
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == sourceId && t.CompanyId == companyId, cancellationToken);

        if (sourceTemplate == null)
        {
            throw new KeyNotFoundException($"Material template entity with ID {sourceId} not found for company {companyId}");
        }

        // Check for duplicate name
        var duplicate = await _context.MaterialTemplates
            .FirstOrDefaultAsync(t => t.CompanyId == companyId && t.Name == newName.Trim(), cancellationToken);
        
        if (duplicate != null)
        {
            throw new InvalidOperationException(
                $"A material template with the name '{newName}' already exists (ID: {duplicate.Id})");
        }

        // Create new template
        var clonedTemplate = new MaterialTemplate
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            DepartmentId = sourceTemplate.DepartmentId,
            Name = newName.Trim(),
            OrderType = sourceTemplate.OrderType,
            InstallationMethodId = sourceTemplate.InstallationMethodId,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            BuildingTypeId = sourceTemplate.BuildingTypeId,
#pragma warning restore CS0618
            PartnerId = sourceTemplate.PartnerId,
            IsDefault = false, // Cloned templates are not default by default
            IsActive = sourceTemplate.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = userId
        };

        // Clone items
        foreach (var sourceItem in sourceTemplate.Items)
        {
            clonedTemplate.Items.Add(new MaterialTemplateItem
            {
                Id = Guid.NewGuid(),
                MaterialTemplateId = clonedTemplate.Id,
                MaterialId = sourceItem.MaterialId,
                Quantity = sourceItem.Quantity,
                UnitOfMeasure = sourceItem.UnitOfMeasure,
                IsSerialised = sourceItem.IsSerialised,
                Notes = sourceItem.Notes,
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.MaterialTemplates.Add(clonedTemplate);
        await _context.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(clonedTemplate, cancellationToken);
    }

    private async Task<MaterialTemplateDto> MapToDtoAsync(MaterialTemplate template, CancellationToken cancellationToken)
    {
        var materialIds = template.Items.Select(i => i.MaterialId).Distinct().ToList();
        Dictionary<Guid, Domain.Inventory.Entities.Material> materials;
        
        if (materialIds.Any())
        {
            var materialsList = await _context.Materials
                .Where(m => materialIds.Contains(m.Id))
                .ToListAsync(cancellationToken);
            materials = materialsList.ToDictionary(m => m.Id);
        }
        else
        {
            materials = new Dictionary<Guid, Domain.Inventory.Entities.Material>();
        }

        return new MaterialTemplateDto
        {
            Id = template.Id,
            CompanyId = template.CompanyId,
            Name = template.Name,
            OrderType = template.OrderType,
            InstallationMethodId = template.InstallationMethodId,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            BuildingTypeId = template.BuildingTypeId,
#pragma warning restore CS0618
            PartnerId = template.PartnerId,
            IsDefault = template.IsDefault,
            IsActive = template.IsActive,
            Items = template.Items.Select(i =>
            {
                var material = materials.GetValueOrDefault(i.MaterialId);
                return new MaterialTemplateItemDto
                {
                    Id = i.Id,
                    MaterialTemplateId = i.MaterialTemplateId,
                    MaterialId = i.MaterialId,
                    MaterialCode = material?.ItemCode ?? string.Empty,
                    MaterialDescription = material?.Description ?? string.Empty,
                    Quantity = i.Quantity,
                    UnitOfMeasure = i.UnitOfMeasure,
                    IsSerialised = i.IsSerialised,
                    Notes = i.Notes
                };
            }).ToList(),
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}

