using CephasOps.Application.Parser.DTOs;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Service interface for email template operations.
/// Tenant-aware: pass companyId or rely on TenantScope. Lookup uses tenant template first, then platform (CompanyId null) fallback.
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Get all email templates for the tenant (and platform fallbacks), optionally filtered by direction.
    /// </summary>
    Task<List<EmailTemplateDto>> GetAllAsync(string? direction = null, Guid? companyId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get email template by ID. Returns null if template belongs to another tenant (unless platform template).
    /// </summary>
    Task<EmailTemplateDto?> GetByIdAsync(Guid id, Guid? companyId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get email template by code. Tenant-first, then platform (CompanyId null) fallback.
    /// </summary>
    Task<EmailTemplateDto?> GetByCodeAsync(string code, Guid? companyId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active templates for a specific entity type (tenant + platform fallbacks).
    /// </summary>
    Task<List<EmailTemplateDto>> GetActiveByEntityTypeAsync(string entityType, Guid? companyId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new email template. Company context required (companyId or TenantScope).
    /// </summary>
    Task<EmailTemplateDto> CreateAsync(CreateEmailTemplateDto dto, Guid userId, Guid? companyId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing email template. Tenant can only update own templates; platform can update platform templates.
    /// </summary>
    Task<EmailTemplateDto> UpdateAsync(Guid id, UpdateEmailTemplateDto dto, Guid userId, Guid? companyId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an email template. Tenant can only delete own templates; platform can delete platform templates.
    /// </summary>
    Task DeleteAsync(Guid id, Guid? companyId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Render template with placeholders. Template must belong to tenant or be platform template.
    /// </summary>
    Task<(string Subject, string Body)> RenderTemplateAsync(Guid templateId, Dictionary<string, string> placeholders, Guid? companyId = null, CancellationToken cancellationToken = default);
}

