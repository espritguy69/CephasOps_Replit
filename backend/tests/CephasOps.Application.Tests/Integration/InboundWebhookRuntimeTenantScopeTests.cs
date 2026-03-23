using CephasOps.Application.Integration;
using CephasOps.Domain.Integration.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Integration;

/// <summary>
/// Regression tests for InboundWebhookRuntime tenant-scope: request with CompanyId sets scope and restores in finally;
/// request without CompanyId uses platform bypass and exits in finally.
/// </summary>
[Collection("TenantScopeTests")]
public class InboundWebhookRuntimeTenantScopeTests
{
    [Fact]
    public async Task ProcessAsync_WhenRequestCompanyIdSet_RestoresTenantScopeInFinally()
    {
        var requestCompanyId = Guid.NewGuid();
        var previousTenantId = Guid.NewGuid();
        TenantScope.CurrentTenantId = previousTenantId;

        var endpoint = new ConnectorEndpoint { Id = Guid.NewGuid(), CompanyId = requestCompanyId };
        var mockRegistry = new Mock<IConnectorRegistry>();
        mockRegistry.Setup(x => x.GetInboundEndpointAsync(It.IsAny<string>(), requestCompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(endpoint);

        var mockReceiptStore = new Mock<IInboundWebhookReceiptStore>();
        mockReceiptStore.Setup(x => x.CreateAsync(It.IsAny<InboundWebhookReceipt>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockReceiptStore.Setup(x => x.UpdateAsync(It.IsAny<InboundWebhookReceipt>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var mockIdempotency = new Mock<IExternalIdempotencyStore>();
        mockIdempotency.Setup(x => x.IsCompletedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        mockIdempotency.Setup(x => x.TryClaimAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mockIdempotency.Setup(x => x.MarkCompletedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var runtime = new InboundWebhookRuntime(
            mockRegistry.Object,
            mockReceiptStore.Object,
            mockIdempotency.Object,
            Array.Empty<IInboundWebhookVerifier>(),
            Array.Empty<IInboundWebhookHandler>(),
            new Mock<ILogger<InboundWebhookRuntime>>().Object);

        var request = new InboundWebhookRequest
        {
            ConnectorKey = "test",
            CompanyId = requestCompanyId,
            RequestBody = "{\"eventType\":\"Test\"}"
        };

        try
        {
            var result = await runtime.ProcessAsync(request);
            result.Accepted.Should().BeTrue();
            TenantScope.CurrentTenantId.Should().Be(previousTenantId, "ProcessAsync must restore tenant scope in finally when request has CompanyId");
        }
        finally
        {
            TenantScope.CurrentTenantId = null;
        }
    }

    [Fact]
    public async Task ProcessAsync_WhenRequestCompanyIdNull_UsesBypassAndExitsInFinally()
    {
        var previousTenantId = Guid.NewGuid();
        TenantScope.CurrentTenantId = previousTenantId;

        var endpoint = new ConnectorEndpoint { Id = Guid.NewGuid(), CompanyId = null };
        var mockRegistry = new Mock<IConnectorRegistry>();
        mockRegistry.Setup(x => x.GetInboundEndpointAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(endpoint);

        var mockReceiptStore = new Mock<IInboundWebhookReceiptStore>();
        mockReceiptStore.Setup(x => x.CreateAsync(It.IsAny<InboundWebhookReceipt>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockReceiptStore.Setup(x => x.UpdateAsync(It.IsAny<InboundWebhookReceipt>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var mockIdempotency = new Mock<IExternalIdempotencyStore>();
        mockIdempotency.Setup(x => x.IsCompletedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        mockIdempotency.Setup(x => x.TryClaimAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mockIdempotency.Setup(x => x.MarkCompletedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var runtime = new InboundWebhookRuntime(
            mockRegistry.Object,
            mockReceiptStore.Object,
            mockIdempotency.Object,
            Array.Empty<IInboundWebhookVerifier>(),
            Array.Empty<IInboundWebhookHandler>(),
            new Mock<ILogger<InboundWebhookRuntime>>().Object);

        var request = new InboundWebhookRequest
        {
            ConnectorKey = "test",
            CompanyId = null,
            RequestBody = "{\"eventType\":\"Test\"}"
        };

        try
        {
            var result = await runtime.ProcessAsync(request);
            result.Accepted.Should().BeTrue();
            TenantSafetyGuard.IsPlatformBypassActive.Should().BeFalse("ProcessAsync must exit platform bypass in finally when request has no CompanyId");
            TenantScope.CurrentTenantId.Should().Be(previousTenantId, "caller scope should be unchanged");
        }
        finally
        {
            TenantScope.CurrentTenantId = null;
        }
    }
}
