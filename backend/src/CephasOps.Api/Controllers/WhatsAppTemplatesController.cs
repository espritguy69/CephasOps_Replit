using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

[ApiController]
[Route("api/whatsapp-templates")]
[Authorize]
public class WhatsAppTemplatesController : ControllerBase
{
    private readonly IWhatsAppTemplateService _whatsAppTemplateService;
    private readonly ILogger<WhatsAppTemplatesController> _logger;

    public WhatsAppTemplatesController(
        IWhatsAppTemplateService whatsAppTemplateService,
        ILogger<WhatsAppTemplatesController> logger)
    {
        _whatsAppTemplateService = whatsAppTemplateService;
        _logger = logger;
    }

    /// <summary>
    /// Get all WhatsApp templates
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<WhatsAppTemplateDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<WhatsAppTemplateDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<WhatsAppTemplateDto>>>> GetTemplates(
        [FromQuery] Guid companyId,
        [FromQuery] string? category = null,
        [FromQuery] string? approvalStatus = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var templates = await _whatsAppTemplateService.GetTemplatesAsync(companyId, category, approvalStatus, isActive, cancellationToken);
            return this.Success(templates, $"Retrieved {templates.Count} WhatsApp templates");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting WhatsApp templates for company {CompanyId}", companyId);
            return this.InternalServerError<List<WhatsAppTemplateDto>>($"Error retrieving WhatsApp templates: {ex.Message}");
        }
    }

    /// <summary>
    /// Get WhatsApp template by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WhatsAppTemplateDto>>> GetTemplateById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _whatsAppTemplateService.GetTemplateByIdAsync(id, cancellationToken);

            if (template == null)
            {
                return this.NotFound<WhatsAppTemplateDto>("WhatsApp template not found");
            }

            return this.Success(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting WhatsApp template {TemplateId}", id);
            return this.InternalServerError<WhatsAppTemplateDto>($"Error retrieving WhatsApp template: {ex.Message}");
        }
    }

    /// <summary>
    /// Get WhatsApp template by code
    /// </summary>
    [HttpGet("by-code/{code}")]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WhatsAppTemplateDto>>> GetTemplateByCode(
        [FromRoute] string code,
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _whatsAppTemplateService.GetTemplateByCodeAsync(companyId, code, cancellationToken);

            if (template == null)
            {
                return this.NotFound<WhatsAppTemplateDto>($"WhatsApp template with code '{code}' not found");
            }

            return this.Success(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting WhatsApp template by code {Code}", code);
            return this.InternalServerError<WhatsAppTemplateDto>($"Error retrieving WhatsApp template: {ex.Message}");
        }
    }

    /// <summary>
    /// Create new WhatsApp template
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppTemplateDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WhatsAppTemplateDto>>> CreateTemplate(
        [FromQuery] Guid companyId,
        [FromBody] CreateWhatsAppTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();
            var template = await _whatsAppTemplateService.CreateTemplateAsync(companyId, dto, userId, cancellationToken);

            return this.CreatedAtAction(
                nameof(GetTemplateById),
                new { id = template.Id },
                template,
                "WhatsApp template created successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<WhatsAppTemplateDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating WhatsApp template");
            return this.InternalServerError<WhatsAppTemplateDto>($"Error creating WhatsApp template: {ex.Message}");
        }
    }

    /// <summary>
    /// Update WhatsApp template
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WhatsAppTemplateDto>>> UpdateTemplate(
        Guid id,
        [FromBody] UpdateWhatsAppTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();
            var template = await _whatsAppTemplateService.UpdateTemplateAsync(id, dto, userId, cancellationToken);

            if (template == null)
            {
                return this.NotFound<WhatsAppTemplateDto>("WhatsApp template not found");
            }

            return this.Success(template, "WhatsApp template updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating WhatsApp template {TemplateId}", id);
            return this.InternalServerError<WhatsAppTemplateDto>($"Error updating WhatsApp template: {ex.Message}");
        }
    }

    /// <summary>
    /// Update WhatsApp template approval status
    /// </summary>
    [HttpPatch("{id}/approval-status")]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WhatsAppTemplateDto>>> UpdateApprovalStatus(
        Guid id,
        [FromBody] UpdateApprovalStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _whatsAppTemplateService.UpdateApprovalStatusAsync(id, request.ApprovalStatus, cancellationToken);

            if (template == null)
            {
                return this.NotFound<WhatsAppTemplateDto>("WhatsApp template not found");
            }

            return this.Success(template, $"Approval status updated to {request.ApprovalStatus}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating approval status for WhatsApp template {TemplateId}", id);
            return this.InternalServerError<WhatsAppTemplateDto>($"Error updating approval status: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete WhatsApp template
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteTemplate(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await _whatsAppTemplateService.DeleteTemplateAsync(id, cancellationToken);

            if (!deleted)
            {
                return this.NotFound("WhatsApp template not found");
            }

            return this.Success("WhatsApp template deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting WhatsApp template {TemplateId}", id);
            return this.InternalServerError($"Error deleting WhatsApp template: {ex.Message}");
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

public record UpdateApprovalStatusRequest(string ApprovalStatus);
