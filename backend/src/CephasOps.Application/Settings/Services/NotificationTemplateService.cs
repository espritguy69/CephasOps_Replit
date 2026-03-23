using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Settings.Services;

public interface INotificationTemplateService
{
    Task<List<NotificationTemplateDto>> GetAllAsync(Guid companyId, bool? isActive = null);
    Task<NotificationTemplateDto?> GetByIdAsync(Guid id);
    Task<NotificationTemplateDto> CreateAsync(Guid companyId, NotificationTemplateDto dto);
    Task<NotificationTemplateDto> UpdateAsync(Guid id, NotificationTemplateDto dto);
    Task DeleteAsync(Guid id);
}

public class NotificationTemplateService : INotificationTemplateService
{
    private readonly ApplicationDbContext _context;

    public NotificationTemplateService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<NotificationTemplateDto>> GetAllAsync(Guid companyId, bool? isActive = null)
    {
        var query = _context.Set<NotificationTemplate>()
            .Where(x => x.CompanyId == companyId && !x.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        var items = await query.OrderBy(x => x.Name).ToListAsync();

        return items.Select(x => new NotificationTemplateDto
        {
            Id = x.Id,
            CompanyId = x.CompanyId,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            Category = x.Category,
            Channels = x.Channels,
            TriggerEvent = x.TriggerEvent,
            IsActive = x.IsActive
        }).ToList();
    }

    public async Task<NotificationTemplateDto?> GetByIdAsync(Guid id)
    {
        var item = await _context.Set<NotificationTemplate>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null) return null;

        return new NotificationTemplateDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            Category = item.Category,
            Channels = item.Channels,
            TriggerEvent = item.TriggerEvent,
            IsActive = item.IsActive
        };
    }

    public async Task<NotificationTemplateDto> CreateAsync(Guid companyId, NotificationTemplateDto dto)
    {
        var item = new NotificationTemplate
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            Channels = dto.Channels,
            TriggerEvent = dto.TriggerEvent,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<NotificationTemplate>().Add(item);
        await _context.SaveChangesAsync();

        return new NotificationTemplateDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            Category = item.Category,
            Channels = item.Channels,
            TriggerEvent = item.TriggerEvent,
            IsActive = item.IsActive
        };
    }

    public async Task<NotificationTemplateDto> UpdateAsync(Guid id, NotificationTemplateDto dto)
    {
        var item = await _context.Set<NotificationTemplate>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null)
            throw new Exception("Notification template not found");

        item.Name = dto.Name;
        item.Description = dto.Description;
        item.Category = dto.Category;
        item.Channels = dto.Channels;
        item.TriggerEvent = dto.TriggerEvent;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new NotificationTemplateDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            Category = item.Category,
            Channels = item.Channels,
            TriggerEvent = item.TriggerEvent,
            IsActive = item.IsActive
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _context.Set<NotificationTemplate>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null)
            throw new Exception("Notification template not found");

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}

