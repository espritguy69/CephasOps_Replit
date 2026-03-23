using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

[ApiController]
[Route("api/sms-templates")]
[Authorize]
public class SmsTemplatesController : ControllerBase
{
    private readonly ISmsTemplateService _smsTemplateService;
    private readonly ILogger<SmsTemplatesController> _logger;

    public SmsTemplatesController(
        ISmsTemplateService smsTemplateService,
        ILogger<SmsTemplatesController> logger)
    {
        _smsTemplateService = smsTemplateService;
        _logger = logger;
    }

    /// <summary>
    /// Get all SMS templates
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SmsTemplateDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<SmsTemplateDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<SmsTemplateDto>>>> GetTemplates(
        [FromQuery] Guid companyId,
        [FromQuery] string? category = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var templates = await _smsTemplateService.GetTemplatesAsync(companyId, category, isActive, cancellationToken);
            return this.Success(templates, $"Retrieved {templates.Count} SMS templates");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SMS templates for company {CompanyId}", companyId);
            return this.InternalServerError<List<SmsTemplateDto>>($"Error retrieving SMS templates: {ex.Message}");
        }
    }

    /// <summary>
    /// Get SMS template by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SmsTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SmsTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SmsTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SmsTemplateDto>>> GetTemplateById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _smsTemplateService.GetTemplateByIdAsync(id, cancellationToken);

            if (template == null)
            {
                return this.NotFound<SmsTemplateDto>("SMS template not found");
            }

            return this.Success(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SMS template {TemplateId}", id);
            return this.InternalServerError<SmsTemplateDto>($"Error retrieving SMS template: {ex.Message}");
        }
    }

    /// <summary>
    /// Get SMS template by code
    /// </summary>
    [HttpGet("by-code/{code}")]
    [ProducesResponseType(typeof(ApiResponse<SmsTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SmsTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SmsTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SmsTemplateDto>>> GetTemplateByCode(
        [FromRoute] string code,
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _smsTemplateService.GetTemplateByCodeAsync(companyId, code, cancellationToken);

            if (template == null)
            {
                return this.NotFound<SmsTemplateDto>($"SMS template with code '{code}' not found");
            }

            return this.Success(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SMS template by code {Code}", code);
            return this.InternalServerError<SmsTemplateDto>($"Error retrieving SMS template: {ex.Message}");
        }
    }

    /// <summary>
    /// Create new SMS template
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SmsTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SmsTemplateDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SmsTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SmsTemplateDto>>> CreateTemplate(
        [FromQuery] Guid companyId,
        [FromBody] CreateSmsTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();
            var template = await _smsTemplateService.CreateTemplateAsync(companyId, dto, userId, cancellationToken);

            return this.CreatedAtAction(
                nameof(GetTemplateById),
                new { id = template.Id },
                template,
                "SMS template created successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<SmsTemplateDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SMS template");
            return this.InternalServerError<SmsTemplateDto>($"Error creating SMS template: {ex.Message}");
        }
    }

    /// <summary>
    /// Update SMS template
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SmsTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SmsTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SmsTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SmsTemplateDto>>> UpdateTemplate(
        Guid id,
        [FromBody] UpdateSmsTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();
            var template = await _smsTemplateService.UpdateTemplateAsync(id, dto, userId, cancellationToken);

            if (template == null)
            {
                return this.NotFound<SmsTemplateDto>("SMS template not found");
            }

            return this.Success(template, "SMS template updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating SMS template {TemplateId}", id);
            return this.InternalServerError<SmsTemplateDto>($"Error updating SMS template: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete SMS template
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteTemplate(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await _smsTemplateService.DeleteTemplateAsync(id, cancellationToken);

            if (!deleted)
            {
                return this.NotFound("SMS template not found");
            }

            return this.Success("SMS template deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting SMS template {TemplateId}", id);
            return this.InternalServerError($"Error deleting SMS template: {ex.Message}");
        }
    }

    /// <summary>
    /// Render SMS message with placeholders
    /// </summary>
    [HttpPost("{id}/render")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> RenderMessage(
        Guid id,
        [FromBody] Dictionary<string, string> placeholders,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await _smsTemplateService.RenderMessageAsync(id, placeholders, cancellationToken);

            var result = new
            {
                message,
                charCount = message.Length
            };

            return this.Success<object>(result, "Message rendered successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering SMS message for template {TemplateId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Error rendering SMS message: {ex.Message}"));
        }
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}
