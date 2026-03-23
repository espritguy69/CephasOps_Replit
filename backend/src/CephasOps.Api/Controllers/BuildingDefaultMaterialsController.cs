using CephasOps.Application.Buildings.DTOs;
using CephasOps.Application.Buildings.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Building default materials management endpoints
/// </summary>
[ApiController]
[Route("api/buildings/{buildingId:guid}/default-materials")]
[Authorize]
public class BuildingDefaultMaterialsController : ControllerBase
{
    private readonly IBuildingDefaultMaterialService _service;
    private readonly ILogger<BuildingDefaultMaterialsController> _logger;

    public BuildingDefaultMaterialsController(
        IBuildingDefaultMaterialService service,
        ILogger<BuildingDefaultMaterialsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all default materials for a building
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<BuildingDefaultMaterialDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<BuildingDefaultMaterialDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<BuildingDefaultMaterialDto>>>> GetBuildingDefaultMaterials(
        [FromRoute] Guid buildingId,
        [FromQuery] Guid? orderTypeId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var materials = await _service.GetBuildingDefaultMaterialsAsync(
                buildingId, orderTypeId, isActive, cancellationToken);
            return this.Success(materials);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default materials for building {BuildingId}", buildingId);
            return this.InternalServerError<List<BuildingDefaultMaterialDto>>($"Failed to get default materials: {ex.Message}");
        }
    }

    /// <summary>
    /// Get a specific default material by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BuildingDefaultMaterialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BuildingDefaultMaterialDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BuildingDefaultMaterialDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingDefaultMaterialDto>>> GetBuildingDefaultMaterial(
        [FromRoute] Guid buildingId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var material = await _service.GetBuildingDefaultMaterialByIdAsync(buildingId, id, cancellationToken);
            if (material == null)
            {
                return this.NotFound<BuildingDefaultMaterialDto>($"Default material {id} not found for building {buildingId}");
            }
            return this.Success(material);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default material {Id} for building {BuildingId}", id, buildingId);
            return this.InternalServerError<BuildingDefaultMaterialDto>($"Failed to get default material: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a new default material for a building
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BuildingDefaultMaterialDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<BuildingDefaultMaterialDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BuildingDefaultMaterialDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BuildingDefaultMaterialDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingDefaultMaterialDto>>> CreateBuildingDefaultMaterial(
        [FromRoute] Guid buildingId,
        [FromBody] CreateBuildingDefaultMaterialDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var material = await _service.CreateBuildingDefaultMaterialAsync(buildingId, dto, cancellationToken);
            return this.CreatedAtAction(
                nameof(GetBuildingDefaultMaterial),
                new { buildingId, id = material.Id },
                material,
                "Default material created successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<BuildingDefaultMaterialDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<BuildingDefaultMaterialDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default material for building {BuildingId}", buildingId);
            return this.InternalServerError<BuildingDefaultMaterialDto>($"Failed to create default material: {ex.Message}");
        }
    }

    /// <summary>
    /// Update an existing default material
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BuildingDefaultMaterialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BuildingDefaultMaterialDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BuildingDefaultMaterialDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingDefaultMaterialDto>>> UpdateBuildingDefaultMaterial(
        [FromRoute] Guid buildingId,
        [FromRoute] Guid id,
        [FromBody] UpdateBuildingDefaultMaterialDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var material = await _service.UpdateBuildingDefaultMaterialAsync(buildingId, id, dto, cancellationToken);
            return this.Success(material, "Default material updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<BuildingDefaultMaterialDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating default material {Id} for building {BuildingId}", id, buildingId);
            return this.InternalServerError<BuildingDefaultMaterialDto>($"Failed to update default material: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a default material
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteBuildingDefaultMaterial(
        [FromRoute] Guid buildingId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _service.DeleteBuildingDefaultMaterialAsync(buildingId, id, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting default material {Id} for building {BuildingId}", id, buildingId);
            return this.InternalServerError($"Failed to delete default material: {ex.Message}");
        }
    }

    /// <summary>
    /// Get summary of default materials for dashboard
    /// </summary>
    [HttpGet("/api/buildings/default-materials/summary")]
    [ProducesResponseType(typeof(ApiResponse<BuildingDefaultMaterialsSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BuildingDefaultMaterialsSummaryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingDefaultMaterialsSummaryDto>>> GetDefaultMaterialsSummary(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = await _service.GetDefaultMaterialsSummaryAsync(cancellationToken);
            return this.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default materials summary");
            return this.InternalServerError<BuildingDefaultMaterialsSummaryDto>($"Failed to get summary: {ex.Message}");
        }
    }
}
