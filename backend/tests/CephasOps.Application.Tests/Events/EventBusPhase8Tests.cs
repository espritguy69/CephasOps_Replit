using CephasOps.Application.Events;
using CephasOps.Application.Workflow.JobObservability;
using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Events;

/// <summary>
/// Phase 8 Event Bus tests: async subscriber dispatch (enqueue vs in-process), JobRun EventId/correlation for async path.
/// </summary>
public class EventBusPhase8Tests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;

    public EventBusPhase8Tests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "EventBusPhase8_" + Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
    }

    public void Dispose() => _dbContext.Dispose();

    [Fact]
    public async Task PublishAsync_WhenAsyncHandlerAndEnqueuerPresent_EnqueuesAndDoesNotCallMarkProcessed()
    {
        var mockStore = new Mock<IEventStore>();
        var mockEnqueuer = new Mock<IAsyncEventEnqueuer>();
        var inProcessInvoked = false;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDomainEventHandler<WorkflowTransitionCompletedEvent>>(
            new TestAsyncWorkflowTransitionHandler(() => inProcessInvoked = true));
        var sp = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(
            sp,
            sp.GetRequiredService<ILogger<DomainEventDispatcher>>(),
            mockStore.Object,
            envelopeBuilder: null,
            jobRunRecorder: null,
            mockEnqueuer.Object);
        var evt = CreateSampleEvent();

        await dispatcher.PublishAsync(evt);

        mockEnqueuer.Verify(
            x => x.EnqueueAsync(evt.EventId, evt, It.IsAny<CancellationToken>()),
            Times.Once);
        mockStore.Verify(
            x => x.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
        inProcessInvoked.Should().BeFalse("async handler must not run in-process when enqueuer is present");
    }

    [Fact]
    public async Task PublishAsync_WhenInProcessAndAsyncHandlersPresent_RunsInProcessThenEnqueuesAndDoesNotMarkProcessed()
    {
        var mockStore = new Mock<IEventStore>();
        var mockEnqueuer = new Mock<IAsyncEventEnqueuer>();
        var inProcessInvoked = false;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDomainEventHandler<WorkflowTransitionCompletedEvent>>(
            new TestWorkflowTransitionHandler(() => inProcessInvoked = true));
        services.AddSingleton<IDomainEventHandler<WorkflowTransitionCompletedEvent>>(
            new TestAsyncWorkflowTransitionHandler(() => { }));
        var sp = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(
            sp,
            sp.GetRequiredService<ILogger<DomainEventDispatcher>>(),
            mockStore.Object,
            envelopeBuilder: null,
            jobRunRecorder: null,
            mockEnqueuer.Object);
        var evt = CreateSampleEvent();

        await dispatcher.PublishAsync(evt);

        inProcessInvoked.Should().BeTrue("in-process handler should run");
        mockEnqueuer.Verify(
            x => x.EnqueueAsync(evt.EventId, evt, It.IsAny<CancellationToken>()),
            Times.Once);
        mockStore.Verify(
            x => x.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PublishAsync_WhenOnlyAsyncHandlersAndNoEnqueuer_MarksProcessedWithoutRunningHandlers()
    {
        var mockStore = new Mock<IEventStore>();
        var asyncInvoked = false;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDomainEventHandler<WorkflowTransitionCompletedEvent>>(
            new TestAsyncWorkflowTransitionHandler(() => asyncInvoked = true));
        var sp = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(
            sp,
            sp.GetRequiredService<ILogger<DomainEventDispatcher>>(),
            mockStore.Object,
            envelopeBuilder: null,
            jobRunRecorder: null,
            asyncEnqueuer: null);
        var evt = CreateSampleEvent();

        await dispatcher.PublishAsync(evt);

        asyncInvoked.Should().BeFalse("async handler must not run in-process when not enqueued");
        mockStore.Verify(
            x => x.MarkProcessedAsync(evt.EventId, true, null, null, It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JobRunRecorderForEvents_StartHandlerRunAsync_CreatesJobRunWithEventIdAndCorrelationId()
    {
        var recorder = new JobRunRecorder(_dbContext);
        var eventRecorder = new JobRunRecorderForEvents(recorder, Mock.Of<ILogger<JobRunRecorderForEvents>>());
        var evt = CreateSampleEvent();
        evt.CorrelationId = "trace-phase8";

        var jobRunId = await eventRecorder.StartHandlerRunAsync(evt, "TestAsyncHandler");

        jobRunId.Should().NotBe(Guid.Empty);
        var run = await _dbContext.JobRuns.FindAsync(jobRunId);
        run.Should().NotBeNull();
        run!.CorrelationId.Should().Be("trace-phase8");
        run.EventId.Should().Be(evt.EventId);
        run.JobType.Should().Be("EventHandling");
        run.TriggerSource.Should().Be("EventBus");
    }

    private static WorkflowTransitionCompletedEvent CreateSampleEvent()
    {
        return new WorkflowTransitionCompletedEvent
        {
            EventId = Guid.NewGuid(),
            EventType = "WorkflowTransitionCompleted",
            OccurredAtUtc = DateTime.UtcNow,
            CorrelationId = "test-correlation",
            CompanyId = Guid.NewGuid(),
            TriggeredByUserId = Guid.NewGuid(),
            Source = "WorkflowEngine",
            WorkflowDefinitionId = Guid.NewGuid(),
            WorkflowJobId = Guid.NewGuid(),
            FromStatus = "Pending",
            ToStatus = "InProgress",
            EntityType = "Order",
            EntityId = Guid.NewGuid()
        };
    }

    private sealed class TestWorkflowTransitionHandler : IDomainEventHandler<WorkflowTransitionCompletedEvent>
    {
        private readonly Action _onHandle;

        public TestWorkflowTransitionHandler(Action onHandle) => _onHandle = onHandle;

        public Task HandleAsync(WorkflowTransitionCompletedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            _onHandle();
            return Task.CompletedTask;
        }
    }

    private sealed class TestAsyncWorkflowTransitionHandler : IAsyncEventSubscriber<WorkflowTransitionCompletedEvent>
    {
        private readonly Action _onHandle;

        public TestAsyncWorkflowTransitionHandler(Action onHandle) => _onHandle = onHandle;

        public Task HandleAsync(WorkflowTransitionCompletedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            _onHandle();
            return Task.CompletedTask;
        }
    }
}
