using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

[ApiController]
[Route("api/workflow/guard-conditions")]
[Authorize]
public class GuardConditionDefinitionsController : ControllerBase
{
    private readonly IGuardConditionDefinitionsService _service;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<GuardConditionDefinitionsController> _logger;

    public GuardConditionDefinitionsController(
        IGuardConditionDefinitionsService service,
        ITenantProvider tenantProvider,
        ILogger<GuardConditionDefinitionsController> logger)
    {
        _service = service;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all guard condition definitions for the current company
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<GuardConditionDefinitionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<GuardConditionDefinitionDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<GuardConditionDefinitionDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<GuardConditionDefinitionDto>>>> GetGuardConditionDefinitions(
        [FromQuery] string? entityType = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var definitions = await _service.GetGuardConditionDefinitionsAsync(
                companyId,
                entityType,
                isActive,
                cancellationToken);

            return this.Success(definitions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting guard condition definitions");
            return this.Error<List<GuardConditionDefinitionDto>>($"Failed to get guard condition definitions: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get guard condition definition by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<GuardConditionDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GuardConditionDefinitionDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<GuardConditionDefinitionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<GuardConditionDefinitionDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GuardConditionDefinitionDto>>> GetGuardConditionDefinition(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        if (companyId == Guid.Empty)
        {
            return this.Unauthorized<GuardConditionDefinitionDto>("Company ID not found.");
        }

        try
        {
            var definition = await _service.GetGuardConditionDefinitionAsync(
                companyId,
                id,
                cancellationToken);

            if (definition == null)
            {
                return this.NotFound<GuardConditionDefinitionDto>($"Guard condition definition with ID '{id}' not found.");
            }

            return this.Success(definition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting guard condition definition {Id}", id);
            return this.Error<GuardConditionDefinitionDto>($"Failed to get guard condition definition: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create a new guard condition definition
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<GuardConditionDefinitionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<GuardConditionDefinitionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<GuardConditionDefinitionDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<GuardConditionDefinitionDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GuardConditionDefinitionDto>>> CreateGuardConditionDefinition(
        [FromBody] CreateGuardConditionDefinitionDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        if (companyId == Guid.Empty)
        {
            return this.Unauthorized<GuardConditionDefinitionDto>("Company ID not found.");
        }

        try
        {
            var definition = await _service.CreateGuardConditionDefinitionAsync(
                companyId,
                dto,
                cancellationToken);

            return this.StatusCode(201, ApiResponse<GuardConditionDefinitionDto>.SuccessResponse(definition, "Guard condition definition created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return this.Error<GuardConditionDefinitionDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating guard condition definition");
            return this.Error<GuardConditionDefinitionDto>($"Failed to create guard condition definition: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update a guard condition definition
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<GuardConditionDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GuardConditionDefinitionDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<GuardConditionDefinitionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<GuardConditionDefinitionDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GuardConditionDefinitionDto>>> UpdateGuardConditionDefinition(
        Guid id,
        [FromBody] UpdateGuardConditionDefinitionDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        if (companyId == Guid.Empty)
        {
            return this.Unauthorized<GuardConditionDefinitionDto>("Company ID not found.");
        }

        try
        {
            var definition = await _service.UpdateGuardConditionDefinitionAsync(
                companyId,
                id,
                dto,
                cancellationToken);

            return this.Success(definition, "Guard condition definition updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<GuardConditionDefinitionDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating guard condition definition {Id}", id);
            return this.Error<GuardConditionDefinitionDto>($"Failed to update guard condition definition: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete a guard condition definition (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteGuardConditionDefinition(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        if (companyId == Guid.Empty)
        {
            return StatusCode(401, ApiResponse.ErrorResponse("Company ID not found."));
        }

        try
        {
            await _service.DeleteGuardConditionDefinitionAsync(
                companyId,
                id,
                cancellationToken);

            return this.StatusCode(204, ApiResponse.SuccessResponse("Guard condition definition deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting guard condition definition {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete guard condition definition: {ex.Message}"));
        }
    }
}

