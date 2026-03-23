using CephasOps.Application.Auth.DTOs;

namespace CephasOps.Application.Auth.Services;

/// <summary>
/// Authentication service interface
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticate user and return JWT tokens
    /// </summary>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current user information
    /// </summary>
    Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Change password for the current user (authenticated). Validates current password; sets MustChangePassword = false.
    /// </summary>
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Change password when login is blocked by MustChangePassword (no token). Validates email + current password and that user has MustChangePassword.
    /// </summary>
    Task ChangePasswordRequiredAsync(string email, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Request password reset. If email exists and user is active, creates token and optionally sends email. Always returns without error to avoid enumeration.
    /// </summary>
    Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset password using one-time token. Validates token, updates password, clears lockout, revokes refresh tokens.
    /// </summary>
    Task ResetPasswordWithTokenAsync(string token, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create tokens for a user (impersonation). SuperAdmin only. Audit-logged. Short-lived token.
    /// </summary>
    Task<LoginResponseDto> CreateTokenForImpersonationAsync(Guid targetUserId, Guid requestedByUserId, CancellationToken cancellationToken = default);
}

