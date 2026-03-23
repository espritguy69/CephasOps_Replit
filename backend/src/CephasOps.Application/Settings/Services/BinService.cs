using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Settings.Services;

public interface IBinService
{
    Task<List<BinDto>> GetAllAsync(Guid companyId, bool? isActive = null);
    Task<BinDto?> GetByIdAsync(Guid id);
    Task<BinDto> CreateAsync(Guid companyId, BinDto dto);
    Task<BinDto> UpdateAsync(Guid id, BinDto dto);
    Task DeleteAsync(Guid id);
}

public class BinService : IBinService
{
    private readonly ApplicationDbContext _context;

    public BinService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BinDto>> GetAllAsync(Guid companyId, bool? isActive = null)
    {
        var query = _context.Set<Bin>()
            .Where(x => x.CompanyId == companyId && !x.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        var items = await query.OrderBy(x => x.Name).ToListAsync();

        return items.Select(x => new BinDto
        {
            Id = x.Id,
            CompanyId = x.CompanyId,
            Code = x.Code,
            Name = x.Name,
            WarehouseId = x.WarehouseId,
            WarehouseName = x.WarehouseName,
            Section = x.Section,
            Row = x.Row,
            Level = x.Level,
            Capacity = x.Capacity,
            CurrentStock = x.CurrentStock,
            UtilizationPercent = x.UtilizationPercent,
            IsActive = x.IsActive
        }).ToList();
    }

    public async Task<BinDto?> GetByIdAsync(Guid id)
    {
        var item = await _context.Set<Bin>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null) return null;

        return new BinDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            WarehouseId = item.WarehouseId,
            WarehouseName = item.WarehouseName,
            Section = item.Section,
            Row = item.Row,
            Level = item.Level,
            Capacity = item.Capacity,
            CurrentStock = item.CurrentStock,
            UtilizationPercent = item.UtilizationPercent,
            IsActive = item.IsActive
        };
    }

    public async Task<BinDto> CreateAsync(Guid companyId, BinDto dto)
    {
        var item = new Bin
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = dto.Code,
            Name = dto.Name,
            WarehouseId = dto.WarehouseId,
            WarehouseName = dto.WarehouseName,
            Section = dto.Section,
            Row = dto.Row,
            Level = dto.Level,
            Capacity = dto.Capacity,
            CurrentStock = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Bin>().Add(item);
        await _context.SaveChangesAsync();

        return new BinDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            WarehouseId = item.WarehouseId,
            WarehouseName = item.WarehouseName,
            Section = item.Section,
            Row = item.Row,
            Level = item.Level,
            Capacity = item.Capacity,
            CurrentStock = item.CurrentStock,
            UtilizationPercent = item.UtilizationPercent,
            IsActive = item.IsActive
        };
    }

    public async Task<BinDto> UpdateAsync(Guid id, BinDto dto)
    {
        var item = await _context.Set<Bin>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null)
            throw new Exception("Bin not found");

        item.Name = dto.Name;
        item.WarehouseId = dto.WarehouseId;
        item.WarehouseName = dto.WarehouseName;
        item.Section = dto.Section;
        item.Row = dto.Row;
        item.Level = dto.Level;
        item.Capacity = dto.Capacity;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new BinDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            WarehouseId = item.WarehouseId,
            WarehouseName = item.WarehouseName,
            Section = item.Section,
            Row = item.Row,
            Level = item.Level,
            Capacity = item.Capacity,
            CurrentStock = item.CurrentStock,
            UtilizationPercent = item.UtilizationPercent,
            IsActive = item.IsActive
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _context.Set<Bin>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null)
            throw new Exception("Bin not found");

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}

