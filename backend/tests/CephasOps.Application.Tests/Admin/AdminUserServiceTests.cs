using CephasOps.Application.Admin.DTOs;
using CephasOps.Application.Admin.Services;
using CephasOps.Application.Audit.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Common.Services;
using CephasOps.Domain.Departments.Entities;
using CephasOps.Domain.Users.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Admin;

/// <summary>
/// Tests for admin user management: list, create (duplicate email), set active (last admin guard). Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class AdminUserServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AdminUserService _service;
    private readonly IPasswordHasher _passwordHasher;
    private readonly Guid _roleId;
    private readonly Guid _adminUserId;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public AdminUserServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<AdminUserService>>().Object;
        _passwordHasher = new CompatibilityPasswordHasher();

        _roleId = Guid.NewGuid();
        _context.Roles.Add(new Role { Id = _roleId, Name = "Admin", Scope = "Global" });
        _context.Roles.Add(new Role { Id = Guid.NewGuid(), Name = "Member", Scope = "Global" });

        _adminUserId = Guid.NewGuid();
        _context.Users.Add(new User
        {
            Id = _adminUserId,
            CompanyId = _companyId,
            Name = "Admin User",
            Email = "admin@test.com",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _context.UserRoles.Add(new UserRole
        {
            UserId = _adminUserId,
            RoleId = _roleId,
            CompanyId = null,
            CreatedAt = DateTime.UtcNow
        });

        _context.SaveChanges();
        _service = new AdminUserService(_context, logger, _passwordHasher);
    }

    [Fact]
    public async Task ListAsync_ReturnsPagedResult()
    {
        var result = await _service.ListAsync(1, 10, null, null, null);

        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items.Should().ContainSingle(u => u.Email == "admin@test.com");
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_Throws()
    {
        var request = new CreateAdminUserRequestDto
        {
            Name = "Other",
            Email = "admin@test.com",
            Password = "password123",
            RoleNames = new List<string> { "Member" }
        };

        var act = () => _service.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*email already exists*");
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesUser()
    {
        var request = new CreateAdminUserRequestDto
        {
            Name = "New User",
            Email = "new@test.com",
            Password = "password123",
            RoleNames = new List<string> { "Member" }
        };

        var id = await _service.CreateAsync(request);

        id.Should().NotBeEmpty();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        user.Should().NotBeNull();
        user!.Email.Should().Be("new@test.com");
        user.Name.Should().Be("New User");
    }

    [Fact]
    public async Task SetActiveAsync_DeactivatingLastActiveAdmin_Throws()
    {
        var act = () => _service.SetActiveAsync(_adminUserId, false, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*last active administrator*");
    }

    [Fact]
    public async Task SetActiveAsync_DeactivatingSelf_Throws()
    {
        var act = () => _service.SetActiveAsync(_adminUserId, false, _adminUserId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot deactivate your own*");
    }

    [Fact]
    public async Task CreateAsync_EmptyRoleNames_Throws()
    {
        var request = new CreateAdminUserRequestDto
        {
            Name = "New User",
            Email = "noroles@test.com",
            Password = "password123",
            RoleNames = new List<string>()
        };

        var act = () => _service.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*least one role*");
    }

    [Fact]
    public async Task CreateAsync_DuplicateDepartmentIds_Throws()
    {
        var deptId = Guid.NewGuid();
        _context.Departments.Add(new Department
        {
            Id = deptId,
            Name = "Dept",
            CompanyId = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var request = new CreateAdminUserRequestDto
        {
            Name = "New User",
            Email = "dupdept@test.com",
            Password = "password123",
            RoleNames = new List<string> { "Member" },
            DepartmentMemberships = new List<AdminUserDepartmentMembershipDto>
            {
                new() { DepartmentId = deptId, Role = "Member", IsDefault = false },
                new() { DepartmentId = deptId, Role = "HOD", IsDefault = false }
            }
        };

        var act = () => _service.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Duplicate department*");
    }

    [Fact]
    public async Task CreateAsync_InvalidDepartmentId_Throws()
    {
        var request = new CreateAdminUserRequestDto
        {
            Name = "New User",
            Email = "invdept@test.com",
            Password = "password123",
            RoleNames = new List<string> { "Member" },
            DepartmentMemberships = new List<AdminUserDepartmentMembershipDto>
            {
                new() { DepartmentId = Guid.NewGuid(), Role = "Member", IsDefault = false }
            }
        };

        var act = () => _service.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*invalid*");
    }

    [Fact]
    public async Task UpdateAsync_EmptyRoleNames_Throws()
    {
        var request = new UpdateAdminUserRequestDto
        {
            Name = "Admin User",
            Email = "admin@test.com",
            RoleNames = new List<string>()
        };

        var act = () => _service.UpdateAsync(_adminUserId, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*least one role*");
    }

    [Fact]
    public async Task CreateAsync_WhenAuditServiceProvided_LogsAudit()
    {
        var auditMock = new Mock<IAuditLogService>();
        auditMock
            .Setup(s => s.LogAuditAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var serviceWithAudit = new AdminUserService(_context, new Mock<ILogger<AdminUserService>>().Object, _passwordHasher, auditMock.Object);

        var request = new CreateAdminUserRequestDto
        {
            Name = "Audit User",
            Email = "audit@test.com",
            Password = "password123",
            RoleNames = new List<string> { "Member" }
        };

        var id = await serviceWithAudit.CreateAsync(request, Guid.NewGuid());

        id.Should().NotBeEmpty();
        auditMock.Verify(
            s => s.LogAuditAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), "User", id, "Created", It.IsAny<string?>(), "Api", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithForceMustChangePassword_SetsFlag()
    {
        var request = new AdminResetPasswordRequestDto
        {
            NewPassword = "newpass123",
            ForceMustChangePassword = true
        };
        await _service.ResetPasswordAsync(_adminUserId, request);

        var user = await _context.Users.FindAsync(_adminUserId);
        user.Should().NotBeNull();
        user!.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithForceMustChangePasswordFalse_ClearsFlag()
    {
        var user = await _context.Users.FindAsync(_adminUserId);
        user!.MustChangePassword = true;
        await _context.SaveChangesAsync();

        var request = new AdminResetPasswordRequestDto
        {
            NewPassword = "newpass123",
            ForceMustChangePassword = false
        };
        await _service.ResetPasswordAsync(_adminUserId, request);

        var updated = await _context.Users.FindAsync(_adminUserId);
        updated!.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordAsync_RevokesExistingRefreshTokensForTargetUser()
    {
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = _adminUserId,
            TokenHash = "hash1",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        });
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = _adminUserId,
            TokenHash = "hash2",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var request = new AdminResetPasswordRequestDto { NewPassword = "newpass123", ForceMustChangePassword = true };
        await _service.ResetPasswordAsync(_adminUserId, request);

        var tokens = await _context.RefreshTokens.Where(rt => rt.UserId == _adminUserId).ToListAsync();
        tokens.Should().HaveCount(2);
        tokens.Should().OnlyContain(rt => rt.IsRevoked && rt.RevokedAt != null);
    }

    [Fact]
    public async Task ResetPasswordAsync_DoesNotRevokeUnrelatedUsersRefreshTokens()
    {
        var otherUserId = Guid.NewGuid();
        _context.Users.Add(new User
        {
            Id = otherUserId,
            Name = "Other",
            Email = "other@test.com",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _context.UserRoles.Add(new UserRole { UserId = otherUserId, RoleId = _roleId, CompanyId = null, CreatedAt = DateTime.UtcNow });
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = otherUserId,
            TokenHash = "otherhash",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var request = new AdminResetPasswordRequestDto { NewPassword = "newpass123", ForceMustChangePassword = false };
        await _service.ResetPasswordAsync(_adminUserId, request);

        var otherToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == otherUserId);
        otherToken.Should().NotBeNull();
        otherToken!.IsRevoked.Should().BeFalse();
        otherToken.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task ResetPasswordAsync_UpdatesPasswordHash()
    {
        var request = new AdminResetPasswordRequestDto { NewPassword = "newpass456", ForceMustChangePassword = false };
        await _service.ResetPasswordAsync(_adminUserId, request);

        var user = await _context.Users.FindAsync(_adminUserId);
        user.Should().NotBeNull();
        user!.PasswordHash.Should().NotBeNullOrEmpty().And.NotBe("hash");
        _passwordHasher.VerifyPassword("newpass456", user.PasswordHash!).Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_LogsAdminPasswordResetAuthEvent()
    {
        var auditMock = new Mock<IAuditLogService>();
        var serviceWithAudit = new AdminUserService(_context, new Mock<ILogger<AdminUserService>>().Object, _passwordHasher, auditMock.Object);
        var request = new AdminResetPasswordRequestDto { NewPassword = "newpass789", ForceMustChangePassword = true };

        await serviceWithAudit.ResetPasswordAsync(_adminUserId, request, actorUserId: Guid.NewGuid());

        auditMock.Verify(
            s => s.LogAuditAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), "Auth", _adminUserId, CephasOps.Application.Auth.AuthEventTypes.AdminPasswordReset, It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context?.Dispose();
    }
}
