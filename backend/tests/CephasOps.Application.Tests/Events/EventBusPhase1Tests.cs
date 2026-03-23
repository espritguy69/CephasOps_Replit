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
/// Phase 1 Event Bus tests: dispatch, persistence, correlation, handler execution, retry/failure.
/// EventStore append requires TenantScope (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class EventBusPhase1Tests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;

    public EventBusPhase1Tests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "EventBus_" + Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
    }

    public void Dispose() => _dbContext.Dispose();

    #region Event dispatch

    [Fact]
    public async Task PublishAsync_WhenStorePresent_AppendsThenDispatchesAndMarksProcessed()
    {
        var mockStore = new Mock<IEventStore>();
        var handled = false;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDomainEventHandler<WorkflowTransitionCompletedEvent>>(
            new TestWorkflowTransitionHandler(() => handled = true));
        var sp = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(sp, sp.GetRequiredService<ILogger<DomainEventDispatcher>>(), mockStore.Object, null);
        var evt = CreateSampleEvent();

        await dispatcher.PublishAsync(evt);

        mockStore.Verify(x => x.AppendAsync(evt, null, It.IsAny<CancellationToken>()), Times.Once);
        mockStore.Verify(x => x.MarkAsProcessingAsync(evt.EventId, It.IsAny<CancellationToken>()), Times.Once);
        handled.Should().BeTrue();
        mockStore.Verify(x => x.MarkProcessedAsync(evt.EventId, true, null, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenNoStore_DispatchesOnly()
    {
        var handled = false;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDomainEventHandler<WorkflowTransitionCompletedEvent>>(
            new TestWorkflowTransitionHandler(() => handled = true));
        var sp = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(sp, sp.GetRequiredService<ILogger<DomainEventDispatcher>>(), eventStore: null, envelopeBuilder: null, jobRunRecorder: null);

        await dispatcher.PublishAsync(CreateSampleEvent());

        handled.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchToHandlersAsync_InvokesAllHandlers()
    {
        var count = 0;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDomainEventHandler<WorkflowTransitionCompletedEvent>>(
            new TestWorkflowTransitionHandler(() => count++));
        services.AddSingleton<IDomainEventHandler<WorkflowTransitionCompletedEvent>>(
            new TestWorkflowTransitionHandler(() => count += 10));
        var sp = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(sp, sp.GetRequiredService<ILogger<DomainEventDispatcher>>(), null, envelopeBuilder: null, null);

        await dispatcher.DispatchToHandlersAsync(CreateSampleEvent());

        count.Should().Be(11);
    }

    #endregion

    #region Event persistence

    [Fact]
    public async Task EventStoreRepository_AppendAsync_PersistsEntry()
    {
        var repo = new EventStoreRepository(_dbContext);
        var evt = CreateSampleEvent();
        evt.CorrelationId = "corr-append-test";

        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = evt.CompanyId;
            await repo.AppendAsync(evt);
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }

        var entry = await _dbContext.Set<EventStoreEntry>().FirstOrDefaultAsync(e => e.EventId == evt.EventId);
        entry.Should().NotBeNull();
        entry!.EventType.Should().Be("WorkflowTransitionCompleted");
        entry.Status.Should().Be("Pending");
        entry.RetryCount.Should().Be(0);
        entry.CorrelationId.Should().Be("corr-append-test");
        entry.CompanyId.Should().Be(evt.CompanyId);
    }

    [Fact]
    public async Task EventStoreRepository_MarkProcessedAsync_Success_SetsProcessed()
    {
        var repo = new EventStoreRepository(_dbContext);
        var evt = CreateSampleEvent();
        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = evt.CompanyId;
            await repo.AppendAsync(evt);
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }

        await repo.MarkProcessedAsync(evt.EventId, success: true);

        var entry = await _dbContext.Set<EventStoreEntry>().FirstOrDefaultAsync(e => e.EventId == evt.EventId);
        entry.Should().NotBeNull();
        entry!.Status.Should().Be("Processed");
        entry.ProcessedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task EventStoreRepository_MarkProcessedAsync_Failure_IncrementsRetryAndSetsFailedAndNextRetryAtUtc()
    {
        var repo = new EventStoreRepository(_dbContext);
        var evt = CreateSampleEvent();
        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = evt.CompanyId;
            await repo.AppendAsync(evt);
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }

        await repo.MarkProcessedAsync(evt.EventId, success: false, "Handler threw");

        var entry = await _dbContext.Set<EventStoreEntry>().FirstOrDefaultAsync(e => e.EventId == evt.EventId);
        entry.Should().NotBeNull();
        entry!.Status.Should().Be("Failed");
        entry.RetryCount.Should().Be(1);
        entry.NextRetryAtUtc.Should().NotBeNull();
        entry.LastError.Should().Be("Handler threw");
    }

    [Fact]
    public async Task EventStoreRepository_AppendInCurrentTransaction_WhenSaveChangesCalled_PersistsEntry()
    {
        var repo = new EventStoreRepository(_dbContext);
        var evt = CreateSampleEvent();
        evt.CorrelationId = "outbox-test";

        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = evt.CompanyId;
            repo.AppendInCurrentTransaction(evt);
            await _dbContext.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }

        var entry = await _dbContext.Set<EventStoreEntry>().AsNoTracking().FirstOrDefaultAsync(e => e.EventId == evt.EventId);
        entry.Should().NotBeNull();
        entry!.EventType.Should().Be("WorkflowTransitionCompleted");
        entry.Status.Should().Be("Pending");
        entry.CorrelationId.Should().Be("outbox-test");
    }

    [Fact]
    public async Task EventStoreRepository_MarkProcessedAsync_AfterMaxRetries_SetsDeadLetter()
    {
        var repo = new EventStoreRepository(_dbContext);
        var evt = CreateSampleEvent();
        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = evt.CompanyId;
            await repo.AppendAsync(evt);
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }

        for (int i = 0; i < EventStoreRepository.MaxRetriesBeforeDeadLetter; i++)
            await repo.MarkProcessedAsync(evt.EventId, success: false, "Simulated failure");

        var entry = await _dbContext.Set<EventStoreEntry>().FirstOrDefaultAsync(e => e.EventId == evt.EventId);
        entry.Should().NotBeNull();
        entry!.Status.Should().Be("DeadLetter");
        entry.RetryCount.Should().Be(EventStoreRepository.MaxRetriesBeforeDeadLetter);
    }

    #endregion

    #region Correlation propagation

    [Fact]
    public void WorkflowTransitionCompletedEvent_CorrelationId_Propagated()
    {
        var evt = CreateSampleEvent();
        evt.CorrelationId = "http-request-123";

        evt.CorrelationId.Should().Be("http-request-123");
        evt.EventId.Should().NotBe(Guid.Empty);
        evt.CompanyId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task JobRunRecorderForEvents_StartHandlerRunAsync_CreatesJobRunWithCorrelationId()
    {
        var recorder = new JobRunRecorder(_dbContext);
        var eventRecorder = new JobRunRecorderForEvents(recorder, Mock.Of<ILogger<JobRunRecorderForEvents>>());
        var evt = CreateSampleEvent();
        evt.CorrelationId = "trace-456";

        var jobRunId = await eventRecorder.StartHandlerRunAsync(evt, "TestHandler");

        jobRunId.Should().NotBe(Guid.Empty);
        var run = await _dbContext.JobRuns.FindAsync(jobRunId);
        run.Should().NotBeNull();
        run!.CorrelationId.Should().Be("trace-456");
        run.JobType.Should().Be("EventHandling");
        run.TriggerSource.Should().Be("EventBus");
    }

    #endregion

    #region Handler execution and JobRun

    [Fact]
    public async Task PublishAsync_WhenJobRunRecorderPresent_CompletesJobRunOnSuccess()
    {
        var mockStore = new Mock<IEventStore>();
        var mockJobRunRecorder = new Mock<IJobRunRecorderForEvents>();
        Guid? completedJobRunId = null;
        mockJobRunRecorder.Setup(x => x.StartHandlerRunAsync(It.IsAny<IDomainEvent>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        mockJobRunRecorder.Setup(x => x.CompleteHandlerRunAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((id, _) => completedJobRunId = id)
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDomainEventHandler<WorkflowTransitionCompletedEvent>>(
            new TestWorkflowTransitionHandler(() => { }));
        var sp = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(sp, sp.GetRequiredService<ILogger<DomainEventDispatcher>>(), mockStore.Object, envelopeBuilder: null, mockJobRunRecorder.Object);

        await dispatcher.PublishAsync(CreateSampleEvent());

        mockJobRunRecorder.Verify(x => x.CompleteHandlerRunAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        completedJobRunId.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishAsync_WhenHandlerFails_FailsJobRunAndMarksEventFailed()
    {
        var mockStore = new Mock<IEventStore>();
        var jobRunId = Guid.NewGuid();
        var mockJobRunRecorder = new Mock<IJobRunRecorderForEvents>();
        mockJobRunRecorder.Setup(x => x.StartHandlerRunAsync(It.IsAny<IDomainEvent>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobRunId);
        Exception? failedEx = null;
        mockJobRunRecorder.Setup(x => x.FailHandlerRunAsync(It.IsAny<Guid>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Exception, CancellationToken>((_, ex, _) => failedEx = ex)
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDomainEventHandler<WorkflowTransitionCompletedEvent>>(
            new TestWorkflowTransitionHandler(() => throw new InvalidOperationException("Handler failed")));
        var sp = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(sp, sp.GetRequiredService<ILogger<DomainEventDispatcher>>(), mockStore.Object, envelopeBuilder: null, mockJobRunRecorder.Object);
        var evt = CreateSampleEvent();

        await dispatcher.PublishAsync(evt);

        mockJobRunRecorder.Verify(x => x.FailHandlerRunAsync(jobRunId, It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
        failedEx.Should().NotBeNull();
        failedEx!.Message.Should().Be("Handler failed");
        mockStore.Verify(x => x.MarkProcessedAsync(evt.EventId, false, It.Is<string>(s => s != null && s.Contains("Handler failed")), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Retry / failure observable via JobRun

    [Fact]
    public async Task EventStoreRepository_MarkProcessedAsync_UnknownEventId_DoesNotThrow()
    {
        var repo = new EventStoreRepository(_dbContext);

        await repo.Invoking(r => r.MarkProcessedAsync(Guid.NewGuid(), true))
            .Should().NotThrowAsync();
    }

    #endregion

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
}
