using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FileEntity = CephasOps.Domain.Files.Entities.File;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Document template service implementation
/// </summary>
public class DocumentTemplateService : IDocumentTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DocumentTemplateService> _logger;

    public DocumentTemplateService(ApplicationDbContext context, ILogger<DocumentTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    /// <summary>
    /// Get file name for a template file ID
    /// </summary>
    private async Task<string?> GetTemplateFileNameAsync(Guid? templateFileId, CancellationToken cancellationToken)
    {
        if (!templateFileId.HasValue)
            return null;
            
        var file = await _context.Set<FileEntity>()
            .Where(f => f.Id == templateFileId.Value)
            .Select(f => f.FileName)
            .FirstOrDefaultAsync(cancellationToken);
            
        return file;
    }

    private static string? SerializeTags(IEnumerable<string>? tags)
    {
        if (tags == null) return null;
        var cleaned = tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct()
            .ToList();
        return cleaned.Count == 0 ? null : string.Join(',', cleaned);
    }

    private static List<string> DeserializeTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return new List<string>();
        }
        return tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct()
            .ToList();
    }

    public async Task<List<DocumentTemplateDto>> GetTemplatesAsync(Guid companyId, string? documentType = null, Guid? partnerId = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting document templates for company {CompanyId}", companyId);

        var query = _context.DocumentTemplates
            .Where(t => t.CompanyId == companyId);

        if (!string.IsNullOrEmpty(documentType))
        {
            query = query.Where(t => t.DocumentType == documentType);
        }

        if (partnerId.HasValue)
        {
            query = query.Where(t => t.PartnerId == partnerId);
        }

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        var templates = await query.OrderBy(t => t.DocumentType).ThenBy(t => t.Name).ToListAsync(cancellationToken);

        // Map to DTOs with file names
        var result = new List<DocumentTemplateDto>();
        foreach (var template in templates)
        {
            var dto = MapToDto(template);
            dto.TemplateFileName = await GetTemplateFileNameAsync(template.TemplateFileId, cancellationToken);
            result.Add(dto);
        }
        return result;
    }

    public async Task<DocumentTemplateDto?> GetTemplateByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting document template {TemplateId} for company {CompanyId}", id, companyId);

        var template = await _context.DocumentTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId, cancellationToken);

        if (template == null) return null;

        var dto = MapToDto(template);
        dto.TemplateFileName = await GetTemplateFileNameAsync(template.TemplateFileId, cancellationToken);
        return dto;
    }

    public async Task<DocumentTemplateDto?> GetEffectiveTemplateAsync(Guid companyId, string documentType, Guid? partnerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting effective document template for company {CompanyId}, type {DocumentType}, partner {PartnerId}", 
            companyId, documentType, partnerId);

        // Try partner-specific first
        var template = await _context.DocumentTemplates
            .Where(t => t.CompanyId == companyId 
                && t.DocumentType == documentType 
                && t.IsActive
                && (partnerId == null || t.PartnerId == partnerId))
            .OrderByDescending(t => t.PartnerId != null ? 1 : 0)
            .FirstOrDefaultAsync(cancellationToken);

        if (template == null) return null;

        var dto = MapToDto(template);
        dto.TemplateFileName = await GetTemplateFileNameAsync(template.TemplateFileId, cancellationToken);
        return dto;
    }

    public async Task<DocumentTemplateDto> CreateTemplateAsync(CreateDocumentTemplateDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating document template for company {CompanyId}", companyId);

        var isActive = dto.IsActive ?? true;

        // Deactivate other templates for the same context if this is being set as active
        if (isActive)
        {
            var existingTemplates = await _context.DocumentTemplates
                .Where(t => t.CompanyId == companyId 
                    && t.DocumentType == dto.DocumentType 
                    && t.PartnerId == dto.PartnerId
                    && t.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingTemplates)
            {
                existing.IsActive = false;
            }
        }

        // Default engine to Handlebars if not specified
        var engine = string.IsNullOrWhiteSpace(dto.Engine) ? "Handlebars" : dto.Engine;
        
        var template = new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = dto.Name,
            DocumentType = dto.DocumentType,
            PartnerId = dto.PartnerId,
            IsActive = isActive,
            Engine = engine,
            HtmlBody = dto.HtmlBody ?? string.Empty,
            JsonSchema = dto.JsonSchema,
            Description = dto.Description,
            Tags = SerializeTags(dto.Tags),
            TemplateFileId = dto.TemplateFileId,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = userId
        };

        _context.DocumentTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = MapToDto(template);
        resultDto.TemplateFileName = await GetTemplateFileNameAsync(template.TemplateFileId, cancellationToken);
        return resultDto;
    }

    public async Task<DocumentTemplateDto> UpdateTemplateAsync(Guid id, UpdateDocumentTemplateDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating document template {TemplateId} for company {CompanyId}", id, companyId);

        var template = await _context.DocumentTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId, cancellationToken);

        if (template == null)
        {
            throw new KeyNotFoundException($"Document template {id} not found");
        }

        if (!string.IsNullOrEmpty(dto.Name))
        {
            template.Name = dto.Name;
        }

        if (dto.IsActive.HasValue)
        {
            template.IsActive = dto.IsActive.Value;
            
            // If activating, deactivate others
            if (dto.IsActive.Value)
            {
                var existingActive = await _context.DocumentTemplates
                    .Where(t => t.CompanyId == companyId 
                        && t.DocumentType == template.DocumentType 
                        && t.PartnerId == template.PartnerId
                        && t.Id != id
                        && t.IsActive)
                    .ToListAsync(cancellationToken);

                foreach (var existing in existingActive)
                {
                    existing.IsActive = false;
                }
            }
        }

        if (!string.IsNullOrEmpty(dto.Engine))
        {
            template.Engine = dto.Engine;
        }

        if (dto.HtmlBody != null)
        {
            template.HtmlBody = dto.HtmlBody;
            template.Version++;
        }

        if (dto.JsonSchema != null)
        {
            template.JsonSchema = dto.JsonSchema;
        }

        if (dto.Description != null)
        {
            template.Description = dto.Description;
        }

        if (dto.Tags != null)
        {
            template.Tags = SerializeTags(dto.Tags);
        }
        
        // Update TemplateFileId (can be set to null to remove file)
        if (dto.TemplateFileId.HasValue || dto.Engine == "CarboneDocx")
        {
            template.TemplateFileId = dto.TemplateFileId;
        }

        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = MapToDto(template);
        resultDto.TemplateFileName = await GetTemplateFileNameAsync(template.TemplateFileId, cancellationToken);
        return resultDto;
    }

    public async Task DeleteTemplateAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting document template {TemplateId} for company {CompanyId}", id, companyId);

        var template = await _context.DocumentTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId, cancellationToken);

        if (template == null)
        {
            throw new KeyNotFoundException($"Document template {id} not found");
        }

        _context.DocumentTemplates.Remove(template);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<DocumentTemplateDto> ActivateTemplateAsync(Guid id, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Activating document template {TemplateId} for company {CompanyId}", id, companyId);

        var template = await _context.DocumentTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId, cancellationToken);

        if (template == null)
        {
            throw new KeyNotFoundException($"Document template {id} not found");
        }

        // Deactivate other templates for the same context
        var existingActive = await _context.DocumentTemplates
            .Where(t => t.CompanyId == companyId 
                && t.DocumentType == template.DocumentType 
                && t.PartnerId == template.PartnerId
                && t.Id != id
                && t.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingActive)
        {
            existing.IsActive = false;
        }

        template.IsActive = true;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = MapToDto(template);
        resultDto.TemplateFileName = await GetTemplateFileNameAsync(template.TemplateFileId, cancellationToken);
        return resultDto;
    }

    public async Task<List<DocumentPlaceholderDefinitionDto>> GetPlaceholderDefinitionsAsync(string documentType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting placeholder definitions for document type {DocumentType}", documentType);

        var definitions = await _context.DocumentPlaceholderDefinitions
            .Where(d => d.DocumentType == documentType)
            .OrderBy(d => d.Key)
            .ToListAsync(cancellationToken);

        return definitions.Select(d => new DocumentPlaceholderDefinitionDto
        {
            Id = d.Id,
            DocumentType = d.DocumentType,
            Key = d.Key,
            Description = d.Description,
            ExampleValue = d.ExampleValue,
            IsRequired = d.IsRequired
        }).ToList();
    }

    private static DocumentTemplateDto MapToDto(DocumentTemplate template)
    {
        return new DocumentTemplateDto
        {
            Id = template.Id,
            CompanyId = template.CompanyId,
            Name = template.Name,
            DocumentType = template.DocumentType,
            PartnerId = template.PartnerId,
            IsActive = template.IsActive,
            Engine = template.Engine,
            HtmlBody = template.HtmlBody,
            JsonSchema = template.JsonSchema,
            Description = template.Description,
            Tags = DeserializeTags(template.Tags),
            Version = template.Version,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            TemplateFileId = template.TemplateFileId
            // Note: TemplateFileName is populated separately after mapping
        };
    }
}

