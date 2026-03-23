using System.Net;
using CephasOps.Api.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CephasOps.Api.Tests.Integration;

/// <summary>
/// Admin/ops API safety: company scoping (403), not-found/out-of-scope (404), input validation (400).
/// </summary>
[Collection("InventoryIntegration")]
public class AdminApiSafetyTests : IClassFixture<CephasOpsWebApplicationFactory>
{
    private readonly CephasOpsWebApplicationFactory _factory;

    public AdminApiSafetyTests(CephasOpsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetReplayPolicy_WhenEventTypeWhitespace_Returns400()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");

        var response = await client.GetAsync("api/event-store/replay-policy/%20");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListEvents_WhenCompanyScopedUserRequestsOtherCompany_Returns403()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyA.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");

        var response = await client.GetAsync($"api/event-store/events?companyId={companyB}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Replay_Cancel_WhenOperationNotFoundOrOutOfScope_Returns404()
    {
        var companyA = Guid.NewGuid();
        var nonExistentOperationId = Guid.NewGuid();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyA.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");

        var response = await client.PostAsync(
            $"api/event-store/replay/operations/{nonExistentOperationId}/cancel",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
