using CephasOps.Application.Parser;
using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
using System.Text;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Parser endpoints
/// </summary>
[ApiController]
[Route("api/parser")]
[Authorize]
public class ParserController : ControllerBase
{
    private readonly IParserService _parserService;
    private readonly IParsedMaterialAliasService _parsedMaterialAliasService;
    private readonly IEmailIngestionService _emailIngestionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ParserController> _logger;
    private readonly ICsvService _csvService;

    // Allowed file extensions for order import
    private static readonly string[] AllowedExtensions = { ".pdf", ".xls", ".xlsx", ".msg" };
    private static readonly long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public ParserController(
        IParserService parserService,
        IParsedMaterialAliasService parsedMaterialAliasService,
        IEmailIngestionService emailIngestionService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<ParserController> logger,
        ICsvService csvService)
    {
        _parserService = parserService;
        _parsedMaterialAliasService = parsedMaterialAliasService;
        _emailIngestionService = emailIngestionService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
        _csvService = csvService;
    }

    /// <summary>
    /// Upload files for order parsing
    /// </summary>
    /// <param name="files">Files to parse (PDF, Excel, Outlook MSG)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parse session with extracted order drafts</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ParseSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB total limit
    public async Task<ActionResult<ParseSessionDto>> UploadFilesForParsing(
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if (companyId == null || userId == null)
        {
            return Unauthorized("Company and user context required");
        }

        if (files == null || files.Count == 0)
        {
            return BadRequest("No files provided");
        }

        // Validate files
        var errors = new List<string>();
        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                errors.Add($"File '{file.FileName}' has unsupported format. Allowed: PDF, Excel (.xls, .xlsx), Outlook (.msg)");
            }
            if (file.Length > MaxFileSize)
            {
                errors.Add($"File '{file.FileName}' exceeds maximum size of 10MB");
            }
        }

        if (errors.Count > 0)
        {
            return BadRequest(new { errors });
        }

        try
        {
            // Create a parse session for the uploaded files
            var session = await _parserService.CreateParseSessionFromFilesAsync(
                files, 
                companyId.Value, 
                userId.Value, 
                cancellationToken);

            _logger.LogInformation(
                "Files uploaded for parsing: {FileCount} files, Session: {SessionId}, User: {UserId}",
                files.Count, session.Id, userId);

            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing uploaded files for parsing");
            return StatusCode(500, new { error = "Failed to process uploaded files", message = ex.Message });
        }
    }

    /// <summary>
    /// Get parse sessions
    /// </summary>
    /// <param name="status">Filter by status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of parse sessions</returns>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiResponse<List<ParseSessionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ParseSessionDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<ParseSessionDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<ParseSessionDto>>>> GetParseSessions(
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return this.Unauthorized<List<ParseSessionDto>>("Company context required");
        }

        try
        {
            var sessions = await _parserService.GetParseSessionsAsync(companyId.Value, status, cancellationToken);
            return this.Success(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parse sessions");
            return this.InternalServerError<List<ParseSessionDto>>($"Failed to get parse sessions: {ex.Message}");
        }
    }

    /// <summary>
    /// Get parse session by ID
    /// </summary>
    /// <param name="id">Parse session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parse session details</returns>
    [HttpGet("sessions/{id}")]
    [ProducesResponseType(typeof(ApiResponse<ParseSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParseSessionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ParseSessionDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ParseSessionDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ParseSessionDto>>> GetParseSession(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return this.Unauthorized<ParseSessionDto>("Company context required");
        }

        try
        {
            var session = await _parserService.GetParseSessionByIdAsync(id, companyId.Value, cancellationToken);
            if (session == null)
            {
                return this.NotFound<ParseSessionDto>($"Parse session with ID {id} not found");
            }

            return this.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parse session: {SessionId}", id);
            return this.InternalServerError<ParseSessionDto>($"Failed to get parse session: {ex.Message}");
        }
    }

    /// <summary>
    /// Retry a failed parse session
    /// </summary>
    /// <param name="id">Parse session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New parse session created for retry</returns>
    [HttpPost("sessions/{id}/retry")]
    [ProducesResponseType(typeof(ApiResponse<ParseSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParseSessionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ParseSessionDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ParseSessionDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ParseSessionDto>>> RetryParseSession(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if (companyId == null || userId == null)
        {
            return this.Unauthorized<ParseSessionDto>("Company and user context required");
        }

        try
        {
            var session = await _parserService.RetryParseSessionAsync(id, companyId.Value, userId.Value, cancellationToken);
            return this.Success(session, "Parse session retry initiated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<ParseSessionDto>($"Parse session with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying parse session: {SessionId}", id);
            return this.InternalServerError<ParseSessionDto>($"Failed to retry parse session: {ex.Message}");
        }
    }

    /// <summary>
    /// Get parsed order drafts for a session
    /// </summary>
    /// <param name="sessionId">Parse session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of parsed order drafts</returns>
    [HttpGet("sessions/{sessionId}/drafts")]
    [ProducesResponseType(typeof(ApiResponse<List<ParsedOrderDraftDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ParsedOrderDraftDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<ParsedOrderDraftDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<ParsedOrderDraftDto>>>> GetParsedOrderDrafts(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return this.Unauthorized<List<ParsedOrderDraftDto>>("Company context required");
        }

        try
        {
            var drafts = await _parserService.GetParsedOrderDraftsAsync(sessionId, companyId.Value, cancellationToken);
            return this.Success(drafts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parsed order drafts");
            return this.InternalServerError<List<ParsedOrderDraftDto>>($"Failed to get parsed order drafts: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if an order already exists for the given Service ID (for duplicate warning before approve).
    /// </summary>
    /// <param name="serviceId">Service ID to check (e.g. TBBN number)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exists flag and existing order id/ticket if found</returns>
    [HttpGet("drafts/check-duplicate")]
    [ProducesResponseType(typeof(ApiResponse<OrderExistsByServiceIdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderExistsByServiceIdDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<OrderExistsByServiceIdDto>>> CheckOrderExistsByServiceId(
        [FromQuery] string? serviceId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return this.Unauthorized<OrderExistsByServiceIdDto>("Company context required");
        }

        var result = await _parserService.CheckOrderExistsByServiceIdAsync(companyId.Value, serviceId ?? string.Empty, cancellationToken);
        return this.Success(result);
    }

    /// <summary>
    /// Get parsed order draft by ID
    /// </summary>
    /// <param name="id">Parsed order draft ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parsed order draft details</returns>
    [HttpGet("drafts/{id}")]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ParsedOrderDraftDto>>> GetParsedOrderDraft(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return this.Unauthorized<ParsedOrderDraftDto>("Company context required");
        }

        try
        {
            var draft = await _parserService.GetParsedOrderDraftByIdAsync(id, companyId.Value, cancellationToken);
            if (draft == null)
            {
                return this.NotFound<ParsedOrderDraftDto>($"Parsed order draft with ID {id} not found");
            }

            return this.Success(draft);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parsed order draft: {DraftId}", id);
            return this.InternalServerError<ParsedOrderDraftDto>($"Failed to get parsed order draft: {ex.Message}");
        }
    }

    /// <summary>
    /// Update a parsed order draft (amend data before approval)
    /// </summary>
    /// <param name="id">Parsed order draft ID</param>
    /// <param name="dto">Updated draft data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated parsed order draft</returns>
    [HttpPut("drafts/{id}")]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ParsedOrderDraftDto>>> UpdateParsedOrderDraft(
        Guid id,
        [FromBody] UpdateParsedOrderDraftDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if (companyId == null || userId == null)
        {
            return this.Unauthorized<ParsedOrderDraftDto>("Company and user context required");
        }

        try
        {
            var draft = await _parserService.UpdateParsedOrderDraftAsync(id, dto, companyId.Value, userId.Value, cancellationToken);
            return this.Success(draft, "Parsed order draft updated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<ParsedOrderDraftDto>($"Parsed order draft with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating parsed order draft: {DraftId}", id);
            return this.InternalServerError<ParsedOrderDraftDto>($"Failed to update parsed order draft: {ex.Message}");
        }
    }

    /// <summary>
    /// Approve a parsed order draft
    /// </summary>
    /// <param name="id">Parsed order draft ID</param>
    /// <param name="dto">Approval data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated parsed order draft</returns>
    [HttpPost("drafts/{id}/approve")]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ParsedOrderDraftDto>>> ApproveParsedOrder(
        Guid id,
        [FromBody] ApproveParsedOrderDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if (companyId == null || userId == null)
        {
            return this.Unauthorized<ParsedOrderDraftDto>("Company and user context required");
        }

        try
        {
            var draft = await _parserService.ApproveParsedOrderAsync(id, dto, companyId.Value, userId.Value, cancellationToken);
            return this.Success(draft, "Parsed order approved successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<ParsedOrderDraftDto>($"Parsed order draft with ID {id} not found");
        }
        catch (BuildingRequiredException ex)
        {
            return BadRequest(new { errorCode = ex.ErrorCode, message = ex.Message, buildingDetection = ex.BuildingDetection });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving parsed order: {DraftId}", id);
            return this.InternalServerError<ParsedOrderDraftDto>($"Failed to approve parsed order: {ex.Message}");
        }
    }

    /// <summary>
    /// Bulk approve multiple parsed order drafts in one call
    /// </summary>
    /// <param name="request">Draft IDs to approve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with succeeded/failed counts and per-draft errors</returns>
    [HttpPost("drafts/bulk-approve")]
    [ProducesResponseType(typeof(ApiResponse<BulkApproveParsedOrdersResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BulkApproveParsedOrdersResultDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<BulkApproveParsedOrdersResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BulkApproveParsedOrdersResultDto>>> BulkApproveParsedOrders(
        [FromBody] BulkApproveParsedOrdersRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if (companyId == null || userId == null)
        {
            return this.Unauthorized<BulkApproveParsedOrdersResultDto>("Company and user context required");
        }

        if (request?.DraftIds == null || request.DraftIds.Count == 0)
        {
            return this.BadRequest<BulkApproveParsedOrdersResultDto>("At least one draft ID is required");
        }

        try
        {
            var result = await _parserService.BulkApproveParsedOrdersAsync(request.DraftIds, companyId.Value, userId.Value, cancellationToken);
            return this.Success(result, $"Bulk approve: {result.SucceededCount} succeeded, {result.FailedCount} failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk approving parsed orders");
            return this.InternalServerError<BulkApproveParsedOrdersResultDto>($"Failed to bulk approve: {ex.Message}");
        }
    }

    /// <summary>
    /// Reject a parsed order draft
    /// </summary>
    /// <param name="id">Parsed order draft ID</param>
    /// <param name="dto">Rejection data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated parsed order draft</returns>
    [HttpPost("drafts/{id}/reject")]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ParsedOrderDraftDto>>> RejectParsedOrder(
        Guid id,
        [FromBody] RejectParsedOrderDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if (companyId == null || userId == null)
        {
            return this.Unauthorized<ParsedOrderDraftDto>("Company and user context required");
        }

        try
        {
            var draft = await _parserService.RejectParsedOrderAsync(id, dto, companyId.Value, userId.Value, cancellationToken);
            return this.Success(draft, "Parsed order rejected successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<ParsedOrderDraftDto>($"Parsed order draft with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting parsed order: {DraftId}", id);
            return this.InternalServerError<ParsedOrderDraftDto>($"Failed to reject parsed order: {ex.Message}");
        }
    }

    /// <summary>
    /// Mark a parsed order draft as approved after order was created through the full order form.
    /// This is called when the user reviews a draft in the CreateOrderPage and successfully creates an order.
    /// </summary>
    /// <param name="id">Parsed order draft ID</param>
    /// <param name="dto">Mark approved data containing the created order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated parsed order draft</returns>
    [HttpPost("drafts/{id}/mark-approved")]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ParsedOrderDraftDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ParsedOrderDraftDto>>> MarkDraftAsApproved(
        Guid id,
        [FromBody] MarkDraftApprovedDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if (companyId == null || userId == null)
        {
            return this.Unauthorized<ParsedOrderDraftDto>("Company and user context required");
        }

        try
        {
            var draft = await _parserService.MarkDraftAsApprovedAsync(id, dto.CreatedOrderId, companyId.Value, userId.Value, cancellationToken);
            return this.Success(draft, "Parsed order draft marked as approved successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<ParsedOrderDraftDto>($"Parsed order draft with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking parsed order draft as approved: {DraftId}", id);
            return this.InternalServerError<ParsedOrderDraftDto>($"Failed to mark parsed order draft as approved: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a parsed material alias (manual resolve: parsed name → Material). Future drafts will auto-resolve this name.
    /// </summary>
    [HttpPost("material-aliases")]
    [ProducesResponseType(typeof(ApiResponse<ParsedMaterialAliasDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ParsedMaterialAliasDto>>> CreateParsedMaterialAlias(
        [FromBody] CreateParsedMaterialAliasRequest request,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if (companyId == null || userId == null)
            return this.Unauthorized<ParsedMaterialAliasDto>("Company and user context required");

        try
        {
            var alias = await _parsedMaterialAliasService.CreateAliasAsync(companyId.Value, userId.Value, request, cancellationToken);
            return this.Success(alias, "Alias created. Future drafts will resolve this name automatically.");
        }
        catch (ArgumentException ex)
        {
            return this.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating parsed material alias");
            return this.InternalServerError<ParsedMaterialAliasDto>(ex.Message);
        }
    }

    /// <summary>
    /// List parsed material aliases for the company (admin/review).
    /// </summary>
    [HttpGet("material-aliases")]
    [ProducesResponseType(typeof(ApiResponse<List<ParsedMaterialAliasDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<ParsedMaterialAliasDto>>>> ListParsedMaterialAliases(
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
            return this.Unauthorized<List<ParsedMaterialAliasDto>>("Company context required");

        try
        {
            var list = await _parsedMaterialAliasService.ListAliasesAsync(companyId.Value, cancellationToken);
            return this.Success(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing parsed material aliases");
            return this.InternalServerError<List<ParsedMaterialAliasDto>>(ex.Message);
        }
    }

    /// <summary>
    /// Get recent unmatched parsed material names aggregated (review queue visibility).
    /// </summary>
    [HttpGet("unmatched-review")]
    [ProducesResponseType(typeof(ApiResponse<List<UnmatchedMaterialReviewItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<UnmatchedMaterialReviewItemDto>>>> GetUnmatchedMaterialReview(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
            return this.Unauthorized<List<UnmatchedMaterialReviewItemDto>>("Company context required");

        try
        {
            var list = await _parserService.GetRecentUnmatchedMaterialNamesAsync(companyId.Value, Math.Clamp(limit, 1, 100), cancellationToken);
            return this.Success(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unmatched material review");
            return this.InternalServerError<List<UnmatchedMaterialReviewItemDto>>(ex.Message);
        }
    }

    /// <summary>
    /// Test email parsing for a specific email account and return detailed diagnostic information
    /// </summary>
    /// <param name="emailAccountId">Email account ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed diagnostic report with errors and parsing results</returns>
    [HttpPost("email-accounts/{emailAccountId}/test-parse")]
    [ProducesResponseType(typeof(ApiResponse<EmailParsingDiagnosticDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmailParsingDiagnosticDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<EmailParsingDiagnosticDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<EmailParsingDiagnosticDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmailParsingDiagnosticDto>>> TestEmailParsing(
        Guid emailAccountId,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            _logger.LogInformation("Email parsing diagnostic test triggered for account {AccountId} by user {UserId}", 
                emailAccountId, _currentUserService.UserId);

            var diagnostic = new EmailParsingDiagnosticDto
            {
                EmailAccountId = emailAccountId,
                TestStartedAt = DateTime.UtcNow
            };

            // Trigger email ingestion and capture detailed results
            var result = await _emailIngestionService.TriggerPollAsync(emailAccountId, companyId, cancellationToken);
            
            diagnostic.EmailAccountName = result.EmailAccountName;
            diagnostic.EmailsFetched = result.EmailsFetched;
            diagnostic.ParseSessionsCreated = result.ParseSessionsCreated;
            diagnostic.DraftsCreated = result.DraftsCreated;
            diagnostic.Errors = result.Errors;
            diagnostic.Success = result.Success;
            diagnostic.ErrorMessage = result.ErrorMessage;
            diagnostic.ProcessedEmails = result.ProcessedEmails;
            diagnostic.TestCompletedAt = DateTime.UtcNow;

            // Get failed parse sessions for this account
            var failedSessions = await _parserService.GetFailedParseSessionsAsync(companyId, cancellationToken);
            diagnostic.FailedSessions = failedSessions.Select(s => new FailedSessionInfo
            {
                SessionId = s.Id,
                Status = s.Status ?? "Unknown",
                ErrorMessage = s.ErrorMessage,
                SourceDescription = s.SourceDescription,
                CreatedAt = s.CreatedAt,
                ParsedOrdersCount = s.ParsedOrdersCount
            }).ToList();

            return this.Success(diagnostic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running email parsing diagnostic for account {EmailAccountId}", emailAccountId);
            return this.InternalServerError<EmailParsingDiagnosticDto>($"Failed to run diagnostic: {ex.Message}");
        }
    }

    /// <summary>
    /// Get parser statistics (sessions, drafts, success rates)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parser statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<ParserStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParserStatisticsDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ParserStatisticsDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ParserStatisticsDto>>> GetParserStatistics(
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var statistics = await _parserService.GetParserStatisticsAsync(companyId, cancellationToken);
            return this.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parser statistics");
            return this.InternalServerError<ParserStatisticsDto>($"Failed to get parser statistics: {ex.Message}");
        }
    }

    /// <summary>
    /// Get parser analytics for dashboard: parse success rate, auto-match rate, confidence distribution, common errors, orders created per day.
    /// </summary>
    /// <param name="fromDate">Period start (UTC date, optional). Default: 30 days before toDate.</param>
    /// <param name="toDate">Period end (UTC date, optional). Default: today.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parser analytics</returns>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(ApiResponse<ParserAnalyticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParserAnalyticsDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ParserAnalyticsDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ParserAnalyticsDto>>> GetParserAnalytics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return this.Unauthorized<ParserAnalyticsDto>("Company context required");
        }

        try
        {
            var analytics = await _parserService.GetParserAnalyticsAsync(companyId.Value, fromDate, toDate, cancellationToken);
            return this.Success(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parser analytics");
            return this.InternalServerError<ParserAnalyticsDto>($"Failed to get parser analytics: {ex.Message}");
        }
    }

    /// <summary>
    /// Get parsed order drafts with comprehensive filtering
    /// </summary>
    /// <param name="validationStatus">Filter by validation status (Pending, Valid, NeedsReview, Rejected)</param>
    /// <param name="sourceType">Filter by source type (Email, FileUpload)</param>
    /// <param name="status">Filter by session status</param>
    /// <param name="serviceId">Search by service ID</param>
    /// <param name="customerName">Search by customer name</param>
    /// <param name="partnerId">Filter by partner ID</param>
    /// <param name="buildingStatus">Filter by building status (Existing, New)</param>
    /// <param name="confidenceMin">Minimum confidence score (0-1, e.g. 0.8 for 80%)</param>
    /// <param name="buildingMatched">When true only drafts with building matched; when false only without</param>
    /// <param name="fromDate">Filter from date</param>
    /// <param name="toDate">Filter to date</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortOrder">Sort order (asc, desc)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of parsed order drafts</returns>
    [HttpGet("drafts")]
    [ProducesResponseType(typeof(ApiResponse<PagedResultDto<ParsedOrderDraftDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResultDto<ParsedOrderDraftDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<PagedResultDto<ParsedOrderDraftDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PagedResultDto<ParsedOrderDraftDto>>>> GetParsedOrderDraftsWithFilters(
        [FromQuery] string? validationStatus = null,
        [FromQuery] string? sourceType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? serviceId = null,
        [FromQuery] string? customerName = null,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] string? buildingStatus = null,
        [FromQuery] decimal? confidenceMin = null,
        [FromQuery] bool? buildingMatched = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = "desc",
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return this.Unauthorized<PagedResultDto<ParsedOrderDraftDto>>("Company context required");
        }

        try
        {
            var result = await _parserService.GetParsedOrderDraftsWithFiltersAsync(
                companyId.Value,
                validationStatus,
                sourceType,
                status,
                serviceId,
                customerName,
                partnerId,
                buildingStatus,
                confidenceMin,
                buildingMatched,
                fromDate,
                toDate,
                page,
                pageSize,
                sortBy,
                sortOrder,
                cancellationToken);

            return this.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parsed order drafts with filters");
            return this.InternalServerError<PagedResultDto<ParsedOrderDraftDto>>($"Failed to get parsed order drafts: {ex.Message}");
        }
    }

    /// <summary>
    /// Export parser logs (sessions and drafts) to CSV
    /// </summary>
    /// <param name="format">Export format: csv or json (default: csv)</param>
    /// <param name="fromDate">Filter from date</param>
    /// <param name="toDate">Filter to date</param>
    /// <param name="status">Filter by session status</param>
    /// <param name="validationStatus">Filter by draft validation status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>CSV or JSON file download</returns>
    [HttpGet("logs/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportParserLogs(
        [FromQuery] string format = "csv",
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? status = null,
        [FromQuery] string? validationStatus = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            // Get parse sessions
            var sessions = await _parserService.GetParseSessionsAsync(companyId, status, cancellationToken);
            
            // Filter by date range if provided
            if (fromDate.HasValue)
            {
                sessions = sessions.Where(s => s.CreatedAt >= fromDate.Value).ToList();
            }
            if (toDate.HasValue)
            {
                sessions = sessions.Where(s => s.CreatedAt <= toDate.Value.AddDays(1)).ToList();
            }

            // Get all drafts for these sessions
            var allDrafts = new List<ParsedOrderDraftDto>();
            foreach (var session in sessions)
            {
                try
                {
                    var drafts = await _parserService.GetParsedOrderDraftsAsync(session.Id, companyId, cancellationToken);
                    if (validationStatus != null)
                    {
                        drafts = drafts.Where(d => d.ValidationStatus == validationStatus).ToList();
                    }
                    allDrafts.AddRange(drafts);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get drafts for session {SessionId}", session.Id);
                }
            }

            // Prepare export data
            var exportData = sessions.Select(s => new
            {
                SessionId = s.Id,
                SessionStatus = s.Status,
                SourceType = s.SourceType ?? "Unknown",
                SourceDescription = s.SourceDescription ?? string.Empty,
                ParsedOrdersCount = s.ParsedOrdersCount,
                ErrorMessage = s.ErrorMessage ?? string.Empty,
                CreatedAt = s.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                CompletedAt = s.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty,
                DraftsCount = allDrafts.Count(d => d.ParseSessionId == s.Id),
                ValidDrafts = allDrafts.Count(d => d.ParseSessionId == s.Id && d.ValidationStatus == "Valid"),
                NeedsReviewDrafts = allDrafts.Count(d => d.ParseSessionId == s.Id && d.ValidationStatus == "NeedsReview"),
                RejectedDrafts = allDrafts.Count(d => d.ParseSessionId == s.Id && d.ValidationStatus == "Rejected")
            }).ToList();

            if (format.ToLowerInvariant() == "json")
            {
                var jsonData = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                var jsonBytes = Encoding.UTF8.GetBytes(jsonData);
                return File(jsonBytes, "application/json", $"parser-logs-{DateTime.UtcNow:yyyy-MM-dd}.json");
            }
            else
            {
                // CSV format
                var csvBytes = _csvService.ExportToCsvBytes(exportData);
                return File(csvBytes, "text/csv", $"parser-logs-{DateTime.UtcNow:yyyy-MM-dd}.csv");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting parser logs");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to export parser logs: {ex.Message}"));
        }
    }
}

/// <summary>
/// DTO for marking a draft as approved after order creation
/// </summary>
public class MarkDraftApprovedDto
{
    /// <summary>
    /// The ID of the order that was created from this draft
    /// </summary>
    public Guid CreatedOrderId { get; set; }
}
