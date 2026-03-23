using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Companies.DTOs;
using CephasOps.Application.Companies.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Vertical management endpoints
/// </summary>
[ApiController]
[Route("api/verticals")]
[Authorize]
public class VerticalsController : ControllerBase
{
    private readonly IVerticalService _verticalService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<VerticalsController> _logger;

    public VerticalsController(
        IVerticalService verticalService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<VerticalsController> logger)
    {
        _verticalService = verticalService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get verticals list
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<VerticalDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<VerticalDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<VerticalDto>>>> GetVerticals(
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId is always null (SuperAdmin/global scope)
        var companyId = (Guid?)null;

        try
        {
            var verticals = await _verticalService.GetVerticalsAsync(companyId, isActive, cancellationToken);
            return this.Success(verticals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting verticals");
            return this.InternalServerError<List<VerticalDto>>($"Failed to get verticals: {ex.Message}");
        }
    }

    /// <summary>
    /// Get vertical by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<VerticalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VerticalDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<VerticalDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<VerticalDto>>> GetVertical(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var vertical = await _verticalService.GetVerticalByIdAsync(id, companyId, cancellationToken);
            if (vertical == null)
            {
                return this.NotFound<VerticalDto>($"Vertical with ID {id} not found");
            }

            return this.Success(vertical);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vertical {VerticalId}", id);
            return this.InternalServerError<VerticalDto>($"Failed to get vertical: {ex.Message}");
        }
    }

    /// <summary>
    /// Create vertical
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VerticalDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<VerticalDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<VerticalDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<VerticalDto>>> CreateVertical(
        [FromBody] CreateVerticalDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return this.BadRequest<VerticalDto>("Invalid model state");
        }

        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var vertical = await _verticalService.CreateVerticalAsync(dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetVertical), new { id = vertical.Id }, vertical, "Vertical created successfully.");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid vertical payload");
            return this.BadRequest<VerticalDto>($"Invalid vertical data: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vertical");
            return this.InternalServerError<VerticalDto>($"Failed to create vertical: {ex.Message}");
        }
    }

    /// <summary>
    /// Update vertical
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<VerticalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VerticalDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<VerticalDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<VerticalDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<VerticalDto>>> UpdateVertical(
        Guid id,
        [FromBody] UpdateVerticalDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var vertical = await _verticalService.UpdateVerticalAsync(id, dto, companyId, cancellationToken);
            return this.Success(vertical, "Vertical updated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<VerticalDto>($"Vertical with ID {id} not found");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid vertical update payload for {VerticalId}", id);
            return this.BadRequest<VerticalDto>($"Invalid vertical data: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vertical {VerticalId}", id);
            return this.InternalServerError<VerticalDto>($"Failed to update vertical: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete vertical
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteVertical(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            await _verticalService.DeleteVerticalAsync(id, companyId, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound($"Vertical with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vertical {VerticalId}", id);
            return this.InternalServerError($"Failed to delete vertical: {ex.Message}");
        }
    }
}
