using CephasOps.Application.Events;
using CephasOps.Application.Events.DTOs;
using CephasOps.Application.Events.Replay;
using FluentAssertions;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Events;

public class ReplayPreviewServiceTests
{
    [Fact]
    public async Task PreviewAsync_ReturnsCountsAndSample_WithoutExecutingHandlers()
    {
        var request = new ReplayRequestDto
        {
            EventType = "WorkflowTransitionCompleted",
            FromOccurredAtUtc = DateTime.UtcNow.AddDays(-7),
            ToOccurredAtUtc = DateTime.UtcNow,
            MaxEvents = 100
        };
        var scopeCompanyId = (Guid?)null;

        var items = new List<EventStoreListItemDto>
        {
            new()
            {
                EventId = Guid.NewGuid(),
                EventType = "WorkflowTransitionCompleted",
                OccurredAtUtc = DateTime.UtcNow.AddDays(-1),
                CreatedAtUtc = DateTime.UtcNow,
                ProcessedAtUtc = DateTime.UtcNow,
                RetryCount = 0,
                Status = "Processed",
                CorrelationId = "c1",
                CompanyId = Guid.NewGuid(),
                EntityType = "Order",
                EntityId = Guid.NewGuid(),
                LastError = null,
                LastHandler = null
            }
        };

        var queryMock = new Mock<IEventStoreQueryService>();
        queryMock
            .Setup(x => x.GetEventsForReplayAsync(It.IsAny<ReplayRequestDto>(), It.IsAny<Guid?>(), It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 1));

        var policy = new OperationalReplayPolicy(new EventReplayPolicy());
        var registry = new ReplayTargetRegistry();
        var metrics = new ReplayMetrics();
        var service = new ReplayPreviewService(queryMock.Object, policy, registry, metrics);

        var result = await service.PreviewAsync(request, scopeCompanyId, CancellationToken.None);

        result.TotalMatched.Should().Be(1);
        result.EvaluatedCount.Should().Be(1);
        result.EligibleCount.Should().Be(1);
        result.BlockedCount.Should().Be(0);
        result.SampleEvents.Should().HaveCount(1);
        result.EventTypesAffected.Should().Contain("WorkflowTransitionCompleted");
        result.SafetyWindowApplied.Should().BeTrue();
        result.SafetyCutoffOccurredAtUtc.Should().NotBeNull();
        result.SafetyWindowMinutes.Should().Be(5);
    }

    [Fact]
    public async Task PreviewAsync_BlockedEventType_CountsBlocked()
    {
        var request = new ReplayRequestDto { MaxEvents = 100 };
        var items = new List<EventStoreListItemDto>
        {
            new()
            {
                EventId = Guid.NewGuid(),
                EventType = "NonExistentEventType",
                OccurredAtUtc = DateTime.UtcNow.AddDays(-1),
                CreatedAtUtc = DateTime.UtcNow,
                ProcessedAtUtc = null,
                RetryCount = 0,
                Status = "Pending",
                CorrelationId = null,
                CompanyId = Guid.NewGuid(),
                EntityType = null,
                EntityId = null,
                LastError = null,
                LastHandler = null
            }
        };

        var queryMock = new Mock<IEventStoreQueryService>();
        queryMock
            .Setup(x => x.GetEventsForReplayAsync(It.IsAny<ReplayRequestDto>(), It.IsAny<Guid?>(), It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 1));

        var policy = new OperationalReplayPolicy(new EventReplayPolicy());
        var registry = new ReplayTargetRegistry();
        var metrics = new ReplayMetrics();
        var service = new ReplayPreviewService(queryMock.Object, policy, registry, metrics);

        var result = await service.PreviewAsync(request, null, CancellationToken.None);

        result.TotalMatched.Should().Be(1);
        result.EvaluatedCount.Should().Be(1);
        result.EligibleCount.Should().Be(0);
        result.BlockedCount.Should().Be(1);
        result.BlockedReasons.Should().NotBeEmpty();
    }
}
