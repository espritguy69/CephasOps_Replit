using CephasOps.Application.Buildings.DTOs;
using CephasOps.Application.Buildings.Services;
using CephasOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Splitters endpoints
/// </summary>
[ApiController]
[Route("api/splitters")]
[Authorize]
public class SplittersController : ControllerBase
{
    private readonly ISplitterService _splitterService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SplittersController> _logger;

    public SplittersController(
        ISplitterService splitterService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<SplittersController> logger)
    {
        _splitterService = splitterService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all splitters for the current company
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SplitterDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<SplitterDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<SplitterDto>>>> GetSplitters(
        [FromQuery] Guid? buildingId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var splitters = await _splitterService.GetSplittersAsync(companyId, buildingId, isActive, cancellationToken);
            return this.Success(splitters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting splitters");
            return this.InternalServerError<List<SplitterDto>>($"Failed to get splitters: {ex.Message}");
        }
    }

    /// <summary>
    /// Get splitter by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SplitterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SplitterDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SplitterDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SplitterDto>>> GetSplitter(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var splitter = await _splitterService.GetSplitterByIdAsync(id, companyId, cancellationToken);
            if (splitter == null)
            {
                return this.NotFound<SplitterDto>($"Splitter with ID {id} not found");
            }

            return this.Success(splitter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting splitter: {SplitterId}", id);
            return this.InternalServerError<SplitterDto>($"Failed to get splitter: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a new splitter
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SplitterDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SplitterDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SplitterDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SplitterDto>>> CreateSplitter(
        [FromBody] CreateSplitterDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = dto.CompanyId ?? _tenantProvider.CurrentTenantId;

        try
        {
            var splitter = await _splitterService.CreateSplitterAsync(dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetSplitter), new { id = splitter.Id }, splitter, "Splitter created successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<SplitterDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating splitter");
            return this.InternalServerError<SplitterDto>($"Failed to create splitter: {ex.Message}");
        }
    }

    /// <summary>
    /// Update an existing splitter
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SplitterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SplitterDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SplitterDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SplitterDto>>> UpdateSplitter(
        Guid id,
        [FromBody] UpdateSplitterDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var splitter = await _splitterService.UpdateSplitterAsync(id, dto, companyId, cancellationToken);
            return this.Success(splitter, "Splitter updated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<SplitterDto>($"Splitter with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating splitter: {SplitterId}", id);
            return this.InternalServerError<SplitterDto>($"Failed to update splitter: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a splitter
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteSplitter(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            await _splitterService.DeleteSplitterAsync(id, companyId, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound($"Splitter with ID {id} not found");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting splitter: {SplitterId}", id);
            return this.InternalServerError($"Failed to delete splitter: {ex.Message}");
        }
    }

    /// <summary>
    /// Update a splitter port
    /// </summary>
    [HttpPut("{splitterId}/ports/{portId}")]
    [ProducesResponseType(typeof(ApiResponse<SplitterPortDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SplitterPortDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SplitterPortDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SplitterPortDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SplitterPortDto>>> UpdateSplitterPort(
        Guid splitterId,
        Guid portId,
        [FromBody] UpdateSplitterPortDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var port = await _splitterService.UpdateSplitterPortAsync(portId, dto, companyId, cancellationToken);
            return this.Success(port, "Splitter port updated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<SplitterPortDto>($"Splitter port with ID {portId} not found");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<SplitterPortDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating splitter port: {PortId}", portId);
            return this.InternalServerError<SplitterPortDto>($"Failed to update splitter port: {ex.Message}");
        }
    }
}
