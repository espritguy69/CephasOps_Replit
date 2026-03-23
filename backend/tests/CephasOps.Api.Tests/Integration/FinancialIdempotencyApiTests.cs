using System.Net;
using System.Text;
using System.Text.Json;
using CephasOps.Api.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CephasOps.Api.Tests.Integration;

/// <summary>
/// API-level verification of financial idempotency: POST create payment/invoice with IdempotencyKey
/// returns same resource on replay; same key in different tenant creates separate resources.
/// Note: These tests require a database provider that supports ExecuteUpdate/ExecuteUpdateAsync (e.g. PostgreSQL).
/// When the test run uses in-memory provider, idempotency store may return 400; run against real DB to verify.
/// </summary>
[Collection("InventoryIntegration")]
[Trait("Category", "FinancialIdempotency")]
public class FinancialIdempotencyApiTests : IClassFixture<CephasOpsWebApplicationFactory>
{
    private readonly CephasOpsWebApplicationFactory _factory;
    private readonly TenantBoundaryTestFixture _boundaryFixture;

    public FinancialIdempotencyApiTests(CephasOpsWebApplicationFactory factory)
    {
        _factory = factory;
        _boundaryFixture = new TenantBoundaryTestFixture(factory);
    }

    [Fact]
    public async Task CreatePayment_SameIdempotencyKeyTwice_ReturnsSamePaymentId_NoDuplicate()
    {
        await _boundaryFixture.SeedAsync();
        using var client = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);

        var dto = new Dictionary<string, object?>
        {
            ["idempotencyKey"] = "api-payment-idem-001",
            ["paymentType"] = "Income",
            ["paymentMethod"] = "BankTransfer",
            ["paymentDate"] = DateTime.UtcNow.Date,
            ["amount"] = 99.99m,
            ["currency"] = "MYR",
            ["payerPayeeName"] = "API Test Payer"
        };
        var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

        var response1 = await client.PostAsync("api/payments", content);
        response1.StatusCode.Should().Be(HttpStatusCode.Created, "first create should succeed: " + await response1.Content.ReadAsStringAsync());
        var id1 = await ParsePaymentIdFromResponseAsync(response1);

        content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        var response2 = await client.PostAsync("api/payments", content);
        response2.StatusCode.Should().Be(HttpStatusCode.Created, "replay with same key should return 201 with same resource: " + await response2.Content.ReadAsStringAsync());
        var id2 = await ParsePaymentIdFromResponseAsync(response2);

        id1.Should().Be(id2, "replay with same idempotency key must return the same payment (no duplicate created)");
    }

    [Fact]
    public async Task CreatePayment_SameIdempotencyKey_DifferentTenant_CreatesSeparatePayments()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var dto = new Dictionary<string, object?>
        {
            ["idempotencyKey"] = "api-payment-shared-key",
            ["paymentType"] = "Income",
            ["paymentMethod"] = "BankTransfer",
            ["paymentDate"] = DateTime.UtcNow.Date,
            ["amount"] = 50m,
            ["currency"] = "MYR",
            ["payerPayeeName"] = "Shared Key Payer"
        };

        var contentA = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        var responseA = await clientA.PostAsync("api/payments", contentA);
        responseA.StatusCode.Should().Be(HttpStatusCode.Created, "tenant A create: " + await responseA.Content.ReadAsStringAsync());
        var idA = await ParsePaymentIdFromResponseAsync(responseA);

        var contentB = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        var responseB = await clientB.PostAsync("api/payments", contentB);
        responseB.StatusCode.Should().Be(HttpStatusCode.Created);
        var idB = await ParsePaymentIdFromResponseAsync(responseB);

        idA.Should().NotBe(idB, "same idempotency key in different tenant must create separate payments (tenant-scoped keys)");
    }

    [Fact]
    public async Task CreatePayment_WithXIdempotencyKeyHeader_ReplayReturnsSamePayment()
    {
        await _boundaryFixture.SeedAsync();
        using var client = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);

        var dto = new Dictionary<string, object?>
        {
            ["paymentType"] = "Income",
            ["paymentMethod"] = "BankTransfer",
            ["paymentDate"] = DateTime.UtcNow.Date,
            ["amount"] = 11m,
            ["currency"] = "MYR",
            ["payerPayeeName"] = "Header Test"
        };
        var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.Remove("X-Idempotency-Key");
        client.DefaultRequestHeaders.Add("X-Idempotency-Key", "api-header-key-001");

        var response1 = await client.PostAsync("api/payments", content);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        var id1 = await ParsePaymentIdFromResponseAsync(response1);

        content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        var response2 = await client.PostAsync("api/payments", content);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);
        var id2 = await ParsePaymentIdFromResponseAsync(response2);

        id1.Should().Be(id2);
    }

    private static async Task<Guid> ParsePaymentIdFromResponseAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        // CreatedAtAction may return raw DTO { id, ... } or wrapped in value
        if (root.TryGetProperty("id", out var idEl) && Guid.TryParse(idEl.GetString(), out var id))
            return id;
        if (root.TryGetProperty("Id", out var idEl2) && Guid.TryParse(idEl2.GetString(), out var id2))
            return id2;
        var data = root.TryGetProperty("Data", out var d) ? d : root.TryGetProperty("data", out var d2) ? d2 : default;
        if (data.ValueKind != JsonValueKind.Undefined && data.ValueKind != JsonValueKind.Null)
        {
            if (data.TryGetProperty("id", out var di) && Guid.TryParse(di.GetString(), out var g))
                return g;
            if (data.TryGetProperty("Id", out var di2) && Guid.TryParse(di2.GetString(), out var g2))
                return g2;
        }
        throw new InvalidOperationException("Could not parse payment id from response: " + json);
    }
}
