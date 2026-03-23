using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// SMS template service implementation
/// </summary>
public class SmsTemplateService : ISmsTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SmsTemplateService> _logger;

    public SmsTemplateService(ApplicationDbContext context, ILogger<SmsTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SmsTemplateDto>> GetTemplatesAsync(Guid companyId, string? category = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting SMS templates for company {CompanyId}", companyId);

        var query = _context.SmsTemplates
            .Where(t => t.CompanyId == companyId && !t.IsDeleted);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(t => t.Category == category);
        }

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        var templates = await query
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return templates.Select(MapToDto).ToList();
    }

    public async Task<SmsTemplateDto?> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await _context.SmsTemplates
            .Where(t => t.Id == id && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return template == null ? null : MapToDto(template);
    }

    /// <summary>
    /// Get SMS template by code. Tenant-first: (CompanyId, Code) then platform fallback (CompanyId null, Code).
    /// </summary>
    public async Task<SmsTemplateDto?> GetTemplateByCodeAsync(Guid companyId, string code, CancellationToken cancellationToken = default)
    {
        var template = await _context.SmsTemplates
            .Where(t => t.CompanyId == companyId && t.Code == code && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
        if (template != null)
            return MapToDto(template);
        template = await _context.SmsTemplates
            .Where(t => t.CompanyId == null && t.Code == code && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
        return template == null ? null : MapToDto(template);
    }

    public async Task<SmsTemplateDto> CreateTemplateAsync(Guid companyId, CreateSmsTemplateDto dto, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating SMS template {Code} for company {CompanyId}", dto.Code, companyId);

        // Check for duplicate code
        var existingTemplate = await _context.SmsTemplates
            .Where(t => t.CompanyId == companyId && t.Code == dto.Code && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingTemplate != null)
        {
            throw new InvalidOperationException($"SMS template with code '{dto.Code}' already exists");
        }

        var template = new SmsTemplate
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            MessageText = dto.MessageText,
            CharCount = dto.MessageText.Length,
            IsActive = dto.IsActive,
            Notes = dto.Notes,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SmsTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SMS template {Code} created with ID {TemplateId}", dto.Code, template.Id);

        return MapToDto(template);
    }

    public async Task<SmsTemplateDto?> UpdateTemplateAsync(Guid id, UpdateSmsTemplateDto dto, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var template = await _context.SmsTemplates
            .Where(t => t.Id == id && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (template == null)
        {
            return null;
        }

        _logger.LogInformation("Updating SMS template {TemplateId}", id);

        if (!string.IsNullOrEmpty(dto.Name))
        {
            template.Name = dto.Name;
        }

        if (dto.Description != null)
        {
            template.Description = dto.Description;
        }

        if (!string.IsNullOrEmpty(dto.Category))
        {
            template.Category = dto.Category;
        }

        if (!string.IsNullOrEmpty(dto.MessageText))
        {
            template.MessageText = dto.MessageText;
            template.CharCount = dto.MessageText.Length;
        }

        if (dto.IsActive.HasValue)
        {
            template.IsActive = dto.IsActive.Value;
        }

        if (dto.Notes != null)
        {
            template.Notes = dto.Notes;
        }

        template.UpdatedByUserId = userId;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SMS template {TemplateId} updated", id);

        return MapToDto(template);
    }

    public async Task<bool> DeleteTemplateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await _context.SmsTemplates
            .Where(t => t.Id == id && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (template == null)
        {
            return false;
        }

        _logger.LogInformation("Deleting SMS template {TemplateId}", id);

        template.IsDeleted = true;
        template.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<string> RenderMessageAsync(Guid templateId, Dictionary<string, string> placeholders, CancellationToken cancellationToken = default)
    {
        var template = await _context.SmsTemplates
            .Where(t => t.Id == templateId && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (template == null)
        {
            throw new InvalidOperationException($"SMS template {templateId} not found");
        }

        var message = template.MessageText;

        foreach (var placeholder in placeholders)
        {
            message = message.Replace($"{{{placeholder.Key}}}", placeholder.Value);
        }

        return message;
    }

    private static SmsTemplateDto MapToDto(SmsTemplate template)
    {
        return new SmsTemplateDto
        {
            Id = template.Id,
            CompanyId = template.CompanyId,
            Code = template.Code,
            Name = template.Name,
            Description = template.Description,
            Category = template.Category,
            MessageText = template.MessageText,
            CharCount = template.CharCount,
            IsActive = template.IsActive,
            Notes = template.Notes,
            CreatedByUserId = template.CreatedByUserId,
            UpdatedByUserId = template.UpdatedByUserId,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}

