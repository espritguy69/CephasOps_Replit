using System.Security.Cryptography;
using System.Text;
using CephasOps.Application.Audit.Services;
using CephasOps.Application.Auth;
using CephasOps.Application.Auth.DTOs;
using CephasOps.Application.Auth.Services;
using CephasOps.Application.Common.Services;
using CephasOps.Application.Subscription;
using CephasOps.Domain.Users.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Auth;

/// <summary>
/// Tests for auth v1.2: last login, must-change-password, change-password-required.
/// Runs in a non-parallel collection so TenantScope is not overwritten by other test classes.
/// </summary>
[Collection("AuthServiceTests")]
public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AuthService _authService;
    private readonly CompatibilityPasswordHasher _passwordHasher;
    private readonly Guid _userId;
    private readonly Guid _companyId;
    private const string TestPassword = "password123";

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _userId = Guid.NewGuid();
        _companyId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        _context.Roles.Add(new Role { Id = roleId, Name = "Member", Scope = "Global" });
        _context.Users.Add(new User
        {
            Id = _userId,
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = DatabaseSeeder.HashPassword(TestPassword),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            MustChangePassword = false,
            CompanyId = _companyId
        });
        _context.UserRoles.Add(new UserRole
        {
            UserId = _userId,
            RoleId = roleId,
            CompanyId = _companyId,
            CreatedAt = DateTime.UtcNow
        });
        TenantScope.CurrentTenantId = _companyId;
        try { _context.SaveChanges(); }
        finally { TenantScope.CurrentTenantId = null; }

        var configData = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "YourSecretKeyForJwtTokenGenerationThatShouldBeAtLeast32Characters",
            ["Jwt:Issuer"] = "CephasOps",
            ["Jwt:Audience"] = "CephasOps",
            ["Jwt:ExpiryMinutes"] = "60"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configData!).Build();
        _passwordHasher = new CompatibilityPasswordHasher();
        var lockoutOptions = Options.Create(new LockoutOptions());
        _authService = new AuthService(_context, configuration, new Mock<ILogger<AuthService>>().Object, _passwordHasher, lockoutOptions);
    }

    private void EnsureTenantScope() => TenantScope.CurrentTenantId = _companyId;

    [Fact]
    public async Task LoginAsync_Success_UpdatesLastLoginAtUtc()
    {
        EnsureTenantScope();
        var request = new LoginRequestDto { Email = "test@example.com", Password = TestPassword };
        var before = DateTime.UtcNow.AddSeconds(-2);

        var response = await _authService.LoginAsync(request);

        response.Should().NotBeNull();
        response.AccessToken.Should().NotBeNullOrEmpty();
        var user = await _context.Users.FindAsync(_userId);
        user!.LastLoginAtUtc.Should().NotBeNull();
        user.LastLoginAtUtc!.Value.Should().BeAfter(before);
    }

    [Fact]
    public async Task LoginAsync_WhenMustChangePassword_ThrowsRequiresPasswordChangeException()
    {
        EnsureTenantScope();
        var user = await _context.Users.FindAsync(_userId);
        user!.MustChangePassword = true;
        TenantScope.CurrentTenantId = _companyId;
        await _context.SaveChangesAsync();

        var request = new LoginRequestDto { Email = "test@example.com", Password = TestPassword };

        var act = () => _authService.LoginAsync(request);

        await act.Should().ThrowAsync<RequiresPasswordChangeException>();
        var after = await _context.Users.FindAsync(_userId);
        after!.LastLoginAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ChangePasswordRequiredAsync_ValidRequest_ClearsMustChangePassword()
    {
        EnsureTenantScope();
        var user = await _context.Users.FindAsync(_userId);
        user!.MustChangePassword = true;
        TenantScope.CurrentTenantId = _companyId;
        try { await _context.SaveChangesAsync(); }
        finally { TenantScope.CurrentTenantId = null; }
        EnsureTenantScope();

        await _authService.ChangePasswordRequiredAsync(
            "test@example.com",
            TestPassword,
            "newpassword456");

        EnsureTenantScope();
        var updated = await _context.Users.AsNoTracking().IgnoreQueryFilters().FirstAsync(u => u.Id == _userId);
        updated.MustChangePassword.Should().BeFalse();
        updated.PasswordHash.Should().NotBeNullOrEmpty();
        _passwordHasher.VerifyPassword("newpassword456", updated.PasswordHash!).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordRequiredAsync_WrongCurrentPassword_Throws()
    {
        EnsureTenantScope();
        var user = await _context.Users.FindAsync(_userId);
        user!.MustChangePassword = true;
        TenantScope.CurrentTenantId = _companyId;
        await _context.SaveChangesAsync();

        var act = () => _authService.ChangePasswordRequiredAsync(
            "test@example.com",
            "wrongpassword",
            "newpassword456");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsMustChangePassword()
    {
        EnsureTenantScope();
        var user = await _context.Users.FindAsync(_userId);
        user!.MustChangePassword = true;
        TenantScope.CurrentTenantId = _companyId;
        try { await _context.SaveChangesAsync(); }
        finally { TenantScope.CurrentTenantId = null; }
        EnsureTenantScope();

        var dto = await _authService.GetCurrentUserAsync(_userId);

        dto.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_WithLegacyHash_RehashesToModernFormat()
    {
        EnsureTenantScope();
        var user = await _context.Users.FindAsync(_userId);
        user!.PasswordHash = DatabaseSeeder.HashPassword(TestPassword);
        TenantScope.CurrentTenantId = _companyId;
        await _context.SaveChangesAsync();
        user.PasswordHash.Should().NotStartWith("$2");

        await _authService.LoginAsync(new LoginRequestDto { Email = "test@example.com", Password = TestPassword });

        var updated = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == _userId);
        updated.PasswordHash.Should().NotBeNullOrEmpty();
        updated.PasswordHash.Should().StartWith("$2");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_IncrementsFailedLoginAttempts()
    {
        EnsureTenantScope();
        var act = () => _authService.LoginAsync(new LoginRequestDto { Email = "test@example.com", Password = "wrong" });
        await act.Should().ThrowAsync<UnauthorizedAccessException>();

        var user = await _context.Users.FindAsync(_userId);
        user!.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task LoginAsync_RepeatedInvalidPasswords_EventuallyLocksAccount()
    {
        EnsureTenantScope();
        var lockoutOptions = Options.Create(new LockoutOptions { MaxFailedAttempts = 2, LockoutMinutes = 15 });
        var authService = new AuthService(_context, new ConfigurationBuilder().Build(), new Mock<ILogger<AuthService>>().Object, _passwordHasher, lockoutOptions);

        var act1 = () => authService.LoginAsync(new LoginRequestDto { Email = "test@example.com", Password = "wrong" });
        await act1.Should().ThrowAsync<UnauthorizedAccessException>();
        var act2 = () => authService.LoginAsync(new LoginRequestDto { Email = "test@example.com", Password = "wrong" });
        await act2.Should().ThrowAsync<UnauthorizedAccessException>();

        var user = await _context.Users.FindAsync(_userId);
        user!.FailedLoginAttempts.Should().Be(2);
        user.LockoutEndUtc.Should().NotBeNull();
        user.LockoutEndUtc!.Value.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task LoginAsync_WhenLocked_ThrowsAccountLockedEvenWithCorrectPassword()
    {
        EnsureTenantScope();
        var user = await _context.Users.FindAsync(_userId);
        user!.LockoutEndUtc = DateTime.UtcNow.AddMinutes(10);
        TenantScope.CurrentTenantId = _companyId;
        await _context.SaveChangesAsync();

        var act = () => _authService.LoginAsync(new LoginRequestDto { Email = "test@example.com", Password = TestPassword });

        await act.Should().ThrowAsync<AccountLockedException>();
    }

    [Fact]
    public async Task LoginAsync_Success_ResetsFailedLoginAttemptsAndLockoutEndUtc()
    {
        EnsureTenantScope();
        var user = await _context.Users.FindAsync(_userId);
        user!.FailedLoginAttempts = 3;
        user.LockoutEndUtc = null;
        TenantScope.CurrentTenantId = _companyId;
        await _context.SaveChangesAsync();

        await _authService.LoginAsync(new LoginRequestDto { Email = "test@example.com", Password = TestPassword });

        var updated = await _context.Users.FindAsync(_userId);
        updated!.FailedLoginAttempts.Should().Be(0);
        updated.LockoutEndUtc.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorizedWithSameMessage()
    {
        EnsureTenantScope();
        var act = () => _authService.LoginAsync(new LoginRequestDto { Email = "nonexistent@example.com", Password = "any" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_Success_RestoresTenantScope()
    {
        EnsureTenantScope();
        var previousTenantId = _companyId;
        TenantScope.CurrentTenantId = previousTenantId;
        try
        {
            await _authService.LoginAsync(new LoginRequestDto { Email = "test@example.com", Password = TestPassword });
        }
        finally
        {
            TenantScope.CurrentTenantId.Should().Be(previousTenantId, "LoginAsync must restore tenant scope in finally");
        }
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_RestoresTenantScope()
    {
        EnsureTenantScope();
        var previousTenantId = _companyId;
        TenantScope.CurrentTenantId = previousTenantId;
        try
        {
            await _authService.Invoking(s => s.LoginAsync(new LoginRequestDto { Email = "test@example.com", Password = "wrong" }))
                .Should().ThrowAsync<UnauthorizedAccessException>();
        }
        finally
        {
            TenantScope.CurrentTenantId.Should().Be(previousTenantId, "LoginAsync must restore tenant scope after failed attempt");
        }
    }

    [Fact]
    public async Task LoginAsync_WithNullCompanyId_SucceedsViaPlatformBypass()
    {
        EnsureTenantScope();
        var user = await _context.Users.FindAsync(_userId);
        user!.CompanyId = null;
        TenantScope.CurrentTenantId = _companyId;
        try { await _context.SaveChangesAsync(); }
        finally { TenantScope.CurrentTenantId = null; }
        TenantScope.CurrentTenantId = null; // so user with CompanyId null is visible to login lookup

        var response = await _authService.LoginAsync(new LoginRequestDto { Email = "test@example.com", Password = TestPassword });

        response.Should().NotBeNull();
        response.AccessToken.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshTokenAsync_RestoresTenantScope()
    {
        EnsureTenantScope();
        var loginResponse = await _authService.LoginAsync(new LoginRequestDto { Email = "test@example.com", Password = TestPassword });
        var previousTenantId = _companyId;
        TenantScope.CurrentTenantId = previousTenantId;
        try
        {
            await _authService.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = loginResponse.RefreshToken! });
        }
        finally
        {
            TenantScope.CurrentTenantId.Should().Be(previousTenantId, "RefreshTokenAsync must restore tenant scope");
        }
    }

    [Fact]
    public async Task ChangePasswordAsync_RestoresTenantScope()
    {
        EnsureTenantScope();
        var previousTenantId = _companyId;
        TenantScope.CurrentTenantId = previousTenantId;
        try
        {
            await _authService.ChangePasswordAsync(_userId, TestPassword, "newpassword456");
        }
        finally
        {
            TenantScope.CurrentTenantId.Should().Be(previousTenantId, "ChangePasswordAsync must restore tenant scope");
        }
    }

    [Fact]
    public async Task ChangePasswordRequiredAsync_RestoresTenantScope()
    {
        EnsureTenantScope();
        var user = await _context.Users.FindAsync(_userId);
        user!.MustChangePassword = true;
        TenantScope.CurrentTenantId = _companyId;
        try { await _context.SaveChangesAsync(); }
        finally { TenantScope.CurrentTenantId = null; }
        var previousTenantId = _companyId; // use _companyId so lookup finds user; assert service restores it
        EnsureTenantScope();
        TenantScope.CurrentTenantId = previousTenantId;
        try
        {
            await _authService.ChangePasswordRequiredAsync("test@example.com", TestPassword, "newpassword789");
        }
        finally
        {
            TenantScope.CurrentTenantId.Should().Be(previousTenantId, "ChangePasswordRequiredAsync must restore tenant scope");
        }
        // Restore for subsequent code if any
        TenantScope.CurrentTenantId = null;
    }

    [Fact]
    public async Task ResetPasswordWithTokenAsync_RestoresTenantScope()
    {
        EnsureTenantScope();
        var rawToken = "scope-restore-token";
        var tokenHash = HashTokenForTest(rawToken);
        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            CreatedAtUtc = DateTime.UtcNow
        });
        TenantScope.CurrentTenantId = _companyId;
        await _context.SaveChangesAsync();

        var previousTenantId = _companyId;
        TenantScope.CurrentTenantId = previousTenantId;
        try
        {
            await _authService.ResetPasswordWithTokenAsync(rawToken, "newpass999");
        }
        finally
        {
            TenantScope.CurrentTenantId.Should().Be(previousTenantId, "ResetPasswordWithTokenAsync must restore tenant scope");
        }
    }

    [Fact]
    public async Task LoginAsync_WhenSubscriptionAccessDenied_ThrowsTenantAccessDeniedWithReason()
    {
        EnsureTenantScope();
        var companyId = Guid.NewGuid();
        var user = await _context.Users.FindAsync(_userId);
        user!.CompanyId = companyId;
        TenantScope.CurrentTenantId = companyId;
        try { await _context.SaveChangesAsync(); }
        finally { TenantScope.CurrentTenantId = null; }
        TenantScope.CurrentTenantId = companyId; // so login finds user

        var mockSubscription = new Mock<ISubscriptionAccessService>();
        mockSubscription
            .Setup(s => s.GetAccessForCompanyAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SubscriptionAccessResult.Deny(SubscriptionDenialReasons.TenantSuspended));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "YourSecretKeyForJwtTokenGenerationThatShouldBeAtLeast32Characters",
                ["Jwt:Issuer"] = "CephasOps",
                ["Jwt:Audience"] = "CephasOps",
                ["Jwt:ExpiryMinutes"] = "60"
            }!).Build();
        var authServiceWithSubscription = new AuthService(
            _context,
            config,
            new Mock<ILogger<AuthService>>().Object,
            _passwordHasher,
            Options.Create(new LockoutOptions()),
            null, null, null, null, null,
            mockSubscription.Object);

        var act = () => authServiceWithSubscription.LoginAsync(new LoginRequestDto { Email = "test@example.com", Password = TestPassword });

        var ex = await act.Should().ThrowAsync<TenantAccessDeniedException>();
        ex.Which.DenialReason.Should().Be(SubscriptionDenialReasons.TenantSuspended);
        user.CompanyId = _companyId;
        TenantScope.CurrentTenantId = _companyId;
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenSubscriptionAccessDenied_ThrowsTenantAccessDeniedWithReason()
    {
        EnsureTenantScope();
        var companyId = Guid.NewGuid();
        var user = await _context.Users.FindAsync(_userId);
        user!.CompanyId = companyId;
        TenantScope.CurrentTenantId = companyId;
        await _context.SaveChangesAsync();

        var loginResponse = await _authService.LoginAsync(new LoginRequestDto { Email = "test@example.com", Password = TestPassword });
        loginResponse.RefreshToken.Should().NotBeNullOrEmpty();

        var mockSubscription = new Mock<ISubscriptionAccessService>();
        mockSubscription
            .Setup(s => s.GetAccessForCompanyAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SubscriptionAccessResult.Deny(SubscriptionDenialReasons.SubscriptionCancelled));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "YourSecretKeyForJwtTokenGenerationThatShouldBeAtLeast32Characters",
                ["Jwt:Issuer"] = "CephasOps",
                ["Jwt:Audience"] = "CephasOps",
                ["Jwt:ExpiryMinutes"] = "60"
            }!).Build();
        var authServiceWithSubscription = new AuthService(
            _context,
            config,
            new Mock<ILogger<AuthService>>().Object,
            _passwordHasher,
            Options.Create(new LockoutOptions()),
            null, null, null, null, null,
            mockSubscription.Object);

        var act = () => authServiceWithSubscription.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = loginResponse.RefreshToken! });

        var ex = await act.Should().ThrowAsync<TenantAccessDeniedException>();
        ex.Which.DenialReason.Should().Be(SubscriptionDenialReasons.SubscriptionCancelled);
        user.CompanyId = _companyId;
        TenantScope.CurrentTenantId = _companyId;
        await _context.SaveChangesAsync();
    }

    // --- Phase D: Password reset ---

    [Fact]
    public async Task ForgotPasswordAsync_NonExistingEmail_DoesNotThrow()
    {
        EnsureTenantScope();
        await _authService.Invoking(s => s.ForgotPasswordAsync("nonexistent@example.com"))
            .Should().NotThrowAsync();
        var count = await _context.PasswordResetTokens.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task ForgotPasswordAsync_ExistingUser_CreatesTokenRecord()
    {
        EnsureTenantScope();
        TenantScope.CurrentTenantId = _companyId;
        await _authService.ForgotPasswordAsync("test@example.com");

        var tokens = await _context.PasswordResetTokens.Where(t => t.UserId == _userId).ToListAsync();
        tokens.Should().ContainSingle();
        tokens[0].UsedAtUtc.Should().BeNull();
        tokens[0].ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ResetPasswordWithTokenAsync_ValidToken_UpdatesPasswordAndClearsLockout()
    {
        EnsureTenantScope();
        var rawToken = "test-reset-token-value";
        var tokenHash = HashTokenForTest(rawToken);
        var expiresAt = DateTime.UtcNow.AddHours(1);
        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAt,
            CreatedAtUtc = DateTime.UtcNow
        });
        TenantScope.CurrentTenantId = _companyId;
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(_userId);
        user!.FailedLoginAttempts = 2;
        user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(10);
        TenantScope.CurrentTenantId = _companyId;
        await _context.SaveChangesAsync();

        await _authService.ResetPasswordWithTokenAsync(rawToken, "newpass123");

        var updated = await _context.Users.FindAsync(_userId);
        updated!.FailedLoginAttempts.Should().Be(0);
        updated.LockoutEndUtc.Should().BeNull();
        _passwordHasher.VerifyPassword("newpass123", updated.PasswordHash!).Should().BeTrue();
        var tokenRecord = await _context.PasswordResetTokens.FirstAsync(t => t.TokenHash == tokenHash);
        tokenRecord.UsedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ResetPasswordWithTokenAsync_ValidToken_RevokesRefreshTokens()
    {
        EnsureTenantScope();
        var rawToken = "test-reset-token-revoke";
        var tokenHash = HashTokenForTest(rawToken);
        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            CreatedAtUtc = DateTime.UtcNow
        });
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TokenHash = "some-hash",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        });
        TenantScope.CurrentTenantId = _companyId;
        await _context.SaveChangesAsync();

        await _authService.ResetPasswordWithTokenAsync(rawToken, "newpass456");

        var refreshTokens = await _context.RefreshTokens.Where(rt => rt.UserId == _userId).ToListAsync();
        refreshTokens.Should().ContainSingle();
        refreshTokens[0].IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordWithTokenAsync_ExpiredToken_Throws()
    {
        EnsureTenantScope();
        var rawToken = "expired-token";
        var tokenHash = HashTokenForTest(rawToken);
        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2)
        });
        TenantScope.CurrentTenantId = _companyId;
        await _context.SaveChangesAsync();

        var act = () => _authService.ResetPasswordWithTokenAsync(rawToken, "newpass");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid or expired reset link.");
    }

    [Fact]
    public async Task ResetPasswordWithTokenAsync_UsedToken_Throws()
    {
        EnsureTenantScope();
        var rawToken = "used-token";
        var tokenHash = HashTokenForTest(rawToken);
        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            UsedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            CreatedAtUtc = DateTime.UtcNow
        });
        TenantScope.CurrentTenantId = _companyId;
        await _context.SaveChangesAsync();

        var act = () => _authService.ResetPasswordWithTokenAsync(rawToken, "newpass");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid or expired reset link.");
    }

    [Fact]
    public async Task ResetPasswordWithTokenAsync_InvalidToken_Throws()
    {
        EnsureTenantScope();
        var act = () => _authService.ResetPasswordWithTokenAsync("invalid-token-value", "newpass123");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid or expired reset link.");
    }

    [Fact]
    public async Task ForgotPasswordAsync_SecondRequest_InvalidatesPreviousToken()
    {
        EnsureTenantScope();
        await _authService.ForgotPasswordAsync("test@example.com");
        var firstToken = await _context.PasswordResetTokens.Where(t => t.UserId == _userId).OrderBy(t => t.CreatedAtUtc).FirstAsync();
        firstToken.UsedAtUtc.Should().BeNull();

        await _authService.ForgotPasswordAsync("test@example.com");

        var previousTokens = await _context.PasswordResetTokens.Where(t => t.UserId == _userId && t.UsedAtUtc != null).ToListAsync();
        previousTokens.Should().ContainSingle();
        previousTokens[0].Id.Should().Be(firstToken.Id);
    }

    private static string HashTokenForTest(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }

    // --- v1.4 Phase 1: Auth event audit logging ---

    [Fact]
    public async Task LoginAsync_Success_LogsLoginSuccessAuditEvent()
    {
        EnsureTenantScope();
        var auditMock = new Mock<IAuditLogService>();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "YourSecretKeyForJwtTokenGenerationThatShouldBeAtLeast32Characters",
            ["Jwt:Issuer"] = "CephasOps",
            ["Jwt:Audience"] = "CephasOps",
            ["Jwt:ExpiryMinutes"] = "60"
        }!).Build();
        var service = new AuthService(
            _context,
            config,
            new Mock<ILogger<AuthService>>().Object,
            _passwordHasher,
            Options.Create(new LockoutOptions()),
            passwordResetOptions: null,
            emailSendingService: null,
            auditLogService: auditMock.Object,
            httpContextAccessor: null);

        await service.LoginAsync(new LoginRequestDto { Email = "test@example.com", Password = TestPassword });

        auditMock.Verify(
            x => x.LogAuditAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), "Auth", It.IsAny<Guid>(), AuthEventTypes.LoginSuccess, It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_LogsLoginFailedAuditEvent()
    {
        EnsureTenantScope();
        var auditMock = new Mock<IAuditLogService>();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "YourSecretKeyForJwtTokenGenerationThatShouldBeAtLeast32Characters",
            ["Jwt:Issuer"] = "CephasOps",
            ["Jwt:Audience"] = "CephasOps",
            ["Jwt:ExpiryMinutes"] = "60"
        }!).Build();
        var service = new AuthService(_context, config, new Mock<ILogger<AuthService>>().Object, _passwordHasher,
            Options.Create(new LockoutOptions()), null, null, auditLogService: auditMock.Object, null);

        await service.Invoking(s => s.LoginAsync(new LoginRequestDto { Email = "test@example.com", Password = "wrong" }))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        auditMock.Verify(
            x => x.LogAuditAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), "Auth", It.IsAny<Guid>(), AuthEventTypes.LoginFailed, It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenLocked_LogsAccountLockedAuditEvent()
    {
        EnsureTenantScope();
        var user = await _context.Users.FindAsync(_userId);
        user!.LockoutEndUtc = DateTime.UtcNow.AddMinutes(10);
        TenantScope.CurrentTenantId = _companyId;
        await _context.SaveChangesAsync();

        var auditMock = new Mock<IAuditLogService>();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "YourSecretKeyForJwtTokenGenerationThatShouldBeAtLeast32Characters",
            ["Jwt:Issuer"] = "CephasOps",
            ["Jwt:Audience"] = "CephasOps",
            ["Jwt:ExpiryMinutes"] = "60"
        }!).Build();
        var service = new AuthService(_context, config, new Mock<ILogger<AuthService>>().Object, _passwordHasher,
            Options.Create(new LockoutOptions()), null, null, auditLogService: auditMock.Object, null);

        await service.Invoking(s => s.LoginAsync(new LoginRequestDto { Email = "test@example.com", Password = TestPassword }))
            .Should().ThrowAsync<AccountLockedException>();

        auditMock.Verify(
            x => x.LogAuditAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), "Auth", It.IsAny<Guid>(), AuthEventTypes.AccountLocked, It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_ExistingUser_LogsPasswordResetRequestedAuditEvent()
    {
        EnsureTenantScope();
        var auditMock = new Mock<IAuditLogService>();
        var config = new ConfigurationBuilder().Build();
        var service = new AuthService(_context, config, new Mock<ILogger<AuthService>>().Object, _passwordHasher,
            Options.Create(new LockoutOptions()), Options.Create(new PasswordResetOptions()), null, auditLogService: auditMock.Object, null);

        TenantScope.CurrentTenantId = _companyId;
        await service.ForgotPasswordAsync("test@example.com");

        auditMock.Verify(
            x => x.LogAuditAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), "Auth", It.IsAny<Guid>(), AuthEventTypes.PasswordResetRequested, It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetPasswordWithTokenAsync_ValidToken_LogsPasswordResetCompletedAuditEvent()
    {
        EnsureTenantScope();
        var rawToken = "reset-audit-token";
        var tokenHash = HashTokenForTest(rawToken);
        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            CreatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var auditMock = new Mock<IAuditLogService>();
        var config = new ConfigurationBuilder().Build();
        var service = new AuthService(_context, config, new Mock<ILogger<AuthService>>().Object, _passwordHasher,
            Options.Create(new LockoutOptions()), null, null, auditLogService: auditMock.Object, null);

        await service.ResetPasswordWithTokenAsync(rawToken, "newpass123");

        auditMock.Verify(
            x => x.LogAuditAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), "Auth", It.IsAny<Guid>(), AuthEventTypes.PasswordResetCompleted, It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = null;
        _context?.Dispose();
    }
}
