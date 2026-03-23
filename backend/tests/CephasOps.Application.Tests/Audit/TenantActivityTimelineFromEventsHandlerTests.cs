using CephasOps.Application.Audit;
using CephasOps.Application.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Audit;

/// <summary>
/// Tests for TenantActivityTimelineFromEventsHandler: tenant context is required for timeline recording;
/// events with CompanyId set are recorded; events with null/empty CompanyId are skipped (no cross-tenant or platform leak).
/// </summary>
public class TenantActivityTimelineFromEventsHandlerTests
{
    private readonly Mock<ITenantActivityService> _timeline;
    private readonly Mock<ILogger<TenantActivityTimelineFromEventsHandler>> _logger;
    private readonly TenantActivityTimelineFromEventsHandler _handler;

    public TenantActivityTimelineFromEventsHandlerTests()
    {
        _timeline = new Mock<ITenantActivityService>();
        _logger = new Mock<ILogger<TenantActivityTimelineFromEventsHandler>>();
        _handler = new TenantActivityTimelineFromEventsHandler(_timeline.Object, _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_OrderCreatedEvent_WithCompanyId_RecordsToTimeline()
    {
        var companyId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var evt = new OrderCreatedEvent
        {
            EventId = Guid.NewGuid(),
            EventType = PlatformEventTypes.OrderCreated,
            CompanyId = companyId,
            OrderId = orderId,
            OccurredAtUtc = DateTime.UtcNow
        };

        await _handler.HandleAsync(evt);

        _timeline.Verify(
            x => x.RecordAsync(
                companyId,
                PlatformEventTypes.OrderCreated,
                "Order",
                orderId,
                "Order created",
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_OrderCreatedEvent_WithNullCompanyId_DoesNotRecord()
    {
        var evt = new OrderCreatedEvent
        {
            EventId = Guid.NewGuid(),
            EventType = PlatformEventTypes.OrderCreated,
            CompanyId = null,
            OrderId = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow
        };

        await _handler.HandleAsync(evt);

        _timeline.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_OrderCreatedEvent_WithEmptyCompanyId_DoesNotRecord()
    {
        var evt = new OrderCreatedEvent
        {
            EventId = Guid.NewGuid(),
            EventType = PlatformEventTypes.OrderCreated,
            CompanyId = Guid.Empty,
            OrderId = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow
        };

        await _handler.HandleAsync(evt);

        _timeline.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_OrderCompletedEvent_WithCompanyId_RecordsToTimeline()
    {
        var companyId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var evt = new OrderCompletedEvent
        {
            EventId = Guid.NewGuid(),
            EventType = PlatformEventTypes.OrderCompleted,
            CompanyId = companyId,
            OrderId = orderId,
            OccurredAtUtc = DateTime.UtcNow
        };

        await _handler.HandleAsync(evt);

        _timeline.Verify(
            x => x.RecordAsync(
                companyId,
                PlatformEventTypes.OrderCompleted,
                "Order",
                orderId,
                "Order completed",
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_OrderStatusChangedEvent_WithCompanyId_RecordsToTimeline()
    {
        var companyId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var evt = new OrderStatusChangedEvent
        {
            EventId = Guid.NewGuid(),
            EventType = PlatformEventTypes.OrderStatusChanged,
            CompanyId = companyId,
            OrderId = orderId,
            NewStatus = "InProgress",
            OccurredAtUtc = DateTime.UtcNow
        };

        await _handler.HandleAsync(evt);

        _timeline.Verify(
            x => x.RecordAsync(
                companyId,
                PlatformEventTypes.OrderStatusChanged,
                "Order",
                orderId,
                "Order status changed",
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
