using System.Net;
using System.Text.Json;
using CephasOps.Api.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CephasOps.Api.Tests.Integration.Orders;

/// <summary>
/// Phase 1 API integration tests for orders: GET /api/orders with valid auth, missing company (403), no auth (401).
/// Uses existing test auth headers (X-Test-User-Id, X-Test-Company-Id, X-Test-Roles).
/// </summary>
[Collection("InventoryIntegration")]
public class OrdersApiTests : IClassFixture<CephasOpsWebApplicationFactory>
{
    private readonly CephasOpsWebApplicationFactory _factory;

    public OrdersApiTests(CephasOpsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetOrders_WithValidAuthAndCompany_Returns200_AndJsonArray()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");

        var response = await client.GetAsync("api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("application/json");

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var data = root.TryGetProperty("Data", out var d) ? d : root.TryGetProperty("data", out var d2) ? d2 : default;
        data.ValueKind.Should().Be(JsonValueKind.Array, "orders response should contain a list");
    }

    [Fact]
    public async Task GetOrders_WithUserButNoCompany_Returns403()
    {
        var userId = Guid.NewGuid();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");
        // No X-Test-Company-Id -> tenant guard should return 403

        var response = await client.GetAsync("api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>Unauthorized access blocked: no auth headers yields 401 or 403.</summary>
    [Fact]
    public async Task GetOrders_WithNoAuth_Returns401Or403()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("api/orders");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }
}
