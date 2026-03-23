using CephasOps.Application.Admin.DTOs;
using CephasOps.Application.Events;
using CephasOps.Application.Events.DTOs;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
using CephasOps.Application.Workflow.JobOrchestration;
using CephasOps.Domain.PlatformSafety;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Admin.Services;

/// <summary>
/// Builds operational overview from existing services (job execution, event store, payout health, admin health).
/// No new persisted data; read-only aggregation for operator visibility.
/// </summary>
public class OperationsOverviewService : IOperationsOverviewService
{
    private static readonly TimeSpan DefaultEventStoreWindow = TimeSpan.FromHours(24);

    private const int MaxGuardViolationsRecent = 50;

    private readonly IJobExecutionQueryService _jobExecutionQuery;
    private readonly IEventStoreQueryService _eventStoreQuery;
    private readonly IPayoutHealthDashboardService _payoutHealthDashboard;
    private readonly IAdminService _adminService;
    private readonly IGuardViolationBuffer? _guardViolationBuffer;
    private readonly ILogger<OperationsOverviewService> _logger;

    public OperationsOverviewService(
        IJobExecutionQueryService jobExecutionQuery,
        IEventStoreQueryService eventStoreQuery,
        IPayoutHealthDashboardService payoutHealthDashboard,
        IAdminService adminService,
        ILogger<OperationsOverviewService> logger,
        IGuardViolationBuffer? guardViolationBuffer = null)
    {
        _jobExecutionQuery = jobExecutionQuery;
        _eventStoreQuery = eventStoreQuery;
        _payoutHealthDashboard = payoutHealthDashboard;
        _adminService = adminService;
        _guardViolationBuffer = guardViolationBuffer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OperationalOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var generatedAt = DateTime.UtcNow;
        var windowEnd = generatedAt;
        var windowStart = windowEnd - DefaultEventStoreWindow;

        var jobSummaryTask = _jobExecutionQuery.GetSummaryAsync(cancellationToken);
        var eventStoreTask = _eventStoreQuery.GetDashboardAsync(windowStart, windowEnd, scopeCompanyId: null, cancellationToken);
        var payoutTask = _payoutHealthDashboard.GetDashboardAsync(cancellationToken);
        var healthTask = _adminService.GetHealthAsync(cancellationToken);

        await Task.WhenAll(jobSummaryTask, eventStoreTask, payoutTask, healthTask).ConfigureAwait(false);

        var jobSummary = await jobSummaryTask.ConfigureAwait(false);
        var eventDashboard = await eventStoreTask.ConfigureAwait(false);
        var payoutDashboard = await payoutTask.ConfigureAwait(false);
        var health = await healthTask.ConfigureAwait(false);

        return new OperationalOverviewDto
        {
            GeneratedAtUtc = generatedAt,
            WindowStartUtc = windowStart,
            WindowEndUtc = windowEnd,
            JobExecutions = new OperationalJobExecutionsDto
            {
                PendingCount = jobSummary.PendingCount,
                RunningCount = jobSummary.RunningCount,
                FailedRetryScheduledCount = jobSummary.FailedRetryScheduledCount,
                DeadLetterCount = jobSummary.DeadLetterCount,
                SucceededCount = jobSummary.SucceededCount
            },
            EventStore = new OperationalEventStoreDto
            {
                EventsInWindow = eventDashboard.EventsToday,
                ProcessedCount = eventDashboard.ProcessedCount,
                FailedCount = eventDashboard.FailedCount,
                DeadLetterCount = eventDashboard.DeadLetterCount,
                ProcessedPercent = eventDashboard.ProcessedPercent,
                FailedPercent = eventDashboard.FailedPercent,
                TopFailingEventTypes = eventDashboard.TopFailingEventTypes
                    .Take(5)
                    .Select(x => new OperationalEventTypeCountDto { EventType = x.EventType, Count = x.Count })
                    .ToList(),
                TopFailingCompanies = eventDashboard.TopFailingCompanies
                    .Take(5)
                    .Select(x => new OperationalCompanyEventCountDto
                    {
                        CompanyId = x.CompanyId,
                        FailedCount = x.FailedCount,
                        DeadLetterCount = x.DeadLetterCount
                    })
                    .ToList()
            },
            PayoutHealth = new OperationalPayoutHealthDto
            {
                CompletedWithSnapshot = payoutDashboard.SnapshotHealth.CompletedWithSnapshot,
                CompletedMissingSnapshot = payoutDashboard.SnapshotHealth.CompletedMissingSnapshot,
                CoveragePercent = payoutDashboard.SnapshotHealth.CoveragePercent,
                LegacyFallbackCount = payoutDashboard.AnomalySummary.LegacyFallbackCount,
                ZeroPayoutCount = payoutDashboard.AnomalySummary.ZeroPayoutCount,
                NegativeMarginCount = payoutDashboard.AnomalySummary.NegativeMarginCount,
                LatestRepairRun = payoutDashboard.LatestRepairRun == null
                    ? null
                    : new OperationalRepairRunDto
                    {
                        Id = payoutDashboard.LatestRepairRun.Id,
                        StartedAt = payoutDashboard.LatestRepairRun.StartedAt,
                        CompletedAt = payoutDashboard.LatestRepairRun.CompletedAt,
                        TotalProcessed = payoutDashboard.LatestRepairRun.TotalProcessed,
                        CreatedCount = payoutDashboard.LatestRepairRun.CreatedCount,
                        ErrorCount = payoutDashboard.LatestRepairRun.ErrorCount,
                        TriggerSource = payoutDashboard.LatestRepairRun.TriggerSource ?? ""
                    }
            },
            SystemHealth = new OperationalSystemHealthDto
            {
                DatabaseConnected = health.Database?.IsConnected ?? false,
                BackgroundJobRunnerStatus = health.BackgroundJobRunner?.Status ?? "Unknown"
            },
            GuardViolations = BuildGuardViolationsSection()
        };
    }

    private OperationalGuardViolationsDto BuildGuardViolationsSection()
    {
        if (_guardViolationBuffer == null)
            return new OperationalGuardViolationsDto();

        var recent = _guardViolationBuffer.GetRecent(MaxGuardViolationsRecent);
        var byGuard = recent
            .GroupBy(v => v.GuardName)
            .Select(g => new OperationalGuardCountDto { GuardName = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        return new OperationalGuardViolationsDto
        {
            TotalRecorded = recent.Count,
            ByGuard = byGuard,
            Recent = recent.Select(v => new OperationalGuardViolationItemDto
            {
                OccurredAtUtc = v.OccurredAtUtc,
                GuardName = v.GuardName,
                Operation = v.Operation,
                Message = v.Message,
                CompanyId = v.CompanyId,
                EntityType = v.EntityType,
                EntityId = v.EntityId,
                EventId = v.EventId
            }).ToList()
        };
    }
}
