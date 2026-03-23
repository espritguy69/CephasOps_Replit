using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Settings.Services;

public interface IBrandService
{
    Task<List<BrandDto>> GetAllAsync(Guid companyId, bool? isActive = null);
    Task<BrandDto?> GetByIdAsync(Guid id);
    Task<BrandDto> CreateAsync(Guid companyId, CreateBrandDto dto);
    Task<BrandDto> UpdateAsync(Guid id, UpdateBrandDto dto);
    Task DeleteAsync(Guid id);
}

public class BrandService : IBrandService
{
    private readonly ApplicationDbContext _context;

    public BrandService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BrandDto>> GetAllAsync(Guid companyId, bool? isActive = null)
    {
        var query = _context.Set<Brand>()
            .Where(x => x.CompanyId == companyId && !x.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        var items = await query.OrderBy(x => x.Name).ToListAsync();

        return items.Select(x => new BrandDto
        {
            Id = x.Id,
            CompanyId = x.CompanyId,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            Country = x.Country,
            Website = x.Website,
            MaterialCount = x.MaterialCount,
            IsActive = x.IsActive
        }).ToList();
    }

    public async Task<BrandDto?> GetByIdAsync(Guid id)
    {
        var item = await _context.Set<Brand>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null) return null;

        return new BrandDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            Country = item.Country,
            Website = item.Website,
            MaterialCount = item.MaterialCount,
            IsActive = item.IsActive
        };
    }

    public async Task<BrandDto> CreateAsync(Guid companyId, CreateBrandDto dto)
    {
        var item = new Brand
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            Country = dto.Country,
            Website = dto.Website,
            MaterialCount = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Brand>().Add(item);
        await _context.SaveChangesAsync();

        return new BrandDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            Country = item.Country,
            Website = item.Website,
            MaterialCount = item.MaterialCount,
            IsActive = item.IsActive
        };
    }

    public async Task<BrandDto> UpdateAsync(Guid id, UpdateBrandDto dto)
    {
        var item = await _context.Set<Brand>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null)
            throw new Exception("Brand not found");

        item.Name = dto.Name;
        item.Description = dto.Description;
        item.Country = dto.Country;
        item.Website = dto.Website;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new BrandDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            Country = item.Country,
            Website = item.Website,
            MaterialCount = item.MaterialCount,
            IsActive = item.IsActive
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _context.Set<Brand>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null)
            throw new Exception("Brand not found");

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}

