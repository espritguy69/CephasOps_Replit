using CephasOps.Application.Auth;
using CephasOps.Application.Auth.DTOs;
using CephasOps.Application.Auth.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Subscription;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Authentication endpoints
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ICurrentUserService currentUserService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Login and retrieve JWT tokens
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT tokens and user information</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), 423)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _authService.LoginAsync(request, cancellationToken);
            return this.Success(response, "Login successful");
        }
        catch (RequiresPasswordChangeException ex)
        {
            _logger.LogInformation("Login blocked (must change password) for email: {Email}", request.Email);
            return StatusCode(403, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Data = new { requiresPasswordChange = true },
                Errors = new List<string> { ex.Message }
            });
        }
        catch (AccountLockedException ex)
        {
            _logger.LogInformation("Login blocked (account locked) for email: {Email}", request.Email);
            return StatusCode(423, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Data = new { accountLocked = true, lockoutEndUtc = ex.LockoutEndUtc },
                Errors = new List<string> { ex.Message }
            });
        }
        catch (TenantAccessDeniedException ex)
        {
            _logger.LogInformation("Login blocked (tenant access denied) for email: {Email}, reason: {Reason}", request.Email, ex.DenialReason);
            return StatusCode(403, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Data = new { denialReason = ex.DenialReason ?? "tenant_suspended" },
                Errors = new List<string> { ex.Message }
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not assigned to a company"))
        {
            _logger.LogInformation("Login blocked (no company assignment) for email: {Email}", request.Email);
            return StatusCode(403, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Login failed for email: {Email}", request.Email);
            return this.Error<LoginResponseDto>("Invalid email or password", 401);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return this.Error<LoginResponseDto>($"Login failed: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New JWT tokens</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Refresh(
        [FromBody] RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request, cancellationToken);
            return this.Success(response, "Token refreshed successfully");
        }
        catch (RequiresPasswordChangeException ex)
        {
            _logger.LogInformation("Refresh blocked (must change password)");
            return StatusCode(403, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Data = new { requiresPasswordChange = true },
                Errors = new List<string> { ex.Message }
            });
        }
        catch (TenantAccessDeniedException ex)
        {
            _logger.LogInformation("Refresh blocked (tenant access denied), reason: {Reason}", ex.DenialReason);
            return StatusCode(403, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Data = new { denialReason = ex.DenialReason ?? "tenant_suspended" },
                Errors = new List<string> { ex.Message }
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not assigned to a company"))
        {
            _logger.LogInformation("Refresh blocked (no company assignment)");
            return StatusCode(403, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Token refresh failed");
            return this.Error<LoginResponseDto>("Invalid refresh token", 401);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return this.Error<LoginResponseDto>($"Token refresh failed: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId == null)
        {
            return this.Error<UserDto>("User not authenticated", 401);
        }

        try
        {
            var user = await _authService.GetCurrentUserAsync(_currentUserService.UserId.Value, cancellationToken);
            return this.Success(user);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Get current user failed");
            return this.Error<UserDto>("User not found", 401);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return this.Error<UserDto>($"Failed to get user information: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Change password (authenticated user). Requires current password.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        [FromBody] ChangePasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return this.Unauthorized<object>("Not authenticated.");

        try
        {
            await _authService.ChangePasswordAsync(userId.Value, request.CurrentPassword, request.NewPassword, cancellationToken);
            return this.Success<object>(new { }, "Password changed.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not assigned to a company"))
        {
            return StatusCode(403, new ApiResponse<object> { Success = false, Message = ex.Message, Errors = new List<string> { ex.Message } });
        }
        catch (UnauthorizedAccessException ex)
        {
            return this.Unauthorized<object>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<object>(ex.Message);
        }
    }

    /// <summary>
    /// Change password when login is blocked by MustChangePassword (no token). Requires email and current password.
    /// </summary>
    [HttpPost("change-password-required")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> ChangePasswordRequired(
        [FromBody] ChangePasswordRequiredRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _authService.ChangePasswordRequiredAsync(request.Email, request.CurrentPassword, request.NewPassword, cancellationToken);
            return this.Success<object>(new { }, "Password changed. You can now sign in.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not assigned to a company"))
        {
            return StatusCode(403, new ApiResponse<object> { Success = false, Message = ex.Message, Errors = new List<string> { ex.Message } });
        }
        catch (UnauthorizedAccessException ex)
        {
            return this.Unauthorized<object>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<object>(ex.Message);
        }
    }

    /// <summary>
    /// Request a password reset email. Always returns success to avoid account enumeration.
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _authService.ForgotPasswordAsync(request.Email ?? "", cancellationToken);
            return this.Success<object>(new { }, "If an account exists for that email, you will receive a password reset link.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot-password");
            return this.Error<object>("An error occurred. Please try again later.", 500);
        }
    }

    /// <summary>
    /// Reset password using the token from the email link.
    /// </summary>
    [HttpPost("reset-password-with-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> ResetPasswordWithToken(
        [FromBody] ResetPasswordWithTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request?.Token))
        {
            return this.BadRequest<object>("Reset token is required.");
        }
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            return this.BadRequest<object>("New password must be at least 6 characters.");
        }
        if (request.NewPassword != request.ConfirmPassword)
        {
            return this.BadRequest<object>("New password and confirmation do not match.");
        }

        try
        {
            await _authService.ResetPasswordWithTokenAsync(request.Token, request.NewPassword, cancellationToken);
            return this.Success<object>(new { }, "Password has been reset. You can now sign in.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not assigned to a company"))
        {
            return StatusCode(403, new ApiResponse<object> { Success = false, Message = ex.Message, Errors = new List<string> { ex.Message } });
        }
        catch (UnauthorizedAccessException ex)
        {
            return this.Unauthorized<object>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<object>(ex.Message);
        }
    }

    // Switch company endpoint removed - multi-company feature disabled
    // [HttpPost("switch-company/{companyId}")]
}

