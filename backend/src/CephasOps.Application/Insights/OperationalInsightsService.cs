using CephasOps.Domain.Events;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Orders.Enums;
using CephasOps.Domain.PlatformGuardian;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Insights;

/// <summary>Read-only aggregation for operational dashboards. Platform health uses platform bypass; tenant endpoints use request-scoped company. Raises TenantAnomalyEvents when thresholds are exceeded.</summary>
public class OperationalInsightsService : IOperationalInsightsService
{
    private const string StatusCompleted = OrderStatus.Completed;
    private const string StatusAssigned = "Assigned";
    private const int StuckOrderThresholdHours = 4;
    private const int StuckOrdersAnomalyThreshold = 5;
    private const double HighFailureRateThreshold = 0.2; // 20%
    private const int AbnormalReplacementsThreshold = 10; // replacements in month

    private readonly ApplicationDbContext _context;

    public OperationalInsightsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PlatformHealthDto> GetPlatformHealthAsync(CancellationToken cancellationToken = default)
    {
        return await TenantScopeExecutor.RunWithPlatformBypassAsync(async ct =>
        {
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var dayAgo = now.AddDays(-1);

            var activeTenants = await _context.Tenants.CountAsync(t => t.IsActive, ct);

            var companyIds = await _context.Companies
                .Where(c => c.TenantId != null)
                .Select(c => c.Id)
                .ToListAsync(ct);

            var ordersToday = 0;
            var completedToday = 0;
            var failedOrders = 0;
            if (companyIds.Count > 0)
            {
                var ordersQuery = _context.Orders
                    .Where(o => o.CompanyId != null && companyIds.Contains(o.CompanyId.Value) && o.CreatedAt >= todayStart);
                ordersToday = await ordersQuery.CountAsync(ct);
                completedToday = await ordersQuery.CountAsync(o => o.Status == StatusCompleted, ct);
                failedOrders = await _context.Orders
                    .CountAsync(o => o.CompanyId != null && companyIds.Contains(o.CompanyId.Value)
                        && (o.Status == OrderStatus.Rejected || o.Status == OrderStatus.DocketsRejected), ct);
            }

            var completionRate = ordersToday > 0 ? (double)completedToday / ordersToday * 100.0 : 0;

            var dailyMetrics = await _context.TenantMetricsDaily
                .Where(d => d.DateUtc >= dayAgo.Date)
                .ToListAsync(ct);
            var healthGroups = dailyMetrics
                .GroupBy(d => d.HealthStatus ?? "Unknown")
                .Select(g => new TenantHealthDistributionItemDto { Status = g.Key, Count = g.Select(x => x.TenantId).Distinct().Count() })
                .ToList();
            if (healthGroups.Count == 0)
                healthGroups.Add(new TenantHealthDistributionItemDto { Status = "Healthy", Count = activeTenants });

            var dayAgoUtc = now.AddDays(-1);
            var eventsProcessed = await _context.EventStore
                .Where(e => e.Status == "Processed" && e.ProcessedAtUtc != null && e.ProcessedAtUtc >= dayAgoUtc)
                .CountAsync(ct);
            var eventFailures = await _context.EventStore
                .Where(e => (e.Status == "Failed" || e.Status == "DeadLetter") && (e.LastErrorAtUtc ?? e.CreatedAtUtc) >= dayAgoUtc)
                .CountAsync(ct);
            var retryQueueSize = await _context.EventStore
                .CountAsync(e => e.Status == "Pending" || (e.Status == "Failed" && e.NextRetryAtUtc != null), ct);
            double? eventLagSeconds = null;
            var oldestPendingUtc = await _context.EventStore
                .Where(e => e.Status == "Pending")
                .MinAsync(e => (DateTime?)e.OccurredAtUtc, ct);
            if (oldestPendingUtc.HasValue)
                eventLagSeconds = (now - oldestPendingUtc.Value).TotalSeconds;

            return new PlatformHealthDto
            {
                ActiveTenants = activeTenants,
                OrdersToday = ordersToday,
                CompletionRate = Math.Round(completionRate, 2),
                AvgCompletionTimeHours = null,
                FailedOrders = failedOrders,
                TenantHealthDistribution = healthGroups,
                EventsProcessed = eventsProcessed,
                EventFailures = eventFailures,
                RetryQueueSize = retryQueueSize,
                EventLagSeconds = eventLagSeconds
            };
        }, cancellationToken);
    }

    public async Task<TenantPerformanceDto> GetTenantPerformanceAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context required: CompanyId cannot be empty.");
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var ordersThisMonth = await _context.Orders
            .Where(o => o.CompanyId == companyId && o.CreatedAt >= monthStart)
            .CountAsync(cancellationToken);
        var completedThisMonth = await _context.Orders
            .Where(o => o.CompanyId == companyId && o.CreatedAt >= monthStart && o.Status == StatusCompleted)
            .CountAsync(cancellationToken);
        var completionRate = ordersThisMonth > 0 ? (double)completedThisMonth / ordersThisMonth * 100.0 : 0;

        var activeInstallers = await _context.ServiceInstallers
            .CountAsync(s => s.CompanyId == companyId && s.IsActive, cancellationToken);

        var deviceReplacements = await _context.OrderMaterialReplacements
            .Where(r => r.CompanyId == companyId)
            .CountAsync(cancellationToken);

        var ordersBreachedSla = await _context.SlaBreaches
            .CountAsync(b => b.CompanyId == companyId && b.DetectedAtUtc >= monthStart, cancellationToken);
        var ordersCompletedWithinSla = Math.Max(0, completedThisMonth - ordersBreachedSla);

        double? avgInstallTimeHours = await GetAvgInstallTimeHoursAsync(companyId, monthStart, null, cancellationToken);
        double? installerResponseTimeHours = await GetInstallerResponseTimeHoursAsync(companyId, monthStart, cancellationToken);

        return new TenantPerformanceDto
        {
            OrdersThisMonth = ordersThisMonth,
            CompletionRate = Math.Round(completionRate, 2),
            AvgInstallTimeHours = avgInstallTimeHours,
            ActiveInstallers = activeInstallers,
            DeviceReplacements = deviceReplacements,
            OrdersCompletedWithinSla = ordersCompletedWithinSla,
            OrdersBreachedSla = ordersBreachedSla,
            InstallerResponseTimeHours = installerResponseTimeHours
        };
    }

    public async Task<OperationsControlDto> GetOperationsControlAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context required: CompanyId cannot be empty.");
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var stuckThreshold = now.AddHours(-StuckOrderThresholdHours);

        var ordersAssignedToday = await _context.Orders
            .Where(o => o.CompanyId == companyId && o.AssignedSiId != null
                && o.UpdatedAt >= todayStart)
            .CountAsync(cancellationToken);
        var ordersCompletedToday = await _context.Orders
            .Where(o => o.CompanyId == companyId && o.Status == StatusCompleted && o.UpdatedAt >= todayStart)
            .CountAsync(cancellationToken);
        var installersActive = await _context.ServiceInstallers
            .CountAsync(s => s.CompanyId == companyId && s.IsActive, cancellationToken);

        var stuckQuery = _context.Orders
            .Where(o => o.CompanyId == companyId
                && o.Status != StatusCompleted
                && o.AssignedSiId != null
                && o.UpdatedAt < stuckThreshold);
        var stuckOrders = await stuckQuery.CountAsync(cancellationToken);
        var stuckList = await stuckQuery
            .OrderBy(o => o.UpdatedAt)
            .Take(20)
            .Select(o => new StuckOrderItemDto
            {
                OrderId = o.Id,
                Status = o.Status ?? "",
                AssignedSiId = o.AssignedSiId,
                UpdatedAtUtc = o.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var tenantId = await _context.Companies
            .Where(c => c.Id == companyId)
            .Select(c => c.TenantId)
            .FirstOrDefaultAsync(cancellationToken);
        var exceptions = 0;
        if (tenantId != Guid.Empty)
        {
            var weekAgo = now.AddDays(-7);
            exceptions = await _context.TenantActivityEvents
                .CountAsync(e => e.TenantId == tenantId && e.EventType == "Exception" && e.TimestampUtc >= weekAgo, cancellationToken);
        }

        var ordersBreachedSlaToday = await _context.SlaBreaches
            .CountAsync(b => b.CompanyId == companyId && b.DetectedAtUtc >= todayStart, cancellationToken);
        var ordersCompletedWithinSlaToday = Math.Max(0, ordersCompletedToday - ordersBreachedSlaToday);
        double? avgInstallTimeToday = await GetAvgInstallTimeHoursAsync(companyId, todayStart, now, cancellationToken);

        var deviceReplacementsMonth = await _context.OrderMaterialReplacements
            .CountAsync(r => r.CompanyId == companyId && r.RecordedAt >= new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc), cancellationToken);
        await EvaluateAndRaiseAnomaliesAsync(tenantId != Guid.Empty ? tenantId : (Guid?)null, companyId, stuckOrders, ordersCompletedToday, ordersAssignedToday, deviceReplacementsMonth, cancellationToken);

        return new OperationsControlDto
        {
            OrdersAssignedToday = ordersAssignedToday,
            OrdersCompletedToday = ordersCompletedToday,
            InstallersActive = installersActive,
            StuckOrders = stuckOrders,
            StuckOrdersList = stuckList,
            Exceptions = exceptions,
            AvgInstallTimeHours = avgInstallTimeToday,
            OrdersCompletedWithinSlaToday = ordersCompletedWithinSlaToday,
            OrdersBreachedSlaToday = ordersBreachedSlaToday
        };
    }

    public async Task<FinancialOverviewDto> GetFinancialOverviewAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context required: CompanyId cannot be empty.");
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var revenueToday = await _context.Invoices
            .Where(i => i.CompanyId == companyId && i.CreatedAt >= todayStart)
            .SumAsync(i => i.TotalAmount, cancellationToken);
        var revenueMonth = await _context.Invoices
            .Where(i => i.CompanyId == companyId && i.CreatedAt >= monthStart)
            .SumAsync(i => i.TotalAmount, cancellationToken);

        var installerPayouts = await _context.OrderPayoutSnapshots
            .Where(s => s.CompanyId == companyId && s.CalculatedAt >= monthStart)
            .SumAsync(s => s.FinalPayout, cancellationToken);

        decimal? profitMarginPercent = null;
        if (revenueMonth > 0 && installerPayouts > 0)
            profitMarginPercent = Math.Round((decimal)((double)(revenueMonth - installerPayouts) / (double)revenueMonth * 100.0), 2);

        var paidOrderIds = await _context.JobEarningRecords
            .Where(j => j.CompanyId == companyId)
            .Select(j => j.OrderId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var pendingPayouts = await _context.OrderPayoutSnapshots
            .Where(s => s.CompanyId == companyId && !paidOrderIds.Contains(s.OrderId))
            .SumAsync(s => s.FinalPayout, cancellationToken);

        return new FinancialOverviewDto
        {
            RevenueToday = revenueToday,
            RevenueMonth = revenueMonth,
            InstallerPayouts = installerPayouts,
            ProfitMarginPercent = profitMarginPercent,
            PendingPayouts = pendingPayouts
        };
    }

    public async Task<RiskQualityDto> GetRiskQualityAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context required: CompanyId cannot be empty.");
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var customerComplaints = await _context.OrderBlockers
            .CountAsync(b => b.CompanyId == companyId && b.CreatedAt >= monthStart, cancellationToken);

        var deviceFailures = await _context.OrderMaterialReplacements
            .Where(r => r.CompanyId == companyId && r.RecordedAt >= monthStart)
            .CountAsync(cancellationToken);

        var rescheduledOrders = await _context.Orders
            .CountAsync(o => o.CompanyId == companyId && o.RescheduleCount > 0 && o.UpdatedAt >= monthStart, cancellationToken);

        var repeatCustomerIssues = 0;
        var customersWithMultiple = await _context.Orders
            .Where(o => o.CompanyId == companyId && o.CreatedAt >= monthStart && o.CustomerPhone != null && o.CustomerPhone != "")
            .GroupBy(o => o.CustomerPhone)
            .Where(g => g.Count() > 1)
            .CountAsync(cancellationToken);
        repeatCustomerIssues = customersWithMultiple;

        var tenantIdForRisk = await _context.Companies.Where(c => c.Id == companyId).Select(c => c.TenantId).FirstOrDefaultAsync(cancellationToken);
        if (tenantIdForRisk != Guid.Empty)
            await EvaluateAndRaiseAnomaliesAsync(tenantIdForRisk, companyId, null, null, null, deviceFailures, cancellationToken);

        return new RiskQualityDto
        {
            CustomerComplaints = customerComplaints,
            DeviceFailures = deviceFailures,
            RescheduledOrders = rescheduledOrders,
            InstallerRatingAverage = null,
            RepeatCustomerIssues = repeatCustomerIssues
        };
    }

    private async Task<double?> GetAvgInstallTimeHoursAsync(Guid companyId, DateTime fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        var completedOrderIds = await _context.Orders
            .Where(o => o.CompanyId == companyId && o.Status == StatusCompleted && o.CreatedAt >= fromUtc && (toUtc == null || o.UpdatedAt <= toUtc))
            .OrderByDescending(o => o.UpdatedAt)
            .Take(100)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);
        if (completedOrderIds.Count == 0) return null;
        var logs = await _context.OrderStatusLogs
            .Where(l => l.CompanyId == companyId && completedOrderIds.Contains(l.OrderId) && (l.ToStatus == StatusAssigned || l.ToStatus == StatusCompleted))
            .Select(l => new { l.OrderId, l.ToStatus, l.CreatedAt })
            .ToListAsync(cancellationToken);
        var byOrder = logs.GroupBy(l => l.OrderId).ToList();
        var durations = new List<double>();
        foreach (var g in byOrder)
        {
            var assigned = g.Where(x => x.ToStatus == StatusAssigned).MinBy(x => x.CreatedAt)?.CreatedAt;
            var completed = g.Where(x => x.ToStatus == StatusCompleted).MinBy(x => x.CreatedAt)?.CreatedAt;
            if (assigned.HasValue && completed.HasValue && completed.Value > assigned.Value)
                durations.Add((completed.Value - assigned.Value).TotalHours);
        }
        if (durations.Count == 0) return null;
        return Math.Round(durations.Average(), 2);
    }

    private async Task<double?> GetInstallerResponseTimeHoursAsync(Guid companyId, DateTime monthStart, CancellationToken cancellationToken)
    {
        var orderIdsWithAssigned = await _context.OrderStatusLogs
            .Where(l => l.CompanyId == companyId && l.ToStatus == StatusAssigned && l.CreatedAt >= monthStart)
            .Select(l => l.OrderId)
            .Distinct()
            .Take(100)
            .ToListAsync(cancellationToken);
        if (orderIdsWithAssigned.Count == 0) return null;
        var orderCreated = await _context.Orders
            .Where(o => o.CompanyId == companyId && orderIdsWithAssigned.Contains(o.Id))
            .Select(o => new { o.Id, o.CreatedAt })
            .ToListAsync(cancellationToken);
        var assignedLogs = await _context.OrderStatusLogs
            .Where(l => l.CompanyId == companyId && orderIdsWithAssigned.Contains(l.OrderId) && l.ToStatus == StatusAssigned)
            .Select(l => new { l.OrderId, l.CreatedAt })
            .ToListAsync(cancellationToken);
        var createdByOrder = orderCreated.ToDictionary(o => o.Id, o => o.CreatedAt);
        var responseHours = assignedLogs
            .Where(l => createdByOrder.TryGetValue(l.OrderId, out var created) && l.CreatedAt > created)
            .Select(l => (l.CreatedAt - createdByOrder[l.OrderId]).TotalHours)
            .Where(h => h >= 0)
            .ToList();
        if (responseHours.Count == 0) return null;
        return Math.Round(responseHours.Average(), 2);
    }

    private async Task EvaluateAndRaiseAnomaliesAsync(Guid? tenantId, Guid companyId, int? stuckOrders, int? ordersCompletedToday, int? ordersAssignedToday, int? deviceReplacementsMonth, CancellationToken cancellationToken)
    {
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty) return;
        var added = 0;
        if (stuckOrders.HasValue && stuckOrders.Value >= StuckOrdersAnomalyThreshold)
        {
            var recent = await _context.TenantAnomalyEvents
                .Where(e => e.TenantId == tenantId && e.Kind == "StuckOrdersAnomaly" && e.OccurredAtUtc >= DateTime.UtcNow.AddHours(-1))
                .CountAsync(cancellationToken);
            if (recent == 0)
            {
                _context.TenantAnomalyEvents.Add(new TenantAnomalyEvent
                {
                    TenantId = tenantId.Value,
                    Kind = "StuckOrdersAnomaly",
                    Severity = stuckOrders.Value >= 10 ? "Critical" : "Warning",
                    Details = $"Stuck orders count: {stuckOrders.Value} (threshold {StuckOrdersAnomalyThreshold})"
                });
                added++;
            }
        }
        if (ordersCompletedToday.HasValue && ordersAssignedToday.HasValue && ordersAssignedToday.Value > 0)
        {
            var failureRate = 1.0 - ((double)ordersCompletedToday.Value / ordersAssignedToday.Value);
            if (failureRate >= HighFailureRateThreshold)
            {
                var recent = await _context.TenantAnomalyEvents
                    .Where(e => e.TenantId == tenantId && e.Kind == "HighFailureRate" && e.OccurredAtUtc >= DateTime.UtcNow.AddHours(-1))
                    .CountAsync(cancellationToken);
                if (recent == 0)
                {
                    _context.TenantAnomalyEvents.Add(new TenantAnomalyEvent
                    {
                        TenantId = tenantId.Value,
                        Kind = "HighFailureRate",
                        Severity = failureRate >= 0.4 ? "Critical" : "Warning",
                        Details = $"Completion rate today: {(1 - failureRate) * 100:F0}% (threshold {(1 - HighFailureRateThreshold) * 100:F0}%)"
                    });
                    added++;
                }
            }
        }
        if (deviceReplacementsMonth.HasValue && deviceReplacementsMonth.Value >= AbnormalReplacementsThreshold)
        {
            var recent = await _context.TenantAnomalyEvents
                .Where(e => e.TenantId == tenantId && e.Kind == "AbnormalMaterialReplacements" && e.OccurredAtUtc >= DateTime.UtcNow.AddHours(-1))
                .CountAsync(cancellationToken);
            if (recent == 0)
            {
                _context.TenantAnomalyEvents.Add(new TenantAnomalyEvent
                {
                    TenantId = tenantId.Value,
                    Kind = "AbnormalMaterialReplacements",
                    Severity = deviceReplacementsMonth.Value >= 25 ? "Critical" : "Warning",
                    Details = $"Device/material replacements this month: {deviceReplacementsMonth.Value} (threshold {AbnormalReplacementsThreshold})"
                });
                added++;
            }
        }
        if (added > 0)
            await _context.SaveChangesAsync(cancellationToken);
    }
}
