using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Settings.Services;

public interface IProductTypeService
{
    Task<List<ProductTypeDto>> GetAllAsync(Guid companyId, bool? isActive = null);
    Task<ProductTypeDto?> GetByIdAsync(Guid id);
    Task<ProductTypeDto> CreateAsync(Guid companyId, ProductTypeDto dto);
    Task<ProductTypeDto> UpdateAsync(Guid id, ProductTypeDto dto);
    Task DeleteAsync(Guid id);
}

public class ProductTypeService : IProductTypeService
{
    private readonly ApplicationDbContext _context;

    public ProductTypeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductTypeDto>> GetAllAsync(Guid companyId, bool? isActive = null)
    {
        var query = _context.Set<ProductType>()
            .Where(x => x.CompanyId == companyId && !x.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        var items = await query.OrderBy(x => x.Name).ToListAsync();

        return items.Select(x => new ProductTypeDto
        {
            Id = x.Id,
            CompanyId = x.CompanyId,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            Category = x.Category,
            RequiresInstallation = x.RequiresInstallation,
            PlanCount = x.PlanCount,
            IsActive = x.IsActive
        }).ToList();
    }

    public async Task<ProductTypeDto?> GetByIdAsync(Guid id)
    {
        var item = await _context.Set<ProductType>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null) return null;

        return new ProductTypeDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            Category = item.Category,
            RequiresInstallation = item.RequiresInstallation,
            PlanCount = item.PlanCount,
            IsActive = item.IsActive
        };
    }

    public async Task<ProductTypeDto> CreateAsync(Guid companyId, ProductTypeDto dto)
    {
        var item = new ProductType
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            RequiresInstallation = dto.RequiresInstallation,
            PlanCount = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ProductType>().Add(item);
        await _context.SaveChangesAsync();

        return new ProductTypeDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            Category = item.Category,
            RequiresInstallation = item.RequiresInstallation,
            PlanCount = item.PlanCount,
            IsActive = item.IsActive
        };
    }

    public async Task<ProductTypeDto> UpdateAsync(Guid id, ProductTypeDto dto)
    {
        var item = await _context.Set<ProductType>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null)
            throw new Exception("Product type not found");

        item.Name = dto.Name;
        item.Description = dto.Description;
        item.Category = dto.Category;
        item.RequiresInstallation = dto.RequiresInstallation;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ProductTypeDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            Category = item.Category,
            RequiresInstallation = item.RequiresInstallation,
            PlanCount = item.PlanCount,
            IsActive = item.IsActive
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _context.Set<ProductType>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null)
            throw new Exception("Product type not found");

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}

