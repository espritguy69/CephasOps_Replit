using CephasOps.Application.Inventory.DTOs;
using CephasOps.Domain.Inventory.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Inventory.Services;

/// <summary>
/// Material category service implementation
/// </summary>
public class MaterialCategoryService : IMaterialCategoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MaterialCategoryService> _logger;

    public MaterialCategoryService(
        ApplicationDbContext context,
        ILogger<MaterialCategoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<MaterialCategoryDto>> GetMaterialCategoriesAsync(Guid? companyId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting material categories for company {CompanyId}", companyId);

        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<MaterialCategoryDto>();

        var query = _context.Set<MaterialCategory>().Where(c => c.CompanyId == effectiveCompanyId.Value);

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        var categories = await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(MapToDto).ToList();
    }

    public async Task<MaterialCategoryDto?> GetMaterialCategoryByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting material category {CategoryId} for company {CompanyId}", id, companyId);

        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return null;

        var category = await _context.Set<MaterialCategory>()
            .Where(c => c.Id == id && c.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return category != null ? MapToDto(category) : null;
    }

    public async Task<MaterialCategoryDto> CreateMaterialCategoryAsync(CreateMaterialCategoryDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to create a material category.");

        _logger.LogInformation("Creating material category for company {CompanyId}", effectiveCompanyId);

        var exists = await _context.Set<MaterialCategory>()
            .AnyAsync(c => c.CompanyId == effectiveCompanyId.Value && c.Name.ToLower() == dto.Name.ToLower().Trim(), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Material category with name '{dto.Name}' already exists");
        }

        var category = new MaterialCategory
        {
            Id = Guid.NewGuid(),
            CompanyId = effectiveCompanyId.Value,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<MaterialCategory>().Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(category);
    }

    public async Task<MaterialCategoryDto> UpdateMaterialCategoryAsync(Guid id, UpdateMaterialCategoryDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating material category {CategoryId} for company {CompanyId}", id, companyId);

        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to update a material category.");

        var category = await _context.Set<MaterialCategory>()
            .Where(c => c.Id == id && c.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (category == null)
        {
            throw new KeyNotFoundException($"Material category with ID {id} not found");
        }

        // Check name uniqueness if changed
        if (!string.IsNullOrEmpty(dto.Name) && dto.Name.Trim() != category.Name)
        {
            var categoryCompanyId = category.CompanyId;
            var exists = await _context.Set<MaterialCategory>()
                .AnyAsync(c => c.CompanyId == categoryCompanyId && 
                              c.Name.ToLower() == dto.Name.ToLower().Trim() && 
                              c.Id != id, cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException($"Material category with name '{dto.Name}' already exists");
            }
        }

        if (!string.IsNullOrEmpty(dto.Name)) category.Name = dto.Name.Trim();
        if (dto.Description != null) category.Description = dto.Description.Trim();
        if (dto.DisplayOrder.HasValue) category.DisplayOrder = dto.DisplayOrder.Value;
        if (dto.IsActive.HasValue) category.IsActive = dto.IsActive.Value;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(category);
    }

    public async Task DeleteMaterialCategoryAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting material category {CategoryId} for company {CompanyId}", id, companyId);

        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to delete a material category.");

        var category = await _context.Set<MaterialCategory>()
            .Where(c => c.Id == id && c.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (category == null)
        {
            throw new KeyNotFoundException($"Material category with ID {id} not found");
        }

        // Check if category is used by any materials
        var isUsed = await _context.Materials
            .AnyAsync(m => m.Category == category.Name, cancellationToken);

        if (isUsed)
        {
            throw new InvalidOperationException($"Cannot delete category '{category.Name}' because it is being used by materials. Deactivate it instead.");
        }

        _context.Set<MaterialCategory>().Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static MaterialCategoryDto MapToDto(MaterialCategory category)
    {
        return new MaterialCategoryDto
        {
            Id = category.Id,
            CompanyId = category.CompanyId,
            Name = category.Name,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        };
    }
}

