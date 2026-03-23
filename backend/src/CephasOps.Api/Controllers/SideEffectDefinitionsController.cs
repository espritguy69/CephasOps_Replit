using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

[ApiController]
[Route("api/workflow/side-effects")]
[Authorize]
public class SideEffectDefinitionsController : ControllerBase
{
    private readonly ISideEffectDefinitionsService _service;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SideEffectDefinitionsController> _logger;

    public SideEffectDefinitionsController(
        ISideEffectDefinitionsService service,
        ITenantProvider tenantProvider,
        ILogger<SideEffectDefinitionsController> logger)
    {
        _service = service;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all side effect definitions for the current company
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SideEffectDefinitionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<SideEffectDefinitionDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<SideEffectDefinitionDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<SideEffectDefinitionDto>>>> GetSideEffectDefinitions(
        [FromQuery] string? entityType = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var definitions = await _service.GetSideEffectDefinitionsAsync(
                companyId,
                entityType,
                isActive,
                cancellationToken);

            return this.Success(definitions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting side effect definitions");
            return this.Error<List<SideEffectDefinitionDto>>($"Failed to get side effect definitions: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get side effect definition by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SideEffectDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SideEffectDefinitionDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SideEffectDefinitionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SideEffectDefinitionDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SideEffectDefinitionDto>>> GetSideEffectDefinition(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        if (companyId == Guid.Empty)
        {
            return this.Unauthorized<SideEffectDefinitionDto>("Company ID not found.");
        }

        try
        {
            var definition = await _service.GetSideEffectDefinitionAsync(
                companyId,
                id,
                cancellationToken);

            if (definition == null)
            {
                return this.NotFound<SideEffectDefinitionDto>($"Side effect definition with ID '{id}' not found.");
            }

            return this.Success(definition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting side effect definition {Id}", id);
            return this.Error<SideEffectDefinitionDto>($"Failed to get side effect definition: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create a new side effect definition
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SideEffectDefinitionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SideEffectDefinitionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SideEffectDefinitionDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SideEffectDefinitionDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SideEffectDefinitionDto>>> CreateSideEffectDefinition(
        [FromBody] CreateSideEffectDefinitionDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        if (companyId == Guid.Empty)
        {
            return this.Unauthorized<SideEffectDefinitionDto>("Company ID not found.");
        }

        try
        {
            var definition = await _service.CreateSideEffectDefinitionAsync(
                companyId,
                dto,
                cancellationToken);

            return this.StatusCode(201, ApiResponse<SideEffectDefinitionDto>.SuccessResponse(definition, "Side effect definition created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return this.Error<SideEffectDefinitionDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating side effect definition");
            return this.Error<SideEffectDefinitionDto>($"Failed to create side effect definition: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update a side effect definition
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SideEffectDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SideEffectDefinitionDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SideEffectDefinitionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SideEffectDefinitionDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SideEffectDefinitionDto>>> UpdateSideEffectDefinition(
        Guid id,
        [FromBody] UpdateSideEffectDefinitionDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        if (companyId == Guid.Empty)
        {
            return this.Unauthorized<SideEffectDefinitionDto>("Company ID not found.");
        }

        try
        {
            var definition = await _service.UpdateSideEffectDefinitionAsync(
                companyId,
                id,
                dto,
                cancellationToken);

            return this.Success(definition, "Side effect definition updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<SideEffectDefinitionDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating side effect definition {Id}", id);
            return this.Error<SideEffectDefinitionDto>($"Failed to update side effect definition: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete a side effect definition (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteSideEffectDefinition(
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
            await _service.DeleteSideEffectDefinitionAsync(
                companyId,
                id,
                cancellationToken);

            return this.StatusCode(204, ApiResponse.SuccessResponse("Side effect definition deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting side effect definition {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete side effect definition: {ex.Message}"));
        }
    }
}

