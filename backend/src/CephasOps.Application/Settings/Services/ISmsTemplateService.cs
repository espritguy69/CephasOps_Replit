using CephasOps.Application.Settings.DTOs;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// SMS template service interface
/// </summary>
public interface ISmsTemplateService
{
    /// <summary>
    /// Get all SMS templates for a company
    /// </summary>
    Task<List<SmsTemplateDto>> GetTemplatesAsync(Guid companyId, string? category = null, bool? isActive = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get SMS template by ID
    /// </summary>
    Task<SmsTemplateDto?> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get SMS template by code
    /// </summary>
    Task<SmsTemplateDto?> GetTemplateByCodeAsync(Guid companyId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create new SMS template
    /// </summary>
    Task<SmsTemplateDto> CreateTemplateAsync(Guid companyId, CreateSmsTemplateDto dto, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update SMS template
    /// </summary>
    Task<SmsTemplateDto?> UpdateTemplateAsync(Guid id, UpdateSmsTemplateDto dto, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete SMS template
    /// </summary>
    Task<bool> DeleteTemplateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Render SMS message with placeholders replaced
    /// </summary>
    Task<string> RenderMessageAsync(Guid templateId, Dictionary<string, string> placeholders, CancellationToken cancellationToken = default);
}

