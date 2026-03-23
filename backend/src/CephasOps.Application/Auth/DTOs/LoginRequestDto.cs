namespace CephasOps.Application.Auth.DTOs;

/// <summary>
/// Login request DTO
/// </summary>
public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Login response DTO
/// </summary>
public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}

/// <summary>
/// Refresh token request DTO
/// </summary>
public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// User information DTO
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public List<string> Roles { get; set; } = new();
    /// <summary>
    /// Permission names (module.action) the user has via their roles. Used for RBAC v2 UI and guards.
    /// </summary>
    public List<string> Permissions { get; set; } = new();
    /// <summary>
    /// When true, user must change password before using the app (e.g. after admin reset).
    /// </summary>
    public bool MustChangePassword { get; set; }
}

/// <summary>
/// Request to change password (authenticated user).
/// </summary>
public class ChangePasswordRequestDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request to change password when login is blocked by MustChangePassword (no token).
/// </summary>
public class ChangePasswordRequiredRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request for forgot-password. Always returns generic success to avoid enumeration.
/// </summary>
public class ForgotPasswordRequestDto
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Request to reset password using a one-time token (from email link).
/// </summary>
public class ResetPasswordWithTokenRequestDto
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

