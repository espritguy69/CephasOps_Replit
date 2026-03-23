using CephasOps.Application.Assets.DTOs;
using CephasOps.Application.Assets.Services;
using CephasOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Asset Type management endpoints
/// </summary>
[ApiController]
[Route("api/asset-types")]
[Authorize]
public class AssetTypesController : ControllerBase
{
    private readonly IAssetTypeService _assetTypeService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<AssetTypesController> _logger;

    public AssetTypesController(
        IAssetTypeService assetTypeService,
        ITenantProvider tenantProvider,
        ILogger<AssetTypesController> logger)
    {
        _assetTypeService = assetTypeService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get asset types list
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AssetTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<List<AssetTypeDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<AssetTypeDto>>>> GetAssetTypes(
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var assetTypes = await _assetTypeService.GetAssetTypesAsync(companyId, isActive, cancellationToken);
            return this.Success(assetTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting asset types");
            return this.InternalServerError<List<AssetTypeDto>>($"Failed to get asset types: {ex.Message}");
        }
    }

    /// <summary>
    /// Get asset type by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AssetTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<AssetTypeDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AssetTypeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AssetTypeDto>>> GetAssetType(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var assetType = await _assetTypeService.GetAssetTypeByIdAsync(id, companyId, cancellationToken);
            if (assetType == null)
            {
                return this.NotFound<AssetTypeDto>($"Asset Type with ID {id} not found");
            }
            return this.Success(assetType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting asset type {AssetTypeId}", id);
            return this.InternalServerError<AssetTypeDto>($"Failed to get asset type: {ex.Message}");
        }
    }

    /// <summary>
    /// Create new asset type
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AssetTypeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<AssetTypeDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AssetTypeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AssetTypeDto>>> CreateAssetType(
        [FromBody] CreateAssetTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var assetType = await _assetTypeService.CreateAssetTypeAsync(dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetAssetType), new { id = assetType.Id }, assetType, "Asset type created successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<AssetTypeDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating asset type");
            return this.InternalServerError<AssetTypeDto>($"Failed to create asset type: {ex.Message}");
        }
    }

    /// <summary>
    /// Update asset type
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AssetTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<AssetTypeDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AssetTypeDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AssetTypeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AssetTypeDto>>> UpdateAssetType(
        Guid id,
        [FromBody] UpdateAssetTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var assetType = await _assetTypeService.UpdateAssetTypeAsync(id, dto, companyId, cancellationToken);
            return this.Success(assetType, "Asset type updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<AssetTypeDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<AssetTypeDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating asset type {AssetTypeId}", id);
            return this.InternalServerError<AssetTypeDto>($"Failed to update asset type: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete asset type
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteAssetType(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            await _assetTypeService.DeleteAssetTypeAsync(id, companyId, cancellationToken);
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
            _logger.LogError(ex, "Error deleting asset type {AssetTypeId}", id);
            return this.InternalServerError($"Failed to delete asset type: {ex.Message}");
        }
    }
}
