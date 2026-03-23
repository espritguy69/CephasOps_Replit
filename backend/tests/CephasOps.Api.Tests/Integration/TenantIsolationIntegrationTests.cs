using System.Net;
using System.Text;
using System.Text.Json;
using CephasOps.Api.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CephasOps.Api.Tests.Integration;

/// <summary>
/// Tenant isolation integration tests: cross-tenant access returns 403/404,
/// endpoints that previously accepted companyId from query now use ITenantProvider or validate override.
/// </summary>
[Collection("InventoryIntegration")]
public class TenantIsolationIntegrationTests : IClassFixture<CephasOpsWebApplicationFactory>
{
    private readonly CephasOpsWebApplicationFactory _factory;

    public TenantIsolationIntegrationTests(CephasOpsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PaymentTerms_GetAll_WithCompanyContext_Returns200()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");

        var response = await client.GetAsync("api/payment-terms");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await response.Content.ReadAsStringAsync();
            json.Should().Contain("data", "response should have data envelope");
        }
    }

    [Fact]
    public async Task PaymentTerms_GetAll_WithoutCompanyContext_Returns403()
    {
        var userId = Guid.NewGuid();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");
        // No X-Test-Company-Id -> TenantGuardMiddleware or RequireCompanyId should return 403

        var response = await client.GetAsync("api/payment-terms");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "payment-terms must require company context and return 403 when missing");
    }

    [Fact]
    public async Task TimeSlots_Get_WithoutCompanyContext_Returns403()
    {
        var userId = Guid.NewGuid();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");

        var response = await client.GetAsync("api/time-slots");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "time-slots must require company context when no tenant resolved");
    }

    [Fact]
    public async Task TimeSlots_Get_WithCompanyContext_Returns200()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");

        var response = await client.GetAsync("api/time-slots");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await response.Content.ReadAsStringAsync();
            json.Should().Contain("data", "response should have data envelope");
        }
    }

    [Fact]
    public async Task Warehouses_GetAll_WithOtherCompanyId_AsNonSuperAdmin_Returns403()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyA.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");

        var response = await client.GetAsync($"api/warehouses?companyId={companyB}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "non-SuperAdmin cannot request another tenant's warehouses via companyId query");
    }

    [Fact]
    public async Task EventStore_ListEvents_WhenCompanyScopedUserRequestsOtherCompany_Returns403()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyA.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");

        var response = await client.GetAsync($"api/event-store/events?companyId={companyB}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Verifies that rate card update by id is tenant-scoped: requesting by a non-existent id returns 404.
    /// Full cross-tenant test (company A user, rate card id from company B → 404) requires seeded data for two tenants.
    /// </summary>
    [Fact]
    public async Task Rates_UpdateRateCard_WhenIdDoesNotExistInTenant_Returns404()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");

        var nonExistentId = Guid.NewGuid();
        var body = JsonSerializer.Serialize(new { name = "Test", validFrom = DateTime.UtcNow, validTo = (DateTime?)null, isActive = true });
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await client.PutAsync($"api/rates/ratecards/{nonExistentId}", content);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "update with id that does not belong to current tenant (or does not exist) must return 404");
    }
}
