using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using CephasOps.Domain.Departments.Entities;
using CephasOps.Domain.Users.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Departments;

/// <summary>
/// Tests for department access enforcement: dept A cannot access dept B inventory/data. Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class DepartmentAccessServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DepartmentAccessService _service;
    private readonly Guid _userId;
    private readonly Guid _deptA;
    private readonly Guid _deptB;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public DepartmentAccessServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        _deptA = Guid.NewGuid();
        _deptB = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<DepartmentAccessService>>().Object;
        var hostEnv = new Mock<IHostEnvironment>();
        hostEnv.Setup(h => h.EnvironmentName).Returns("Development");
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(_userId);
        currentUser.Setup(c => c.IsSuperAdmin).Returns(false);
        currentUser.Setup(c => c.Roles).Returns(new List<string> { "Member" });
        _service = new DepartmentAccessService(_context, currentUser.Object, hostEnv.Object, logger);
    }

    [Fact]
    public async Task ResolveDepartmentScopeAsync_UserInDeptA_RequestingDeptB_ThrowsUnauthorized()
    {
        // Arrange: user has membership only in dept A
        await SeedUserWithDepartmentMembershipAsync(_userId, _deptA);
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(_userId);
        currentUser.Setup(c => c.IsSuperAdmin).Returns(false);
        currentUser.Setup(c => c.Roles).Returns(new List<string> { "Member" });
        var service = new DepartmentAccessService(_context, currentUser.Object, CreateHostEnv(), new Mock<ILogger<DepartmentAccessService>>().Object);

        // Act
        var act = async () => await service.ResolveDepartmentScopeAsync(_deptB);

        // Assert: must not allow access to dept B
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*do not have access to this department*");
    }

    [Fact]
    public async Task ResolveDepartmentScopeAsync_UserInDeptA_RequestingDeptA_ReturnsDeptA()
    {
        await SeedUserWithDepartmentMembershipAsync(_userId, _deptA);
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(_userId);
        currentUser.Setup(c => c.IsSuperAdmin).Returns(false);
        currentUser.Setup(c => c.Roles).Returns(new List<string> { "Member" });
        var service = new DepartmentAccessService(_context, currentUser.Object, CreateHostEnv(), new Mock<ILogger<DepartmentAccessService>>().Object);

        var result = await service.ResolveDepartmentScopeAsync(_deptA);

        result.Should().Be(_deptA);
    }

    [Fact]
    public async Task GetAccessAsync_UserWithNoMemberships_AndNotAdmin_ReturnsNone()
    {
        // No department memberships seeded; user is not Admin/SuperAdmin
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(_userId);
        currentUser.Setup(c => c.IsSuperAdmin).Returns(false);
        currentUser.Setup(c => c.Roles).Returns(new List<string> { "Member" });
        var service = new DepartmentAccessService(_context, currentUser.Object, CreateHostEnv(), new Mock<ILogger<DepartmentAccessService>>().Object);

        var result = await service.GetAccessAsync();

        result.HasGlobalAccess.Should().BeFalse();
        result.DepartmentIds.Should().BeEmpty();
        result.DefaultDepartmentId.Should().BeNull();
    }

    [Fact]
    public async Task GetAccessAsync_UserWithNoMemberships_ButAdmin_ReturnsGlobal()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(_userId);
        currentUser.Setup(c => c.IsSuperAdmin).Returns(false);
        currentUser.Setup(c => c.Roles).Returns(new List<string> { "Admin" });
        var service = new DepartmentAccessService(_context, currentUser.Object, CreateHostEnv(), new Mock<ILogger<DepartmentAccessService>>().Object);

        var result = await service.GetAccessAsync();

        result.HasGlobalAccess.Should().BeTrue();
    }

    private static IHostEnvironment CreateHostEnv()
    {
        var mock = new Mock<IHostEnvironment>();
        mock.Setup(h => h.EnvironmentName).Returns("Development");
        return mock.Object;
    }

    private async Task SeedUserWithDepartmentMembershipAsync(Guid userId, Guid departmentId)
    {
        var companyId = _companyId;
        var dept = new Department
        {
            Id = departmentId,
            CompanyId = companyId,
            Name = "Dept",
            Code = "D1",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Departments.Add(dept);
        var membership = new DepartmentMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DepartmentId = departmentId,
            CompanyId = companyId,
            Role = "Member",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DepartmentMemberships.Add(membership);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context?.Dispose();
    }
}
