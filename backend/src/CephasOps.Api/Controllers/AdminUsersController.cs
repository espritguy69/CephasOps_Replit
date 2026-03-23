using CephasOps.Application.Admin.DTOs;
using CephasOps.Application.Admin.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Admin-only user management: list, create, update, activate/deactivate, roles, reset password.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        IAdminUserService adminUserService,
        ICurrentUserService currentUserService,
        ILogger<AdminUsersController> logger)
    {
        _adminUserService = adminUserService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get paged list of users with optional search and filters.
    /// </summary>
    [HttpGet]
    [RequirePermission(PermissionCatalog.AdminUsersView)]
    [ProducesResponseType(typeof(ApiResponse<AdminUserListResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AdminUserListResultDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<AdminUserListResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AdminUserListResultDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _adminUserService.ListAsync(page, pageSize, search, role, isActive, cancellationToken);
            return this.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing admin users");
            return this.InternalServerError<AdminUserListResultDto>(ex.Message);
        }
    }

    /// <summary>
    /// Get all role names for dropdowns.
    /// </summary>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetRoles(CancellationToken cancellationToken = default)
    {
        try
        {
            var roles = await _adminUserService.GetRoleNamesAsync(cancellationToken);
            return this.Success(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles");
            return this.InternalServerError<List<string>>(ex.Message);
        }
    }

    /// <summary>
    /// Get single user detail.
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionCatalog.AdminUsersView)]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDetailDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AdminUserDetailDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _adminUserService.GetByIdAsync(id, cancellationToken);
            if (user == null)
                return this.NotFound<AdminUserDetailDto>("User not found");
            return this.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return this.InternalServerError<AdminUserDetailDto>(ex.Message);
        }
    }

    /// <summary>
    /// Create a new user.
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionCatalog.AdminUsersEdit)]
    [ProducesResponseType(typeof(ApiResponse<CreateAdminUserResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateAdminUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorUserId = _currentUserService.UserId;
            var id = await _adminUserService.CreateAsync(request, actorUserId, cancellationToken);
            var result = new CreateAdminUserResultDto { Id = id };
            return this.CreatedAtAction(nameof(GetById), new { id }, (object)result, "User created.");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<object>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return this.InternalServerError<object>(ex.Message);
        }
    }

    /// <summary>
    /// Update user profile and roles/departments.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid id,
        [FromBody] UpdateAdminUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorUserId = _currentUserService.UserId;
            await _adminUserService.UpdateAsync(id, request, actorUserId, cancellationToken);
            return this.Success<object>(new { }, "User updated.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return this.NotFound<object>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<object>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return this.InternalServerError<object>(ex.Message);
        }
    }

    /// <summary>
    /// Set user active or inactive.
    /// </summary>
    [HttpPatch("{id:guid}/active")]
    [RequirePermission(PermissionCatalog.AdminUsersEdit)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> SetActive(
        Guid id,
        [FromBody] SetUserActiveRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
            return this.Unauthorized<object>("Not authenticated.");

        try
        {
            await _adminUserService.SetActiveAsync(id, request.IsActive, currentUserId.Value, cancellationToken);
            return this.Success<object>(new { }, request.IsActive ? "User activated." : "User deactivated.");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<object>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting active for user {UserId}", id);
            return this.InternalServerError<object>(ex.Message);
        }
    }

    /// <summary>
    /// Set user roles.
    /// </summary>
    [HttpPut("{id:guid}/roles")]
    [RequirePermission(PermissionCatalog.AdminUsersEdit)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> SetRoles(
        Guid id,
        [FromBody] SetUserRolesRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
            return this.Unauthorized<object>("Not authenticated.");

        try
        {
            await _adminUserService.SetRolesAsync(id, request.RoleNames ?? new List<string>(), currentUserId.Value, cancellationToken);
            return this.Success<object>(new { }, "Roles updated.");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<object>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting roles for user {UserId}", id);
            return this.InternalServerError<object>(ex.Message);
        }
    }

    /// <summary>
    /// Reset user password (admin sets new temporary password).
    /// </summary>
    [HttpPost("{id:guid}/reset-password")]
    [RequirePermission(PermissionCatalog.AdminUsersResetPassword)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(
        Guid id,
        [FromBody] AdminResetPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorUserId = _currentUserService.UserId;
            await _adminUserService.ResetPasswordAsync(id, request, actorUserId, cancellationToken);
            return this.Success<object>(new { }, "Password has been reset.");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<object>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", id);
            return this.InternalServerError<object>(ex.Message);
        }
    }
}
