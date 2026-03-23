using CephasOps.Application.Events;
using CephasOps.Application.Events.Replay;
using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Events;

public class EventReplayPolicyTests
{
    private readonly IEventReplayPolicy _policy = new EventReplayPolicy();

    [Fact]
    public void WorkflowTransitionCompleted_IsReplayAllowed()
    {
        _policy.IsReplayAllowed("WorkflowTransitionCompleted").Should().BeTrue();
        _policy.IsReplayBlocked("WorkflowTransitionCompleted").Should().BeFalse();
    }

    [Fact]
    public void OrderCreated_IsReplayAllowed()
    {
        _policy.IsReplayAllowed(PlatformEventTypes.OrderCreated).Should().BeTrue();
        _policy.IsReplayAllowed("OrderCreated").Should().BeTrue();
    }

    [Theory]
    [InlineData("UnknownEvent")]
    [InlineData("NonExistentEventType")]
    [InlineData("")]
    public void UnknownOrEmpty_IsNotReplayAllowed(string eventType)
    {
        _policy.IsReplayAllowed(eventType).Should().BeFalse();
        if (!string.IsNullOrEmpty(eventType))
            _policy.IsReplayBlocked(eventType).Should().BeTrue();
    }
}

public class EventTypeRegistryTests
{
    private readonly IEventTypeRegistry _registry = new EventTypeRegistry();

    [Fact]
    public void GetEventType_WorkflowTransitionCompleted_ReturnsType()
    {
        _registry.GetEventType("WorkflowTransitionCompleted").Should().Be(typeof(WorkflowTransitionCompletedEvent));
    }

    [Fact]
    public void Deserialize_ValidWorkflowTransitionPayload_ReturnsEvent()
    {
        var json = """{"EventId":"a1b2c3d4-0000-0000-0000-000000000001","EventType":"WorkflowTransitionCompleted","OccurredAtUtc":"2026-03-09T12:00:00Z","CorrelationId":"c1","CompanyId":"b2b2b2b2-0000-0000-0000-000000000002","FromStatus":"Pending","ToStatus":"Assigned","EntityType":"Order","EntityId":"e3e3e3e3-0000-0000-0000-000000000003","WorkflowJobId":"f4f4f4f4-0000-0000-0000-000000000004","WorkflowDefinitionId":"d5d5d5d5-0000-0000-0000-000000000005"}""";
        var evt = _registry.Deserialize("WorkflowTransitionCompleted", json);
        evt.Should().NotBeNull();
        evt!.EventId.Should().Be(Guid.Parse("a1b2c3d4-0000-0000-0000-000000000001"));
        evt.EventType.Should().Be("WorkflowTransitionCompleted");
    }

    [Fact]
    public void Deserialize_UnknownEventType_ReturnsNull()
    {
        _registry.Deserialize("UnknownType", "{}").Should().BeNull();
    }
}

/// <summary>
/// Regression tests for EventReplayService tenant-scope: dispatch runs under event's CompanyId and restores scope in finally.
/// </summary>
[Collection("TenantScopeTests")]
public class EventReplayServiceTenantScopeTests
{
    [Fact]
    public async Task ReplayAsync_WhenEntryHasCompanyId_RestoresTenantScopeAfterDispatch()
    {
        var eventId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var entry = new EventStoreEntry
        {
            EventId = eventId,
            EventType = "WorkflowTransitionCompleted",
            Payload = """{"EventId":"a1b2c3d4-0000-0000-0000-000000000001","EventType":"WorkflowTransitionCompleted","OccurredAtUtc":"2026-03-09T12:00:00Z","CompanyId":"b2b2b2b2-0000-0000-0000-000000000002","FromStatus":"Pending","ToStatus":"Assigned","EntityType":"Order","EntityId":"e3e3e3e3-0000-0000-0000-000000000003","WorkflowJobId":"f4f4f4f4-0000-0000-0000-000000000004","WorkflowDefinitionId":"d5d5d5d5-0000-0000-0000-000000000005"}""",
            CompanyId = companyId
        };

        var mockStore = new Mock<IEventStore>();
        mockStore.Setup(x => x.GetByEventIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(entry);
        mockStore.Setup(x => x.MarkAsProcessingAsync(eventId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var mockDispatcher = new Mock<IDomainEventDispatcher>();
        mockDispatcher
            .Setup(x => x.DispatchToHandlersAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var replayContextAccessor = new ReplayExecutionContextAccessor();
        var replayPolicy = new EventReplayPolicy();
        var replayService = new EventReplayService(
            mockStore.Object,
            mockDispatcher.Object,
            new EventTypeRegistry(),
            replayPolicy,
            replayContextAccessor,
            new Mock<ILogger<EventReplayService>>().Object);

        var outerScope = Guid.NewGuid(); // simulate caller's tenant scope
        TenantScope.CurrentTenantId = outerScope;
        try
        {
            var result = await replayService.ReplayAsync(eventId, scopeCompanyId: null, initiatedByUserId: null);
            result.Success.Should().BeTrue();
            TenantScope.CurrentTenantId.Should().Be(outerScope, "ReplayAsync must restore tenant scope in finally to caller's value");
        }
        finally
        {
            TenantScope.CurrentTenantId = null;
        }
    }

    [Fact]
    public async Task ReplayAsync_WhenEntryHasNoCompanyId_RestoresBypassAndScopeAfterDispatch()
    {
        var eventId = Guid.NewGuid();
        var entry = new EventStoreEntry
        {
            EventId = eventId,
            EventType = "WorkflowTransitionCompleted",
            Payload = """{"EventId":"a1b2c3d4-0000-0000-0000-000000000001","EventType":"WorkflowTransitionCompleted","OccurredAtUtc":"2026-03-09T12:00:00Z","CompanyId":null,"FromStatus":"Pending","ToStatus":"Assigned","EntityType":"Order","EntityId":"e3e3e3e3-0000-0000-0000-000000000003","WorkflowJobId":"f4f4f4f4-0000-0000-0000-000000000004","WorkflowDefinitionId":"d5d5d5d5-0000-0000-0000-000000000005"}""",
            CompanyId = null
        };

        var mockStore = new Mock<IEventStore>();
        mockStore.Setup(x => x.GetByEventIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(entry);
        mockStore.Setup(x => x.MarkAsProcessingAsync(eventId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var mockDispatcher = new Mock<IDomainEventDispatcher>();
        mockDispatcher
            .Setup(x => x.DispatchToHandlersAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var replayContextAccessor = new ReplayExecutionContextAccessor();
        var replayService = new EventReplayService(
            mockStore.Object,
            mockDispatcher.Object,
            new EventTypeRegistry(),
            new EventReplayPolicy(),
            replayContextAccessor,
            new Mock<ILogger<EventReplayService>>().Object);

        var outerScope = Guid.NewGuid();
        TenantScope.CurrentTenantId = outerScope;
        try
        {
            var result = await replayService.ReplayAsync(eventId, scopeCompanyId: null, initiatedByUserId: null);
            result.Success.Should().BeTrue();
            TenantScope.CurrentTenantId.Should().Be(outerScope, "ReplayAsync (no CompanyId) must exit bypass and restore tenant scope in finally");
        }
        finally
        {
            TenantScope.CurrentTenantId = null;
        }
    }

    [Fact]
    public async Task ReplayAsync_WhenScopeCompanyIdDoesNotMatchEntry_ReturnsNotInScope()
    {
        var eventId = Guid.NewGuid();
        var eventCompanyId = Guid.NewGuid();
        var scopeCompanyId = Guid.NewGuid();
        var entry = new EventStoreEntry
        {
            EventId = eventId,
            EventType = "WorkflowTransitionCompleted",
            Payload = "{}",
            CompanyId = eventCompanyId
        };

        var mockStore = new Mock<IEventStore>();
        mockStore.Setup(x => x.GetByEventIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(entry);

        var replayService = new EventReplayService(
            mockStore.Object,
            new Mock<IDomainEventDispatcher>().Object,
            new EventTypeRegistry(),
            new EventReplayPolicy(),
            new ReplayExecutionContextAccessor(),
            new Mock<ILogger<EventReplayService>>().Object);

        var result = await replayService.ReplayAsync(eventId, scopeCompanyId, null);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Event not in scope.");
    }

    [Fact]
    public async Task RetryAsync_WhenScopeCompanyIdDoesNotMatchEntry_ReturnsNotInScope()
    {
        var eventId = Guid.NewGuid();
        var eventCompanyId = Guid.NewGuid();
        var scopeCompanyId = Guid.NewGuid();
        var entry = new EventStoreEntry
        {
            EventId = eventId,
            EventType = "WorkflowTransitionCompleted",
            Payload = "{}",
            CompanyId = eventCompanyId
        };

        var mockStore = new Mock<IEventStore>();
        mockStore.Setup(x => x.GetByEventIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(entry);

        var replayService = new EventReplayService(
            mockStore.Object,
            new Mock<IDomainEventDispatcher>().Object,
            new EventTypeRegistry(),
            new EventReplayPolicy(),
            new ReplayExecutionContextAccessor(),
            new Mock<ILogger<EventReplayService>>().Object);

        var result = await replayService.RetryAsync(eventId, scopeCompanyId, null);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Event not in scope.");
    }
}
