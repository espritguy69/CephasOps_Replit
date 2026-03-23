using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using CephasOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// VIP emails management endpoints
/// </summary>
[ApiController]
[Route("api/vip-emails")]
[Authorize]
public class VipEmailsController : ControllerBase
{
    private readonly IVipEmailService _vipEmailService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<VipEmailsController> _logger;

    public VipEmailsController(
        IVipEmailService vipEmailService,
        ICurrentUserService currentUserService,
        ILogger<VipEmailsController> logger)
    {
        _vipEmailService = vipEmailService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get all VIP emails
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of VIP emails</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<VipEmailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<VipEmailDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<VipEmailDto>>>> GetAll(CancellationToken cancellationToken = default)
    {
        try
        {
            var vipEmails = await _vipEmailService.GetAllAsync(cancellationToken);
            return this.Success(vipEmails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting VIP emails");
            return this.InternalServerError<List<VipEmailDto>>($"Failed to get VIP emails: {ex.Message}");
        }
    }

    /// <summary>
    /// Get VIP email by ID
    /// </summary>
    /// <param name="id">VIP email ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VIP email details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<VipEmailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VipEmailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<VipEmailDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<VipEmailDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var vipEmail = await _vipEmailService.GetByIdAsync(id, cancellationToken);
            if (vipEmail == null)
            {
                return this.NotFound<VipEmailDto>($"VIP email with ID {id} not found");
            }
            return this.Success(vipEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting VIP email: {VipEmailId}", id);
            return this.InternalServerError<VipEmailDto>($"Failed to get VIP email: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a new VIP email entry
    /// </summary>
    /// <param name="dto">VIP email data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created VIP email</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VipEmailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<VipEmailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<VipEmailDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<VipEmailDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<VipEmailDto>>> Create(
        [FromBody] CreateVipEmailDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<VipEmailDto>("User context required");
        }

        // Validate email address
        if (string.IsNullOrWhiteSpace(dto.EmailAddress))
        {
            return this.BadRequest<VipEmailDto>("Email address is required");
        }

        try
        {
            var vipEmail = await _vipEmailService.CreateAsync(dto, userId.Value, cancellationToken);
            return this.CreatedAtAction(nameof(GetById), new { id = vipEmail.Id }, vipEmail, "VIP email created successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<VipEmailDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating VIP email");
            return this.InternalServerError<VipEmailDto>($"Failed to create VIP email: {ex.Message}");
        }
    }

    /// <summary>
    /// Update an existing VIP email entry
    /// </summary>
    /// <param name="id">VIP email ID</param>
    /// <param name="dto">Updated VIP email data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated VIP email</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<VipEmailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VipEmailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<VipEmailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<VipEmailDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<VipEmailDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<VipEmailDto>>> Update(
        Guid id,
        [FromBody] UpdateVipEmailDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<VipEmailDto>("User context required");
        }

        try
        {
            var vipEmail = await _vipEmailService.UpdateAsync(id, dto, userId.Value, cancellationToken);
            return this.Success(vipEmail, "VIP email updated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<VipEmailDto>($"VIP email with ID {id} not found");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<VipEmailDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating VIP email: {VipEmailId}", id);
            return this.InternalServerError<VipEmailDto>($"Failed to update VIP email: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a VIP email entry
    /// </summary>
    /// <param name="id">VIP email ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _vipEmailService.DeleteAsync(id, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound($"VIP email with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting VIP email: {VipEmailId}", id);
            return this.InternalServerError($"Failed to delete VIP email: {ex.Message}");
        }
    }
}
