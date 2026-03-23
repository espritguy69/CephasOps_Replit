using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Material templates management endpoints
/// </summary>
[ApiController]
[Route("api/material-templates")]
[Authorize]
public class MaterialTemplatesController : ControllerBase
{
    private readonly IMaterialTemplateService _materialTemplateService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<MaterialTemplatesController> _logger;

    public MaterialTemplatesController(
        IMaterialTemplateService materialTemplateService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<MaterialTemplatesController> logger)
    {
        _materialTemplateService = materialTemplateService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get material templates
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<MaterialTemplateDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<MaterialTemplateDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<MaterialTemplateDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<MaterialTemplateDto>>>> GetMaterialTemplates(
        [FromQuery] string? orderType = null,
        [FromQuery] Guid? installationMethodId = null,
        [FromQuery] Guid? buildingTypeId = null,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return this.Unauthorized<List<MaterialTemplateDto>>("Company context required");
        }

        try
        {
            var templates = await _materialTemplateService.GetTemplatesAsync(
                companyId ?? Guid.Empty, orderType, installationMethodId, buildingTypeId, partnerId, isActive, cancellationToken);
            return this.Success(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting material templates");
            return this.InternalServerError<List<MaterialTemplateDto>>($"Failed to get material templates: {ex.Message}");
        }
    }

    /// <summary>
    /// Get material template by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MaterialTemplateDto>>> GetMaterialTemplate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return this.Unauthorized<MaterialTemplateDto>("Company context required");
        }

        try
        {
            var template = await _materialTemplateService.GetTemplateByIdAsync(id, companyId ?? Guid.Empty, cancellationToken);
            if (template == null)
            {
                return this.NotFound<MaterialTemplateDto>($"Material template with ID {id} not found");
            }

            return this.Success(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting material template: {TemplateId}", id);
            return this.InternalServerError<MaterialTemplateDto>($"Failed to get material template: {ex.Message}");
        }
    }

    /// <summary>
    /// Get effective material template for order context
    /// </summary>
    [HttpGet("effective")]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MaterialTemplateDto>>> GetEffectiveTemplate(
        [FromQuery] string orderType,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] Guid? installationMethodId = null,
        [FromQuery] Guid? buildingTypeId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return this.Unauthorized<MaterialTemplateDto>("Company context required");
        }

        if (string.IsNullOrWhiteSpace(orderType))
        {
            return ValidationError<MaterialTemplateDto>("OrderType is required.");
        }

        try
        {
            var template = await _materialTemplateService.GetEffectiveTemplateAsync(
                companyId ?? Guid.Empty, partnerId, orderType, installationMethodId, buildingTypeId, cancellationToken);
            
            if (template == null)
            {
                return this.NotFound<MaterialTemplateDto>("No effective template found for the given context");
            }

            return this.Success(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effective material template");
            return this.InternalServerError<MaterialTemplateDto>($"Failed to get effective template: {ex.Message}");
        }
    }

    /// <summary>
    /// Create material template
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MaterialTemplateDto>>> CreateMaterialTemplate(
        [FromBody] CreateMaterialTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if ((companyId == null && !_currentUserService.IsSuperAdmin) || userId == null)
        {
            return this.Unauthorized<MaterialTemplateDto>("Company and user context required");
        }

        var validationErrors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            validationErrors.Add("Name is required.");
        }
        if (string.IsNullOrWhiteSpace(dto.OrderType))
        {
            validationErrors.Add("OrderType is required.");
        }
        if (dto.Items == null || dto.Items.Count == 0)
        {
            validationErrors.Add("At least one item is required.");
        }
        if (validationErrors.Count > 0)
        {
            return ValidationError<MaterialTemplateDto>(validationErrors.ToArray());
        }

        try
        {
            var template = await _materialTemplateService.CreateTemplateAsync(
                dto, companyId ?? Guid.Empty, userId.Value, cancellationToken);
            return this.CreatedAtAction(nameof(GetMaterialTemplate), new { id = template.Id }, template, "Material template created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating material template");
            return this.InternalServerError<MaterialTemplateDto>($"Failed to create material template: {ex.Message}");
        }
    }

    /// <summary>
    /// Update material template
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MaterialTemplateDto>>> UpdateMaterialTemplate(
        Guid id,
        [FromBody] UpdateMaterialTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if ((companyId == null && !_currentUserService.IsSuperAdmin) || userId == null)
        {
            return this.Unauthorized<MaterialTemplateDto>("Company and user context required");
        }

        var hasUpdates = dto.DepartmentId.HasValue
            || dto.Name != null
            || dto.InstallationMethodId.HasValue
            || dto.IsDefault.HasValue
            || dto.IsActive.HasValue
            || dto.Items != null;

        if (!hasUpdates)
        {
            return ValidationError<MaterialTemplateDto>("At least one field must be provided for update.");
        }

        try
        {
            var template = await _materialTemplateService.UpdateTemplateAsync(
                id, dto, companyId ?? Guid.Empty, userId.Value, cancellationToken);
            return this.Success(template, "Material template updated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<MaterialTemplateDto>($"Material template with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating material template: {TemplateId}", id);
            return this.InternalServerError<MaterialTemplateDto>($"Failed to update material template: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete material template
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteMaterialTemplate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return this.Unauthorized("Company context required");
        }

        try
        {
            await _materialTemplateService.DeleteTemplateAsync(id, companyId ?? Guid.Empty, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound($"Material template with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting material template: {TemplateId}", id);
            return this.InternalServerError($"Failed to delete material template: {ex.Message}");
        }
    }

    /// <summary>
    /// Set material template as default
    /// </summary>
    [HttpPost("{id}/set-default")]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MaterialTemplateDto>>> SetAsDefault(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if ((companyId == null && !_currentUserService.IsSuperAdmin) || userId == null)
        {
            return this.Unauthorized<MaterialTemplateDto>("Company and user context required");
        }

        try
        {
            var template = await _materialTemplateService.SetAsDefaultAsync(
                id, companyId ?? Guid.Empty, userId.Value, cancellationToken);
            return this.Success(template, "Material template set as default successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<MaterialTemplateDto>($"Material template with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting material template as default: {TemplateId}", id);
            return this.InternalServerError<MaterialTemplateDto>($"Failed to set template as default: {ex.Message}");
        }
    }

    /// <summary>
    /// Clone a material template
    /// </summary>
    [HttpPost("{id}/clone")]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<MaterialTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MaterialTemplateDto>>> CloneTemplate(
        Guid id,
        [FromBody] CloneMaterialTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if ((companyId == null && !_currentUserService.IsSuperAdmin) || userId == null)
        {
            return this.Unauthorized<MaterialTemplateDto>("Company and user context required");
        }

        if (dto == null || string.IsNullOrWhiteSpace(dto.NewName))
        {
            return ValidationError<MaterialTemplateDto>("NewName is required.");
        }

        try
        {
            var template = await _materialTemplateService.CloneTemplateAsync(
                id, dto.NewName, companyId ?? Guid.Empty, userId.Value, cancellationToken);
            return this.CreatedAtAction(nameof(GetMaterialTemplate), new { id = template.Id }, template, "Material template cloned successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<MaterialTemplateDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<MaterialTemplateDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning material template: {TemplateId}", id);
            return this.InternalServerError<MaterialTemplateDto>($"Failed to clone material template: {ex.Message}");
        }
    }

    private ActionResult<ApiResponse<T>> ValidationError<T>(params string[] errors)
    {
        var errorList = new List<string> { "VALIDATION_ERROR" };
        errorList.AddRange(errors);
        return this.Error<T>(errorList, "Validation failed", 400);
    }
}
