using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Inventory.Services;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Settings.Services;

public interface IWarehouseService
{
    Task<List<WarehouseDto>> GetAllAsync(Guid companyId, bool? isActive = null);
    Task<WarehouseDto?> GetByIdAsync(Guid id);
    Task<WarehouseDto> CreateAsync(Guid companyId, WarehouseDto dto);
    Task<WarehouseDto> UpdateAsync(Guid id, WarehouseDto dto);
    Task DeleteAsync(Guid id);
}

public class WarehouseService : IWarehouseService
{
    private readonly ApplicationDbContext _context;
    private readonly ILocationAutoCreateService? _locationAutoCreateService;
    private readonly ILogger<WarehouseService>? _logger;

    public WarehouseService(
        ApplicationDbContext context,
        ILocationAutoCreateService? locationAutoCreateService = null,
        ILogger<WarehouseService>? logger = null)
    {
        _context = context;
        _locationAutoCreateService = locationAutoCreateService;
        _logger = logger;
    }

    public async Task<List<WarehouseDto>> GetAllAsync(Guid companyId, bool? isActive = null)
    {
        var query = _context.Set<Warehouse>()
            .Where(x => x.CompanyId == companyId && !x.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        var items = await query.OrderBy(x => x.Name).ToListAsync();

        return items.Select(x => new WarehouseDto
        {
            Id = x.Id,
            CompanyId = x.CompanyId,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            Address = x.Address,
            ManagerId = x.ManagerId,
            ManagerName = x.ManagerName,
            BinCount = x.BinCount,
            Capacity = x.Capacity,
            CurrentStock = x.CurrentStock,
            UtilizationPercent = x.UtilizationPercent,
            IsActive = x.IsActive
        }).ToList();
    }

    public async Task<WarehouseDto?> GetByIdAsync(Guid id)
    {
        var item = await _context.Set<Warehouse>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null) return null;

        return new WarehouseDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            Address = item.Address,
            ManagerId = item.ManagerId,
            ManagerName = item.ManagerName,
            BinCount = item.BinCount,
            Capacity = item.Capacity,
            CurrentStock = item.CurrentStock,
            UtilizationPercent = item.UtilizationPercent,
            IsActive = item.IsActive
        };
    }

    public async Task<WarehouseDto> CreateAsync(Guid companyId, WarehouseDto dto)
    {
        var item = new Warehouse
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            Address = dto.Address,
            ManagerId = dto.ManagerId,
            ManagerName = dto.ManagerName,
            BinCount = 0,
            Capacity = dto.Capacity,
            CurrentStock = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Warehouse>().Add(item);
        await _context.SaveChangesAsync();

        // Auto-create stock location if service is available
        if (_locationAutoCreateService != null)
        {
            try
            {
                await _locationAutoCreateService.CreateLocationForWarehouseAsync(
                    companyId,
                    item.Id,
                    item.Name);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to auto-create location for Warehouse {WarehouseId}", item.Id);
                // Don't fail warehouse creation if location creation fails
            }
        }

        return new WarehouseDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            Address = item.Address,
            ManagerId = item.ManagerId,
            ManagerName = item.ManagerName,
            BinCount = item.BinCount,
            Capacity = item.Capacity,
            CurrentStock = item.CurrentStock,
            UtilizationPercent = item.UtilizationPercent,
            IsActive = item.IsActive
        };
    }

    public async Task<WarehouseDto> UpdateAsync(Guid id, WarehouseDto dto)
    {
        var item = await _context.Set<Warehouse>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null)
            throw new Exception("Warehouse not found");

        item.Name = dto.Name;
        item.Description = dto.Description;
        item.Address = dto.Address;
        item.ManagerId = dto.ManagerId;
        item.ManagerName = dto.ManagerName;
        item.Capacity = dto.Capacity;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new WarehouseDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            Address = item.Address,
            ManagerId = item.ManagerId,
            ManagerName = item.ManagerName,
            BinCount = item.BinCount,
            Capacity = item.Capacity,
            CurrentStock = item.CurrentStock,
            UtilizationPercent = item.UtilizationPercent,
            IsActive = item.IsActive
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _context.Set<Warehouse>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null)
            throw new Exception("Warehouse not found");

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}

