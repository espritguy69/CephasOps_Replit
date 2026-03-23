using CephasOps.Application.Admin.DTOs;

namespace CephasOps.Application.Admin.Services;

/// <summary>
/// Admin-only user management: list, create, update, activate/deactivate, change roles, reset password.
/// </summary>
public interface IAdminUserService
{
    /// <summary>
    /// Get paged list of users with optional search and filters. Not department-scoped (admin sees all).
    /// </summary>
    Task<AdminUserListResultDto> ListAsync(
        int page,
        int pageSize,
        string? search,
        string? roleName,
        bool? isActive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get single user detail by id.
    /// </summary>
    Task<AdminUserDetailDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all role names for dropdowns.
    /// </summary>
    Task<List<string>> GetRoleNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new user. Validates duplicate email. Returns created user id.
    /// </summary>
    /// <param name="request">User creation data.</param>
    /// <param name="actorUserId">Current user id for audit (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Guid> CreateAsync(CreateAdminUserRequestDto request, Guid? actorUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user profile and roles/departments. Validates duplicate email (excluding self).
    /// </summary>
    /// <param name="userId">User to update.</param>
    /// <param name="request">Updated profile and roles.</param>
    /// <param name="actorUserId">Current user id for audit (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(Guid userId, UpdateAdminUserRequestDto request, Guid? actorUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set user active/inactive. Fails if deactivating self or last active admin.
    /// </summary>
    Task SetActiveAsync(Guid userId, bool isActive, Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set user roles. Fails if removing last SuperAdmin/Admin.
    /// </summary>
    Task SetRolesAsync(Guid userId, List<string> roleNames, Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset user password (admin sets new password). Password must be hashed by implementation.
    /// </summary>
    /// <param name="userId">User whose password to reset.</param>
    /// <param name="request">New password (to be hashed).</param>
    /// <param name="actorUserId">Current user id for audit (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ResetPasswordAsync(Guid userId, AdminResetPasswordRequestDto request, Guid? actorUserId = null, CancellationToken cancellationToken = default);
}
