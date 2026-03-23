using CephasOps.Application.Pnl.DTOs;
using CephasOps.Application.Pnl.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Domain.Pnl.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// PnL Type management endpoints
/// </summary>
[ApiController]
[Route("api/pnl-types")]
[Authorize]
public class PnlTypesController : ControllerBase
{
    private readonly IPnlTypeService _pnlTypeService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<PnlTypesController> _logger;

    public PnlTypesController(
        IPnlTypeService pnlTypeService,
        ITenantProvider tenantProvider,
        ILogger<PnlTypesController> logger)
    {
        _pnlTypeService = pnlTypeService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get PnL types list (flat)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PnlTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<PnlTypeDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<PnlTypeDto>>>> GetPnlTypes(
        [FromQuery] PnlTypeCategory? category = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var pnlTypes = await _pnlTypeService.GetPnlTypesAsync(companyId, category, isActive, cancellationToken);
            return this.Success(pnlTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PnL types");
            return this.InternalServerError<List<PnlTypeDto>>($"Failed to get PnL types: {ex.Message}");
        }
    }

    /// <summary>
    /// Get PnL types as hierarchical tree
    /// </summary>
    [HttpGet("tree")]
    [ProducesResponseType(typeof(ApiResponse<List<PnlTypeTreeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<PnlTypeTreeDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<PnlTypeTreeDto>>>> GetPnlTypeTree(
        [FromQuery] PnlTypeCategory? category = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var tree = await _pnlTypeService.GetPnlTypeTreeAsync(companyId, category, isActive, cancellationToken);
            return this.Success(tree);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PnL type tree");
            return this.InternalServerError<List<PnlTypeTreeDto>>($"Failed to get PnL type tree: {ex.Message}");
        }
    }

    /// <summary>
    /// Get PnL types that can be used in transactions
    /// </summary>
    [HttpGet("transactional")]
    [ProducesResponseType(typeof(ApiResponse<List<PnlTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<PnlTypeDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<PnlTypeDto>>>> GetTransactionalPnlTypes(
        [FromQuery] PnlTypeCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var pnlTypes = await _pnlTypeService.GetTransactionalPnlTypesAsync(companyId, category, cancellationToken);
            return this.Success(pnlTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactional PnL types");
            return this.InternalServerError<List<PnlTypeDto>>($"Failed to get transactional PnL types: {ex.Message}");
        }
    }

    /// <summary>
    /// Get PnL type by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PnlTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PnlTypeDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PnlTypeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PnlTypeDto>>> GetPnlType(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var pnlType = await _pnlTypeService.GetPnlTypeByIdAsync(id, companyId, cancellationToken);
            if (pnlType == null)
            {
                return this.NotFound<PnlTypeDto>($"PnL Type with ID {id} not found");
            }
            return this.Success(pnlType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PnL type {PnlTypeId}", id);
            return this.InternalServerError<PnlTypeDto>($"Failed to get PnL type: {ex.Message}");
        }
    }

    /// <summary>
    /// Create new PnL type
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PnlTypeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<PnlTypeDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PnlTypeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PnlTypeDto>>> CreatePnlType(
        [FromBody] CreatePnlTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var pnlType = await _pnlTypeService.CreatePnlTypeAsync(dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetPnlType), new { id = pnlType.Id }, pnlType, "PnL type created successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<PnlTypeDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PnL type");
            return this.InternalServerError<PnlTypeDto>($"Failed to create PnL type: {ex.Message}");
        }
    }

    /// <summary>
    /// Update PnL type
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PnlTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PnlTypeDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PnlTypeDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PnlTypeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PnlTypeDto>>> UpdatePnlType(
        Guid id,
        [FromBody] UpdatePnlTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var pnlType = await _pnlTypeService.UpdatePnlTypeAsync(id, dto, companyId, cancellationToken);
            return this.Success(pnlType, "PnL type updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<PnlTypeDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<PnlTypeDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating PnL type {PnlTypeId}", id);
            return this.InternalServerError<PnlTypeDto>($"Failed to update PnL type: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete PnL type
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeletePnlType(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            await _pnlTypeService.DeletePnlTypeAsync(id, companyId, cancellationToken);
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
            _logger.LogError(ex, "Error deleting PnL type {PnlTypeId}", id);
            return this.InternalServerError($"Failed to delete PnL type: {ex.Message}");
        }
    }
}

