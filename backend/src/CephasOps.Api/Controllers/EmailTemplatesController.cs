using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using CephasOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Email templates management endpoints. Tenant-scoped; company from ITenantProvider.
/// </summary>
[ApiController]
[Route("api/email-templates")]
[Authorize]
public class EmailTemplatesController : ControllerBase
{
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<EmailTemplatesController> _logger;

    public EmailTemplatesController(
        IEmailTemplateService emailTemplateService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<EmailTemplatesController> logger)
    {
        _emailTemplateService = emailTemplateService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all email templates, optionally filtered by direction (Incoming/Outgoing)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EmailTemplateDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<EmailTemplateDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<EmailTemplateDto>>>> GetEmailTemplates(
        [FromQuery] string? direction = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var templates = await _emailTemplateService.GetAllAsync(direction, companyId, cancellationToken);
            return this.Success(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email templates");
            return this.InternalServerError<List<EmailTemplateDto>>($"Failed to get email templates: {ex.Message}");
        }
    }

    /// <summary>
    /// Get email template by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmailTemplateDto>>> GetEmailTemplate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var template = await _emailTemplateService.GetByIdAsync(id, companyId, cancellationToken);
            if (template == null)
            {
                return this.NotFound<EmailTemplateDto>($"Email template with ID {id} not found");
            }

            return this.Success(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email template {TemplateId}", id);
            return this.InternalServerError<EmailTemplateDto>($"Failed to get email template: {ex.Message}");
        }
    }

    /// <summary>
    /// Get email template by code
    /// </summary>
    [HttpGet("by-code/{code}")]
    [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmailTemplateDto>>> GetEmailTemplateByCode(
        string code,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var template = await _emailTemplateService.GetByCodeAsync(code, companyId, cancellationToken);
            if (template == null)
            {
                return this.NotFound<EmailTemplateDto>($"Email template with code '{code}' not found");
            }

            return this.Success(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email template by code {Code}", code);
            return this.InternalServerError<EmailTemplateDto>($"Failed to get email template: {ex.Message}");
        }
    }

    /// <summary>
    /// Get active templates for entity type
    /// </summary>
    [HttpGet("by-entity-type/{entityType}")]
    [ProducesResponseType(typeof(ApiResponse<List<EmailTemplateDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<EmailTemplateDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<EmailTemplateDto>>>> GetEmailTemplatesByEntityType(
        string entityType,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var templates = await _emailTemplateService.GetActiveByEntityTypeAsync(entityType, companyId, cancellationToken);
            return this.Success(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email templates for entity type {EntityType}", entityType);
            return this.InternalServerError<List<EmailTemplateDto>>($"Failed to get email templates: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a new email template
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmailTemplateDto>>> CreateEmailTemplate(
        [FromBody] CreateEmailTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<EmailTemplateDto>("User context required");
        }
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var template = await _emailTemplateService.CreateAsync(dto, userId.Value, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetEmailTemplate), new { id = template.Id }, template, "Email template created successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<EmailTemplateDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating email template");
            return this.InternalServerError<EmailTemplateDto>($"Failed to create email template: {ex.Message}");
        }
    }

    /// <summary>
    /// Update an existing email template
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmailTemplateDto>>> UpdateEmailTemplate(
        Guid id,
        [FromBody] UpdateEmailTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<EmailTemplateDto>("User context required");
        }
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var template = await _emailTemplateService.UpdateAsync(id, dto, userId.Value, companyId, cancellationToken);
            return this.Success(template, "Email template updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<EmailTemplateDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email template {TemplateId}", id);
            return this.InternalServerError<EmailTemplateDto>($"Failed to update email template: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete an email template
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteEmailTemplate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            await _emailTemplateService.DeleteAsync(id, companyId, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting email template {TemplateId}", id);
            return this.InternalServerError($"Failed to delete email template: {ex.Message}");
        }
    }
}

