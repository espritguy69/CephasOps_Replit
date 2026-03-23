using System.Net;
using CephasOps.Api.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CephasOps.Api.Tests.Integration;

/// <summary>SLA Breach API: tenant endpoints require company context; platform summary requires admin.</summary>
[Collection("InventoryIntegration")]
public class SlaBreachApiTests : IClassFixture<CephasOpsWebApplicationFactory>
{
    private readonly CephasOpsWebApplicationFactory _factory;

    public SlaBreachApiTests(CephasOpsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Summary_WhenTenantUserWithCompany_Returns200()
    {
        var companyId = Guid.NewGuid();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");

        var response = await client.GetAsync("api/insights/sla/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Summary_WhenNoCompany_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");

        var response = await client.GetAsync("api/insights/sla/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OrdersAtRisk_WhenTenantUserWithCompany_Returns200()
    {
        var companyId = Guid.NewGuid();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");

        var response = await client.GetAsync("api/insights/sla/orders-at-risk");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PlatformSlaSummary_WhenSuperAdmin_Returns200()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "SuperAdmin");

        var response = await client.GetAsync("api/insights/platform-sla-summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PlatformSlaSummary_WhenMemberOnly_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");

        var response = await client.GetAsync("api/insights/platform-sla-summary");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
