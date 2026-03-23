using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using CephasOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// VIP groups management endpoints
/// </summary>
[ApiController]
[Route("api/vip-groups")]
[Authorize]
public class VipGroupsController : ControllerBase
{
    private readonly IVipGroupService _vipGroupService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<VipGroupsController> _logger;

    public VipGroupsController(
        IVipGroupService vipGroupService,
        ICurrentUserService currentUserService,
        ILogger<VipGroupsController> logger)
    {
        _vipGroupService = vipGroupService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get all VIP groups
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of VIP groups sorted by priority</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<VipGroupDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<VipGroupDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<VipGroupDto>>>> GetAll(CancellationToken cancellationToken = default)
    {
        try
        {
            var groups = await _vipGroupService.GetAllAsync(cancellationToken);
            return this.Success(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting VIP groups");
            return this.InternalServerError<List<VipGroupDto>>($"Failed to get VIP groups: {ex.Message}");
        }
    }

    /// <summary>
    /// Get VIP group by ID
    /// </summary>
    /// <param name="id">Group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VIP group details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<VipGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VipGroupDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<VipGroupDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<VipGroupDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var group = await _vipGroupService.GetByIdAsync(id, cancellationToken);
            if (group == null)
            {
                return this.NotFound<VipGroupDto>($"VIP group with ID {id} not found");
            }
            return this.Success(group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting VIP group: {GroupId}", id);
            return this.InternalServerError<VipGroupDto>($"Failed to get VIP group: {ex.Message}");
        }
    }

    /// <summary>
    /// Get VIP group by code
    /// </summary>
    /// <param name="code">Group code (e.g., PROCUREMENT_VIP)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VIP group details</returns>
    [HttpGet("by-code/{code}")]
    [ProducesResponseType(typeof(ApiResponse<VipGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VipGroupDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<VipGroupDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<VipGroupDto>>> GetByCode(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var group = await _vipGroupService.GetByCodeAsync(code, cancellationToken);
            if (group == null)
            {
                return this.NotFound<VipGroupDto>($"VIP group with code '{code}' not found");
            }
            return this.Success(group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting VIP group by code: {Code}", code);
            return this.InternalServerError<VipGroupDto>($"Failed to get VIP group: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a new VIP group
    /// </summary>
    /// <param name="dto">VIP group data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created VIP group</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VipGroupDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<VipGroupDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<VipGroupDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<VipGroupDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<VipGroupDto>>> Create(
        [FromBody] CreateVipGroupDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<VipGroupDto>("User context required");
        }

        try
        {
            var group = await _vipGroupService.CreateAsync(dto, userId.Value, cancellationToken);
            return this.CreatedAtAction(nameof(GetById), new { id = group.Id }, group, "VIP group created successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<VipGroupDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating VIP group");
            return this.InternalServerError<VipGroupDto>($"Failed to create VIP group: {ex.Message}");
        }
    }

    /// <summary>
    /// Update an existing VIP group
    /// </summary>
    /// <param name="id">Group ID</param>
    /// <param name="dto">Updated VIP group data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated VIP group</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<VipGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VipGroupDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<VipGroupDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<VipGroupDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<VipGroupDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<VipGroupDto>>> Update(
        Guid id,
        [FromBody] UpdateVipGroupDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<VipGroupDto>("User context required");
        }

        try
        {
            var group = await _vipGroupService.UpdateAsync(id, dto, userId.Value, cancellationToken);
            return this.Success(group, "VIP group updated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<VipGroupDto>($"VIP group with ID {id} not found");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<VipGroupDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating VIP group: {GroupId}", id);
            return this.InternalServerError<VipGroupDto>($"Failed to update VIP group: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a VIP group
    /// </summary>
    /// <param name="id">Group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _vipGroupService.DeleteAsync(id, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound($"VIP group with ID {id} not found");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting VIP group: {GroupId}", id);
            return this.InternalServerError($"Failed to delete VIP group: {ex.Message}");
        }
    }
}
