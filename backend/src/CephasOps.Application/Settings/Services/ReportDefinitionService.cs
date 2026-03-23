using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Settings.Services;

public interface IReportDefinitionService
{
    Task<List<ReportDefinitionDto>> GetAllAsync(Guid companyId, bool? isActive = null);
    Task<ReportDefinitionDto?> GetByIdAsync(Guid id);
    Task<ReportDefinitionDto> CreateAsync(Guid companyId, ReportDefinitionDto dto);
    Task<ReportDefinitionDto> UpdateAsync(Guid id, ReportDefinitionDto dto);
    Task DeleteAsync(Guid id);
}

public class ReportDefinitionService : IReportDefinitionService
{
    private readonly ApplicationDbContext _context;

    public ReportDefinitionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ReportDefinitionDto>> GetAllAsync(Guid companyId, bool? isActive = null)
    {
        var query = _context.Set<ReportDefinition>()
            .Where(x => x.CompanyId == companyId && !x.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        var items = await query.OrderBy(x => x.Name).ToListAsync();

        return items.Select(x => new ReportDefinitionDto
        {
            Id = x.Id,
            CompanyId = x.CompanyId,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            Category = x.Category,
            Format = x.Format,
            Schedule = x.Schedule,
            LastGenerated = x.LastGenerated,
            IsActive = x.IsActive
        }).ToList();
    }

    public async Task<ReportDefinitionDto?> GetByIdAsync(Guid id)
    {
        var item = await _context.Set<ReportDefinition>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null) return null;

        return new ReportDefinitionDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            Category = item.Category,
            Format = item.Format,
            Schedule = item.Schedule,
            LastGenerated = item.LastGenerated,
            IsActive = item.IsActive
        };
    }

    public async Task<ReportDefinitionDto> CreateAsync(Guid companyId, ReportDefinitionDto dto)
    {
        var item = new ReportDefinition
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            Format = dto.Format,
            Schedule = dto.Schedule,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ReportDefinition>().Add(item);
        await _context.SaveChangesAsync();

        return new ReportDefinitionDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            Category = item.Category,
            Format = item.Format,
            Schedule = item.Schedule,
            LastGenerated = item.LastGenerated,
            IsActive = item.IsActive
        };
    }

    public async Task<ReportDefinitionDto> UpdateAsync(Guid id, ReportDefinitionDto dto)
    {
        var item = await _context.Set<ReportDefinition>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null)
            throw new Exception("Report definition not found");

        item.Name = dto.Name;
        item.Description = dto.Description;
        item.Category = dto.Category;
        item.Format = dto.Format;
        item.Schedule = dto.Schedule;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ReportDefinitionDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            Category = item.Category,
            Format = item.Format,
            Schedule = item.Schedule,
            LastGenerated = item.LastGenerated,
            IsActive = item.IsActive
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _context.Set<ReportDefinition>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null)
            throw new Exception("Report definition not found");

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}

