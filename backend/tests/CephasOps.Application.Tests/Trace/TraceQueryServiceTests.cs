using CephasOps.Application.Trace;
using CephasOps.Application.Trace.DTOs;
using CephasOps.Domain.Events;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Trace;

[Collection("TenantScopeTests")]
public class TraceQueryServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public TraceQueryServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TraceQuery_" + Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    private void SetTenantScope() => TenantScope.CurrentTenantId = _companyId;

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_EmptyId_ReturnsEmptyTimeline()
    {
        var service = new TraceQueryService(_context);
        var result = await service.GetByCorrelationIdAsync("", null);
        result.LookupKind.Should().Be("CorrelationId");
        result.LookupValue.Should().Be("");
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_UnknownCorrelation_ReturnsEmptyTimeline()
    {
        var service = new TraceQueryService(_context);
        var result = await service.GetByCorrelationIdAsync("unknown-correlation", null);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_AssemblesTimelineChronologically()
    {
        SetTenantScope();
        var companyId = _companyId;
        var correlationId = "corr-123";
        var t1 = DateTime.UtcNow.AddMinutes(-10);
        var t2 = DateTime.UtcNow.AddMinutes(-5);
        var t3 = DateTime.UtcNow;

        var def = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            EntityType = "Order",
            Name = "Test",
            IsActive = true,
            CreatedAt = t1
        };
        _context.WorkflowDefinitions.Add(def);
        var wj = new WorkflowJob
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            WorkflowDefinitionId = def.Id,
            EntityType = "Order",
            EntityId = Guid.NewGuid(),
            CurrentStatus = "Pending",
            TargetStatus = "Assigned",
            State = WorkflowJobState.Succeeded,
            CorrelationId = correlationId,
            CreatedAt = t1,
            StartedAt = t2,
            CompletedAt = t3,
            UpdatedAt = t3
        };
        _context.WorkflowJobs.Add(wj);

        var evt = new EventStoreEntry
        {
            EventId = Guid.NewGuid(),
            EventType = "OrderCreated",
            OccurredAtUtc = t2,
            CreatedAtUtc = t2,
            Status = "Processed",
            ProcessedAtUtc = t3,
            CorrelationId = correlationId,
            CompanyId = companyId,
            Source = "Api"
        };
        _context.EventStore.Add(evt);

        var jobRun = new JobRun
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            JobName = "TestJob",
            JobType = "Test",
            Status = "Succeeded",
            StartedAtUtc = t2,
            CompletedAtUtc = t3,
            CorrelationId = correlationId,
            CreatedAtUtc = t2,
            UpdatedAtUtc = t3
        };
        _context.JobRuns.Add(jobRun);
        SetTenantScope();
        await _context.SaveChangesAsync();

        SetTenantScope();
        var service = new TraceQueryService(_context);
        var result = await service.GetByCorrelationIdAsync(correlationId, _companyId);

        result.LookupKind.Should().Be("CorrelationId");
        result.LookupValue.Should().Be(correlationId);
        result.Items.Should().NotBeEmpty();
        result.Items.Should().BeInAscendingOrder(x => x.TimestampUtc);
        result.Items.Select(x => x.ItemType).Should().Contain("WorkflowTransitionRequested");
        result.Items.Select(x => x.ItemType).Should().Contain("EventEmitted");
        result.Items.Select(x => x.ItemType).Should().Contain("BackgroundJobStarted");
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_CompanyScoped_FiltersByCompany()
    {
        SetTenantScope();
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var correlationId = "corr-scoped";

        var defA = new WorkflowDefinition { Id = Guid.NewGuid(), CompanyId = companyA, EntityType = "Order", Name = "Test", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.WorkflowDefinitions.Add(defA);
        _context.WorkflowJobs.Add(new WorkflowJob
        {
            Id = Guid.NewGuid(),
            CompanyId = companyA,
            WorkflowDefinitionId = defA.Id,
            EntityType = "Order",
            EntityId = Guid.NewGuid(),
            CurrentStatus = "Pending",
            TargetStatus = "Assigned",
            State = WorkflowJobState.Pending,
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var defB = new WorkflowDefinition { Id = Guid.NewGuid(), CompanyId = companyB, EntityType = "Order", Name = "Test", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.WorkflowDefinitions.Add(defB);
        _context.WorkflowJobs.Add(new WorkflowJob
        {
            Id = Guid.NewGuid(),
            CompanyId = companyB,
            WorkflowDefinitionId = defB.Id,
            EntityType = "Order",
            EntityId = Guid.NewGuid(),
            CurrentStatus = "Pending",
            TargetStatus = "Assigned",
            State = WorkflowJobState.Pending,
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        SetTenantScope();
        await _context.SaveChangesAsync();

        var service = new TraceQueryService(_context);
        TenantScope.CurrentTenantId = companyA;
        var resultA = await service.GetByCorrelationIdAsync(correlationId, companyA);
        TenantScope.CurrentTenantId = companyB;
        var resultB = await service.GetByCorrelationIdAsync(correlationId, companyB);

        resultA.Items.Should().HaveCount(1);
        resultB.Items.Should().HaveCount(1);
        resultA.Items.Single().CompanyId.Should().Be(companyA);
        resultB.Items.Single().CompanyId.Should().Be(companyB);
    }

    [Fact]
    public async Task GetByEventIdAsync_NotFound_ReturnsNull()
    {
        var service = new TraceQueryService(_context);
        var result = await service.GetByEventIdAsync(Guid.NewGuid(), null);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEventIdAsync_Found_ReturnsTimelineWithEvent()
    {
        var companyId = _companyId;
        var eventId = Guid.NewGuid();
        var correlationId = "corr-evt";
        var evt = new EventStoreEntry
        {
            EventId = eventId,
            EventType = "TestEvent",
            OccurredAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            Status = "Processed",
            CorrelationId = correlationId,
            CompanyId = companyId
        };
        _context.EventStore.Add(evt);
        await _context.SaveChangesAsync();

        var service = new TraceQueryService(_context);
        var result = await service.GetByEventIdAsync(eventId, null);
        result.Should().NotBeNull();
        result!.LookupKind.Should().Be("EventId");
        result.LookupValue.Should().Be(eventId.ToString());
        result.Items.Should().ContainSingle(x => x.RelatedIdKind == "Event" && x.RelatedId == eventId);
    }

    [Fact]
    public async Task GetByEventIdAsync_WrongCompany_ReturnsNull()
    {
        var companyId = _companyId;
        var eventId = Guid.NewGuid();
        var evt = new EventStoreEntry
        {
            EventId = eventId,
            EventType = "Test",
            OccurredAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            Status = "Pending",
            CompanyId = companyId
        };
        _context.EventStore.Add(evt);
        await _context.SaveChangesAsync();

        var service = new TraceQueryService(_context);
        var result = await service.GetByEventIdAsync(eventId, Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByJobRunIdAsync_NotFound_ReturnsNull()
    {
        var service = new TraceQueryService(_context);
        var result = await service.GetByJobRunIdAsync(Guid.NewGuid(), null);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByJobRunIdAsync_Found_ReturnsTimeline()
    {
        var companyId = _companyId;
        var jobRunId = Guid.NewGuid();
        var run = new JobRun
        {
            Id = jobRunId,
            CompanyId = companyId,
            JobName = "Test",
            JobType = "Test",
            Status = "Succeeded",
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            CompletedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            UpdatedAtUtc = DateTime.UtcNow
        };
        _context.JobRuns.Add(run);
        await _context.SaveChangesAsync();

        var service = new TraceQueryService(_context);
        var result = await service.GetByJobRunIdAsync(jobRunId, null);
        result.Should().NotBeNull();
        result!.LookupKind.Should().Be("JobRunId");
        result.Items.Should().Contain(x => x.RelatedIdKind == "JobRun" && x.RelatedId == jobRunId);
        result.Items.Count(x => x.RelatedIdKind == "JobRun").Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task GetByWorkflowJobIdAsync_NotFound_ReturnsNull()
    {
        var service = new TraceQueryService(_context);
        var result = await service.GetByWorkflowJobIdAsync(Guid.NewGuid(), null);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByWorkflowJobIdAsync_Found_ReturnsTimeline()
    {
        var companyId = _companyId;
        var def = new WorkflowDefinition { Id = Guid.NewGuid(), CompanyId = companyId, EntityType = "Order", Name = "Test", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.WorkflowDefinitions.Add(def);
        var wjId = Guid.NewGuid();
        var wj = new WorkflowJob
        {
            Id = wjId,
            CompanyId = companyId,
            WorkflowDefinitionId = def.Id,
            EntityType = "Order",
            EntityId = Guid.NewGuid(),
            CurrentStatus = "Pending",
            TargetStatus = "Assigned",
            State = WorkflowJobState.Succeeded,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowJobs.Add(wj);
        await _context.SaveChangesAsync();

        var service = new TraceQueryService(_context);
        var result = await service.GetByWorkflowJobIdAsync(wjId, null);
        result.Should().NotBeNull();
        result!.LookupKind.Should().Be("WorkflowJobId");
        result.Items.Should().Contain(x => x.RelatedIdKind == "WorkflowJob" && x.RelatedId == wjId);
        result.Items.Count(x => x.RelatedIdKind == "WorkflowJob").Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task GetByEntityAsync_ReturnsMatchingWorkflowAndEvents()
    {
        var companyId = _companyId;
        var entityType = "Order";
        var entityId = Guid.NewGuid();

        var def = new WorkflowDefinition { Id = Guid.NewGuid(), CompanyId = companyId, EntityType = "Order", Name = "Test", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.WorkflowDefinitions.Add(def);
        _context.WorkflowJobs.Add(new WorkflowJob
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            WorkflowDefinitionId = def.Id,
            EntityType = entityType,
            EntityId = entityId,
            CurrentStatus = "Pending",
            TargetStatus = "Assigned",
            State = WorkflowJobState.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.EventStore.Add(new EventStoreEntry
        {
            EventId = Guid.NewGuid(),
            EventType = "OrderCreated",
            OccurredAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            Status = "Processed",
            EntityType = entityType,
            EntityId = entityId,
            CompanyId = companyId
        });
        await _context.SaveChangesAsync();

        var service = new TraceQueryService(_context);
        var result = await service.GetByEntityAsync(entityType, entityId, null);
        result.LookupKind.Should().Be("Entity");
        result.LookupValue.Should().Contain(entityType).And.Contain(entityId.ToString());
        result.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(x => x.EntityType == entityType && x.EntityId == entityId);
    }

    [Fact]
    public async Task GetByEntityAsync_EmptyEntityType_StillAllowed_ReturnsEmptyWhenNoData()
    {
        var service = new TraceQueryService(_context);
        var result = await service.GetByEntityAsync("NonExistent", Guid.NewGuid(), null);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_WithLimit_AppliesPagination()
    {
        var companyId = _companyId;
        var correlationId = "corr-paginate";
        var def = new WorkflowDefinition { Id = Guid.NewGuid(), CompanyId = companyId, EntityType = "Order", Name = "Test", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.WorkflowDefinitions.Add(def);
        for (int i = 0; i < 5; i++)
        {
            _context.WorkflowJobs.Add(new WorkflowJob
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                WorkflowDefinitionId = def.Id,
                EntityType = "Order",
                EntityId = Guid.NewGuid(),
                CurrentStatus = "A",
                TargetStatus = "B",
                State = WorkflowJobState.Pending,
                CorrelationId = correlationId,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                UpdatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        var service = new TraceQueryService(_context);
        var result = await service.GetByCorrelationIdAsync(correlationId, null, new TraceQueryOptions { Limit = 2 });

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetMetricsAsync_ReturnsCounts()
    {
        var companyId = _companyId;
        var now = DateTime.UtcNow;
        _context.EventStore.Add(new EventStoreEntry
        {
            EventId = Guid.NewGuid(),
            EventType = "E1",
            OccurredAtUtc = now.AddMinutes(-1),
            CreatedAtUtc = now,
            Status = "Failed",
            CompanyId = companyId
        });
        _context.EventStore.Add(new EventStoreEntry
        {
            EventId = Guid.NewGuid(),
            EventType = "E2",
            OccurredAtUtc = now.AddMinutes(-2),
            CreatedAtUtc = now,
            Status = "DeadLetter",
            CompanyId = companyId
        });
        _context.JobRuns.Add(new JobRun
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            JobName = "J1",
            JobType = "T1",
            Status = "Failed",
            StartedAtUtc = now.AddMinutes(-10),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await _context.SaveChangesAsync();

        var service = new TraceQueryService(_context);
        var result = await service.GetMetricsAsync(now.AddHours(-24), now.AddHours(1), null);

        result.FailedEventsCount.Should().Be(1);
        result.DeadLetterEventsCount.Should().Be(1);
        result.FailedJobRunsCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMetricsAsync_CompanyScoped_FiltersByCompany()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var now = DateTime.UtcNow;
        _context.EventStore.Add(new EventStoreEntry
        {
            EventId = Guid.NewGuid(),
            EventType = "E1",
            OccurredAtUtc = now,
            CreatedAtUtc = now,
            Status = "Failed",
            CompanyId = companyA
        });
        _context.EventStore.Add(new EventStoreEntry
        {
            EventId = Guid.NewGuid(),
            EventType = "E2",
            OccurredAtUtc = now,
            CreatedAtUtc = now,
            Status = "Failed",
            CompanyId = companyB
        });
        await _context.SaveChangesAsync();

        var service = new TraceQueryService(_context);
        var resultA = await service.GetMetricsAsync(now.AddHours(-1), now.AddHours(1), companyA);
        var resultB = await service.GetMetricsAsync(now.AddHours(-1), now.AddHours(1), companyB);

        resultA.FailedEventsCount.Should().Be(1);
        resultB.FailedEventsCount.Should().Be(1);
    }
}
