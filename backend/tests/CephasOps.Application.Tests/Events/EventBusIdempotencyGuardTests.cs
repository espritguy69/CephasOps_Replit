using CephasOps.Application.Events;
using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Events;

/// <summary>
/// Event Bus idempotency guard: duplicate handler execution is skipped, failure is recorded, replay is safe.
/// Uses SQLite in-memory so EventProcessingLogStore.ExecuteUpdateAsync (relational) is supported.
/// </summary>
public class EventBusIdempotencyGuardTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;

    public EventBusIdempotencyGuardTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task TryClaimAsync_WhenNoRow_InsertsAndReturnsTrue()
    {
        var store = new EventProcessingLogStore(_dbContext, CreateLogger<EventProcessingLogStore>());
        var eventId = Guid.NewGuid();
        var handlerName = "TestHandler";

        var claimed = await store.TryClaimAsync(eventId, handlerName, null, "corr-1");

        claimed.Should().BeTrue();
        var row = await _dbContext.EventProcessingLog.FirstOrDefaultAsync(e => e.EventId == eventId && e.HandlerName == handlerName);
        row.Should().NotBeNull();
        row!.State.Should().Be(EventProcessingLog.States.Processing);
        row.AttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task TryClaimAsync_WhenAlreadyCompleted_ReturnsFalse()
    {
        var store = new EventProcessingLogStore(_dbContext, CreateLogger<EventProcessingLogStore>());
        var eventId = Guid.NewGuid();
        var handlerName = "TestHandler";
        _dbContext.EventProcessingLog.Add(new EventProcessingLog
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            HandlerName = handlerName,
            State = EventProcessingLog.States.Completed,
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            CompletedAtUtc = DateTime.UtcNow,
            AttemptCount = 1
        });
        await _dbContext.SaveChangesAsync();

        var claimed = await store.TryClaimAsync(eventId, handlerName, null, null);

        claimed.Should().BeFalse();
    }

    [Fact]
    public async Task MarkCompletedAsync_SetsStateCompleted()
    {
        var store = new EventProcessingLogStore(_dbContext, CreateLogger<EventProcessingLogStore>());
        var eventId = Guid.NewGuid();
        var handlerName = "TestHandler";
        _dbContext.EventProcessingLog.Add(new EventProcessingLog
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            HandlerName = handlerName,
            State = EventProcessingLog.States.Processing,
            StartedAtUtc = DateTime.UtcNow,
            AttemptCount = 1
        });
        await _dbContext.SaveChangesAsync();

        await store.MarkCompletedAsync(eventId, handlerName);

        var row = await _dbContext.EventProcessingLog.AsNoTracking().FirstAsync(e => e.EventId == eventId && e.HandlerName == handlerName);
        row.State.Should().Be(EventProcessingLog.States.Completed);
        row.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkFailedAsync_SetsStateFailedAndError()
    {
        var store = new EventProcessingLogStore(_dbContext, CreateLogger<EventProcessingLogStore>());
        var eventId = Guid.NewGuid();
        var handlerName = "TestHandler";
        _dbContext.EventProcessingLog.Add(new EventProcessingLog
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            HandlerName = handlerName,
            State = EventProcessingLog.States.Processing,
            StartedAtUtc = DateTime.UtcNow,
            AttemptCount = 1
        });
        await _dbContext.SaveChangesAsync();

        await store.MarkFailedAsync(eventId, handlerName, "Something broke");

        var row = await _dbContext.EventProcessingLog.AsNoTracking().FirstAsync(e => e.EventId == eventId && e.HandlerName == handlerName);
        row.State.Should().Be(EventProcessingLog.States.Failed);
        row.Error.Should().Be("Something broke");
        row.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Dispatcher_WithStore_SecondDispatchSkipsHandler_WhenFirstCompleted()
    {
        var runCount = 0;
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(opts => opts.UseSqlite(conn));
        services.AddScoped<IEventProcessingLogStore, EventProcessingLogStore>();
        services.AddSingleton<IDomainEventHandler<WorkflowTransitionCompletedEvent>>(
            new CountingHandler(() => runCount++));
        var sp = services.BuildServiceProvider();
        using (var initScope = sp.CreateScope())
        {
            var ctx = initScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await ctx.Database.EnsureCreatedAsync();
        }

        var evt = new WorkflowTransitionCompletedEvent
        {
            EventId = Guid.NewGuid(),
            EventType = "WorkflowTransitionCompleted",
            OccurredAtUtc = DateTime.UtcNow,
            CorrelationId = "c1",
            CompanyId = Guid.NewGuid(),
            TriggeredByUserId = null,
            Source = "Test",
            WorkflowDefinitionId = Guid.NewGuid(),
            WorkflowTransitionId = Guid.NewGuid(),
            WorkflowJobId = Guid.NewGuid(),
            FromStatus = "A",
            ToStatus = "B",
            EntityType = "Order",
            EntityId = Guid.NewGuid()
        };

        using (var scope = sp.CreateScope())
        {
            var dispatcher = new DomainEventDispatcher(
                scope.ServiceProvider,
                scope.ServiceProvider.GetRequiredService<ILogger<DomainEventDispatcher>>(),
                eventStore: null,
                envelopeBuilder: null,
                jobRunRecorder: null,
                asyncEnqueuer: null,
                replayContextAccessor: null,
                processingLogStore: scope.ServiceProvider.GetRequiredService<IEventProcessingLogStore>());
            await dispatcher.DispatchToHandlersAsync(evt);
        }
        runCount.Should().Be(1);

        using (var scope = sp.CreateScope())
        {
            var dispatcher = new DomainEventDispatcher(
                scope.ServiceProvider,
                scope.ServiceProvider.GetRequiredService<ILogger<DomainEventDispatcher>>(),
                eventStore: null,
                envelopeBuilder: null,
                jobRunRecorder: null,
                asyncEnqueuer: null,
                replayContextAccessor: null,
                processingLogStore: scope.ServiceProvider.GetRequiredService<IEventProcessingLogStore>());
            await dispatcher.DispatchToHandlersAsync(evt);
        }
        runCount.Should().Be(1, "second dispatch should skip handler (already completed)");
    }

    [Fact]
    public async Task Dispatcher_WithoutStore_RunsHandlerEveryTime()
    {
        var runCount = 0;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDomainEventHandler<WorkflowTransitionCompletedEvent>>(
            new CountingHandler(() => runCount++));
        var sp = services.BuildServiceProvider();

        var evt = new WorkflowTransitionCompletedEvent
        {
            EventId = Guid.NewGuid(),
            EventType = "WorkflowTransitionCompleted",
            OccurredAtUtc = DateTime.UtcNow,
            CorrelationId = "c1",
            CompanyId = Guid.NewGuid(),
            TriggeredByUserId = null,
            Source = "Test",
            WorkflowDefinitionId = Guid.NewGuid(),
            WorkflowTransitionId = Guid.NewGuid(),
            WorkflowJobId = Guid.NewGuid(),
            FromStatus = "A",
            ToStatus = "B",
            EntityType = "Order",
            EntityId = Guid.NewGuid()
        };

        var dispatcher = new DomainEventDispatcher(
            sp, sp.GetRequiredService<ILogger<DomainEventDispatcher>>(),
            eventStore: null, envelopeBuilder: null, jobRunRecorder: null, asyncEnqueuer: null, replayContextAccessor: null, processingLogStore: null);
        await dispatcher.DispatchToHandlersAsync(evt);
        await dispatcher.DispatchToHandlersAsync(evt);
        runCount.Should().Be(2);
    }

    [Fact]
    public async Task IsCompletedAsync_ReturnsTrue_WhenStateCompleted()
    {
        var store = new EventProcessingLogStore(_dbContext, CreateLogger<EventProcessingLogStore>());
        var eventId = Guid.NewGuid();
        var handlerName = "H1";
        _dbContext.EventProcessingLog.Add(new EventProcessingLog
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            HandlerName = handlerName,
            State = EventProcessingLog.States.Completed,
            StartedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow,
            AttemptCount = 1
        });
        await _dbContext.SaveChangesAsync();

        var completed = await store.IsCompletedAsync(eventId, handlerName);
        completed.Should().BeTrue();
    }

    private static ILogger<T> CreateLogger<T>()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug));
        return services.BuildServiceProvider().GetRequiredService<ILogger<T>>();
    }

    private sealed class CountingHandler : IDomainEventHandler<WorkflowTransitionCompletedEvent>
    {
        private readonly Action _onHandle;

        public CountingHandler(Action onHandle) => _onHandle = onHandle;

        public Task HandleAsync(WorkflowTransitionCompletedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            _onHandle();
            return Task.CompletedTask;
        }
    }
}
