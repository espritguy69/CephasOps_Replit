using CephasOps.Application.Events;
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
/// Phase 6: Distributed dispatcher — FOR UPDATE SKIP LOCKED, parallel processing, retry backoff.
/// Uses SQLite in-memory (relational provider with SKIP LOCKED support). EventStore append requires TenantScope (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class EventBusPhase6Tests : IDisposable
{
    private const string SharedMemoryConnection = "Data Source=phase6shared;Mode=Memory;Cache=Shared";
    private readonly ApplicationDbContext _context;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public EventBusPhase6Tests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(SharedMemoryConnection)
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

    [Fact(Skip = "FOR UPDATE SKIP LOCKED is PostgreSQL-specific. Run with PostgreSQL for distributed dispatch validation.")]
    public async Task DistributedDispatch_LocksRows_TwoConcurrentClaimsReturnDisjointSets()
    {
        var repo = new EventStoreRepository(_context);
        const int eventCount = 10;
        for (int i = 0; i < eventCount; i++)
        {
            var evt = CreateSampleEvent();
            await repo.AppendAsync(evt);
        }

        IReadOnlyList<EventStoreEntry> batch1;
        IReadOnlyList<EventStoreEntry> batch2;
        var t1 = Task.Run(async () =>
        {
            using var scope = CreateScopeWithSharedDb();
            var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
            return await store.ClaimNextPendingBatchAsync(5, EventStoreRepository.MaxRetriesBeforeDeadLetter);
        });
        var t2 = Task.Run(async () =>
        {
            using var scope = CreateScopeWithSharedDb();
            var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
            return await store.ClaimNextPendingBatchAsync(5, EventStoreRepository.MaxRetriesBeforeDeadLetter);
        });

        batch1 = await t1;
        batch2 = await t2;

        var ids1 = batch1.Select(e => e.EventId).ToHashSet();
        var ids2 = batch2.Select(e => e.EventId).ToHashSet();
        ids1.Should().NotIntersectWith(ids2, "FOR UPDATE SKIP LOCKED ensures each claim gets distinct rows");
        (batch1.Count + batch2.Count).Should().Be(eventCount, "all events should be claimed across the two batches");
    }

    [Fact(Skip = "Dispatcher uses ClaimNextPendingBatchAsync (PostgreSQL FOR UPDATE SKIP LOCKED). Run with PostgreSQL for full Phase 6 validation.")]
    public async Task ParallelDispatcherProcessesEvents_AllEventsInBatchProcessed()
    {
        var repo = new EventStoreRepository(_context);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(opts => opts.UseSqlite(SharedMemoryConnection));
        services.AddScoped<IEventStore, EventStoreRepository>();
        services.AddSingleton<IDomainEventHandler<WorkflowTransitionCompletedEvent>>(new TestWorkflowTransitionHandler(() => { }));
        services.AddSingleton<IEventTypeRegistry, EventTypeRegistry>();
        services.AddScoped<IDomainEventDispatcher>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<DomainEventDispatcher>>();
            var store = sp.GetRequiredService<IEventStore>();
            return new DomainEventDispatcher(sp, logger, store, null);
        });
        services.Configure<EventBusDispatcherOptions>(o =>
        {
            o.BatchSize = 10;
            o.MaxConcurrentDispatchers = 4;
            o.PollingIntervalSeconds = 1;
        });
        var sp = services.BuildServiceProvider();

        const int n = 6;
        for (int i = 0; i < n; i++)
        {
            var evt = CreateSampleEvent();
            await repo.AppendAsync(evt);
        }

        var options = Options.Create(new EventBusDispatcherOptions { BatchSize = 10, MaxConcurrentDispatchers = 4, PollingIntervalSeconds = 1 });
        var hosted = new EventStoreDispatcherHostedService(sp, sp.GetRequiredService<ILogger<EventStoreDispatcherHostedService>>(), options, null);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var run = hosted.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(2), CancellationToken.None);
        cts.Cancel();
        try
        {
            await run;
        }
        catch (OperationCanceledException) { }

        var entries = await _context.Set<EventStoreEntry>().Where(e => e.Status == "Processed" || e.Status == "Processing").ToListAsync();
        entries.Count.Should().Be(n, "all appended events should be processed or in progress");
    }

    [Fact]
    public async Task RetryBackoffAppliedCorrectly_NextRetryAtUtcFollowsExponentialSchedule()
    {
        var repo = new EventStoreRepository(_context);
        var evt = CreateSampleEvent();
        await repo.AppendAsync(evt);

        var before = DateTime.UtcNow;
        await repo.MarkProcessedAsync(evt.EventId, success: false, "Fail");
        var entry1 = await _context.Set<EventStoreEntry>().AsNoTracking().FirstAsync(e => e.EventId == evt.EventId);
        entry1.NextRetryAtUtc.Should().NotBeNull();
        var delay1 = (entry1.NextRetryAtUtc!.Value - before).TotalSeconds;
        delay1.Should().BeInRange(55, 65, "first retry: +1 minute (60s)");

        await repo.MarkProcessedAsync(evt.EventId, success: false, "Fail");
        var entry2 = await _context.Set<EventStoreEntry>().AsNoTracking().FirstAsync(e => e.EventId == evt.EventId);
        entry2.NextRetryAtUtc.Should().NotBeNull();
        var setAt2 = entry2.LastErrorAtUtc ?? entry2.NextRetryAtUtc!.Value;
        var delay2 = (entry2.NextRetryAtUtc!.Value - setAt2).TotalSeconds;
        delay2.Should().BeInRange(295, 305, "second retry: +5 minutes (300s)");

        await repo.MarkProcessedAsync(evt.EventId, success: false, "Fail");
        var entry3 = await _context.Set<EventStoreEntry>().AsNoTracking().FirstAsync(e => e.EventId == evt.EventId);
        entry3.NextRetryAtUtc.Should().NotBeNull();
        var setAt3 = entry3.LastErrorAtUtc ?? entry3.NextRetryAtUtc!.Value;
        var delay3 = (entry3.NextRetryAtUtc!.Value - setAt3).TotalSeconds;
        delay3.Should().BeInRange(895, 905, "third retry: +15 minutes (900s)");

        await repo.MarkProcessedAsync(evt.EventId, success: false, "Fail");
        var entry4 = await _context.Set<EventStoreEntry>().AsNoTracking().FirstAsync(e => e.EventId == evt.EventId);
        entry4.NextRetryAtUtc.Should().NotBeNull();
        var setAt4 = entry4.LastErrorAtUtc ?? entry4.NextRetryAtUtc!.Value;
        var delay4 = (entry4.NextRetryAtUtc!.Value - setAt4).TotalSeconds;
        delay4.Should().BeInRange(3595, 3605, "fourth retry: +60 minutes (3600s)");

        await repo.MarkProcessedAsync(evt.EventId, success: false, "Fail");
        var entry5 = await _context.Set<EventStoreEntry>().AsNoTracking().FirstAsync(e => e.EventId == evt.EventId);
        entry5.Status.Should().Be("DeadLetter", "fifth failure → dead-letter");
        entry5.NextRetryAtUtc.Should().BeNull();
    }

    private IServiceScope CreateScopeWithSharedDb()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(opts => opts.UseSqlite(SharedMemoryConnection));
        services.AddScoped<IEventStore, EventStoreRepository>();
        var sp = services.BuildServiceProvider();
        var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.OpenConnection();
        return scope;
    }

    private WorkflowTransitionCompletedEvent CreateSampleEvent()
    {
        return new WorkflowTransitionCompletedEvent
        {
            EventId = Guid.NewGuid(),
            EventType = "WorkflowTransitionCompleted",
            OccurredAtUtc = DateTime.UtcNow,
            CorrelationId = "test-phase6",
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
