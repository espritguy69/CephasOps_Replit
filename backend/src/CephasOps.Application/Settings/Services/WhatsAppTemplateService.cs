using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// WhatsApp template service implementation
/// </summary>
public class WhatsAppTemplateService : IWhatsAppTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WhatsAppTemplateService> _logger;

    public WhatsAppTemplateService(ApplicationDbContext context, ILogger<WhatsAppTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<WhatsAppTemplateDto>> GetTemplatesAsync(Guid companyId, string? category = null, string? approvalStatus = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting WhatsApp templates for company {CompanyId}", companyId);

        var query = _context.WhatsAppTemplates
            .Where(t => t.CompanyId == companyId && !t.IsDeleted);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(t => t.Category == category);
        }

        if (!string.IsNullOrEmpty(approvalStatus))
        {
            query = query.Where(t => t.ApprovalStatus == approvalStatus);
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

    public async Task<WhatsAppTemplateDto?> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await _context.WhatsAppTemplates
            .Where(t => t.Id == id && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return template == null ? null : MapToDto(template);
    }

    public async Task<WhatsAppTemplateDto?> GetTemplateByCodeAsync(Guid companyId, string code, CancellationToken cancellationToken = default)
    {
        var template = await _context.WhatsAppTemplates
            .Where(t => t.CompanyId == companyId && t.Code == code && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return template == null ? null : MapToDto(template);
    }

    public async Task<WhatsAppTemplateDto> CreateTemplateAsync(Guid companyId, CreateWhatsAppTemplateDto dto, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating WhatsApp template {Code} for company {CompanyId}", dto.Code, companyId);

        // Check for duplicate code
        var existingTemplate = await _context.WhatsAppTemplates
            .Where(t => t.CompanyId == companyId && t.Code == dto.Code && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingTemplate != null)
        {
            throw new InvalidOperationException($"WhatsApp template with code '{dto.Code}' already exists");
        }

        var template = new WhatsAppTemplate
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            TemplateId = dto.TemplateId,
            ApprovalStatus = dto.ApprovalStatus,
            MessageBody = dto.MessageBody,
            Language = dto.Language ?? "en",
            IsActive = dto.IsActive,
            Notes = dto.Notes,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Set submitted date if status is Pending or higher
        if (dto.ApprovalStatus == "Pending" || dto.ApprovalStatus == "Approved")
        {
            template.SubmittedAt = DateTime.UtcNow;
        }

        // Set approved date if status is Approved
        if (dto.ApprovalStatus == "Approved")
        {
            template.ApprovedAt = DateTime.UtcNow;
        }

        _context.WhatsAppTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("WhatsApp template {Code} created with ID {TemplateId}", dto.Code, template.Id);

        return MapToDto(template);
    }

    public async Task<WhatsAppTemplateDto?> UpdateTemplateAsync(Guid id, UpdateWhatsAppTemplateDto dto, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var template = await _context.WhatsAppTemplates
            .Where(t => t.Id == id && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (template == null)
        {
            return null;
        }

        _logger.LogInformation("Updating WhatsApp template {TemplateId}", id);

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

        if (dto.TemplateId != null)
        {
            template.TemplateId = dto.TemplateId;
        }

        if (!string.IsNullOrEmpty(dto.ApprovalStatus))
        {
            var oldStatus = template.ApprovalStatus;
            template.ApprovalStatus = dto.ApprovalStatus;

            // Update timestamps based on status changes
            if (oldStatus != "Pending" && dto.ApprovalStatus == "Pending")
            {
                template.SubmittedAt = DateTime.UtcNow;
            }

            if (dto.ApprovalStatus == "Approved" && template.ApprovedAt == null)
            {
                template.ApprovedAt = DateTime.UtcNow;
            }
        }

        if (dto.MessageBody != null)
        {
            template.MessageBody = dto.MessageBody;
        }

        if (dto.Language != null)
        {
            template.Language = dto.Language;
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

        _logger.LogInformation("WhatsApp template {TemplateId} updated", id);

        return MapToDto(template);
    }

    public async Task<bool> DeleteTemplateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await _context.WhatsAppTemplates
            .Where(t => t.Id == id && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (template == null)
        {
            return false;
        }

        _logger.LogInformation("Deleting WhatsApp template {TemplateId}", id);

        template.IsDeleted = true;
        template.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<WhatsAppTemplateDto?> UpdateApprovalStatusAsync(Guid id, string approvalStatus, CancellationToken cancellationToken = default)
    {
        var template = await _context.WhatsAppTemplates
            .Where(t => t.Id == id && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (template == null)
        {
            return null;
        }

        _logger.LogInformation("Updating WhatsApp template {TemplateId} approval status to {ApprovalStatus}", id, approvalStatus);

        var oldStatus = template.ApprovalStatus;
        template.ApprovalStatus = approvalStatus;

        // Update timestamps
        if (oldStatus != "Pending" && approvalStatus == "Pending")
        {
            template.SubmittedAt = DateTime.UtcNow;
        }

        if (approvalStatus == "Approved" && template.ApprovedAt == null)
        {
            template.ApprovedAt = DateTime.UtcNow;
        }

        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(template);
    }

    private static WhatsAppTemplateDto MapToDto(WhatsAppTemplate template)
    {
        return new WhatsAppTemplateDto
        {
            Id = template.Id,
            CompanyId = template.CompanyId,
            Code = template.Code,
            Name = template.Name,
            Description = template.Description,
            Category = template.Category,
            TemplateId = template.TemplateId,
            ApprovalStatus = template.ApprovalStatus,
            MessageBody = template.MessageBody,
            Language = template.Language,
            IsActive = template.IsActive,
            Notes = template.Notes,
            SubmittedAt = template.SubmittedAt,
            ApprovedAt = template.ApprovedAt,
            CreatedByUserId = template.CreatedByUserId,
            UpdatedByUserId = template.UpdatedByUserId,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}

