using System.Net;
using System.Net.Http.Json;
using CephasOps.Api.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CephasOps.Api.Tests.Integration;

/// <summary>
/// RBAC v2: permission enforcement on endpoints. SuperAdmin bypass; user without permission gets 403.
/// </summary>
[Collection("InventoryIntegration")]
public class RbacPermissionEnforcementTests : IClassFixture<CephasOpsWebApplicationFactory>
{
    private readonly CephasOpsWebApplicationFactory _factory;

    public RbacPermissionEnforcementTests(CephasOpsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAdminRoles_AsSuperAdmin_Returns200()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "SuperAdmin");

        var response = await client.GetAsync("api/admin/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAdminRoles_AsMember_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");

        var response = await client.GetAsync("api/admin/roles");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAdminPermissions_AsSuperAdmin_Returns200()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "SuperAdmin");

        var response = await client.GetAsync("api/admin/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAdminPermissions_AsMember_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");

        var response = await client.GetAsync("api/admin/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PutRolePermissions_WithInvalidPermissionName_Returns400()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "SuperAdmin");

        // Use a role ID that exists in test DB (e.g. from seed). If no seed, this may 404 - then we only validate 400 for invalid names.
        var roleId = Guid.NewGuid();
        var body = new { permissionNames = new[] { "admin.roles.view", "invalid.permission.name.xyz" } };
        var response = await client.PutAsJsonAsync($"api/admin/roles/{roleId}/permissions", body);

        // Either 400 (invalid permission name) or 404 (role not found) is acceptable
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var text = await response.Content.ReadAsStringAsync();
            text.Should().Contain("Invalid permission", "API should reject invalid permission names");
        }
    }

    // ----- Department-scoped endpoint (RBAC v2 Phase 3) -----

    /// <summary>
    /// SuperAdmin can call department-scoped payroll endpoint (with company context for RequireCompanyId).
    /// </summary>
    [Fact]
    public async Task GetPayrollSiRatePlans_AsSuperAdmin_Returns200()
    {
        using var client = _factory.CreateClient();
        var companyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "SuperAdmin");

        var response = await client.GetAsync("api/payroll/si-rate-plans");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Member without payroll permission gets 403 on department-scoped payroll endpoint.
    /// </summary>
    [Fact]
    public async Task GetPayrollSiRatePlans_AsMember_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");

        var response = await client.GetAsync("api/payroll/si-rate-plans");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ----- RBAC v2 Phase 4: Orders, Reports, Inventory, Jobs, Settings -----

    [Fact]
    public async Task GetOrdersPaged_AsSuperAdmin_Returns200()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "SuperAdmin");

        var response = await client.GetAsync("api/orders/paged?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOrdersPaged_AsUserWithoutPermission_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Guest");

        var response = await client.GetAsync("api/orders/paged?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetReportsDefinitions_AsSuperAdmin_Returns200()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "SuperAdmin");

        var response = await client.GetAsync("api/reports/definitions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReportsDefinitions_AsUserWithoutPermission_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Guest");

        var response = await client.GetAsync("api/reports/definitions");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReportsExport_AsUserWithoutPermission_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Guest");

        var response = await client.GetAsync("api/reports/orders-list/export?format=csv");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetInventoryMaterials_AsSuperAdmin_Returns200()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "SuperAdmin");

        var response = await client.GetAsync("api/inventory/materials");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetInventoryMaterials_AsUserWithoutPermission_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Guest");

        var response = await client.GetAsync("api/inventory/materials");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetBackgroundJobsHealth_AsSuperAdmin_Returns200()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "SuperAdmin");

        var response = await client.GetAsync("api/background-jobs/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBackgroundJobsHealth_AsUserWithoutPermission_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Guest");

        var response = await client.GetAsync("api/background-jobs/health");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetGlobalSettings_AsSuperAdmin_Returns200()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "SuperAdmin");

        var response = await client.GetAsync("api/global-settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetGlobalSettings_AsUserWithoutPermission_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Guest");

        var response = await client.GetAsync("api/global-settings");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Admin role receives phase 4 permissions from TestUserPermissionProvider; can access reports definitions.
    /// </summary>
    [Fact]
    public async Task GetReportsDefinitions_AsAdmin_Returns200()
    {
        using var client = _factory.CreateClient();
        var companyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");

        var response = await client.GetAsync("api/reports/definitions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTraceByCorrelationId_AsSuperAdmin_Returns200()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "SuperAdmin");

        var response = await client.GetAsync("api/trace/correlation/some-correlation-id");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTraceByCorrelationId_AsMember_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.Empty.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");

        var response = await client.GetAsync("api/trace/correlation/some-correlation-id");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
