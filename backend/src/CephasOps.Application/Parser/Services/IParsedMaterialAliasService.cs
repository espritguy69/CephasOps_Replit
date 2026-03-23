using CephasOps.Application.Parser.DTOs;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Service for parser material aliases: manual resolve creates reusable alias so future drafts auto-match.
/// </summary>
public interface IParsedMaterialAliasService
{
    /// <summary>
    /// Create an alias from parsed name to Material (e.g. after user manually matches "Legacy ONU Plug" → Material X).
    /// Normalizes alias text; validates Material belongs to company. Source set to ParserManualResolve.
    /// </summary>
    Task<ParsedMaterialAliasDto> CreateAliasAsync(Guid companyId, Guid userId, CreateParsedMaterialAliasRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// List aliases for the company (for admin/review).
    /// </summary>
    Task<List<ParsedMaterialAliasDto>> ListAliasesAsync(Guid companyId, CancellationToken cancellationToken = default);
}
