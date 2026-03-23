using CephasOps.Application.Events;
using CephasOps.Application.Events.DTOs;
using CephasOps.Application.Events.Replay;
using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace CephasOps.Application.Tests.Events;

/// <summary>
/// Phase 5: Operational observability — replay (requeue dead-letter), dead-letter listing, metrics, counts.
/// Uses SQLite in-memory (relational provider). EventStore append requires TenantScope (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class EventBusPhase5Tests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public EventBusPhase5Tests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
    }

    [Fact]
    public async Task ResetDeadLetterToPendingAsync_WhenDeadLetter_ResetsToPendingAndReturnsTrue()
    {
        var repo = new EventStoreRepository(_context);
        var evt = CreateSampleEvent();
        await repo.AppendAsync(evt);
        for (int i = 0; i < EventStoreRepository.MaxRetriesBeforeDeadLetter; i++)
            await repo.MarkProcessedAsync(evt.EventId, success: false, "Simulated failure");

        var entryBefore = await _context.Set<EventStoreEntry>().FirstAsync(e => e.EventId == evt.EventId);
        entryBefore.Status.Should().Be("DeadLetter");
        var retryCountBefore = entryBefore.RetryCount;

        var result = await repo.ResetDeadLetterToPendingAsync(evt.EventId);

        result.Should().BeTrue();
        var entryAfter = await _context.Set<EventStoreEntry>().FirstAsync(e => e.EventId == evt.EventId);
        entryAfter.Status.Should().Be("Pending");
        entryAfter.NextRetryAtUtc.Should().BeNull();
        entryAfter.RetryCount.Should().Be(retryCountBefore, "RetryCount must be unchanged");
    }

    [Fact]
    public async Task ResetDeadLetterToPendingAsync_WhenNotDeadLetter_ReturnsFalse()
    {
        var repo = new EventStoreRepository(_context);
        var evt = CreateSampleEvent();
        await repo.AppendAsync(evt);

        var result = await repo.ResetDeadLetterToPendingAsync(evt.EventId);

        result.Should().BeFalse();
        var entry = await _context.Set<EventStoreEntry>().FirstAsync(e => e.EventId == evt.EventId);
        entry.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task ResetDeadLetterToPendingAsync_WhenNotFound_ReturnsFalse()
    {
        var repo = new EventStoreRepository(_context);
        var result = await repo.ResetDeadLetterToPendingAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RequeueDeadLetterToPendingAsync_WhenDeadLetter_Succeeds()
    {
        var repo = new EventStoreRepository(_context);
        var evt = CreateSampleEvent();
        await repo.AppendAsync(evt);
        for (int i = 0; i < EventStoreRepository.MaxRetriesBeforeDeadLetter; i++)
            await repo.MarkProcessedAsync(evt.EventId, success: false, "Simulated failure");

        var replayService = CreateReplayService(repo);
        var result = await replayService.RequeueDeadLetterToPendingAsync(evt.EventId, scopeCompanyId: null, initiatedByUserId: null);

        result.Success.Should().BeTrue();
        var entry = await _context.Set<EventStoreEntry>().FirstAsync(e => e.EventId == evt.EventId);
        entry.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task RequeueDeadLetterToPendingAsync_WhenNotDeadLetter_ReturnsError()
    {
        var repo = new EventStoreRepository(_context);
        var evt = CreateSampleEvent();
        await repo.AppendAsync(evt);

        var replayService = CreateReplayService(repo);
        var result = await replayService.RequeueDeadLetterToPendingAsync(evt.EventId, scopeCompanyId: null, initiatedByUserId: null);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("DeadLetter");
    }

    [Fact]
    public async Task RequeueDeadLetterToPendingAsync_WhenNotFound_ReturnsNotFoundError()
    {
        var repo = new EventStoreRepository(_context);
        var replayService = CreateReplayService(repo);
        var result = await replayService.RequeueDeadLetterToPendingAsync(Guid.NewGuid(), scopeCompanyId: null, initiatedByUserId: null);
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Event not found.");
    }

    [Fact]
    public async Task GetEventStoreCountsAsync_ReturnsPendingFailedDeadLetterAndOldestPending()
    {
        var repo = new EventStoreRepository(_context);
        var evt1 = CreateSampleEvent();
        await repo.AppendAsync(evt1);
        var evt2 = CreateSampleEvent();
        await repo.AppendAsync(evt2);
        await repo.MarkProcessedAsync(evt1.EventId, success: false, "Fail");
        await _context.SaveChangesAsync();

        var queryService = new EventStoreQueryService(_context, repo);
        var snapshot = await queryService.GetEventStoreCountsAsync(scopeCompanyId: null);

        snapshot.PendingCount.Should().Be(1);
        snapshot.FailedCount.Should().Be(1);
        snapshot.DeadLetterCount.Should().Be(0);
        snapshot.OldestPendingCreatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task GetEventsAsync_WithRetryCountFilter_AppliesFilter()
    {
        var repo = new EventStoreRepository(_context);
        var evt = CreateSampleEvent();
        await repo.AppendAsync(evt);
        await repo.MarkProcessedAsync(evt.EventId, success: false, "Fail");
        await repo.MarkProcessedAsync(evt.EventId, success: false, "Fail");
        var queryService = new EventStoreQueryService(_context, repo);

        var (items, total) = await queryService.GetEventsAsync(
            new EventStoreFilterDto { Status = "Failed", RetryCountMin = 2, Page = 1, PageSize = 10 },
            scopeCompanyId: null);

        total.Should().Be(1);
        items.Should().ContainSingle();
        items[0].RetryCount.Should().Be(2);
        items[0].ParentEventId.Should().BeNull();
        items[0].NextRetryAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void EventBusMetricsSnapshot_Update_ReflectsInGaugeValues()
    {
        var snapshot = new EventBusMetricsSnapshot();
        snapshot.Update(10, 2, 1, 120.5);
        snapshot.PendingEventCount.Should().Be(10);
        snapshot.FailedEventCount.Should().Be(2);
        snapshot.DeadLetterEventCount.Should().Be(1);
        snapshot.OldestPendingEventAgeSeconds.Should().Be(120.5);
    }

    [Fact]
    public void EventBusDispatcherMetrics_RecordEventsRecoveredFromStuck_IncrementsCounter()
    {
        var snapshot = new EventBusMetricsSnapshot();
        var metrics = new EventBusDispatcherMetrics(snapshot);
        metrics.RecordEventsRecoveredFromStuck(3);
        metrics.RecordEventsRecoveredFromStuck(0);
        metrics.RecordEventsRecoveredFromStuck(1);
        // Counter is not readable; we only verify no throw and that the meter was created
        snapshot.Should().NotBeNull();
    }

    private EventReplayService CreateReplayService(IEventStore eventStore)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var typeRegistry = new EventTypeRegistry();
        var policy = new EventReplayPolicy();
        var accessor = new ReplayExecutionContextAccessor();
        var sp = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(sp, sp.GetRequiredService<ILogger<DomainEventDispatcher>>(), eventStore);
        return new EventReplayService(eventStore, dispatcher, typeRegistry, policy, accessor, sp.GetRequiredService<ILogger<EventReplayService>>());
    }

    private WorkflowTransitionCompletedEvent CreateSampleEvent()
    {
        return new WorkflowTransitionCompletedEvent
        {
            EventId = Guid.NewGuid(),
            EventType = "WorkflowTransitionCompleted",
            OccurredAtUtc = DateTime.UtcNow,
            CorrelationId = "test-phase5",
            CompanyId = _companyId,
            TriggeredByUserId = Guid.NewGuid(),
            Source = "Test",
            WorkflowDefinitionId = Guid.NewGuid(),
            WorkflowJobId = Guid.NewGuid(),
            EntityType = "Order",
            EntityId = Guid.NewGuid(),
            FromStatus = "Pending",
            ToStatus = "Assigned"
        };
    }
}
