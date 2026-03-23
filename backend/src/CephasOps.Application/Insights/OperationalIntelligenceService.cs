using CephasOps.Domain.Orders.Enums;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Insights;

/// <summary>Read-only, rule-based operational intelligence. All tenant methods are company-scoped; platform summary uses bypass for aggregation only.</summary>
public class OperationalIntelligenceService : IOperationalIntelligenceService
{
    private readonly ApplicationDbContext _context;
    private readonly OperationalIntelligenceOptions _options;

    public OperationalIntelligenceService(ApplicationDbContext context, Microsoft.Extensions.Options.IOptions<OperationalIntelligenceOptions>? options = null)
    {
        _context = context;
        _options = options?.Value ?? new OperationalIntelligenceOptions();
    }

    public async Task<OperationalIntelligenceSummaryDto> GetSummaryAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context required: CompanyId cannot be empty.");

        var orders = await GetOrdersAtRiskAsync(companyId, null, cancellationToken);
        var installers = await GetInstallersAtRiskAsync(companyId, null, cancellationToken);
        var buildings = await GetBuildingsAtRiskAsync(companyId, null, cancellationToken);

        int critical = 0, warning = 0, info = 0;
        foreach (var o in orders) { CountSeverity(o.Severity, ref critical, ref warning, ref info); }
        foreach (var i in installers) { CountSeverity(i.Severity, ref critical, ref warning, ref info); }
        foreach (var b in buildings) { CountSeverity(b.Severity, ref critical, ref warning, ref info); }

        return new OperationalIntelligenceSummaryDto
        {
            OrdersAtRiskCount = orders.Count,
            InstallersAtRiskCount = installers.Count,
            BuildingsAtRiskCount = buildings.Count,
            CriticalCount = critical,
            WarningCount = warning,
            InfoCount = info,
            GeneratedAtUtc = DateTime.UtcNow
        };
    }

    public async Task<IReadOnlyList<OrderRiskSignalDto>> GetOrdersAtRiskAsync(Guid companyId, string? severity = null, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context required: CompanyId cannot be empty.");

        var now = DateTime.UtcNow;
        var stuckThreshold = now.AddHours(-_options.StuckOrderThresholdHours);
        var likelyStuckThreshold = now.AddHours(-_options.StuckOrderThresholdHours * _options.LikelyStuckSoonPercentOfThreshold);
        var silentThreshold = now.AddHours(-_options.SilentOrderThresholdHours);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CompanyId == companyId && o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Rejected)
            .Select(o => new
            {
                o.Id,
                o.ServiceId,
                o.TicketId,
                o.Status,
                o.AssignedSiId,
                o.UpdatedAt,
                o.RescheduleCount,
                o.KpiDueAt,
                o.CreatedAt,
                o.BuildingId
            })
            .Take((_options.MaxResultsPerList * 2)) // fetch extra for filtering
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(o => o.Id).ToList();
        var blockerCounts = await _context.OrderBlockers
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId && orderIds.Contains(b.OrderId))
            .GroupBy(b => b.OrderId)
            .Select(g => new { OrderId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var replacementCounts = await _context.OrderMaterialReplacements
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId && orderIds.Contains(r.OrderId))
            .GroupBy(r => r.OrderId)
            .Select(g => new { OrderId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var lastLogByOrder = await _context.OrderStatusLogs
            .AsNoTracking()
            .Where(l => l.CompanyId == companyId && orderIds.Contains(l.OrderId))
            .GroupBy(l => l.OrderId)
            .Select(g => new { OrderId = g.Key, LastAt = g.Max(l => l.CreatedAt) })
            .ToListAsync(cancellationToken);

        var blockerMap = blockerCounts.ToDictionary(x => x.OrderId, x => x.Count);
        var replacementMap = replacementCounts.ToDictionary(x => x.OrderId, x => x.Count);
        var lastLogMap = lastLogByOrder.ToDictionary(x => x.OrderId, x => x.LastAt);

        var results = new List<OrderRiskSignalDto>();
        foreach (var o in orders)
        {
            var reasons = new List<IntelligenceExplanationDto>();
            string maxSeverity = "Info";

            if (o.AssignedSiId != null && o.UpdatedAt < stuckThreshold)
            {
                reasons.Add(new IntelligenceExplanationDto
                {
                    RuleCode = "StuckOrder",
                    Summary = "Order is stuck: assigned to installer with no status update for over threshold.",
                    Detail = $"AssignedSiId set, status {o.Status}, last update {o.UpdatedAt:O} (threshold {_options.StuckOrderThresholdHours}h).",
                    SourceCount = 1,
                    Severity = "Critical"
                });
                maxSeverity = "Critical";
            }
            else if (o.AssignedSiId != null && o.UpdatedAt < likelyStuckThreshold)
            {
                reasons.Add(new IntelligenceExplanationDto
                {
                    RuleCode = "LikelyStuckSoon",
                    Summary = "Order may become stuck soon: no recent activity.",
                    Detail = $"Last update {o.UpdatedAt:O} (>{_options.LikelyStuckSoonPercentOfThreshold * 100}% of stuck threshold).",
                    SourceCount = 1,
                    Severity = "Warning"
                });
                if (maxSeverity != "Critical") maxSeverity = "Warning";
            }

            if (o.RescheduleCount >= _options.RescheduleHeavyThreshold)
            {
                reasons.Add(new IntelligenceExplanationDto
                {
                    RuleCode = "RescheduleHeavy",
                    Summary = "Order has been rescheduled multiple times.",
                    Detail = $"RescheduleCount = {o.RescheduleCount} (threshold {_options.RescheduleHeavyThreshold}).",
                    SourceCount = o.RescheduleCount,
                    Severity = "Warning"
                });
                if (maxSeverity == "Info") maxSeverity = "Warning";
            }

            var blockers = blockerMap.GetValueOrDefault(o.Id, 0);
            if (blockers >= _options.OrderBlockerCountThreshold)
            {
                reasons.Add(new IntelligenceExplanationDto
                {
                    RuleCode = "BlockerAccumulation",
                    Summary = "Order has multiple blockers.",
                    Detail = $"Blocker count = {blockers} (threshold {_options.OrderBlockerCountThreshold}).",
                    SourceCount = blockers,
                    Severity = "Warning"
                });
                if (maxSeverity == "Info") maxSeverity = "Warning";
            }

            var replacements = replacementMap.GetValueOrDefault(o.Id, 0);
            if (replacements >= _options.ReplacementHeavyPerOrderThreshold)
            {
                reasons.Add(new IntelligenceExplanationDto
                {
                    RuleCode = "ReplacementHeavy",
                    Summary = "Order has multiple material replacements.",
                    Detail = $"Replacement count = {replacements} (threshold {_options.ReplacementHeavyPerOrderThreshold}).",
                    SourceCount = replacements,
                    Severity = "Warning"
                });
                if (maxSeverity == "Info") maxSeverity = "Warning";
            }

            if (lastLogMap.TryGetValue(o.Id, out var lastLog) && lastLog < silentThreshold && o.AssignedSiId != null)
            {
                reasons.Add(new IntelligenceExplanationDto
                {
                    RuleCode = "SilentOrder",
                    Summary = "No recent activity on order beyond threshold.",
                    Detail = $"Last status log {lastLog:O} (>{_options.SilentOrderThresholdHours}h).",
                    SourceCount = 1,
                    Severity = "Info"
                });
                if (maxSeverity == "Info") maxSeverity = "Info";
            }

            if (o.KpiDueAt.HasValue && o.CreatedAt < o.KpiDueAt)
            {
                var elapsed = (now - o.CreatedAt).TotalSeconds;
                var total = (o.KpiDueAt.Value - o.CreatedAt).TotalSeconds;
                if (total > 0 && elapsed / total >= _options.SlaNearingBreachPercent)
                {
                    reasons.Add(new IntelligenceExplanationDto
                    {
                        RuleCode = "SlaNearingBreach",
                        Summary = "Order is nearing SLA breach (KPI due).",
                        Detail = $"KpiDueAt {o.KpiDueAt:O}; elapsed {elapsed / 3600:F1}h of ~{total / 3600:F1}h.",
                        SourceCount = 1,
                        Severity = "Warning"
                    });
                    if (maxSeverity == "Info") maxSeverity = "Warning";
                }
            }

            if (reasons.Count == 0) continue;
            if (severity != null && !string.Equals(maxSeverity, severity, StringComparison.OrdinalIgnoreCase)) continue;
            if (results.Count >= _options.MaxResultsPerList) break;

            results.Add(new OrderRiskSignalDto
            {
                OrderId = o.Id,
                OrderRef = o.ServiceId ?? o.TicketId,
                CompanyId = companyId,
                Status = o.Status,
                AssignedSiId = o.AssignedSiId,
                UpdatedAtUtc = o.UpdatedAt,
                Severity = maxSeverity,
                DetectedAtUtc = now,
                Reasons = reasons
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<InstallerRiskSignalDto>> GetInstallersAtRiskAsync(Guid companyId, string? severity = null, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context required: CompanyId cannot be empty.");

        var now = DateTime.UtcNow;
        var lookback = now.AddDays(-_options.ReplacementLookbackDays);
        var stuckThreshold = now.AddHours(-_options.StuckOrderThresholdHours);

        var installers = await _context.ServiceInstallers
            .AsNoTracking()
            .Where(s => s.CompanyId == companyId && s.IsActive)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync(cancellationToken);

        var results = new List<InstallerRiskSignalDto>();
        foreach (var si in installers)
        {
            var assignedOrderIds = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CompanyId == companyId && o.AssignedSiId == si.Id)
                .OrderByDescending(o => o.UpdatedAt)
                .Take(_options.InstallerPeerWindowSize)
                .Select(o => o.Id)
                .ToListAsync(cancellationToken);

            if (assignedOrderIds.Count == 0) continue;

            var blockerCount = await _context.OrderBlockers
                .AsNoTracking()
                .CountAsync(b => b.CompanyId == companyId && b.RaisedBySiId == si.Id && b.RaisedAt >= lookback, cancellationToken);
            var replacementCount = await _context.OrderMaterialReplacements
                .AsNoTracking()
                .CountAsync(r => r.CompanyId == companyId && r.ReplacedBySiId == si.Id && r.RecordedAt >= lookback, cancellationToken);
            var stuckCount = await _context.Orders
                .AsNoTracking()
                .CountAsync(o => o.CompanyId == companyId && o.AssignedSiId == si.Id && o.Status != OrderStatus.Completed && o.UpdatedAt < stuckThreshold, cancellationToken);

            var ordersWithIssues = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CompanyId == companyId && assignedOrderIds.Contains(o.Id))
                .Select(o => o.Id)
                .ToListAsync(cancellationToken);
            var issueCount = 0;
            if (ordersWithIssues.Count > 0)
            {
                var blockersOnOrders = await _context.OrderBlockers.CountAsync(b => b.CompanyId == companyId && ordersWithIssues.Contains(b.OrderId), cancellationToken);
                var replacementsOnOrders = await _context.OrderMaterialReplacements.CountAsync(r => r.CompanyId == companyId && ordersWithIssues.Contains(r.OrderId), cancellationToken);
                issueCount = blockersOnOrders + replacementsOnOrders;
            }

            var reasons = new List<IntelligenceExplanationDto>();
            string maxSeverity = "Info";

            if (blockerCount >= _options.InstallerBlockerCountThreshold)
            {
                reasons.Add(new IntelligenceExplanationDto
                {
                    RuleCode = "InstallerRepeatedBlockers",
                    Summary = "Installer has repeated blockers on assigned orders.",
                    Detail = $"{blockerCount} blockers in last {_options.ReplacementLookbackDays} days (threshold {_options.InstallerBlockerCountThreshold}).",
                    SourceCount = blockerCount,
                    Severity = blockerCount >= 5 ? "Critical" : "Warning"
                });
                maxSeverity = blockerCount >= 5 ? "Critical" : "Warning";
            }
            if (replacementCount >= _options.InstallerReplacementCountThreshold)
            {
                reasons.Add(new IntelligenceExplanationDto
                {
                    RuleCode = "InstallerHighReplacements",
                    Summary = "Installer has high material replacement frequency.",
                    Detail = $"{replacementCount} replacements in last {_options.ReplacementLookbackDays} days (threshold {_options.InstallerReplacementCountThreshold}).",
                    SourceCount = replacementCount,
                    Severity = replacementCount >= 10 ? "Critical" : "Warning"
                });
                if (maxSeverity != "Critical") maxSeverity = replacementCount >= 10 ? "Critical" : "Warning";
            }
            if (stuckCount > 0)
            {
                reasons.Add(new IntelligenceExplanationDto
                {
                    RuleCode = "InstallerStuckOrders",
                    Summary = "Installer has orders currently stuck.",
                    Detail = $"{stuckCount} assigned order(s) with no update for >{_options.StuckOrderThresholdHours}h.",
                    SourceCount = stuckCount,
                    Severity = stuckCount >= 3 ? "Critical" : "Warning"
                });
                if (maxSeverity != "Critical") maxSeverity = stuckCount >= 3 ? "Critical" : "Warning";
            }
            var issueRatio = assignedOrderIds.Count > 0 ? (double)issueCount / assignedOrderIds.Count : 0;
            if (assignedOrderIds.Count >= 5 && issueRatio >= _options.InstallerIssueRatioThreshold)
            {
                reasons.Add(new IntelligenceExplanationDto
                {
                    RuleCode = "InstallerIssueRatio",
                    Summary = "Installer issue ratio (blockers + replacements) is high vs recent orders.",
                    Detail = $"{issueCount} issues on last {assignedOrderIds.Count} orders (ratio {issueRatio:P0}, threshold {_options.InstallerIssueRatioThreshold:P0}).",
                    SourceCount = issueCount,
                    Severity = "Warning"
                });
                if (maxSeverity == "Info") maxSeverity = "Warning";
            }

            if (reasons.Count == 0) continue;
            if (severity != null && !string.Equals(maxSeverity, severity, StringComparison.OrdinalIgnoreCase)) continue;
            if (results.Count >= _options.MaxResultsPerList) break;

            results.Add(new InstallerRiskSignalDto
            {
                InstallerId = si.Id,
                InstallerDisplayName = si.Name,
                CompanyId = companyId,
                Severity = maxSeverity,
                DetectedAtUtc = now,
                Reasons = reasons
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<BuildingRiskSignalDto>> GetBuildingsAtRiskAsync(Guid companyId, string? severity = null, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context required: CompanyId cannot be empty.");

        var now = DateTime.UtcNow;
        var lookback = now.AddDays(-_options.ReplacementLookbackDays);

        var orderBuildingIds = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CompanyId == companyId && o.BuildingId != Guid.Empty)
            .Select(o => o.BuildingId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var results = new List<BuildingRiskSignalDto>();
        var buildingNames = await _context.Buildings
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId && orderBuildingIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.Name ?? b.Code ?? b.Id.ToString(), cancellationToken);

        foreach (var buildingId in orderBuildingIds)
        {
            var orderIdsAtBuilding = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CompanyId == companyId && o.BuildingId == buildingId)
                .Select(o => o.Id)
                .ToListAsync(cancellationToken);
            if (orderIdsAtBuilding.Count == 0) continue;

            var blockerCount = await _context.OrderBlockers.CountAsync(b => b.CompanyId == companyId && orderIdsAtBuilding.Contains(b.OrderId), cancellationToken);
            var replacementCount = await _context.OrderMaterialReplacements
                .CountAsync(r => r.CompanyId == companyId && orderIdsAtBuilding.Contains(r.OrderId) && r.RecordedAt >= lookback, cancellationToken);
            var ordersWithBlockers = await _context.OrderBlockers
                .AsNoTracking()
                .Where(b => b.CompanyId == companyId && orderIdsAtBuilding.Contains(b.OrderId))
                .Select(b => b.OrderId)
                .Distinct()
                .CountAsync(cancellationToken);

            var reasons = new List<IntelligenceExplanationDto>();
            string maxSeverity = "Info";

            if (ordersWithBlockers >= _options.BuildingRecurrenceThreshold)
            {
                reasons.Add(new IntelligenceExplanationDto
                {
                    RuleCode = "BuildingRepeatedBlockers",
                    Summary = "Multiple orders at this building have had blockers.",
                    Detail = $"{ordersWithBlockers} orders with blockers (threshold {_options.BuildingRecurrenceThreshold}).",
                    SourceCount = ordersWithBlockers,
                    Severity = "Warning"
                });
                maxSeverity = "Warning";
            }
            if (replacementCount >= _options.BuildingReplacementThreshold)
            {
                reasons.Add(new IntelligenceExplanationDto
                {
                    RuleCode = "BuildingRepeatedReplacements",
                    Summary = "High material replacement count at this building.",
                    Detail = $"{replacementCount} replacements in last {_options.ReplacementLookbackDays} days (threshold {_options.BuildingReplacementThreshold}).",
                    SourceCount = replacementCount,
                    Severity = replacementCount >= 8 ? "Critical" : "Warning"
                });
                if (maxSeverity != "Critical") maxSeverity = replacementCount >= 8 ? "Critical" : "Warning";
            }

            if (reasons.Count == 0) continue;
            if (severity != null && !string.Equals(maxSeverity, severity, StringComparison.OrdinalIgnoreCase)) continue;
            if (results.Count >= _options.MaxResultsPerList) break;

            results.Add(new BuildingRiskSignalDto
            {
                BuildingId = buildingId,
                BuildingDisplayName = buildingNames.GetValueOrDefault(buildingId),
                CompanyId = companyId,
                Severity = maxSeverity,
                DetectedAtUtc = now,
                Reasons = reasons
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<TenantRiskSignalDto>> GetTenantRiskSignalsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context required: CompanyId cannot be empty.");

        var now = DateTime.UtcNow;
        var stuckThreshold = now.AddHours(-_options.StuckOrderThresholdHours);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var reasons = new List<IntelligenceExplanationDto>();
        string maxSeverity = "Info";

        var stuckOrders = await _context.Orders
            .CountAsync(o => o.CompanyId == companyId && o.AssignedSiId != null && o.Status != OrderStatus.Completed && o.UpdatedAt < stuckThreshold, cancellationToken);
        if (stuckOrders >= _options.TenantStuckOrdersAnomalyThreshold)
        {
            reasons.Add(new IntelligenceExplanationDto
            {
                RuleCode = "TenantStuckSpike",
                Summary = "Spike in stuck orders for this tenant.",
                Detail = $"{stuckOrders} stuck orders (threshold {_options.TenantStuckOrdersAnomalyThreshold}).",
                SourceCount = stuckOrders,
                Severity = stuckOrders >= 10 ? "Critical" : "Warning"
            });
            maxSeverity = stuckOrders >= 10 ? "Critical" : "Warning";
        }

        var completedMonth = await _context.Orders
            .CountAsync(o => o.CompanyId == companyId && o.Status == OrderStatus.Completed && o.UpdatedAt >= monthStart, cancellationToken);
        var replacementsMonth = await _context.OrderMaterialReplacements
            .CountAsync(r => r.CompanyId == companyId && r.RecordedAt >= monthStart, cancellationToken);
        var ratio = completedMonth > 0 ? (double)replacementsMonth / completedMonth : 0;
        if (completedMonth >= 5 && ratio >= _options.TenantAbnormalReplacementRatioThreshold)
        {
            reasons.Add(new IntelligenceExplanationDto
            {
                RuleCode = "TenantAbnormalReplacementRatio",
                Summary = "Abnormal replacement ratio this month.",
                Detail = $"{replacementsMonth} replacements / {completedMonth} completed = {ratio:P0} (threshold {_options.TenantAbnormalReplacementRatioThreshold:P0}).",
                SourceCount = replacementsMonth,
                Severity = "Warning"
            });
            if (maxSeverity == "Info") maxSeverity = "Warning";
        }

        if (reasons.Count == 0)
            return Array.Empty<TenantRiskSignalDto>();

        var tenantId = await _context.Companies.Where(c => c.Id == companyId).Select(c => c.TenantId).FirstOrDefaultAsync(cancellationToken);
        return new List<TenantRiskSignalDto>
        {
            new TenantRiskSignalDto
            {
                CompanyId = companyId,
                TenantId = tenantId != Guid.Empty ? tenantId : null,
                Severity = maxSeverity,
                DetectedAtUtc = now,
                Reasons = reasons
            }
        };
    }

    public async Task<OperationalIntelligenceSummaryDto> GetPlatformSummaryAsync(CancellationToken cancellationToken = default)
    {
        return await TenantScopeExecutor.RunWithPlatformBypassAsync(async ct =>
        {
            var companyIds = await _context.Companies
                .Where(c => c.TenantId != null)
                .Select(c => c.Id)
                .ToListAsync(ct);
            int totalOrders = 0, totalInstallers = 0, totalBuildings = 0, critical = 0, warning = 0, info = 0;
            foreach (var cid in companyIds)
            {
                try
                {
                    var summary = await GetSummaryAsync(cid, ct);
                    totalOrders += summary.OrdersAtRiskCount;
                    totalInstallers += summary.InstallersAtRiskCount;
                    totalBuildings += summary.BuildingsAtRiskCount;
                    critical += summary.CriticalCount;
                    warning += summary.WarningCount;
                    info += summary.InfoCount;
                }
                catch
                {
                    // Skip tenant on error to avoid leaking; platform summary is best-effort
                }
            }
            return new OperationalIntelligenceSummaryDto
            {
                OrdersAtRiskCount = totalOrders,
                InstallersAtRiskCount = totalInstallers,
                BuildingsAtRiskCount = totalBuildings,
                CriticalCount = critical,
                WarningCount = warning,
                InfoCount = info,
                GeneratedAtUtc = DateTime.UtcNow
            };
        }, cancellationToken);
    }

    private static void CountSeverity(string sev, ref int critical, ref int warning, ref int info)
    {
        if (string.Equals(sev, "Critical", StringComparison.OrdinalIgnoreCase)) critical++;
        else if (string.Equals(sev, "Warning", StringComparison.OrdinalIgnoreCase)) warning++;
        else info++;
    }
}
