using CephasOps.Application.Admin.DTOs;
using CephasOps.Application.Admin.Services;
using CephasOps.Application.Events;
using CephasOps.Application.Events.DTOs;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
using CephasOps.Application.Workflow.JobOrchestration;
using CephasOps.Domain.PlatformSafety;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Admin;

/// <summary>
/// Tests for operational overview: shape, empty state, and aggregation from existing sources.
/// </summary>
public class OperationsOverviewServiceTests
{
    [Fact]
    public async Task GetOverviewAsync_ReturnsExpectedShape_WithAllSections()
    {
        var jobSummary = new JobExecutionSummaryDto
        {
            PendingCount = 1,
            RunningCount = 0,
            FailedRetryScheduledCount = 2,
            DeadLetterCount = 0,
            SucceededCount = 10
        };
        var eventDashboard = new EventStoreDashboardDto
        {
            EventsToday = 100,
            ProcessedCount = 95,
            FailedCount = 3,
            DeadLetterCount = 2,
            ProcessedPercent = 95.0,
            FailedPercent = 5.0,
            TopFailingEventTypes = new List<EventTypeCountDto> { new() { EventType = "OrderCompleted", Count = 2 } },
            TopFailingCompanies = new List<CompanyEventCountDto> { new() { CompanyId = Guid.NewGuid(), FailedCount = 1, DeadLetterCount = 1 } }
        };
        var payoutDashboard = new PayoutHealthDashboardDto
        {
            SnapshotHealth = new PayoutSnapshotHealthDto
            {
                CompletedWithSnapshot = 80,
                CompletedMissingSnapshot = 5,
                NormalFlowCount = 70,
                RepairJobCount = 10,
                UnknownProvenanceCount = 0,
                BackfillCount = 0,
                ManualBackfillCount = 0
            },
            AnomalySummary = new PayoutAnomalySummaryDto
            {
                LegacyFallbackCount = 1,
                ZeroPayoutCount = 0,
                NegativeMarginCount = 0,
                CustomOverrideCount = 0,
                OrdersWithWarningsCount = 0
            },
            LatestRepairRun = new RepairRunSummaryDto
            {
                Id = Guid.NewGuid(),
                StartedAt = DateTime.UtcNow.AddHours(-1),
                CompletedAt = DateTime.UtcNow,
                TotalProcessed = 10,
                CreatedCount = 2,
                ErrorCount = 0,
                TriggerSource = "Scheduler"
            }
        };
        var health = new SystemHealthDto
        {
            IsHealthy = true,
            CheckedAt = DateTime.UtcNow,
            Database = new DatabaseHealthDto { IsConnected = true },
            BackgroundJobRunner = new BackgroundJobRunnerHealthDto { Status = "Healthy" }
        };

        var jobMock = new Mock<IJobExecutionQueryService>();
        jobMock.Setup(x => x.GetSummaryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(jobSummary);
        var eventMock = new Mock<IEventStoreQueryService>();
        eventMock.Setup(x => x.GetDashboardAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(eventDashboard);
        var payoutMock = new Mock<IPayoutHealthDashboardService>();
        payoutMock.Setup(x => x.GetDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(payoutDashboard);
        var adminMock = new Mock<IAdminService>();
        adminMock.Setup(x => x.GetHealthAsync(It.IsAny<CancellationToken>())).ReturnsAsync(health);
        var loggerMock = new Mock<ILogger<OperationsOverviewService>>();

        var sut = new OperationsOverviewService(
            jobMock.Object,
            eventMock.Object,
            payoutMock.Object,
            adminMock.Object,
            loggerMock.Object);

        var result = await sut.GetOverviewAsync(CancellationToken.None);

        result.Should().NotBeNull();
        result.GeneratedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.WindowEndUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.WindowStartUtc.Should().BeBefore(result.WindowEndUtc);

        result.JobExecutions.Should().NotBeNull();
        result.JobExecutions.PendingCount.Should().Be(1);
        result.JobExecutions.FailedRetryScheduledCount.Should().Be(2);
        result.JobExecutions.SucceededCount.Should().Be(10);

        result.EventStore.Should().NotBeNull();
        result.EventStore.EventsInWindow.Should().Be(100);
        result.EventStore.ProcessedCount.Should().Be(95);
        result.EventStore.FailedCount.Should().Be(3);
        result.EventStore.DeadLetterCount.Should().Be(2);
        result.EventStore.TopFailingEventTypes.Should().HaveCount(1);
        result.EventStore.TopFailingEventTypes[0].EventType.Should().Be("OrderCompleted");
        result.EventStore.TopFailingEventTypes[0].Count.Should().Be(2);
        result.EventStore.TopFailingCompanies.Should().HaveCount(1);

        result.PayoutHealth.Should().NotBeNull();
        result.PayoutHealth.CompletedWithSnapshot.Should().Be(80);
        result.PayoutHealth.CompletedMissingSnapshot.Should().Be(5);
        result.PayoutHealth.LegacyFallbackCount.Should().Be(1);
        result.PayoutHealth.LatestRepairRun.Should().NotBeNull();
        result.PayoutHealth.LatestRepairRun!.TriggerSource.Should().Be("Scheduler");
        result.PayoutHealth.LatestRepairRun.ErrorCount.Should().Be(0);

        result.SystemHealth.Should().NotBeNull();
        result.SystemHealth.DatabaseConnected.Should().BeTrue();
        result.SystemHealth.BackgroundJobRunnerStatus.Should().Be("Healthy");

        result.GuardViolations.Should().NotBeNull();
        result.GuardViolations.ByGuard.Should().NotBeNull();
        result.GuardViolations.Recent.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOverviewAsync_EmptyData_ReturnsValidShape_NoThrow()
    {
        var jobSummary = new JobExecutionSummaryDto();
        var eventDashboard = new EventStoreDashboardDto
        {
            EventsToday = 0,
            ProcessedCount = 0,
            FailedCount = 0,
            DeadLetterCount = 0,
            ProcessedPercent = 0,
            FailedPercent = 0,
            TopFailingEventTypes = new List<EventTypeCountDto>(),
            TopFailingCompanies = new List<CompanyEventCountDto>()
        };
        var payoutDashboard = new PayoutHealthDashboardDto
        {
            SnapshotHealth = new PayoutSnapshotHealthDto
            {
                CompletedWithSnapshot = 0,
                CompletedMissingSnapshot = 0,
                NormalFlowCount = 0,
                RepairJobCount = 0,
                UnknownProvenanceCount = 0,
                BackfillCount = 0,
                ManualBackfillCount = 0
            },
            AnomalySummary = new PayoutAnomalySummaryDto(),
            LatestRepairRun = null
        };
        var health = new SystemHealthDto
        {
            Database = new DatabaseHealthDto { IsConnected = false },
            BackgroundJobRunner = new BackgroundJobRunnerHealthDto { Status = "Unknown" }
        };

        var jobMock = new Mock<IJobExecutionQueryService>();
        jobMock.Setup(x => x.GetSummaryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(jobSummary);
        var eventMock = new Mock<IEventStoreQueryService>();
        eventMock.Setup(x => x.GetDashboardAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(eventDashboard);
        var payoutMock = new Mock<IPayoutHealthDashboardService>();
        payoutMock.Setup(x => x.GetDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(payoutDashboard);
        var adminMock = new Mock<IAdminService>();
        adminMock.Setup(x => x.GetHealthAsync(It.IsAny<CancellationToken>())).ReturnsAsync(health);
        var loggerMock = new Mock<ILogger<OperationsOverviewService>>();

        var sut = new OperationsOverviewService(
            jobMock.Object,
            eventMock.Object,
            payoutMock.Object,
            adminMock.Object,
            loggerMock.Object);

        var result = await sut.GetOverviewAsync(CancellationToken.None);

        result.Should().NotBeNull();
        result.JobExecutions.Should().NotBeNull();
        result.JobExecutions.PendingCount.Should().Be(0);
        result.JobExecutions.DeadLetterCount.Should().Be(0);
        result.EventStore.Should().NotBeNull();
        result.EventStore.EventsInWindow.Should().Be(0);
        result.EventStore.TopFailingEventTypes.Should().BeEmpty();
        result.EventStore.TopFailingCompanies.Should().BeEmpty();
        result.PayoutHealth.Should().NotBeNull();
        result.PayoutHealth.LatestRepairRun.Should().BeNull();
        result.SystemHealth.Should().NotBeNull();
        result.SystemHealth.DatabaseConnected.Should().BeFalse();
        result.SystemHealth.BackgroundJobRunnerStatus.Should().Be("Unknown");

        result.GuardViolations.Should().NotBeNull();
        result.GuardViolations.TotalRecorded.Should().Be(0);
        result.GuardViolations.ByGuard.Should().BeEmpty();
        result.GuardViolations.Recent.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOverviewAsync_CapsTopFailingLists_AtFive()
    {
        var eventDashboard = new EventStoreDashboardDto
        {
            EventsToday = 50,
            ProcessedCount = 40,
            FailedCount = 10,
            DeadLetterCount = 0,
            ProcessedPercent = 80,
            FailedPercent = 20,
            TopFailingEventTypes = Enumerable.Range(0, 10).Select(i => new EventTypeCountDto { EventType = $"Type{i}", Count = 10 - i }).ToList(),
            TopFailingCompanies = Enumerable.Range(0, 7).Select(_ => new CompanyEventCountDto { CompanyId = Guid.NewGuid(), FailedCount = 1, DeadLetterCount = 0 }).ToList()
        };

        var jobMock = new Mock<IJobExecutionQueryService>();
        jobMock.Setup(x => x.GetSummaryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new JobExecutionSummaryDto());
        var eventMock = new Mock<IEventStoreQueryService>();
        eventMock.Setup(x => x.GetDashboardAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(eventDashboard);
        var payoutMock = new Mock<IPayoutHealthDashboardService>();
        payoutMock.Setup(x => x.GetDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new PayoutHealthDashboardDto());
        var adminMock = new Mock<IAdminService>();
        adminMock.Setup(x => x.GetHealthAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SystemHealthDto());
        var loggerMock = new Mock<ILogger<OperationsOverviewService>>();

        var sut = new OperationsOverviewService(
            jobMock.Object,
            eventMock.Object,
            payoutMock.Object,
            adminMock.Object,
            loggerMock.Object);

        var result = await sut.GetOverviewAsync(CancellationToken.None);

        result.EventStore.TopFailingEventTypes.Should().HaveCount(5);
        result.EventStore.TopFailingCompanies.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetOverviewAsync_WhenGuardViolationBufferHasEntries_ReturnsThemInGuardViolationsSection()
    {
        var buffer = new InMemoryGuardViolationBufferForTests();
        buffer.Record(new GuardViolationEntry
        {
            OccurredAtUtc = DateTime.UtcNow.AddMinutes(-1),
            GuardName = "TenantSafetyGuard",
            Operation = "SaveChanges",
            Message = "Tenant context required."
        });
        buffer.Record(new GuardViolationEntry
        {
            OccurredAtUtc = DateTime.UtcNow.AddMinutes(-2),
            GuardName = "TenantSafetyGuard",
            Operation = "SaveChanges",
            Message = "Tenant context required."
        });
        buffer.Record(new GuardViolationEntry
        {
            OccurredAtUtc = DateTime.UtcNow.AddMinutes(-3),
            GuardName = "FinancialIsolationGuard",
            Operation = "CreateInvoice",
            Message = "Company context required."
        });

        var jobMock = new Mock<IJobExecutionQueryService>();
        jobMock.Setup(x => x.GetSummaryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new JobExecutionSummaryDto());
        var eventMock = new Mock<IEventStoreQueryService>();
        eventMock.Setup(x => x.GetDashboardAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(new EventStoreDashboardDto());
        var payoutMock = new Mock<IPayoutHealthDashboardService>();
        payoutMock.Setup(x => x.GetDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new PayoutHealthDashboardDto());
        var adminMock = new Mock<IAdminService>();
        adminMock.Setup(x => x.GetHealthAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SystemHealthDto());
        var loggerMock = new Mock<ILogger<OperationsOverviewService>>();

        var sut = new OperationsOverviewService(
            jobMock.Object,
            eventMock.Object,
            payoutMock.Object,
            adminMock.Object,
            loggerMock.Object,
            buffer);

        var result = await sut.GetOverviewAsync(CancellationToken.None);

        result.GuardViolations.Should().NotBeNull();
        result.GuardViolations.TotalRecorded.Should().Be(3);
        result.GuardViolations.ByGuard.Should().HaveCount(2);
        result.GuardViolations.ByGuard.Should().Contain(g => g.GuardName == "TenantSafetyGuard" && g.Count == 2);
        result.GuardViolations.ByGuard.Should().Contain(g => g.GuardName == "FinancialIsolationGuard" && g.Count == 1);
        result.GuardViolations.Recent.Should().HaveCount(3);
        result.GuardViolations.Recent.Should().OnlyContain(v => !string.IsNullOrEmpty(v.GuardName) && !string.IsNullOrEmpty(v.Operation));
    }

    private sealed class InMemoryGuardViolationBufferForTests : IGuardViolationBuffer
    {
        private readonly List<GuardViolationEntry> _list = new();

        public void Record(GuardViolationEntry entry) => _list.Add(entry);

        public IReadOnlyList<GuardViolationEntry> GetRecent(int maxCount) =>
            _list.OrderByDescending(e => e.OccurredAtUtc).Take(maxCount).ToList();
    }
}
