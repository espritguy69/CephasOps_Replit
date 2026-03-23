using CephasOps.Application.Parser.DTOs;
using CephasOps.Domain.Parser.Entities;
using Microsoft.AspNetCore.Http;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Shared enrichment service for ParsedOrderDraft entities.
/// Centralizes building matching, PDF fallback, date normalization, and validation status logic.
/// Used by both Upload and Email pipelines to ensure feature parity.
/// </summary>
public interface IParsedOrderDraftEnrichmentService
{
    /// <summary>
    /// Enrich draft with building matching, PDF fallback, and date normalization
    /// </summary>
    Task EnrichDraftAsync(
        ParsedOrderDraft draft,
        TimeExcelParseResult parseResult,
        IFormFile sourceFile,
        Guid companyId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Set validation status on draft with correct precedence (validation errors first)
    /// </summary>
    void SetValidationStatus(
        ParsedOrderDraft draft,
        TimeExcelParseResult parseResult,
        string sourceFileName,
        bool autoApprove = false);
}

