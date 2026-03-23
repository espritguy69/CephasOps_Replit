using CephasOps.Application.Events;
using CephasOps.Application.Integration;
using CephasOps.Domain.Integration.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CephasOps.Application.Tests.Integration;

public class OutboundIntegrationBusTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "OutboundBus_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task PublishAsync_creates_no_delivery_when_no_endpoints_match()
    {
        await using var context = CreateContext();
        var def = new ConnectorDefinition
        {
            Id = Guid.NewGuid(),
            ConnectorKey = "test",
            DisplayName = "Test",
            ConnectorType = "Webhook",
            Direction = "Outbound",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.ConnectorDefinitions.Add(def);
        await context.SaveChangesAsync();

        var registry = new ConnectorRegistry(context);
        var store = new OutboundDeliveryStore(context);
        var dispatcher = new FakeHttpDispatcher();
        var mappers = new List<IIntegrationPayloadMapper> { new DefaultIntegrationPayloadMapper() };
        var signers = new List<IOutboundSigner> { new NoOpOutboundSigner() };
        var bus = new OutboundIntegrationBus(registry, store, dispatcher, mappers, signers, NullLogger<OutboundIntegrationBus>.Instance);

        var envelope = new PlatformEventEnvelope
        {
            EventId = Guid.NewGuid(),
            EventName = "Order.Created",
            OccurredAtUtc = DateTime.UtcNow,
            CompanyId = Guid.NewGuid(),
            Payload = "{}"
        };

        await bus.PublishAsync(envelope);

        var deliveries = await context.OutboundIntegrationDeliveries.ToListAsync();
        Assert.Empty(deliveries);
    }

    [Fact]
    public async Task PublishAsync_creates_delivery_when_endpoint_matches()
    {
        await using var context = CreateContext();
        var def = new ConnectorDefinition
        {
            Id = Guid.NewGuid(),
            ConnectorKey = "webhook1",
            DisplayName = "Webhook 1",
            ConnectorType = "Webhook",
            Direction = "Outbound",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.ConnectorDefinitions.Add(def);
        var companyId = Guid.NewGuid();
        var endpoint = new ConnectorEndpoint
        {
            Id = Guid.NewGuid(),
            ConnectorDefinitionId = def.Id,
            CompanyId = companyId,
            EndpointUrl = "https://example.com/hook",
            HttpMethod = "POST",
            RetryCount = 2,
            TimeoutSeconds = 10,
            IsActive = true,
            IsPaused = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.ConnectorEndpoints.Add(endpoint);
        await context.SaveChangesAsync();

        var registry = new ConnectorRegistry(context);
        var store = new OutboundDeliveryStore(context);
        var dispatcher = new FakeHttpDispatcher { Success = true };
        var mappers = new List<IIntegrationPayloadMapper> { new DefaultIntegrationPayloadMapper() };
        var signers = new List<IOutboundSigner> { new NoOpOutboundSigner() };
        var bus = new OutboundIntegrationBus(registry, store, dispatcher, mappers, signers, NullLogger<OutboundIntegrationBus>.Instance);

        var envelope = new PlatformEventEnvelope
        {
            EventId = Guid.NewGuid(),
            EventName = "Order.Created",
            OccurredAtUtc = DateTime.UtcNow,
            CompanyId = companyId,
            Payload = "{}"
        };

        await bus.PublishAsync(envelope);

        var deliveries = await context.OutboundIntegrationDeliveries.ToListAsync();
        Assert.Single(deliveries);
        Assert.Equal(OutboundIntegrationDelivery.Statuses.Delivered, deliveries[0].Status);
        Assert.Equal(envelope.EventId, deliveries[0].SourceEventId);
        Assert.Equal(endpoint.Id, deliveries[0].ConnectorEndpointId);
    }

    private sealed class FakeHttpDispatcher : IOutboundHttpDispatcher
    {
        public bool Success { get; set; }

        public Task<HttpDispatchResult> SendAsync(string url, string httpMethod, string payloadJson,
            IReadOnlyDictionary<string, string> headers, int timeoutSeconds, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HttpDispatchResult
            {
                Success = Success,
                HttpStatusCode = Success ? 200 : 500,
                DurationMs = 1
            });
        }
    }
}
