using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Companies.Enums;
using CephasOps.Domain.Departments.Entities;
using CephasOps.Domain.Users.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace CephasOps.Api.Tests.Infrastructure;

/// <summary>
/// Seeds two isolated tenants (A and B) with minimal data for tenant boundary tests.
/// Use with IClassFixture and call SeedAsync() once per test or per test class.
/// Warehouse entities are not seeded (Warehouse may not be in test DbContext model); use API to create in tests.
/// </summary>
public class TenantBoundaryTestFixture
{
    private readonly CephasOpsWebApplicationFactory _factory;
    private bool _seeded;

    public Guid CompanyA { get; private set; }
    public Guid CompanyB { get; private set; }
    public Guid UserAId { get; private set; }
    public Guid UserBId { get; private set; }
    public Guid DepartmentAId { get; private set; }
    public Guid DepartmentBId { get; private set; }

    public TenantBoundaryTestFixture(CephasOpsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Seeds Tenant A and Tenant B with companies, departments, users, and warehouses.
    /// Idempotent: safe to call multiple times; only seeds once per fixture instance.
    /// </summary>
    public async Task SeedAsync()
    {
        if (_seeded)
            return;

        CompanyA = Guid.NewGuid();
        CompanyB = Guid.NewGuid();
        UserAId = Guid.NewGuid();
        UserBId = Guid.NewGuid();
        DepartmentAId = Guid.NewGuid();
        DepartmentBId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Test seeding writes tenant-scoped entities without HTTP tenant context; use platform bypass (same as DatabaseSeeder).
        TenantSafetyGuard.EnterPlatformBypass();
        try
        {
            // Clear tenant-sensitive data so tests are deterministic
            db.DepartmentMemberships.RemoveRange(db.DepartmentMemberships);
            db.Departments.RemoveRange(db.Departments);
            db.Users.RemoveRange(db.Users);
            db.Companies.RemoveRange(db.Companies);
            await db.SaveChangesAsync();

            var now = DateTime.UtcNow;

        var companyEntityA = new Company
        {
            Id = CompanyA,
            LegalName = "Tenant A Company",
            ShortName = "TA",
            Code = "TA",
            IsActive = true,
            Status = CompanyStatus.Active,
            CreatedAt = now
        };
        var companyEntityB = new Company
        {
            Id = CompanyB,
            LegalName = "Tenant B Company",
            ShortName = "TB",
            Code = "TB",
            IsActive = true,
            Status = CompanyStatus.Active,
            CreatedAt = now
        };
        db.Companies.Add(companyEntityA);
        db.Companies.Add(companyEntityB);

        var deptA = new Department
        {
            Id = DepartmentAId,
            CompanyId = CompanyA,
            Name = "Dept A",
            Code = "DA",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var deptB = new Department
        {
            Id = DepartmentBId,
            CompanyId = CompanyB,
            Name = "Dept B",
            Code = "DB",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Departments.Add(deptA);
        db.Departments.Add(deptB);

        var userA = new User
        {
            Id = UserAId,
            Name = "User A",
            Email = "usera@tenant-a.test",
            CompanyId = CompanyA,
            IsActive = true,
            CreatedAt = now
        };
        var userB = new User
        {
            Id = UserBId,
            Name = "User B",
            Email = "userb@tenant-b.test",
            CompanyId = CompanyB,
            IsActive = true,
            CreatedAt = now
        };
        db.Users.Add(userA);
        db.Users.Add(userB);

        var membershipA = new DepartmentMembership
        {
            Id = Guid.NewGuid(),
            UserId = UserAId,
            DepartmentId = DepartmentAId,
            CompanyId = CompanyA,
            Role = "Member",
            IsDefault = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var membershipB = new DepartmentMembership
        {
            Id = Guid.NewGuid(),
            UserId = UserBId,
            DepartmentId = DepartmentBId,
            CompanyId = CompanyB,
            Role = "Member",
            IsDefault = true,
            CreatedAt = now,
            UpdatedAt = now
        };
            db.DepartmentMemberships.Add(membershipA);
            db.DepartmentMemberships.Add(membershipB);

            await db.SaveChangesAsync();
            _seeded = true;
        }
        finally
        {
            TenantSafetyGuard.ExitPlatformBypass();
        }
    }
}
