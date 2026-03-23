using CephasOps.Application.Auth.Services;
using CephasOps.Domain.Users.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Auth;

/// <summary>
/// Tests for v1.4 Phase 3 session management (list and revoke refresh tokens). Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class UserSessionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserSessionService _service;
    private readonly Guid _userId;
    private readonly Guid _roleId;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public UserSessionServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        _roleId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _context.Roles.Add(new Role { Id = _roleId, Name = "Member", Scope = "Global" });
        _context.Users.Add(new User
        {
            Id = _userId,
            CompanyId = _companyId,
            Name = "Test User",
            Email = "test@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _context.UserRoles.Add(new UserRole { UserId = _userId, RoleId = _roleId, CompanyId = null, CreatedAt = DateTime.UtcNow });
        _context.SaveChanges();

        _service = new UserSessionService(_context);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
    }

    [Fact]
    public async Task GetSessionsAsync_ReturnsSessionsWithUserEmail()
    {
        var tokenId = Guid.NewGuid();
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = tokenId,
            UserId = _userId,
            TokenHash = "h1",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            CreatedFromIp = "1.2.3.4",
            UserAgent = "Mozilla/5.0"
        });
        await _context.SaveChangesAsync();

        var list = await _service.GetSessionsAsync(activeOnly: true);

        list.Should().ContainSingle();
        list[0].SessionId.Should().Be(tokenId);
        list[0].UserId.Should().Be(_userId);
        list[0].UserEmail.Should().Be("test@example.com");
        list[0].IpAddress.Should().Be("1.2.3.4");
        list[0].UserAgent.Should().Be("Mozilla/5.0");
        list[0].IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task GetSessionsAsync_ActiveOnly_ExcludesRevokedTokens()
    {
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TokenHash = "h1",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var list = await _service.GetSessionsAsync(activeOnly: true);

        list.Should().BeEmpty();
    }

    [Fact]
    public async Task RevokeSessionAsync_MarksTokenRevoked()
    {
        var tokenId = Guid.NewGuid();
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = tokenId,
            UserId = _userId,
            TokenHash = "h1",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _service.RevokeSessionAsync(tokenId);

        result.Should().BeTrue();
        var token = await _context.RefreshTokens.FindAsync(tokenId);
        token.Should().NotBeNull();
        token!.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeSessionAsync_NotFound_ReturnsFalse()
    {
        var result = await _service.RevokeSessionAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeAllSessionsForUserAsync_RevokesAllForUser()
    {
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TokenHash = "h1",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        });
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TokenHash = "h2",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var count = await _service.RevokeAllSessionsForUserAsync(_userId);

        count.Should().Be(2);
        var tokens = await _context.RefreshTokens.Where(rt => rt.UserId == _userId).ToListAsync();
        tokens.Should().OnlyContain(rt => rt.IsRevoked && rt.RevokedAt != null);
    }

    [Fact]
    public async Task GetSessionsForUserAsync_ReturnsOnlyThatUserSessions()
    {
        var otherUserId = Guid.NewGuid();
        _context.Users.Add(new User { Id = otherUserId, Name = "Other", Email = "other@test.com", IsActive = true, CreatedAt = DateTime.UtcNow });
        _context.UserRoles.Add(new UserRole { UserId = otherUserId, RoleId = _roleId, CompanyId = null, CreatedAt = DateTime.UtcNow });
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TokenHash = "h1",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        });
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = otherUserId,
            TokenHash = "h2",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var list = await _service.GetSessionsForUserAsync(_userId);

        list.Should().ContainSingle();
        list[0].UserId.Should().Be(_userId);
    }
}
