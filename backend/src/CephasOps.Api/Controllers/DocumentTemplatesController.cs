using CephasOps.Application.Settings;
using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using CephasOps.Api.Common;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Document templates management endpoints
/// </summary>
[ApiController]
[Route("api/document-templates")]
[Authorize]
public class DocumentTemplatesController : ControllerBase
{
    private static readonly Regex PlaceholderRegex = new("{{\\s*([A-Za-z0-9_]+)\\s*}}", RegexOptions.Compiled);
    private static readonly List<string> DefaultVariables =
    [
        "CustomerName",
        "Address",
        "GeranNo",
        "TitleNumber",
        "LotCode",
        "AccountNo",
        "Mukim",
        "TypeOfTitle"
    ];

    private static readonly List<string> DefaultCategories =
    [
        "Invoice",
        "JobDocket",
        "RmaForm",
        "PurchaseOrder",
        "Quotation",
        "BOQ",
        "DeliveryOrder",
        "PaymentReceipt"
    ];

    private readonly IDocumentTemplateService _documentTemplateService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly CarboneSettings _carboneSettings;
    private readonly ILogger<DocumentTemplatesController> _logger;

    public DocumentTemplatesController(
        IDocumentTemplateService documentTemplateService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IOptions<CarboneSettings> carboneSettings,
        ILogger<DocumentTemplatesController> logger)
    {
        _documentTemplateService = documentTemplateService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _carboneSettings = carboneSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Get document templates
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DocumentTemplateDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<DocumentTemplateDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<DocumentTemplateDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<DocumentTemplateDto>>>> GetDocumentTemplates(
        [FromQuery] string? documentType = null,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return this.Unauthorized<List<DocumentTemplateDto>>("Company context required");
        }

        try
        {
            var templates = await _documentTemplateService.GetTemplatesAsync(
                companyId.Value, documentType, partnerId, isActive, cancellationToken);
            return this.Success(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document templates");
            return this.InternalServerError<List<DocumentTemplateDto>>($"Failed to get document templates: {ex.Message}");
        }
    }

    /// <summary>
    /// Get document template by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DocumentTemplateDto>>> GetDocumentTemplate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return this.Unauthorized<DocumentTemplateDto>("Company context required");
        }

        try
        {
            var template = await _documentTemplateService.GetTemplateByIdAsync(id, companyId.Value, cancellationToken);
            if (template == null)
            {
                return this.NotFound<DocumentTemplateDto>($"Document template with ID {id} not found");
            }

            return this.Success(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document template: {TemplateId}", id);
            return this.InternalServerError<DocumentTemplateDto>($"Failed to get document template: {ex.Message}");
        }
    }

    /// <summary>
    /// Get placeholder definitions for document type
    /// </summary>
    [HttpGet("placeholders/{documentType}")]
    [ProducesResponseType(typeof(ApiResponse<List<DocumentPlaceholderDefinitionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<DocumentPlaceholderDefinitionDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<DocumentPlaceholderDefinitionDto>>>> GetPlaceholderDefinitions(
        string documentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var definitions = await _documentTemplateService.GetPlaceholderDefinitionsAsync(documentType, cancellationToken);
            return this.Success(definitions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting placeholder definitions for {DocumentType}", documentType);
            return this.InternalServerError<List<DocumentPlaceholderDefinitionDto>>($"Failed to get placeholder definitions: {ex.Message}");
        }
    }

    /// <summary>
    /// Create document template
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DocumentTemplateDto>>> CreateDocumentTemplate(
        [FromBody] CreateDocumentTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if (companyId == null || userId == null)
        {
            return this.Unauthorized<DocumentTemplateDto>("Company and user context required");
        }

        try
        {
            var template = await _documentTemplateService.CreateTemplateAsync(
                dto, companyId.Value, userId.Value, cancellationToken);
            return this.CreatedAtAction(nameof(GetDocumentTemplate), new { id = template.Id }, template, "Document template created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document template");
            return this.InternalServerError<DocumentTemplateDto>($"Failed to create document template: {ex.Message}");
        }
    }

    /// <summary>
    /// Update document template
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DocumentTemplateDto>>> UpdateDocumentTemplate(
        Guid id,
        [FromBody] UpdateDocumentTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if (companyId == null || userId == null)
        {
            return this.Unauthorized<DocumentTemplateDto>("Company and user context required");
        }

        try
        {
            var template = await _documentTemplateService.UpdateTemplateAsync(
                id, dto, companyId.Value, userId.Value, cancellationToken);
            return this.Success(template, "Document template updated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<DocumentTemplateDto>($"Document template with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document template: {TemplateId}", id);
            return this.InternalServerError<DocumentTemplateDto>($"Failed to update document template: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete document template
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteDocumentTemplate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return this.Unauthorized("Company context required");
        }

        try
        {
            await _documentTemplateService.DeleteTemplateAsync(id, companyId.Value, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound($"Document template with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document template: {TemplateId}", id);
            return this.InternalServerError($"Failed to delete document template: {ex.Message}");
        }
    }

    /// <summary>
    /// Activate document template
    /// </summary>
    [HttpPost("{id}/activate")]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DocumentTemplateDto>>> ActivateTemplate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if (companyId == null || userId == null)
        {
            return this.Unauthorized<DocumentTemplateDto>("Company and user context required");
        }

        try
        {
            var template = await _documentTemplateService.ActivateTemplateAsync(
                id, companyId.Value, userId.Value, cancellationToken);
            return this.Success(template, "Document template activated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<DocumentTemplateDto>($"Document template with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating document template: {TemplateId}", id);
            return this.InternalServerError<DocumentTemplateDto>($"Failed to activate template: {ex.Message}");
        }
    }

    /// <summary>
    /// Get Carbone engine status
    /// </summary>
    [HttpGet("carbone-status")]
    [ProducesResponseType(typeof(ApiResponse<CarboneStatusDto>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<CarboneStatusDto>> GetCarboneStatus()
    {
        var result = new CarboneStatusDto
        {
            Enabled = _carboneSettings.Enabled,
            Configured = _carboneSettings.IsValid()
        };
        return this.Success(result);
    }

    /// <summary>
    /// Publish document template (validate placeholders and activate)
    /// </summary>
    [HttpPost("{id}/publish")]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DocumentTemplateDto>>> PublishTemplate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if (companyId == null || userId == null)
        {
            return this.Unauthorized<DocumentTemplateDto>("Company and user context required");
        }

        try
        {
            var template = await _documentTemplateService.GetTemplateByIdAsync(id, companyId.Value, cancellationToken);
            if (template == null)
            {
                return this.NotFound<DocumentTemplateDto>($"Document template with ID {id} not found");
            }

            var unknown = FindUnknownPlaceholders(template.HtmlBody, DefaultVariables);
            if (unknown.Count > 0)
            {
                return this.Error<DocumentTemplateDto>(unknown.Select(u => $"Unknown placeholder: {u}").ToList(),
                    "Unknown placeholders detected", 400);
            }

            var activated = await _documentTemplateService.ActivateTemplateAsync(id, companyId.Value, userId.Value, cancellationToken);
            return this.Success(activated, "Document template published successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing document template: {TemplateId}", id);
            return this.InternalServerError<DocumentTemplateDto>($"Failed to publish template: {ex.Message}");
        }
    }

    /// <summary>
    /// Duplicate document template
    /// </summary>
    [HttpPost("{id}/duplicate")]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DocumentTemplateDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DocumentTemplateDto>>> DuplicateTemplate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if (companyId == null || userId == null)
        {
            return this.Unauthorized<DocumentTemplateDto>("Company and user context required");
        }

        try
        {
            var template = await _documentTemplateService.GetTemplateByIdAsync(id, companyId.Value, cancellationToken);
            if (template == null)
            {
                return this.NotFound<DocumentTemplateDto>($"Document template with ID {id} not found");
            }

            var duplicate = await _documentTemplateService.CreateTemplateAsync(new CreateDocumentTemplateDto
            {
                Name = $"{template.Name} (Copy)",
                DocumentType = template.DocumentType,
                PartnerId = template.PartnerId,
                Engine = template.Engine,
                HtmlBody = template.HtmlBody,
                JsonSchema = template.JsonSchema,
                Description = template.Description,
                Tags = template.Tags,
                TemplateFileId = template.TemplateFileId,
                IsActive = false
            }, companyId.Value, userId.Value, cancellationToken);

            return this.Success(duplicate, "Document template duplicated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating document template: {TemplateId}", id);
            return this.InternalServerError<DocumentTemplateDto>($"Failed to duplicate template: {ex.Message}");
        }
    }

    /// <summary>
    /// Test render a template with provided data
    /// </summary>
    [HttpPost("test-render")]
    [ProducesResponseType(typeof(ApiResponse<TestRenderResponseDto>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<TestRenderResponseDto>> TestRender([FromBody] TestRenderRequestDto dto)
    {
        try
        {
            var content = dto.TemplateContent ?? string.Empty;
            var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (dto.DataJson != null)
            {
                foreach (var entry in dto.DataJson)
                {
                    data[entry.Key] = entry.Value;
                }
            }

            var placeholders = ExtractPlaceholders(content);
            var warnings = placeholders
                .Where(p => !data.ContainsKey(p))
                .Select(p => $"Missing value for {p}")
                .ToList();

            var renderedHtml = PlaceholderRegex.Replace(content, match =>
            {
                var key = match.Groups[1].Value;
                if (!data.TryGetValue(key, out var value) || value == null)
                {
                    return string.Empty;
                }
                var stringValue = ResolveValue(value);
                return WebUtility.HtmlEncode(stringValue);
            });

            return this.Success(new TestRenderResponseDto
            {
                RenderedHtml = renderedHtml,
                Warnings = warnings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template");
            return this.InternalServerError<TestRenderResponseDto>($"Failed to render template: {ex.Message}");
        }
    }

    /// <summary>
    /// Get allowed variables for templates
    /// </summary>
    [HttpGet("variables")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<List<string>>> GetVariables()
    {
        return this.Success(DefaultVariables);
    }

    /// <summary>
    /// Get available template categories
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<List<string>>> GetCategories()
    {
        return this.Success(DefaultCategories);
    }

    private static List<string> ExtractPlaceholders(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new List<string>();
        }
        return PlaceholderRegex.Matches(content)
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .ToList();
    }

    private static List<string> FindUnknownPlaceholders(string content, List<string> allowed)
    {
        var allowedSet = new HashSet<string>(allowed);
        return ExtractPlaceholders(content)
            .Where(placeholder => !allowedSet.Contains(placeholder))
            .ToList();
    }

    private static string ResolveValue(object value)
    {
        return value switch
        {
            null => string.Empty,
            JsonElement jsonElement => jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString() ?? string.Empty,
                JsonValueKind.Number => jsonElement.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Object => jsonElement.GetRawText(),
                JsonValueKind.Array => jsonElement.GetRawText(),
                _ => string.Empty
            },
            _ => value.ToString() ?? string.Empty
        };
    }
}

/// <summary>
/// DTO for Carbone status
/// </summary>
public class CarboneStatusDto
{
    public bool Enabled { get; set; }
    public bool Configured { get; set; }
}
