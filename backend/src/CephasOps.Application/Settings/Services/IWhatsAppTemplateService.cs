using CephasOps.Application.Settings.DTOs;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// WhatsApp template service interface
/// </summary>
public interface IWhatsAppTemplateService
{
    /// <summary>
    /// Get all WhatsApp templates for a company
    /// </summary>
    Task<List<WhatsAppTemplateDto>> GetTemplatesAsync(Guid companyId, string? category = null, string? approvalStatus = null, bool? isActive = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get WhatsApp template by ID
    /// </summary>
    Task<WhatsAppTemplateDto?> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get WhatsApp template by code
    /// </summary>
    Task<WhatsAppTemplateDto?> GetTemplateByCodeAsync(Guid companyId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create new WhatsApp template
    /// </summary>
    Task<WhatsAppTemplateDto> CreateTemplateAsync(Guid companyId, CreateWhatsAppTemplateDto dto, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update WhatsApp template
    /// </summary>
    Task<WhatsAppTemplateDto?> UpdateTemplateAsync(Guid id, UpdateWhatsAppTemplateDto dto, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete WhatsApp template
    /// </summary>
    Task<bool> DeleteTemplateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update approval status
    /// </summary>
    Task<WhatsAppTemplateDto?> UpdateApprovalStatusAsync(Guid id, string approvalStatus, CancellationToken cancellationToken = default);
}

