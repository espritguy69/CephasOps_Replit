using CephasOps.Application.Events;
using CephasOps.Application.Events.DTOs;
using CephasOps.Application.Events.Replay;
using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Events;

/// <summary>
/// Phase 7: Distributed reliability — lease, poison classification, attempt history, bulk actions, stuck recovery respects lease.
/// Uses in-memory SQLite where possible; PostgreSQL required for claim/lock tests. EventStore append requires TenantScope (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class EventBusPhase7Tests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public EventBusPhase7Tests()
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
    public void FailureClassifier_ValidationException_IsNonRetryable()
    {
        var classifier = new FailureClassifier();
        var ex = new ArgumentException("validation failed");
        classifier.IsNonRetryable(ex).Should().BeTrue();
        classifier.GetErrorType(ex).Should().Contain("Validation");
    }

    [Fact]
    public void FailureClassifier_TimeoutException_IsRetryable()
    {
        var classifier = new FailureClassifier();
        var ex = new TimeoutException("request timed out");
        classifier.IsNonRetryable(ex).Should().BeFalse();
    }

    [Fact]
    public async Task MarkProcessedAsync_WhenSuccess_ClearsLeaseFields()
    {
        var repo = new EventStoreRepository(_context);
        var evt = CreateSampleEvent();
        await repo.AppendAsync(evt);
        var entry = await _context.Set<EventStoreEntry>().FirstAsync(e => e.EventId == evt.EventId);
        entry.Status = "Processing";
        entry.ProcessingNodeId = "node-1";
        entry.ProcessingLeaseExpiresAtUtc = DateTime.UtcNow.AddMinutes(5);
        entry.LastClaimedAtUtc = DateTime.UtcNow;
        entry.LastClaimedBy = "node-1";
        await _context.SaveChangesAsync();

        await repo.MarkProcessedAsync(evt.EventId, success: true, null, null, null, false);

        var after = await _context.Set<EventStoreEntry>().AsNoTracking().FirstAsync(e => e.EventId == evt.EventId);
        after.Status.Should().Be("Processed");
        after.ProcessingNodeId.Should().BeNull();
        after.ProcessingLeaseExpiresAtUtc.Should().BeNull();
        after.LastClaimedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task MarkProcessedAsync_WhenNonRetryable_MovesToDeadLetterImmediately()
    {
        var repo = new EventStoreRepository(_context);
        var evt = CreateSampleEvent();
        await repo.AppendAsync(evt);

        await repo.MarkProcessedAsync(evt.EventId, success: false, "Invalid payload", "Handler", "Validation", isNonRetryable: true);

        var entry = await _context.Set<EventStoreEntry>().AsNoTracking().FirstAsync(e => e.EventId == evt.EventId);
        entry.Status.Should().Be("DeadLetter");
        entry.LastErrorType.Should().Be("Validation");
        entry.NextRetryAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task BulkResetDeadLetterToPendingAsync_ResetsMatchingEvents()
    {
        var repo = new EventStoreRepository(_context);
        var evt1 = CreateSampleEvent();
        var evt2 = CreateSampleEvent();
        await repo.AppendAsync(evt1);
        await repo.AppendAsync(evt2);
        foreach (var id in new[] { evt1.EventId, evt2.EventId })
        {
            for (int i = 0; i < EventStoreRepository.MaxRetriesBeforeDeadLetter; i++)
                await repo.MarkProcessedAsync(id, false, "Fail");
        }

        var filter = new EventStoreBulkFilter { MaxCount = 10 };
        var count = await repo.BulkResetDeadLetterToPendingAsync(filter);

        count.Should().Be(2);
        var entries = await _context.Set<EventStoreEntry>().AsNoTracking().Where(e => e.EventId == evt1.EventId || e.EventId == evt2.EventId).ToListAsync();
        entries.Should().OnlyContain(e => e.Status == "Pending");
    }

    [Fact]
    public async Task BulkReplayDeadLetter_DryRun_ReturnsCountWithoutUpdating()
    {
        var repo = new EventStoreRepository(_context);
        var evt = CreateSampleEvent();
        await repo.AppendAsync(evt);
        for (int i = 0; i < EventStoreRepository.MaxRetriesBeforeDeadLetter; i++)
            await repo.MarkProcessedAsync(evt.EventId, false, "Fail");

        var bulkService = new EventBulkReplayService(repo, Microsoft.Extensions.Logging.Abstractions.NullLogger<EventBulkReplayService>.Instance, null);
        var filter = new EventStoreFilterDto { Status = "DeadLetter", PageSize = 1000 };
        var result = await bulkService.ReplayDeadLetterByFilterAsync(filter, null, null, dryRun: true);

        result.Success.Should().BeTrue();
        result.DryRun.Should().BeTrue();
        result.CountAffected.Should().Be(1);

        var entry = await _context.Set<EventStoreEntry>().AsNoTracking().FirstAsync(e => e.EventId == evt.EventId);
        entry.Status.Should().Be("DeadLetter", "dry-run must not change data");
    }

    [Fact]
    public async Task BulkReplayDeadLetter_Actual_ResetsAndReturnsCount()
    {
        var repo = new EventStoreRepository(_context);
        var evt = CreateSampleEvent();
        await repo.AppendAsync(evt);
        for (int i = 0; i < EventStoreRepository.MaxRetriesBeforeDeadLetter; i++)
            await repo.MarkProcessedAsync(evt.EventId, false, "Fail");

        var bulkService = new EventBulkReplayService(repo, Microsoft.Extensions.Logging.Abstractions.NullLogger<EventBulkReplayService>.Instance, null);
        var filter = new EventStoreFilterDto { Status = "DeadLetter", PageSize = 1000 };
        var result = await bulkService.ReplayDeadLetterByFilterAsync(filter, null, null, dryRun: false);

        result.Success.Should().BeTrue();
        result.DryRun.Should().BeFalse();
        result.CountAffected.Should().Be(1);

        var entry = await _context.Set<EventStoreEntry>().AsNoTracking().FirstAsync(e => e.EventId == evt.EventId);
        entry.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task RetryableFailure_SchedulesNextRetry()
    {
        var repo = new EventStoreRepository(_context);
        var evt = CreateSampleEvent();
        await repo.AppendAsync(evt);

        await repo.MarkProcessedAsync(evt.EventId, success: false, "Transient error", "Handler", null, isNonRetryable: false);

        var entry = await _context.Set<EventStoreEntry>().AsNoTracking().FirstAsync(e => e.EventId == evt.EventId);
        entry.Status.Should().Be("Failed");
        entry.NextRetryAtUtc.Should().NotBeNull();
        entry.RetryCount.Should().Be(1);
    }

    private WorkflowTransitionCompletedEvent CreateSampleEvent()
    {
        return new WorkflowTransitionCompletedEvent
        {
            EventId = Guid.NewGuid(),
            EventType = "WorkflowTransitionCompleted",
            OccurredAtUtc = DateTime.UtcNow,
            CorrelationId = "test-phase7",
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
