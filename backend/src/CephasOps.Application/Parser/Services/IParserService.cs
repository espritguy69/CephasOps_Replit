using CephasOps.Application.Parser.DTOs;
using Microsoft.AspNetCore.Http;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Parser service interface
/// </summary>
public interface IParserService
{
    /// <summary>
    /// Get parse sessions
    /// </summary>
    Task<List<ParseSessionDto>> GetParseSessionsAsync(Guid companyId, string? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get parse session by ID
    /// </summary>
    Task<ParseSessionDto?> GetParseSessionByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get parsed order drafts for a session
    /// </summary>
    Task<List<ParsedOrderDraftDto>> GetParsedOrderDraftsAsync(Guid parseSessionId, Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get parsed order draft by ID
    /// </summary>
    Task<ParsedOrderDraftDto?> GetParsedOrderDraftByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update a parsed order draft (amend data before approval)
    /// </summary>
    Task<ParsedOrderDraftDto> UpdateParsedOrderDraftAsync(Guid id, UpdateParsedOrderDraftDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve a parsed order draft
    /// </summary>
    Task<ParsedOrderDraftDto> ApproveParsedOrderAsync(Guid id, ApproveParsedOrderDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve multiple parsed order drafts in one call. Returns succeeded/failed counts and per-draft errors.
    /// </summary>
    Task<BulkApproveParsedOrdersResultDto> BulkApproveParsedOrdersAsync(IReadOnlyList<Guid> draftIds, Guid companyId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reject a parsed order draft
    /// </summary>
    Task<ParsedOrderDraftDto> RejectParsedOrderAsync(Guid id, RejectParsedOrderDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a parsed order draft as approved after order was created through the full order form.
    /// This is called when the user reviews a draft in the CreateOrderPage and successfully creates an order.
    /// </summary>
    Task<ParsedOrderDraftDto> MarkDraftAsApprovedAsync(Guid id, Guid createdOrderId, Guid companyId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a parse session from uploaded files (PDF, Excel, Outlook MSG)
    /// </summary>
    Task<ParseSessionDto> CreateParseSessionFromFilesAsync(List<IFormFile> files, Guid companyId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get failed parse sessions with error details
    /// </summary>
    Task<List<ParseSessionDto>> GetFailedParseSessionsAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get parsed order drafts with comprehensive filtering and pagination
    /// </summary>
    Task<PagedResultDto<ParsedOrderDraftDto>> GetParsedOrderDraftsWithFiltersAsync(
        Guid companyId,
        string? validationStatus = null,
        string? sourceType = null,
        string? status = null,
        string? serviceId = null,
        string? customerName = null,
        Guid? partnerId = null,
        string? buildingStatus = null,
        decimal? confidenceMin = null,
        bool? buildingMatched = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50,
        string? sortBy = null,
        string? sortOrder = "desc",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get parser statistics (sessions, drafts, success rates)
    /// </summary>
    Task<ParserStatisticsDto> GetParserStatisticsAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get parser analytics for dashboard: parse success rate, auto-match rate, confidence distribution, common errors, orders created per day.
    /// </summary>
    /// <param name="companyId">Company context (multi-tenant SaaS — required).</param>
    /// <param name="fromDate">Period start (UTC). If null, last 30 days from toDate.</param>
    /// <param name="toDate">Period end (UTC). If null, today end.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ParserAnalyticsDto> GetParserAnalyticsAsync(Guid companyId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retry a failed parse session by re-processing the original source
    /// </summary>
    Task<ParseSessionDto> RetryParseSessionAsync(Guid id, Guid companyId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an order already exists for the given Service ID (for duplicate warning before approve).
    /// Uses same normalization as ApproveParsedOrderAsync.
    /// </summary>
    Task<OrderExistsByServiceIdDto> CheckOrderExistsByServiceIdAsync(Guid companyId, string serviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent unmatched parsed material names aggregated by company (review queue visibility).
    /// </summary>
    Task<List<UnmatchedMaterialReviewItemDto>> GetRecentUnmatchedMaterialNamesAsync(Guid companyId, int limit = 50, CancellationToken cancellationToken = default);
}

