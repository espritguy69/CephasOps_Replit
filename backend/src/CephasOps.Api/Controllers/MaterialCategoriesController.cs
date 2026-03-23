using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Inventory.DTOs;
using CephasOps.Application.Inventory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Material Categories API Controller
/// </summary>
[Authorize]
[ApiController]
[Route("api/settings/material-categories")]
public class MaterialCategoriesController : ControllerBase
{
    private readonly IMaterialCategoryService _service;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<MaterialCategoriesController> _logger;

    public MaterialCategoriesController(
        IMaterialCategoryService service,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<MaterialCategoriesController> logger)
    {
        _service = service;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all material categories
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<MaterialCategoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<MaterialCategoryDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<MaterialCategoryDto>>>> GetAll(
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var companyId = _tenantProvider.CurrentTenantId;
            var categories = await _service.GetMaterialCategoriesAsync(companyId, isActive, cancellationToken);
            return this.Success(categories, $"Retrieved {categories.Count} material categories");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting material categories");
            return this.Error<List<MaterialCategoryDto>>("Error retrieving material categories", 500);
        }
    }

    /// <summary>
    /// Get material category by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<MaterialCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MaterialCategoryDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<MaterialCategoryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MaterialCategoryDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var companyId = _tenantProvider.CurrentTenantId;
            var category = await _service.GetMaterialCategoryByIdAsync(id, companyId, cancellationToken);
            
            if (category == null)
            {
                return this.NotFound<MaterialCategoryDto>($"Material category with ID {id} not found");
            }

            return this.Success(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting material category {CategoryId}", id);
            return this.Error<MaterialCategoryDto>("Error retrieving material category", 500);
        }
    }

    /// <summary>
    /// Create a new material category
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MaterialCategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<MaterialCategoryDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MaterialCategoryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MaterialCategoryDto>>> Create(
        [FromBody] CreateMaterialCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var companyId = _tenantProvider.CurrentTenantId;
            var category = await _service.CreateMaterialCategoryAsync(dto, companyId, cancellationToken);
            
            return CreatedAtAction(
                nameof(GetById),
                new { id = category.Id },
                ApiResponse<MaterialCategoryDto>.SuccessResponse(category, "Material category created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return this.Error<MaterialCategoryDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating material category");
            return this.Error<MaterialCategoryDto>("Error creating material category", 500);
        }
    }

    /// <summary>
    /// Update an existing material category
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<MaterialCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MaterialCategoryDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<MaterialCategoryDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MaterialCategoryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MaterialCategoryDto>>> Update(
        Guid id,
        [FromBody] UpdateMaterialCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var companyId = _tenantProvider.CurrentTenantId;
            var category = await _service.UpdateMaterialCategoryAsync(id, dto, companyId, cancellationToken);
            
            return this.Success(category, "Material category updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<MaterialCategoryDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.Error<MaterialCategoryDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating material category {CategoryId}", id);
            return this.Error<MaterialCategoryDto>("Error updating material category", 500);
        }
    }

    /// <summary>
    /// Delete a material category
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var companyId = _tenantProvider.CurrentTenantId;
            await _service.DeleteMaterialCategoryAsync(id, companyId, cancellationToken);
            
            return this.StatusCode(204, ApiResponse.SuccessResponse("Material category deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(400, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting material category {CategoryId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse("Error deleting material category"));
        }
    }
}

