using System.Text.Json;
using CephasOps.Application.Audit.Services;
using CephasOps.Application.Auth.DTOs;
using CephasOps.Application.Auth;
using CephasOps.Application.Authorization;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Subscription;
using CephasOps.Application.Parser.Services;
using CephasOps.Domain.Users.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CephasOps.Application.Auth.Services;

/// <summary>
/// Authentication service implementation
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IPasswordHasher _passwordHasher;
    private readonly LockoutOptions _lockoutOptions;
    private readonly PasswordResetOptions _passwordResetOptions;
    private readonly IEmailSendingService? _emailSendingService;
    private readonly IAuditLogService? _auditLogService;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IUserPermissionProvider? _userPermissionProvider;
    private readonly ISubscriptionAccessService? _subscriptionAccessService;

    public AuthService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IPasswordHasher passwordHasher,
        IOptions<LockoutOptions> lockoutOptions,
        IOptions<PasswordResetOptions>? passwordResetOptions = null,
        IEmailSendingService? emailSendingService = null,
        IAuditLogService? auditLogService = null,
        IHttpContextAccessor? httpContextAccessor = null,
        IUserPermissionProvider? userPermissionProvider = null,
        ISubscriptionAccessService? subscriptionAccessService = null)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _passwordHasher = passwordHasher;
        _lockoutOptions = lockoutOptions?.Value ?? new LockoutOptions();
        _passwordResetOptions = passwordResetOptions?.Value ?? new PasswordResetOptions();
        _emailSendingService = emailSendingService;
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
        _userPermissionProvider = userPermissionProvider;
        _subscriptionAccessService = subscriptionAccessService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login attempt failed: User not found for email {Email}", request.Email);
            await LogAuthEventAsync(AuthEventTypes.LoginFailed, null, cancellationToken);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt failed: User {Email} is not active", request.Email);
            await LogAuthEventAsync(AuthEventTypes.LoginFailed, user.Id, cancellationToken);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Tenant scope or platform bypass required before any User write. Legacy users (no company) use bypass.
        var useBypass = !user.CompanyId.HasValue || user.CompanyId.Value == Guid.Empty;
        Guid? previousTenantId = null;
        if (useBypass)
        {
            _logger.LogInformation("Login for legacy user {Email} (no company) via platform bypass", request.Email);
            TenantSafetyGuard.EnterPlatformBypass();
        }
        else
        {
            previousTenantId = TenantScope.CurrentTenantId;
            TenantScope.CurrentTenantId = user.CompanyId;
        }
        try
        {
        // Verify password hash
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            _logger.LogWarning("Login attempt failed: User {Email} has no password hash", request.Email);
            await LogAuthEventAsync(AuthEventTypes.LoginFailed, user.Id, cancellationToken);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Account lockout (v1.3 Phase C): block if currently locked
        var nowUtc = DateTime.UtcNow;
        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > nowUtc)
        {
            _logger.LogWarning("Login blocked: User {Email} is locked until {LockoutEnd}", request.Email, user.LockoutEndUtc);
            await LogAuthEventAsync(AuthEventTypes.AccountLocked, user.Id, cancellationToken);
            throw new AccountLockedException(user.LockoutEndUtc);
        }

        var passwordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!passwordValid)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= _lockoutOptions.MaxFailedAttempts)
            {
                user.LockoutEndUtc = nowUtc.AddMinutes(_lockoutOptions.LockoutMinutes);
                _logger.LogWarning("User {Email} locked after {Count} failed attempts until {LockoutEnd}", request.Email, user.FailedLoginAttempts, user.LockoutEndUtc);
            }
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Login attempt failed: Invalid password for user {Email}", request.Email);
            await LogAuthEventAsync(AuthEventTypes.LoginFailed, user.Id, cancellationToken);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (user.MustChangePassword)
        {
            _logger.LogInformation("Login blocked: User {Email} must change password", request.Email);
            throw new RequiresPasswordChangeException();
        }

        if (_passwordHasher.NeedsRehash(user.PasswordHash))
        {
            user.PasswordHash = _passwordHasher.HashPassword(request.Password);
            _logger.LogInformation("Rehashed legacy password to modern format for user {Email}", user.Email);
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        user.LastLoginAtUtc = DateTime.UtcNow;

        var userId = user.Id;
        var userDto = await GetCurrentUserAsync(userId, cancellationToken);
        var companyId = await ResolveUserCompanyIdAsync(userId, cancellationToken);
        if (_subscriptionAccessService != null)
        {
            var access = await _subscriptionAccessService.GetAccessForCompanyAsync(companyId, cancellationToken);
            if (!access.Allowed)
            {
                _logger.LogWarning("Login blocked: tenant access denied for user {Email}, reason {Reason}", request.Email, access.DenialReason);
                await LogAuthEventAsync(AuthEventTypes.LoginFailed, userId, cancellationToken);
                throw new TenantAccessDeniedException(access.DenialReason);
            }
        }
        var tokenResult = GenerateJwtToken(userId, user.Email, companyId, userDto.Roles);
        var accessToken = tokenResult.Token;
        var expiresAt = tokenResult.ExpiresAt;
        var refreshTokenValue = GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshTokenValue);

        var (createdFromIp, userAgent) = GetIpAndUserAgent();
        // Store refresh token in database
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(30), // Refresh tokens valid for 30 days
            CreatedAt = DateTime.UtcNow,
            CreatedFromIp = createdFromIp,
            UserAgent = userAgent
        };

        // Revoke old refresh tokens for this user
        var oldTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
        
        foreach (var oldToken in oldTokens)
        {
            oldToken.IsRevoked = true;
            oldToken.RevokedAt = DateTime.UtcNow;
        }

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        await LogAuthEventAsync(AuthEventTypes.LoginSuccess, user.Id, cancellationToken);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = expiresAt,
            User = userDto
        };
        }
        finally
        {
            if (useBypass)
                TenantSafetyGuard.ExitPlatformBypass();
            else
                TenantScope.CurrentTenantId = previousTenantId;
        }
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        // Hash the provided refresh token to compare with stored hash
        var refreshTokenHash = HashToken(request.RefreshToken);

        // Find the refresh token and user without tenant filter (request may have no tenant context)
        var storedToken = await _context.RefreshTokens
            .IgnoreQueryFilters()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == refreshTokenHash && !rt.IsRevoked, cancellationToken);

        if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token validation failed: Token not found or expired");
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        if (storedToken.User == null || !storedToken.User.IsActive)
        {
            _logger.LogWarning("Refresh token validation failed: User not found or inactive");
            throw new UnauthorizedAccessException("User not found or inactive");
        }

        if (storedToken.User.MustChangePassword)
        {
            _logger.LogWarning("Refresh blocked: User must change password");
            throw new RequiresPasswordChangeException();
        }

        if (!storedToken.User.CompanyId.HasValue || storedToken.User.CompanyId.Value == Guid.Empty)
        {
            _logger.LogWarning("Refresh blocked: User has no company assignment");
            throw new InvalidOperationException("User account is not assigned to a company. Please contact your administrator.");
        }
        var previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = storedToken.User.CompanyId;
        try
        {
        // Revoke the old refresh token
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;

        // Generate new tokens
        var userDto = await GetCurrentUserAsync(storedToken.UserId, cancellationToken);
        var companyId = await ResolveUserCompanyIdAsync(storedToken.UserId, cancellationToken);
        if (_subscriptionAccessService != null)
        {
            var access = await _subscriptionAccessService.GetAccessForCompanyAsync(companyId, cancellationToken);
            if (!access.Allowed)
            {
                _logger.LogWarning("Refresh blocked: tenant access denied for user {UserId}, reason {Reason}", storedToken.UserId, access.DenialReason);
                throw new TenantAccessDeniedException(access.DenialReason);
            }
        }
        var tokenResult = GenerateJwtToken(storedToken.UserId, storedToken.User.Email, companyId, userDto.Roles);
        var accessToken = tokenResult.Token;
        var expiresAt = tokenResult.ExpiresAt;
        var newRefreshTokenValue = GenerateRefreshToken();
        var newRefreshTokenHash = HashToken(newRefreshTokenValue);

        var (createdFromIp, userAgent) = GetIpAndUserAgent();
        // Store new refresh token
        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = storedToken.UserId,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            CreatedFromIp = createdFromIp,
            UserAgent = userAgent
        };

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        await LogAuthEventAsync(AuthEventTypes.TokenRefresh, storedToken.UserId, cancellationToken);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshTokenValue,
            ExpiresAt = expiresAt,
            User = userDto
        };
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }
    }

    /// <summary>Resolves the primary company (tenant) id for the user for JWT. User.CompanyId or first department's company.</summary>
    private async Task<Guid?> ResolveUserCompanyIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var userCompanyId = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.CompanyId)
            .FirstOrDefaultAsync(cancellationToken);
        if (userCompanyId.HasValue && userCompanyId.Value != Guid.Empty)
            return userCompanyId;
        var fromDept = await _context.DepartmentMemberships
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Join(_context.Departments, m => m.DepartmentId, d => d.Id, (m, d) => d.CompanyId)
            .FirstOrDefaultAsync(cancellationToken);
        return fromDept;
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        // Company feature removed - no longer loading companies
        // Get user roles
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .ToListAsync(cancellationToken);

        var roles = userRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Name)
            .ToList();

        var permissions = _userPermissionProvider != null
            ? (await _userPermissionProvider.GetPermissionNamesAsync(userId, cancellationToken)).ToList()
            : new List<string>();

        return new UserDto
        {
            Id = userId,
            Name = user.Name ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Phone = user.Phone,
            Roles = roles,
            Permissions = permissions,
            MustChangePassword = user.MustChangePassword
        };
    }

    public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            throw new InvalidOperationException("New password must be at least 6 characters.");

        // Tenant-safe: scope by current tenant when set so FindAsync bypass is avoided
        var tenantId = CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        var user = (tenantId.HasValue && tenantId.Value != Guid.Empty)
            ? await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.CompanyId == tenantId.Value, cancellationToken)
            : await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
            throw new UnauthorizedAccessException("User not found.");

        if (string.IsNullOrEmpty(user.PasswordHash) || !_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        if (!user.CompanyId.HasValue || user.CompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("User account is not assigned to a company. Please contact your administrator.");

        var previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = user.CompanyId;
        try
        {
        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        user.MustChangePassword = false;

        var oldTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);
        foreach (var rt in oldTokens)
        {
            rt.IsRevoked = true;
            rt.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User {UserId} changed password", userId);
        await LogAuthEventAsync(AuthEventTypes.PasswordChanged, userId, cancellationToken);
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }
    }

    public async Task ChangePasswordRequiredAsync(string email, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var emailNorm = email?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(emailNorm))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            throw new InvalidOperationException("New password must be at least 6 characters.");

        // Lookup by email without tenant filter so flow works when called with tenant scope from session
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == emailNorm, cancellationToken);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.MustChangePassword)
            throw new UnauthorizedAccessException("Password change is not required for this account.");

        if (string.IsNullOrEmpty(user.PasswordHash) || !_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.CompanyId.HasValue || user.CompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("User account is not assigned to a company. Please contact your administrator.");

        var previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = user.CompanyId;
        try
        {
        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        user.MustChangePassword = false;
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User {Email} completed required password change", user.Email);
        await LogAuthEventAsync(AuthEventTypes.PasswordChanged, user.Id, cancellationToken);
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailNorm = email?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(emailNorm))
            return;

        // Forgot-password has no tenant context (e.g. from public page); find user without filter then set scope for any write
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == emailNorm && u.IsActive, cancellationToken);

        if (user == null)
            return;

        var nowUtc = DateTime.UtcNow;
        var expiryMinutes = _passwordResetOptions.TokenExpiryMinutes > 0 ? _passwordResetOptions.TokenExpiryMinutes : 60;
        var expiresAtUtc = nowUtc.AddMinutes(expiryMinutes);

        var rawToken = GenerateRefreshToken();
        var tokenHash = HashToken(rawToken);

        var previousTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var t in previousTokens)
        {
            t.UsedAtUtc = nowUtc;
        }

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = nowUtc
        };
        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync(cancellationToken);

        await LogAuthEventAsync(AuthEventTypes.PasswordResetRequested, user.Id, cancellationToken);

        if (_passwordResetOptions.EmailAccountId.HasValue && _emailSendingService != null)
        {
            var baseUrl = ( _passwordResetOptions.FrontendResetUrlBase ?? "" ).TrimEnd('/');
            var resetLink = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";
            var subject = "Reset your CephasOps password";
            var body = $"Use the link below to reset your password. The link expires in {expiryMinutes} minutes.\r\n\r\n{resetLink}\r\n\r\nIf you did not request this, you can ignore this email.";
            try
            {
                var result = await _emailSendingService.SendEmailAsync(
                    _passwordResetOptions.EmailAccountId.Value,
                    user.Email,
                    subject,
                    body,
                    cancellationToken: cancellationToken);
                if (!result.Success)
                    _logger.LogWarning("Failed to send password reset email to {Email}: {Error}", user.Email, result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send password reset email to {Email}", user.Email);
            }
        }
        else
        {
            _logger.LogInformation("Password reset requested for {Email}; no email sent (EmailAccountId or IEmailSendingService not configured)", user.Email);
        }
    }

    public async Task ResetPasswordWithTokenAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedAccessException("Invalid or expired reset link.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            throw new InvalidOperationException("New password must be at least 6 characters.");

        var tokenHash = HashToken(token);
        var nowUtc = DateTime.UtcNow;

        // Token lookup without tenant filter: reset link has no tenant context; User included for scope and save
        var resetToken = await _context.PasswordResetTokens
            .IgnoreQueryFilters()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UsedAtUtc == null && t.ExpiresAtUtc > nowUtc, cancellationToken);

        if (resetToken == null || resetToken.User == null)
            throw new UnauthorizedAccessException("Invalid or expired reset link.");

        var user = resetToken.User;

        if (!user.CompanyId.HasValue || user.CompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("User account is not assigned to a company. Please contact your administrator.");

        var previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = user.CompanyId;
        try
        {
        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        user.MustChangePassword = false;
        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;

        resetToken.UsedAtUtc = nowUtc;

        var oldRefreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .ToListAsync(cancellationToken);
        foreach (var rt in oldRefreshTokens)
        {
            rt.IsRevoked = true;
            rt.RevokedAt = nowUtc;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Password reset completed for user {Email} via reset token", user.Email);
        await LogAuthEventAsync(AuthEventTypes.PasswordResetCompleted, user.Id, cancellationToken);
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }
    }

    public async Task<LoginResponseDto> CreateTokenForImpersonationAsync(Guid targetUserId, Guid requestedByUserId, CancellationToken cancellationToken = default)
    {
        return await TenantScopeExecutor.RunWithPlatformBypassAsync(async (ct) =>
        {
            var user = await _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == targetUserId, ct);
            if (user == null || !user.IsActive)
                throw new InvalidOperationException("Target user not found or inactive.");

            if (!user.CompanyId.HasValue || user.CompanyId.Value == Guid.Empty)
                throw new InvalidOperationException("Target user has no company.");
            var companyId = user.CompanyId;
            var roleNames = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == targetUserId)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .ToListAsync(ct);
            var permissions = _userPermissionProvider != null
                ? (await _userPermissionProvider.GetPermissionNamesAsync(targetUserId, ct)).ToList()
                : new List<string>();
            var userDto = new UserDto
            {
                Id = targetUserId,
                Name = user.Name ?? "",
                Email = user.Email ?? "",
                Phone = user.Phone,
                Roles = roleNames,
                Permissions = permissions,
                MustChangePassword = user.MustChangePassword
            };

            var jwtExpiryMinutes = 60; // Short-lived for impersonation
            var tokenResult = GenerateJwtTokenWithExpiry(userId: targetUserId, email: user.Email ?? "", companyId, roleNames, jwtExpiryMinutes);
            var accessToken = tokenResult.Token;
            var expiresAt = tokenResult.ExpiresAt;

            _logger.LogWarning("Impersonation: requestedBy={RequestedBy}, targetUser={TargetUser}, targetEmail={Email}", requestedByUserId, targetUserId, user.Email);
            await LogAuthEventAsync(AuthEventTypes.Impersonation, targetUserId, cancellationToken);

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = "",
                ExpiresAt = expiresAt,
                User = userDto
            };
        }, cancellationToken);
    }

    private (string Token, DateTime ExpiresAt) GenerateJwtTokenWithExpiry(Guid userId, string email, Guid? companyId, List<string> roles, int expiryMinutes)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "YourSecretKeyForJwtTokenGenerationThatShouldBeAtLeast32Characters";
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "CephasOps";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "CephasOps";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("sub", userId.ToString()),
            new Claim(ClaimTypes.Email, email)
        };

        if (companyId.HasValue)
        {
            claims.Add(new Claim("companyId", companyId.Value.ToString()));
            claims.Add(new Claim("company_id", companyId.Value.ToString()));
        }

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expiresAt);
    }

    private async Task LogAuthEventAsync(string action, Guid? userId, CancellationToken cancellationToken)
    {
        if (_auditLogService == null) return;
        var (ip, userAgent) = GetIpAndUserAgent();
        var metadataJson = string.IsNullOrEmpty(userAgent) ? null : JsonSerializer.Serialize(new { userAgent });
        var entityId = userId ?? Guid.Empty;
        await _auditLogService.LogAuditAsync(
            companyId: null,
            userId: userId,
            entityType: "Auth",
            entityId,
            action,
            fieldChangesJson: null,
            channel: "Api",
            ipAddress: ip,
            metadataJson,
            cancellationToken);
    }

    private (string? Ip, string? UserAgent) GetIpAndUserAgent()
    {
        var ctx = _httpContextAccessor?.HttpContext;
        if (ctx == null) return (null, null);
        var ip = ctx.Connection?.RemoteIpAddress?.ToString();
        var ua = ctx.Request?.Headers?.TryGetValue("User-Agent", out var uaVal) == true ? uaVal.ToString() : null;
        return (ip, ua);
    }

    private (string Token, DateTime ExpiresAt) GenerateJwtToken(Guid userId, string email, Guid? companyId, List<string> roles)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "YourSecretKeyForJwtTokenGenerationThatShouldBeAtLeast32Characters";
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "CephasOps";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "CephasOps";
        var jwtExpiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("sub", userId.ToString()),
            new Claim(ClaimTypes.Email, email)
        };

        if (companyId.HasValue)
        {
            claims.Add(new Claim("companyId", companyId.Value.ToString()));
            claims.Add(new Claim("company_id", companyId.Value.ToString()));
        }

        // Add roles to claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(jwtExpiryMinutes);
        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expiresAt);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "YourSecretKeyForJwtTokenGenerationThatShouldBeAtLeast32Characters";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = false // We want to validate expired tokens
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }
}

