using CephasOps.Application.Settings.DTOs;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Document generation service interface
/// </summary>
public interface IDocumentGenerationService
{
    Task<GeneratedDocumentDto> GenerateInvoiceDocumentAsync(Guid invoiceId, Guid companyId, Guid? templateId = null, CancellationToken cancellationToken = default);
    /// <summary>Renders invoice as HTML for preview (no PDF, no storage).</summary>
    Task<string> RenderInvoiceHtmlAsync(Guid invoiceId, Guid companyId, Guid? templateId = null, CancellationToken cancellationToken = default);
    Task<GeneratedDocumentDto> GenerateJobDocketAsync(Guid orderId, Guid companyId, Guid? templateId = null, CancellationToken cancellationToken = default);
    Task<GeneratedDocumentDto> GenerateRmaFormAsync(Guid rmaRequestId, Guid companyId, Guid? templateId = null, CancellationToken cancellationToken = default);
    Task<GeneratedDocumentDto> GenerateDocumentAsync(GenerateDocumentDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<GeneratedDocumentDto>> GetGeneratedDocumentsAsync(Guid companyId, string? referenceEntity = null, Guid? referenceId = null, string? documentType = null, CancellationToken cancellationToken = default);
    Task<GeneratedDocumentDto?> GetGeneratedDocumentByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
}

