using System.Net;
using System.Text.Json;
using CephasOps.Api.Tests.Infrastructure;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Departments.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CephasOps.Api.Tests.Integration;

/// <summary>
/// Minimal API smoke tests: health, ProblemDetails/correlationId, RBAC department guard, key endpoints 200.
/// Uses CephasOpsWebApplicationFactory (Testing env, InMemory DB, Test auth headers).
/// </summary>
[Collection("InventoryIntegration")]
public class ApiSmokeTests : IClassFixture<CephasOpsWebApplicationFactory>
{
    private readonly CephasOpsWebApplicationFactory _factory;

    public ApiSmokeTests(CephasOpsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_Returns200_AndDatabaseConnected()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "SuperAdmin");

        var response = await client.GetAsync("api/admin/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var data = root.TryGetProperty("Data", out var d) ? d : root.TryGetProperty("data", out var d2) ? d2 : default;
        if (data.ValueKind == JsonValueKind.Object)
        {
            var isHealthy = data.TryGetProperty("IsHealthy", out var h) && h.GetBoolean()
                           || data.TryGetProperty("isHealthy", out var h2) && h2.GetBoolean();
            isHealthy.Should().BeTrue("health should report IsHealthy true");

            var db = data.TryGetProperty("Database", out var db1) ? db1 : data.TryGetProperty("database", out var db2) ? db2 : default;
            if (db.ValueKind == JsonValueKind.Object)
            {
                var connected = db.TryGetProperty("IsConnected", out var c) && c.GetBoolean()
                                || db.TryGetProperty("isConnected", out var c2) && c2.GetBoolean();
                connected.Should().BeTrue("database should be connected (InMemory in Testing)");
            }
        }
    }

    [Fact]
    public async Task CorrelationId_ResponseIncludesHeader()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "SuperAdmin");
        client.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);

        var response = await client.GetAsync("api/admin/health");
        response.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
        values!.FirstOrDefault().Should().Be(correlationId);
    }

    [Fact]
    public async Task CorrelationId_PropagatedInResponseHeader()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");
        client.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);

        var response = await client.GetAsync("api/nonexistent-route-for-smoke");
        response.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
        values!.FirstOrDefault().Should().Be(correlationId);
    }

    [Fact]
    public async Task ProblemDetails_WhenReturned_IncludesCorrelationIdInExtensions()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");
        client.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);

        var response = await client.GetAsync("api/nonexistent-route-for-smoke");
        response.Headers.TryGetValues("X-Correlation-Id", out var headerValues).Should().BeTrue();
        headerValues!.FirstOrDefault().Should().Be(correlationId);

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
        if (contentType.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase))
        {
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var hasCorrelationId = root.TryGetProperty("correlationId", out var ext) && ext.ValueKind == JsonValueKind.String;
            (hasCorrelationId || root.TryGetProperty("extensions", out var extensions) && extensions.TryGetProperty("correlationId", out _)).Should().BeTrue(
                "ProblemDetails response must include correlationId (GlobalExceptionHandler adds it to Extensions)");
        }
    }

    [Fact]
    public async Task DepartmentScoped_UserInDeptA_RequestingDeptB_Returns403()
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

        var response = await client.GetAsync($"api/inventory/materials?departmentId={deptBId}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DepartmentScoped_UserInDeptA_RequestingDeptA_Returns200()
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

        var response = await client.GetAsync($"api/inventory/materials?departmentId={deptAId}");
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
                    LegalName = "Smoke Test Company",
                    ShortName = "STC",
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
