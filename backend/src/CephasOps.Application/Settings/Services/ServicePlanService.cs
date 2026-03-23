using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Settings.Services;

public interface IServicePlanService
{
    Task<List<ServicePlanDto>> GetAllAsync(Guid companyId, bool? isActive = null);
    Task<ServicePlanDto?> GetByIdAsync(Guid id);
    Task<ServicePlanDto> CreateAsync(Guid companyId, ServicePlanDto dto);
    Task<ServicePlanDto> UpdateAsync(Guid id, ServicePlanDto dto);
    Task DeleteAsync(Guid id);
}

public class ServicePlanService : IServicePlanService
{
    private readonly ApplicationDbContext _context;

    public ServicePlanService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ServicePlanDto>> GetAllAsync(Guid companyId, bool? isActive = null)
    {
        var query = _context.Set<ServicePlan>()
            .Where(x => x.CompanyId == companyId && !x.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        var items = await query.OrderBy(x => x.Name).ToListAsync();

        return items.Select(x => new ServicePlanDto
        {
            Id = x.Id,
            CompanyId = x.CompanyId,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            ProductTypeId = x.ProductTypeId,
            ProductTypeName = x.ProductTypeName,
            SpeedMbps = x.SpeedMbps,
            MonthlyPrice = x.MonthlyPrice,
            SetupFee = x.SetupFee,
            ContractMonths = x.ContractMonths,
            IsActive = x.IsActive
        }).ToList();
    }

    public async Task<ServicePlanDto?> GetByIdAsync(Guid id)
    {
        var item = await _context.Set<ServicePlan>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null) return null;

        return new ServicePlanDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            ProductTypeId = item.ProductTypeId,
            ProductTypeName = item.ProductTypeName,
            SpeedMbps = item.SpeedMbps,
            MonthlyPrice = item.MonthlyPrice,
            SetupFee = item.SetupFee,
            ContractMonths = item.ContractMonths,
            IsActive = item.IsActive
        };
    }

    public async Task<ServicePlanDto> CreateAsync(Guid companyId, ServicePlanDto dto)
    {
        var item = new ServicePlan
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            ProductTypeId = dto.ProductTypeId,
            ProductTypeName = dto.ProductTypeName,
            SpeedMbps = dto.SpeedMbps,
            MonthlyPrice = dto.MonthlyPrice,
            SetupFee = dto.SetupFee,
            ContractMonths = dto.ContractMonths,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ServicePlan>().Add(item);
        await _context.SaveChangesAsync();

        return new ServicePlanDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            ProductTypeId = item.ProductTypeId,
            ProductTypeName = item.ProductTypeName,
            SpeedMbps = item.SpeedMbps,
            MonthlyPrice = item.MonthlyPrice,
            SetupFee = item.SetupFee,
            ContractMonths = item.ContractMonths,
            IsActive = item.IsActive
        };
    }

    public async Task<ServicePlanDto> UpdateAsync(Guid id, ServicePlanDto dto)
    {
        var item = await _context.Set<ServicePlan>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null)
            throw new Exception("Service plan not found");

        item.Name = dto.Name;
        item.Description = dto.Description;
        item.ProductTypeId = dto.ProductTypeId;
        item.ProductTypeName = dto.ProductTypeName;
        item.SpeedMbps = dto.SpeedMbps;
        item.MonthlyPrice = dto.MonthlyPrice;
        item.SetupFee = dto.SetupFee;
        item.ContractMonths = dto.ContractMonths;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ServicePlanDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            ProductTypeId = item.ProductTypeId,
            ProductTypeName = item.ProductTypeName,
            SpeedMbps = item.SpeedMbps,
            MonthlyPrice = item.MonthlyPrice,
            SetupFee = item.SetupFee,
            ContractMonths = item.ContractMonths,
            IsActive = item.IsActive
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _context.Set<ServicePlan>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null)
            throw new Exception("Service plan not found");

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}

