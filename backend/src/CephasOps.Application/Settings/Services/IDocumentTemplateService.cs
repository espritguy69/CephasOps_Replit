using CephasOps.Application.Settings.DTOs;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Document template service interface
/// </summary>
public interface IDocumentTemplateService
{
    Task<List<DocumentTemplateDto>> GetTemplatesAsync(Guid companyId, string? documentType = null, Guid? partnerId = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<DocumentTemplateDto?> GetTemplateByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<DocumentTemplateDto?> GetEffectiveTemplateAsync(Guid companyId, string documentType, Guid? partnerId, CancellationToken cancellationToken = default);
    Task<DocumentTemplateDto> CreateTemplateAsync(CreateDocumentTemplateDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<DocumentTemplateDto> UpdateTemplateAsync(Guid id, UpdateDocumentTemplateDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteTemplateAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<DocumentTemplateDto> ActivateTemplateAsync(Guid id, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<DocumentPlaceholderDefinitionDto>> GetPlaceholderDefinitionsAsync(string documentType, CancellationToken cancellationToken = default);
}

