using System.Net;
using System.Net.Http.Headers;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Departments.Entities;
using CephasOps.Api.Tests.Infrastructure;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CephasOps.Api.Tests.Integration;

/// <summary>
/// Integration tests proving department-scoped inventory access:
/// a user in department A cannot access department B's inventory (403).
/// </summary>
[Collection("InventoryIntegration")]
public class InventoryDepartmentAccessIntegrationTests : IClassFixture<CephasOpsWebApplicationFactory>
{
    private readonly CephasOpsWebApplicationFactory _factory;

    public InventoryDepartmentAccessIntegrationTests(CephasOpsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMaterials_UserInDeptA_RequestingDeptB_Returns403()
    {
        var companyId = Guid.NewGuid();
        var deptAId = Guid.NewGuid();
        var deptBId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await SeedDepartmentScopeDataAsync(companyId, deptAId, deptBId, userId);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");

        var response = await client.GetAsync(
            $"api/inventory/materials?departmentId={deptBId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("do not have access to this department", because: "API must return forbidden message");
    }

    [Fact]
    public async Task GetMaterials_UserInDeptA_RequestingDeptA_Returns200()
    {
        var companyId = Guid.NewGuid();
        var deptAId = Guid.NewGuid();
        var deptBId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await SeedDepartmentScopeDataAsync(companyId, deptAId, deptBId, userId);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");

        var response = await client.GetAsync(
            $"api/inventory/materials?departmentId={deptAId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task SeedDepartmentScopeDataAsync(
        Guid companyId,
        Guid deptAId,
        Guid deptBId,
        Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await IntegrationTestDbSeeder.PurgeThenSeedAsync(companyId,
            async ct =>
            {
                db.DepartmentMemberships.RemoveRange(db.DepartmentMemberships);
                db.Departments.RemoveRange(db.Departments);
                db.Companies.RemoveRange(db.Companies);
                await db.SaveChangesAsync(ct);
            },
            async ct =>
            {
                var company = new Company
                {
                    Id = companyId,
                    LegalName = "Test Company",
                    ShortName = "TC",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                db.Companies.Add(company);

                var deptA = new Department
                {
                    Id = deptAId,
                    CompanyId = companyId,
                    Name = "Dept A",
                    Code = "DA",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                var deptB = new Department
                {
                    Id = deptBId,
                    CompanyId = companyId,
                    Name = "Dept B",
                    Code = "DB",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.Departments.Add(deptA);
                db.Departments.Add(deptB);

                var membership = new DepartmentMembership
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    DepartmentId = deptAId,
                    CompanyId = companyId,
                    Role = "Member",
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.DepartmentMemberships.Add(membership);

                await db.SaveChangesAsync(ct);
            });
    }
}
