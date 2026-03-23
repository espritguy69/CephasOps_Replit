using CephasOps.Application.Buildings.DTOs;
using CephasOps.Application.Buildings.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// SplitterType management endpoints
/// </summary>
[ApiController]
[Route("api/splitter-types")]
[Authorize]
public class SplitterTypesController : ControllerBase
{
    private readonly ISplitterTypeService _splitterTypeService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<SplitterTypesController> _logger;

    public SplitterTypesController(
        ISplitterTypeService splitterTypeService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<SplitterTypesController> logger)
    {
        _splitterTypeService = splitterTypeService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    /// <summary>
    /// Get splitter types list
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SplitterTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<SplitterTypeDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<SplitterTypeDto>>>> GetSplitterTypes(
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
            return this.Error<List<SplitterTypeDto>>("You do not have access to this department", 403);
        }

        try
        {
            var splitterTypes = await _splitterTypeService.GetSplitterTypesAsync(companyId, departmentScope, isActive, cancellationToken);
            return this.Success(splitterTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting splitter types");
            return this.InternalServerError<List<SplitterTypeDto>>($"Failed to get splitter types: {ex.Message}");
        }
    }

    /// <summary>
    /// Get splitter type by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SplitterTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SplitterTypeDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SplitterTypeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SplitterTypeDto>>> GetSplitterType(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var splitterType = await _splitterTypeService.GetSplitterTypeByIdAsync(id, companyId, cancellationToken);
            if (splitterType == null)
            {
                return this.NotFound<SplitterTypeDto>($"SplitterType with ID {id} not found");
            }
            return this.Success(splitterType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting splitter type {SplitterTypeId}", id);
            return this.InternalServerError<SplitterTypeDto>($"Failed to get splitter type: {ex.Message}");
        }
    }

    /// <summary>
    /// Create new splitter type
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SplitterTypeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SplitterTypeDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SplitterTypeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SplitterTypeDto>>> CreateSplitterType(
        [FromBody] CreateSplitterTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var splitterType = await _splitterTypeService.CreateSplitterTypeAsync(dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetSplitterType), new { id = splitterType.Id }, splitterType, "Splitter type created successfully.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Duplicate splitter type detected");
            return this.BadRequest<SplitterTypeDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating splitter type");
            return this.InternalServerError<SplitterTypeDto>($"Failed to create splitter type: {ex.Message}");
        }
    }

    /// <summary>
    /// Update splitter type
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SplitterTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SplitterTypeDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SplitterTypeDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SplitterTypeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SplitterTypeDto>>> UpdateSplitterType(
        Guid id,
        [FromBody] UpdateSplitterTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var splitterType = await _splitterTypeService.UpdateSplitterTypeAsync(id, dto, companyId, cancellationToken);
            return this.Success(splitterType, "Splitter type updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<SplitterTypeDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Duplicate splitter type detected during update");
            return this.BadRequest<SplitterTypeDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating splitter type {SplitterTypeId}", id);
            return this.InternalServerError<SplitterTypeDto>($"Failed to update splitter type: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete splitter type
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteSplitterType(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            await _splitterTypeService.DeleteSplitterTypeAsync(id, companyId, cancellationToken);
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
            _logger.LogError(ex, "Error deleting splitter type {SplitterTypeId}", id);
            return this.InternalServerError($"Failed to delete splitter type: {ex.Message}");
        }
    }
}
