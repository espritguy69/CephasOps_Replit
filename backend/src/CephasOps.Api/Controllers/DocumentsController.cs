using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Workflow.JobOrchestration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Document generation endpoints
/// </summary>
[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly IDocumentGenerationJobEnqueuer? _documentGenerationJobEnqueuer;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentGenerationService documentGenerationService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<DocumentsController> logger,
        IDocumentGenerationJobEnqueuer? documentGenerationJobEnqueuer = null)
    {
        _documentGenerationService = documentGenerationService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
        _documentGenerationJobEnqueuer = documentGenerationJobEnqueuer;
    }

    /// <summary>
    /// Enqueue document generation to run asynchronously (JobExecution). Returns immediately with 202 Accepted.
    /// </summary>
    [HttpPost("generate-async")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> GenerateDocumentAsync(
        [FromBody] GenerateDocumentAsyncRequest request,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if (companyId == null)
            return this.Unauthorized<object>("Company context required");
        if (_documentGenerationJobEnqueuer == null)
            return this.BadRequest<object>("Async document generation is not configured.");
        if (request?.DocumentType == null || request.EntityId == Guid.Empty)
            return ValidationError<object>("DocumentType and EntityId are required.");
        try
        {
            await _documentGenerationJobEnqueuer.EnqueueAsync(
                request.DocumentType,
                request.EntityId,
                companyId.Value,
                userId,
                request.ReferenceEntity,
                request.TemplateId,
                request.Format,
                request.DataJson,
                request.ReplaceExisting ?? false,
                cancellationToken: cancellationToken);
            return this.Accepted(ApiResponse.SuccessResponse(new { message = "Document generation job enqueued." }));
        }
        catch (ArgumentException ex)
        {
            return this.BadRequest(ApiResponse.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Generate invoice document
    /// </summary>
    [HttpPost("invoices/{invoiceId}")]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status501NotImplemented)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GeneratedDocumentDto>>> GenerateInvoiceDocument(
        Guid invoiceId,
        [FromQuery] Guid? templateId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return this.Unauthorized<GeneratedDocumentDto>("Company context required");
        }

        if (invoiceId == Guid.Empty)
        {
            return ValidationError<GeneratedDocumentDto>("InvoiceId is required.");
        }

        try
        {
            var document = await _documentGenerationService.GenerateInvoiceDocumentAsync(
                invoiceId, companyId.Value, templateId, cancellationToken);
            return this.Success(document, "Invoice document generated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<GeneratedDocumentDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<GeneratedDocumentDto>(ex.Message);
        }
        catch (NotImplementedException ex)
        {
            return StatusCode(501, this.Error<GeneratedDocumentDto>(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice document: {InvoiceId}", invoiceId);
            return this.InternalServerError<GeneratedDocumentDto>($"Failed to generate invoice document: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate job docket
    /// </summary>
    [HttpPost("orders/{orderId}/docket")]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status501NotImplemented)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GeneratedDocumentDto>>> GenerateJobDocket(
        Guid orderId,
        [FromQuery] Guid? templateId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return this.Unauthorized<GeneratedDocumentDto>("Company context required");
        }

        if (orderId == Guid.Empty)
        {
            return ValidationError<GeneratedDocumentDto>("OrderId is required.");
        }

        try
        {
            var document = await _documentGenerationService.GenerateJobDocketAsync(
                orderId, companyId.Value, templateId, cancellationToken);
            return this.Success(document, "Job docket generated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<GeneratedDocumentDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<GeneratedDocumentDto>(ex.Message);
        }
        catch (NotImplementedException ex)
        {
            return StatusCode(501, this.Error<GeneratedDocumentDto>(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating job docket: {OrderId}", orderId);
            return this.InternalServerError<GeneratedDocumentDto>($"Failed to generate job docket: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate RMA form
    /// </summary>
    [HttpPost("rma/{rmaRequestId}")]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status501NotImplemented)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GeneratedDocumentDto>>> GenerateRmaForm(
        Guid rmaRequestId,
        [FromQuery] Guid? templateId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return this.Unauthorized<GeneratedDocumentDto>("Company context required");
        }

        if (rmaRequestId == Guid.Empty)
        {
            return ValidationError<GeneratedDocumentDto>("RmaRequestId is required.");
        }

        try
        {
            var document = await _documentGenerationService.GenerateRmaFormAsync(
                rmaRequestId, companyId.Value, templateId, cancellationToken);
            return this.Success(document, "RMA form generated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<GeneratedDocumentDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<GeneratedDocumentDto>(ex.Message);
        }
        catch (NotImplementedException ex)
        {
            return StatusCode(501, this.Error<GeneratedDocumentDto>(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating RMA form: {RmaRequestId}", rmaRequestId);
            return this.InternalServerError<GeneratedDocumentDto>($"Failed to generate RMA form: {ex.Message}");
        }
    }

    /// <summary>
    /// Get generated documents
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<GeneratedDocumentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<GeneratedDocumentDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<GeneratedDocumentDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<GeneratedDocumentDto>>>> GetGeneratedDocuments(
        [FromQuery] string? referenceEntity = null,
        [FromQuery] Guid? referenceId = null,
        [FromQuery] string? documentType = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return this.Unauthorized<List<GeneratedDocumentDto>>("Company context required");
        }

        try
        {
            var documents = await _documentGenerationService.GetGeneratedDocumentsAsync(
                companyId.Value, referenceEntity, referenceId, documentType, cancellationToken);
            return this.Success(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting generated documents");
            return this.InternalServerError<List<GeneratedDocumentDto>>($"Failed to get generated documents: {ex.Message}");
        }
    }

    /// <summary>
    /// Get generated document by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedDocumentDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GeneratedDocumentDto>>> GetGeneratedDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return this.Unauthorized<GeneratedDocumentDto>("Company context required");
        }

        try
        {
            var document = await _documentGenerationService.GetGeneratedDocumentByIdAsync(
                id, companyId.Value, cancellationToken);
            if (document == null)
            {
                return this.NotFound<GeneratedDocumentDto>($"Generated document with ID {id} not found");
            }

            return this.Success(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting generated document: {DocumentId}", id);
            return this.InternalServerError<GeneratedDocumentDto>($"Failed to get generated document: {ex.Message}");
        }
    }

    private ActionResult<ApiResponse<T>> ValidationError<T>(params string[] errors)
    {
        var errorList = new List<string> { "VALIDATION_ERROR" };
        errorList.AddRange(errors);
        return this.Error<T>(errorList, "Validation failed", 400);
    }
}

/// <summary>Request body for POST api/documents/generate-async (Phase 5).</summary>
public class GenerateDocumentAsyncRequest
{
    public string? DocumentType { get; set; }
    public Guid EntityId { get; set; }
    public string? ReferenceEntity { get; set; }
    public Guid? TemplateId { get; set; }
    public string? Format { get; set; }
    public string? DataJson { get; set; }
    public bool? ReplaceExisting { get; set; }
}
