using CephasOps.Application.Buildings.DTOs;
using CephasOps.Application.Buildings.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// BuildingType management endpoints
/// </summary>
[ApiController]
[Route("api/building-types")]
[Authorize]
public class BuildingTypesController : ControllerBase
{
    private readonly IBuildingTypeService _buildingTypeService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<BuildingTypesController> _logger;

    public BuildingTypesController(
        IBuildingTypeService buildingTypeService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<BuildingTypesController> logger)
    {
        _buildingTypeService = buildingTypeService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    /// <summary>
    /// Get building types list
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<BuildingTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<BuildingTypeDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<BuildingTypeDto>>>> GetBuildingTypes(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        Guid? departmentScope;
        try
        {
            departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return this.Error<List<BuildingTypeDto>>("You do not have access to this department", 403);
        }

        try
        {
            var buildingTypes = await _buildingTypeService.GetBuildingTypesAsync(companyId, departmentScope, isActive, cancellationToken);
            return this.Success(buildingTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting building types");
            return this.InternalServerError<List<BuildingTypeDto>>($"Failed to get building types: {ex.Message}");
        }
    }

    /// <summary>
    /// Get building type by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BuildingTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BuildingTypeDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BuildingTypeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingTypeDto>>> GetBuildingType(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var buildingType = await _buildingTypeService.GetBuildingTypeByIdAsync(id, companyId, cancellationToken);
            if (buildingType == null)
            {
                return this.NotFound<BuildingTypeDto>($"BuildingType with ID {id} not found");
            }
            return this.Success(buildingType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting building type {BuildingTypeId}", id);
            return this.InternalServerError<BuildingTypeDto>($"Failed to get building type: {ex.Message}");
        }
    }

    /// <summary>
    /// Create new building type
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BuildingTypeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<BuildingTypeDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BuildingTypeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingTypeDto>>> CreateBuildingType(
        [FromBody] CreateBuildingTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var buildingType = await _buildingTypeService.CreateBuildingTypeAsync(dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetBuildingType), new { id = buildingType.Id }, buildingType, "Building type created successfully.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Duplicate building type detected");
            return this.BadRequest<BuildingTypeDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating building type");
            return this.InternalServerError<BuildingTypeDto>($"Failed to create building type: {ex.Message}");
        }
    }

    /// <summary>
    /// Update building type
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BuildingTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BuildingTypeDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BuildingTypeDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BuildingTypeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingTypeDto>>> UpdateBuildingType(
        Guid id,
        [FromBody] UpdateBuildingTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var buildingType = await _buildingTypeService.UpdateBuildingTypeAsync(id, dto, companyId, cancellationToken);
            return this.Success(buildingType, "Building type updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<BuildingTypeDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Duplicate building type detected during update");
            return this.BadRequest<BuildingTypeDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating building type {BuildingTypeId}", id);
            return this.InternalServerError<BuildingTypeDto>($"Failed to update building type: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete building type
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteBuildingType(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            await _buildingTypeService.DeleteBuildingTypeAsync(id, companyId, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting building type {BuildingTypeId}", id);
            return this.InternalServerError($"Failed to delete building type: {ex.Message}");
        }
    }
}
