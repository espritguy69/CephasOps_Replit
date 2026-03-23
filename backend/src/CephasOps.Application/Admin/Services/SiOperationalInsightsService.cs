using CephasOps.Application.Admin.DTOs;
using CephasOps.Domain.Orders.Enums;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Admin.Services;

/// <summary>
/// Lightweight operational intelligence for Service Installer field operations.
/// Read-only aggregation from OrderStatusLog, Order, OrderMaterialReplacement.
/// Truthful about partial coverage and data gaps.
/// </summary>
public class SiOperationalInsightsService : ISiOperationalInsightsService
{
    private const int MaxTopReasons = 10;
    private const int MaxByInstaller = 20;
    private const int MaxBuildings = 20;
    private const int MaxStuckOrders = 50;
    private const int MaxChurnOrders = 50;
    private const int DefaultStuckThresholdDays = 7;
    private const int DefaultChurnThresholdTransitions = 5;
    private const int MaxPatternSampleOrders = 5;
    private const int MaxPatternSampleBuildings = 5;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<SiOperationalInsightsService> _logger;

    public SiOperationalInsightsService(
        ApplicationDbContext context,
        ILogger<SiOperationalInsightsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SiOperationalInsightsDto> GetInsightsAsync(Guid companyId, int windowDays = 90, CancellationToken cancellationToken = default)
    {
        var windowStart = DateTime.UtcNow.AddDays(-windowDays);
        var result = new SiOperationalInsightsDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            CompanyId = companyId,
            WindowDays = windowDays
        };
        result.CompletionPerformance.StuckThresholdDays = DefaultStuckThresholdDays;
        result.RescheduleBlockerPatterns.ChurnThresholdTransitions = DefaultChurnThresholdTransitions;

        try
        {
            await PopulateCompletionPerformanceAsync(companyId, windowStart, result.CompletionPerformance, cancellationToken).ConfigureAwait(false);
            await PopulateRescheduleBlockerPatternsAsync(companyId, windowStart, result.RescheduleBlockerPatterns, cancellationToken).ConfigureAwait(false);
            await PopulateMaterialReplacementPatternsAsync(companyId, windowStart, result.MaterialReplacementPatterns, cancellationToken).ConfigureAwait(false);
            await PopulateAssuranceReworkAsync(companyId, windowStart, result.AssuranceRework, cancellationToken).ConfigureAwait(false);
            await PopulateOperationalHotspotsAsync(companyId, windowStart, result.OperationalHotspots, cancellationToken).ConfigureAwait(false);
            await PopulateBuildingReliabilityAsync(companyId, windowStart, result, cancellationToken).ConfigureAwait(false);
            await PopulateOrderFailurePatternsAsync(companyId, windowStart, result, cancellationToken).ConfigureAwait(false);
            await PopulatePatternClustersAsync(companyId, windowStart, result, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SI operational insights partial failure for company {CompanyId}", companyId);
            result.DataQualityNote = "One or more aggregations failed; results may be partial. See logs.";
        }

        result.DataGaps.Add("Completion time uses last Assigned → first OrderCompleted in window; reschedules can shorten apparent duration.");
        result.DataGaps.Add("Area/project type is not consistently available; hotspots use BuildingId/BuildingName only.");
        result.DataGaps.Add("Repeat visits by customer/building are approximated by transition churn and reschedule count.");
        result.DataGaps.Add("Building Reliability Score is for prioritization only; uses reschedule/blocker/churn/stuck/assurance/replacement counts in window.");
        result.DataGaps.Add("Order failure patterns are heuristic and for operational review only; they do not imply cause.");
        result.DataGaps.Add("Pattern clusters show where multiple signals align at one building; they do not prove root cause.");

        return result;
    }

    private async Task PopulateCompletionPerformanceAsync(
        Guid companyId,
        DateTime windowStart,
        SiCompletionPerformanceDto dto,
        CancellationToken cancellationToken)
    {
        var completionLogs = await _context.OrderStatusLogs
            .AsNoTracking()
            .Where(l => l.CompanyId == companyId && l.ToStatus == OrderStatus.OrderCompleted && l.CreatedAt >= windowStart)
            .Select(l => new { l.OrderId, CompletedAt = l.CreatedAt, l.TriggeredBySiId })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (completionLogs.Count == 0)
        {
            dto.OrdersCompletedInWindow = 0;
            return;
        }

        var orderIds = completionLogs.Select(x => x.OrderId).Distinct().ToList();
        var assignedLogs = await _context.OrderStatusLogs
            .AsNoTracking()
            .Where(l => l.CompanyId == companyId && orderIds.Contains(l.OrderId) && l.ToStatus == OrderStatus.Assigned && l.CreatedAt >= windowStart)
            .Select(l => new { l.OrderId, l.CreatedAt })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var durations = new List<(Guid OrderId, double Hours, Guid? SiId)>();
        foreach (var comp in completionLogs)
        {
            var orderId = comp.OrderId;
            var completedAt = comp.CompletedAt;
            var lastAssigned = assignedLogs.Where(a => a.OrderId == orderId && a.CreatedAt < completedAt).OrderByDescending(a => a.CreatedAt).FirstOrDefault();
            if (lastAssigned == null) continue;
            var hours = (completedAt - lastAssigned.CreatedAt).TotalHours;
            if (hours < 0) continue;
            durations.Add((orderId, hours, comp.TriggeredBySiId));
        }

        if (durations.Count == 0)
        {
            dto.OrdersCompletedInWindow = completionLogs.Count;
            return;
        }

        dto.OrdersCompletedInWindow = durations.Count;
        dto.AverageAssignedToCompleteHours = Math.Round(durations.Average(d => d.Hours), 2);

        var bySi = durations.Where(d => d.SiId.HasValue).GroupBy(d => d.SiId!.Value)
            .Select(g => new SiInstallerAverageDto { SiId = g.Key, AverageHours = Math.Round(g.Average(x => x.Hours), 2), OrderCount = g.Count() })
            .OrderByDescending(x => x.OrderCount)
            .Take(MaxByInstaller)
            .ToList();
        var siIds = bySi.Select(x => x.SiId).ToList();
        var siNames = await _context.ServiceInstallers
            .AsNoTracking()
            .Where(si => si.CompanyId == companyId && siIds.Contains(si.Id))
            .Select(si => new { si.Id, si.Name })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var nameMap = siNames.ToDictionary(x => x.Id, x => x.Name ?? x.Id.ToString());
        foreach (var row in bySi)
        {
            row.SiDisplayName = row.SiId.HasValue && nameMap.TryGetValue(row.SiId.Value, out var name) ? name : null;
        }
        dto.ByInstaller = bySi;

        var stuckThreshold = DateTime.UtcNow.AddDays(-DefaultStuckThresholdDays);
        var activeStatuses = new[] { OrderStatus.Pending, OrderStatus.Assigned, OrderStatus.OnTheWay, OrderStatus.MetCustomer, OrderStatus.Blocker, OrderStatus.ReschedulePendingApproval };
        var stuck = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CompanyId == companyId && activeStatuses.Contains(o.Status) && o.UpdatedAt < stuckThreshold)
            .OrderBy(o => o.UpdatedAt)
            .Take(MaxStuckOrders)
            .Select(o => new SiStuckOrderDto
            {
                OrderId = o.Id,
                Status = o.Status,
                DaysInCurrentStatus = (int)(DateTime.UtcNow - o.UpdatedAt).TotalDays
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        dto.OrdersStuckLongerThanDays = stuck;
    }

    private async Task PopulateRescheduleBlockerPatternsAsync(
        Guid companyId,
        DateTime windowStart,
        SiRescheduleBlockerPatternsDto dto,
        CancellationToken cancellationToken)
    {
        var rescheduleReasons = await _context.OrderStatusLogs
            .AsNoTracking()
            .Where(l => l.CompanyId == companyId && l.ToStatus == OrderStatus.ReschedulePendingApproval && l.CreatedAt >= windowStart)
            .GroupBy(l => l.TransitionReason ?? "(none)")
            .Select(g => new SiReasonCountDto { Reason = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(MaxTopReasons)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        dto.TopRescheduleReasons = rescheduleReasons;

        var blockerReasons = await _context.OrderStatusLogs
            .AsNoTracking()
            .Where(l => l.CompanyId == companyId && l.ToStatus == OrderStatus.Blocker && l.CreatedAt >= windowStart)
            .GroupBy(l => l.TransitionReason ?? "(none)")
            .Select(g => new SiReasonCountDto { Reason = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(MaxTopReasons)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        dto.TopBlockerReasons = blockerReasons;

        var churn = await _context.OrderStatusLogs
            .AsNoTracking()
            .Where(l => l.CompanyId == companyId && l.CreatedAt >= windowStart)
            .GroupBy(l => l.OrderId)
            .Select(g => new
            {
                OrderId = g.Key,
                TransitionCount = g.Count(),
                RescheduleCount = g.Count(x => x.ToStatus == OrderStatus.ReschedulePendingApproval),
                BlockerCount = g.Count(x => x.ToStatus == OrderStatus.Blocker)
            })
            .Where(x => x.TransitionCount >= DefaultChurnThresholdTransitions)
            .OrderByDescending(x => x.TransitionCount)
            .Take(MaxChurnOrders)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        dto.OrdersWithHighChurn = churn.Select(x => new SiOrderChurnDto
        {
            OrderId = x.OrderId,
            TransitionCount = x.TransitionCount,
            RescheduleCount = x.RescheduleCount,
            BlockerCount = x.BlockerCount
        }).ToList();
        dto.ChurnThresholdTransitions = DefaultChurnThresholdTransitions;
    }

    private async Task PopulateMaterialReplacementPatternsAsync(
        Guid companyId,
        DateTime windowStart,
        SiMaterialReplacementPatternsDto dto,
        CancellationToken cancellationToken)
    {
        var replacements = await _context.OrderMaterialReplacements
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId && r.RecordedAt >= windowStart)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        dto.TopReplacementReasons = replacements
            .GroupBy(r => r.ReplacementReason ?? "(none)")
            .OrderByDescending(g => g.Count())
            .Take(MaxTopReasons)
            .Select(g => new SiReasonCountDto { Reason = g.Key, Count = g.Count() })
            .ToList();

        var bySi = replacements.Where(r => r.ReplacedBySiId.HasValue)
            .GroupBy(r => r.ReplacedBySiId!.Value)
            .OrderByDescending(g => g.Count())
            .Take(MaxByInstaller)
            .Select(g => new SiInstallerCountDto { SiId = g.Key, Count = g.Count() })
            .ToList();
        var siIds = bySi.Select(x => x.SiId).ToList();
        var siNames = await _context.ServiceInstallers
            .AsNoTracking()
            .Where(si => si.CompanyId == companyId && siIds.Contains(si.Id))
            .Select(si => new { si.Id, si.Name })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var nameMap = siNames.ToDictionary(x => x.Id, x => x.Name ?? x.Id.ToString());
        foreach (var row in bySi)
        {
            row.SiDisplayName = row.SiId.HasValue && nameMap.TryGetValue(row.SiId.Value, out var name) ? name : null;
        }
        dto.ByInstaller = bySi;

        var orderReplacementCounts = replacements.GroupBy(r => r.OrderId).Select(g => g.Count()).ToList();
        dto.OrdersWithMultipleReplacements = orderReplacementCounts.Count(c => c > 1);
    }

    private async Task PopulateAssuranceReworkAsync(
        Guid companyId,
        DateTime windowStart,
        SiAssuranceReworkDto dto,
        CancellationToken cancellationToken)
    {
        var assuranceTypeId = await _context.OrderTypes
            .AsNoTracking()
            .Where(ot => ot.CompanyId == companyId && ot.Code == "ASSURANCE")
            .Select(ot => ot.Id)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (assuranceTypeId == default)
        {
            dto.AssuranceOrdersCompletedInWindow = 0;
            dto.AssuranceOrdersWithReplacement = 0;
            return;
        }

        var completedInWindow = await _context.OrderStatusLogs
            .AsNoTracking()
            .Where(l => l.CompanyId == companyId && l.ToStatus == OrderStatus.OrderCompleted && l.CreatedAt >= windowStart)
            .Select(l => l.OrderId)
            .Distinct()
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (completedInWindow.Count == 0)
        {
            dto.AssuranceOrdersCompletedInWindow = 0;
            dto.AssuranceOrdersWithReplacement = 0;
            return;
        }

        var assuranceCompleted = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CompanyId == companyId && completedInWindow.Contains(o.Id) && o.OrderTypeId == assuranceTypeId)
            .Select(o => new { o.Id, o.Issue })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        dto.AssuranceOrdersCompletedInWindow = assuranceCompleted.Count;

        var assuranceOrderIds = assuranceCompleted.Select(o => o.Id).ToList();
        var withReplacement = await _context.OrderMaterialReplacements
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId && assuranceOrderIds.Contains(r.OrderId))
            .Select(r => r.OrderId)
            .Distinct()
            .CountAsync(cancellationToken).ConfigureAwait(false);
        dto.AssuranceOrdersWithReplacement = withReplacement;

        dto.TopAssuranceIssues = assuranceCompleted
            .Where(o => !string.IsNullOrWhiteSpace(o.Issue))
            .GroupBy(o => o.Issue!)
            .OrderByDescending(g => g.Count())
            .Take(MaxTopReasons)
            .Select(g => new SiReasonCountDto { Reason = g.Key, Count = g.Count() })
            .ToList();
    }

    private async Task PopulateOperationalHotspotsAsync(
        Guid companyId,
        DateTime windowStart,
        SiOperationalHotspotsDto dto,
        CancellationToken cancellationToken)
    {
        var logs = await _context.OrderStatusLogs
            .AsNoTracking()
            .Where(l => l.CompanyId == companyId && l.CreatedAt >= windowStart &&
                (l.ToStatus == OrderStatus.ReschedulePendingApproval || l.ToStatus == OrderStatus.Blocker))
            .Select(l => new { l.OrderId, l.ToStatus })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (logs.Count == 0)
        {
            dto.CoverageNote = "Area/building/project type not consistently available; hotspots use BuildingId/BuildingName only.";
            return;
        }

        var orderIds = logs.Select(l => l.OrderId).Distinct().ToList();
        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CompanyId == companyId && orderIds.Contains(o.Id))
            .Select(o => new { o.Id, o.BuildingId, o.BuildingName })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var byBuilding = logs
            .Join(orders, l => l.OrderId, o => o.Id, (l, o) => new { o.BuildingId, o.BuildingName, l.ToStatus })
            .GroupBy(x => new { x.BuildingId, x.BuildingName })
            .Select(g => new SiBuildingCountDto
            {
                BuildingId = g.Key.BuildingId,
                BuildingName = g.Key.BuildingName,
                RescheduleCount = g.Count(x => x.ToStatus == OrderStatus.ReschedulePendingApproval),
                BlockerCount = g.Count(x => x.ToStatus == OrderStatus.Blocker)
            })
            .OrderByDescending(x => x.RescheduleCount + x.BlockerCount)
            .Take(MaxBuildings)
            .ToList();
        dto.BuildingsWithMostDisruptions = byBuilding;
        dto.CoverageNote = "Area/project type not consistently available; hotspots use BuildingId/BuildingName only.";
    }

    /// <summary>
    /// Building Reliability Score: explainable band and contributing factors per building.
    /// Uses same buildings as OperationalHotspots; enriches with churn, stuck, assurance, replacement counts.
    /// For operator prioritization only; not for automated enforcement.
    /// </summary>
    private async Task PopulateBuildingReliabilityAsync(
        Guid companyId,
        DateTime windowStart,
        SiOperationalInsightsDto result,
        CancellationToken cancellationToken)
    {
        result.BuildingReliability.InterpretationNote = "Score uses reschedule/blocker events, high-churn orders, stuck orders, assurance-with-replacement, and orders with replacements in the window. For review and prioritization only; data may be partial.";
        var buildings = result.OperationalHotspots.BuildingsWithMostDisruptions;
        if (buildings.Count == 0)
            return;

        var buildingIds = buildings.Select(b => b.BuildingId).ToHashSet();

        // High-churn order IDs -> BuildingId
        var churnOrderIds = result.RescheduleBlockerPatterns.OrdersWithHighChurn.Select(c => c.OrderId).Distinct().ToList();
        List<(Guid Id, Guid BuildingId)> churnOrderToBuilding;
        if (churnOrderIds.Count > 0)
        {
            var churnRows = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CompanyId == companyId && churnOrderIds.Contains(o.Id))
                .Select(o => new { o.Id, o.BuildingId })
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            churnOrderToBuilding = churnRows.Select(x => (x.Id, x.BuildingId)).ToList();
        }
        else
            churnOrderToBuilding = new List<(Guid Id, Guid BuildingId)>();
        var churnByBuilding = churnOrderToBuilding
            .Where(x => buildingIds.Contains(x.BuildingId))
            .GroupBy(x => x.BuildingId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Stuck order IDs -> BuildingId
        var stuckOrderIds = result.CompletionPerformance.OrdersStuckLongerThanDays.Select(s => s.OrderId).Distinct().ToList();
        List<(Guid Id, Guid BuildingId)> stuckOrderToBuilding;
        if (stuckOrderIds.Count > 0)
        {
            var stuckRows = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CompanyId == companyId && stuckOrderIds.Contains(o.Id))
                .Select(o => new { o.Id, o.BuildingId })
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            stuckOrderToBuilding = stuckRows.Select(x => (x.Id, x.BuildingId)).ToList();
        }
        else
            stuckOrderToBuilding = new List<(Guid Id, Guid BuildingId)>();
        var stuckByBuilding = stuckOrderToBuilding
            .Where(x => buildingIds.Contains(x.BuildingId))
            .GroupBy(x => x.BuildingId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Assurance orders (completed in window) with at least one replacement -> BuildingId counts
        var assuranceTypeId = await _context.OrderTypes
            .AsNoTracking()
            .Where(ot => ot.CompanyId == companyId && ot.Code == "ASSURANCE")
            .Select(ot => ot.Id)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var assuranceWithReplacementByBuilding = new Dictionary<Guid, int>();
        if (assuranceTypeId != default)
        {
            var completedInWindow = await _context.OrderStatusLogs
                .AsNoTracking()
                .Where(l => l.CompanyId == companyId && l.ToStatus == OrderStatus.OrderCompleted && l.CreatedAt >= windowStart)
                .Select(l => l.OrderId)
                .Distinct()
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            if (completedInWindow.Count > 0)
            {
                var assuranceCompleted = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.CompanyId == companyId && completedInWindow.Contains(o.Id) && o.OrderTypeId == assuranceTypeId)
                    .Select(o => new { o.Id, o.BuildingId })
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
                var assuranceOrderIds = assuranceCompleted.Select(o => o.Id).ToList();
                var withReplacement = assuranceOrderIds.Count > 0
                    ? await _context.OrderMaterialReplacements
                        .AsNoTracking()
                        .Where(r => r.CompanyId == companyId && assuranceOrderIds.Contains(r.OrderId))
                        .Select(r => r.OrderId)
                        .Distinct()
                        .ToListAsync(cancellationToken).ConfigureAwait(false)
                    : new List<Guid>();
                var buildingCounts = assuranceCompleted
                    .Where(o => withReplacement.Contains(o.Id))
                    .GroupBy(o => o.BuildingId)
                    .ToDictionary(g => g.Key, g => g.Count());
                foreach (var kv in buildingCounts.Where(kv => buildingIds.Contains(kv.Key)))
                    assuranceWithReplacementByBuilding[kv.Key] = kv.Value;
            }
        }

        // Orders with at least one replacement in window -> BuildingId counts
        var replacementOrderIds = await _context.OrderMaterialReplacements
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId && r.RecordedAt >= windowStart)
            .Select(r => r.OrderId)
            .Distinct()
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var ordersWithReplacementsByBuilding = new Dictionary<Guid, int>();
        if (replacementOrderIds.Count > 0)
        {
            var orderBuildings = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CompanyId == companyId && replacementOrderIds.Contains(o.Id))
                .Select(o => new { o.Id, o.BuildingId })
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var g in orderBuildings.Where(x => buildingIds.Contains(x.BuildingId)).GroupBy(x => x.BuildingId))
                ordersWithReplacementsByBuilding[g.Key] = g.Count();
        }

        // Build items with band and reason summary
        foreach (var b in buildings)
        {
            var rescheduleCount = b.RescheduleCount;
            var blockerCount = b.BlockerCount;
            var highChurnCount = churnByBuilding.TryGetValue(b.BuildingId, out var hc) ? hc : 0;
            var stuckCount = stuckByBuilding.TryGetValue(b.BuildingId, out var sc) ? sc : 0;
            var assuranceReplCount = assuranceWithReplacementByBuilding.TryGetValue(b.BuildingId, out var ar) ? ar : 0;
            var ordersWithReplCount = ordersWithReplacementsByBuilding.TryGetValue(b.BuildingId, out var ow) ? ow : 0;

            var (band, reasonSummary) = ComputeBuildingReliabilityBandAndReason(
                rescheduleCount, blockerCount, highChurnCount, stuckCount, assuranceReplCount, ordersWithReplCount);

            result.BuildingReliability.Buildings.Add(new SiBuildingReliabilityItemDto
            {
                BuildingId = b.BuildingId,
                BuildingName = b.BuildingName,
                Band = band,
                RescheduleCount = rescheduleCount,
                BlockerCount = blockerCount,
                HighChurnOrderCount = highChurnCount,
                StuckOrderCount = stuckCount,
                AssuranceWithReplacementCount = assuranceReplCount,
                OrdersWithReplacementsCount = ordersWithReplCount,
                ReasonSummary = reasonSummary
            });
        }
    }

    private static (string Band, string ReasonSummary) ComputeBuildingReliabilityBandAndReason(
        int rescheduleCount, int blockerCount, int highChurnOrderCount, int stuckOrderCount,
        int assuranceWithReplacementCount, int ordersWithReplacementsCount)
    {
        var totalDisruption = rescheduleCount + blockerCount;
        bool highRisk = totalDisruption >= 10 || stuckOrderCount >= 2 || highChurnOrderCount >= 3;
        bool moderateRisk = !highRisk && (totalDisruption >= 4 || stuckOrderCount >= 1 || highChurnOrderCount >= 1
            || assuranceWithReplacementCount >= 2 || ordersWithReplacementsCount >= 3);
        string band = highRisk ? "HighRisk" : moderateRisk ? "ModerateRisk" : "LowRisk";

        var parts = new List<string>();
        if (totalDisruption >= 4) parts.Add("Frequent reschedules and blockers");
        if (stuckOrderCount > 0) parts.Add($"{stuckOrderCount} stuck order(s)");
        if (highChurnOrderCount > 0) parts.Add($"{highChurnOrderCount} high-churn order(s)");
        if (assuranceWithReplacementCount >= 2) parts.Add("Elevated assurance/rework with replacement");
        if (ordersWithReplacementsCount >= 3) parts.Add("Multiple orders with replacements");
        if (parts.Count == 0) parts.Add("Some disruption in window; overall low risk");
        var reasonSummary = string.Join("; ", parts) + ".";

        return (band, reasonSummary);
    }

    /// <summary>
    /// Order failure pattern detection: recurring operational/technical patterns from existing data.
    /// Explainable rules only; for operational review and prioritization.
    /// </summary>
    private async Task PopulateOrderFailurePatternsAsync(
        Guid companyId,
        DateTime windowStart,
        SiOperationalInsightsDto result,
        CancellationToken cancellationToken)
    {
        result.OrderFailurePatterns.InterpretationNote = "Patterns are derived from reschedule/blocker logs, churn, stuck orders, assurance and replacement data in the window. For operational review only; strength indicates confidence (Strong Signal = high-confidence rule, Review Needed = heuristic or volume-driven).";
        var patterns = result.OrderFailurePatterns.Patterns;

        // 1. Blocker + reschedule on same order (in window)
        var ordersWithBlocker = await _context.OrderStatusLogs
            .AsNoTracking()
            .Where(l => l.CompanyId == companyId && l.ToStatus == OrderStatus.Blocker && l.CreatedAt >= windowStart)
            .Select(l => l.OrderId)
            .Distinct()
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var ordersWithReschedule = await _context.OrderStatusLogs
            .AsNoTracking()
            .Where(l => l.CompanyId == companyId && l.ToStatus == OrderStatus.ReschedulePendingApproval && l.CreatedAt >= windowStart)
            .Select(l => l.OrderId)
            .Distinct()
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var blockerAndRescheduleOrderIds = ordersWithBlocker.Intersect(ordersWithReschedule).ToList();
        if (blockerAndRescheduleOrderIds.Count > 0)
        {
            patterns.Add(new SiOrderFailurePatternItemDto
            {
                PatternId = "BlockerAndRescheduleOnSameOrder",
                PatternName = "Blocker + reschedule on same order",
                Count = blockerAndRescheduleOrderIds.Count,
                SampleOrderIds = blockerAndRescheduleOrderIds.Take(MaxPatternSampleOrders).ToList(),
                Explanation = "May indicate access or coordination issues; order experienced both blocker and reschedule in window.",
                Strength = "StrongSignal"
            });
        }

        // 2. Assurance orders with 2+ replacements (in window)
        var assuranceTypeId = await _context.OrderTypes
            .AsNoTracking()
            .Where(ot => ot.CompanyId == companyId && ot.Code == "ASSURANCE")
            .Select(ot => ot.Id)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (assuranceTypeId != default)
        {
            var replacementCountByOrder = await _context.OrderMaterialReplacements
                .AsNoTracking()
                .Where(r => r.CompanyId == companyId && r.RecordedAt >= windowStart)
                .GroupBy(r => r.OrderId)
                .Select(g => new { OrderId = g.Key, Count = g.Count() })
                .Where(x => x.Count >= 2)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var assuranceOrderIds = replacementCountByOrder.Select(x => x.OrderId).ToList();
            if (assuranceOrderIds.Count > 0)
            {
                var assuranceWithMultipleRepl = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.CompanyId == companyId && assuranceOrderIds.Contains(o.Id) && o.OrderTypeId == assuranceTypeId)
                    .Select(o => o.Id)
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
                if (assuranceWithMultipleRepl.Count > 0)
                {
                    patterns.Add(new SiOrderFailurePatternItemDto
                    {
                        PatternId = "AssuranceWithMultipleReplacements",
                        PatternName = "Replacement-heavy assurance orders",
                        Count = assuranceWithMultipleRepl.Count,
                        SampleOrderIds = assuranceWithMultipleRepl.Take(MaxPatternSampleOrders).ToList(),
                        Explanation = "May indicate device/ONT quality or environment issues; assurance order required multiple replacements.",
                        Strength = "StrongSignal"
                    });
                }
            }
        }

        // 3. High-churn orders concentrated in one building (2+ high-churn orders per building)
        var churnOrderIds = result.RescheduleBlockerPatterns.OrdersWithHighChurn.Select(c => c.OrderId).Distinct().ToList();
        if (churnOrderIds.Count >= 2)
        {
            var churnBuildingRows = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CompanyId == companyId && churnOrderIds.Contains(o.Id))
                .Select(o => new { o.Id, o.BuildingId })
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var churnByBuilding = churnBuildingRows
                .GroupBy(x => x.BuildingId)
                .Where(g => g.Count() >= 2)
                .Select(g => new { BuildingId = g.Key, OrderIds = g.Select(x => x.Id).ToList() })
                .ToList();
            if (churnByBuilding.Count > 0)
            {
                var totalOrders = churnByBuilding.Sum(b => b.OrderIds.Count);
                var sampleBuildingIds = churnByBuilding.Take(MaxPatternSampleBuildings).Select(b => b.BuildingId).ToList();
                patterns.Add(new SiOrderFailurePatternItemDto
                {
                    PatternId = "HighChurnConcentratedInBuilding",
                    PatternName = "High-churn orders concentrated in building",
                    Count = churnByBuilding.Count,
                    SampleBuildingIds = sampleBuildingIds,
                    Explanation = "May indicate building-level access or coordination issues; multiple high-churn orders at same building.",
                    Strength = "StrongSignal"
                });
            }
        }

        // 4. Stuck orders that also have high churn
        var stuckOrderIds = result.CompletionPerformance.OrdersStuckLongerThanDays.Select(s => s.OrderId).ToHashSet();
        var stuckAndChurn = result.RescheduleBlockerPatterns.OrdersWithHighChurn
            .Select(c => c.OrderId)
            .Where(id => stuckOrderIds.Contains(id))
            .Distinct()
            .ToList();
        if (stuckAndChurn.Count > 0)
        {
            patterns.Add(new SiOrderFailurePatternItemDto
            {
                PatternId = "StuckOrdersWithHighChurn",
                PatternName = "Orders stuck after multiple transition attempts",
                Count = stuckAndChurn.Count,
                SampleOrderIds = stuckAndChurn.Take(MaxPatternSampleOrders).ToList(),
                Explanation = "Orders stuck after many transition attempts; may need escalation or QA review.",
                Strength = "StrongSignal"
            });
        }

        // 5. Buildings with 3+ orders that had at least one replacement in window
        var replacementOrderIds = await _context.OrderMaterialReplacements
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId && r.RecordedAt >= windowStart)
            .Select(r => r.OrderId)
            .Distinct()
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (replacementOrderIds.Count >= 3)
        {
            var orderToBuilding = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CompanyId == companyId && replacementOrderIds.Contains(o.Id))
                .Select(o => new { o.Id, o.BuildingId })
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var buildingReplacementCounts = orderToBuilding
                .GroupBy(x => x.BuildingId)
                .Where(g => g.Count() >= 3)
                .Select(g => g.Key)
                .ToList();
            if (buildingReplacementCounts.Count > 0)
            {
                patterns.Add(new SiOrderFailurePatternItemDto
                {
                    PatternId = "BuildingWithManyReplacementOrders",
                    PatternName = "Repeated replacement-heavy activity at same building",
                    Count = buildingReplacementCounts.Count,
                    SampleBuildingIds = buildingReplacementCounts.Take(MaxPatternSampleBuildings).ToList(),
                    Explanation = "Multiple orders with replacements at same building; may indicate infrastructure or device issues.",
                    Strength = "StrongSignal"
                });
            }
        }

        // 6. Same assurance issue repeated (2+ orders with same Order.Issue)
        var topIssues = result.AssuranceRework.TopAssuranceIssues.Where(r => r.Count >= 2).ToList();
        foreach (var issue in topIssues.Take(3))
        {
            patterns.Add(new SiOrderFailurePatternItemDto
            {
                PatternId = "SameAssuranceIssueRepeated",
                PatternName = "Same assurance issue across multiple orders",
                Count = issue.Count,
                Explanation = $"Issue \"{((issue.Reason ?? "(none)").Length > 40 ? (issue.Reason ?? "(none)")[..40] + "…" : (issue.Reason ?? "(none)"))}\" on {issue.Count} assurance orders; may indicate systematic or documentation pattern.",
                Strength = "StrongSignal",
                Limitations = "Sample order IDs not included; use Assurance section for issue drill-down."
            });
        }

        // 7. Disruption concentrated by order type (top order types by reschedule+blocker event count)
        var logsByOrder = await _context.OrderStatusLogs
            .AsNoTracking()
            .Where(l => l.CompanyId == companyId && l.CreatedAt >= windowStart &&
                (l.ToStatus == OrderStatus.ReschedulePendingApproval || l.ToStatus == OrderStatus.Blocker))
            .Select(l => new { l.OrderId, l.ToStatus })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (logsByOrder.Count > 0)
        {
            var orderIds = logsByOrder.Select(l => l.OrderId).Distinct().ToList();
            var orderTypeMap = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CompanyId == companyId && orderIds.Contains(o.Id))
                .Select(o => new { o.Id, o.OrderTypeId })
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var typeNames = await _context.OrderTypes
                .AsNoTracking()
                .Where(ot => ot.CompanyId == companyId && orderTypeMap.Select(m => m.OrderTypeId).Distinct().Contains(ot.Id))
                .Select(ot => new { ot.Id, ot.Code, ot.Name })
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var typeNameDict = typeNames.ToDictionary(t => t.Id, t => t.Name ?? t.Code ?? t.Id.ToString());
            var eventsByOrderType = orderTypeMap
                .Join(logsByOrder, o => o.Id, l => l.OrderId, (o, l) => o.OrderTypeId)
                .GroupBy(x => x)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .ToList();
            if (eventsByOrderType.Count > 0)
            {
                var topType = eventsByOrderType.First();
                var typeName = typeNameDict.TryGetValue(topType.Key, out var n) ? n : topType.Key.ToString();
                patterns.Add(new SiOrderFailurePatternItemDto
                {
                    PatternId = "DisruptionConcentratedByOrderType",
                    PatternName = "Disruption concentrated by order type",
                    Count = topType.Count(),
                    Explanation = $"Most reschedule/blocker events in window are for order type \"{typeName}\". Concentration may reflect volume; review whether this order type has inherent risk.",
                    Strength = "ReviewNeeded",
                    Limitations = "Volume-driven; high count may be due to order mix rather than failure rate."
                });
            }
        }

        // 8. Disruption concentrated by installer (reschedule/blocker events by TriggeredBySiId)
        const int MinDisruptionEventsPerInstaller = 5;
        var disruptionLogsBySi = await _context.OrderStatusLogs
            .AsNoTracking()
            .Where(l => l.CompanyId == companyId && l.CreatedAt >= windowStart &&
                (l.ToStatus == OrderStatus.ReschedulePendingApproval || l.ToStatus == OrderStatus.Blocker) &&
                l.TriggeredBySiId != null)
            .GroupBy(l => l.TriggeredBySiId!.Value)
            .Select(g => new { SiId = g.Key, EventCount = g.Count() })
            .Where(x => x.EventCount >= MinDisruptionEventsPerInstaller)
            .OrderByDescending(x => x.EventCount)
            .Take(5)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (disruptionLogsBySi.Count > 0)
        {
            var siIds = disruptionLogsBySi.Select(x => x.SiId).ToList();
            var siNames = await _context.ServiceInstallers
                .AsNoTracking()
                .Where(si => si.CompanyId == companyId && siIds.Contains(si.Id))
                .Select(si => new { si.Id, si.Name })
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var nameDict = siNames.ToDictionary(x => x.Id, x => x.Name ?? (string?)null);
            patterns.Add(new SiOrderFailurePatternItemDto
            {
                PatternId = "DisruptionConcentratedByInstaller",
                PatternName = "Disruption concentrated by installer",
                Count = disruptionLogsBySi.Count,
                SampleInstallerIds = siIds,
                SampleInstallerDisplayNames = siIds.Select(id => nameDict.TryGetValue(id, out var name) ? name : null).ToList(),
                Explanation = "Installers associated with many reschedule/blocker events in window. Review assignment mix, training, or routing; concentration does not imply the installer caused the issue.",
                Strength = "ReviewNeeded",
                Limitations = "Installer may be the one reporting the disruption (e.g. logging the blocker), not necessarily the root cause. Use for operational review only."
            });
        }

        // 9. Replacement activity concentrated by installer (from MaterialReplacementPatterns.ByInstaller)
        const int MinReplacementsPerInstaller = 3;
        var replacementByInstaller = result.MaterialReplacementPatterns.ByInstaller
            .Where(x => x.SiId.HasValue && x.Count >= MinReplacementsPerInstaller)
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();
        if (replacementByInstaller.Count > 0)
        {
            patterns.Add(new SiOrderFailurePatternItemDto
            {
                PatternId = "ReplacementActivityConcentratedByInstaller",
                PatternName = "Replacement activity concentrated by installer",
                Count = replacementByInstaller.Count,
                SampleInstallerIds = replacementByInstaller.Select(x => x.SiId!.Value).ToList(),
                SampleInstallerDisplayNames = replacementByInstaller.Select(x => x.SiDisplayName).ToList(),
                Explanation = "Installers with high replacement count in window. May reflect assignment mix, stock/device handling, or environment; does not imply fault. Review for training or process.",
                Strength = "ReviewNeeded",
                Limitations = "Association only; may reflect job mix or building/device factors rather than installer performance."
            });
        }
    }

    /// <summary>
    /// Pattern cluster detection: buildings where multiple operational signals align.
    /// Only creates clusters when at least two signals are present; does not claim certainty.
    /// </summary>
    private async Task PopulatePatternClustersAsync(
        Guid companyId,
        DateTime windowStart,
        SiOperationalInsightsDto result,
        CancellationToken cancellationToken)
    {
        result.PatternClusters.InterpretationNote = "Clusters show buildings where multiple signals align (e.g. high reliability risk + high-churn orders + replacement-heavy orders). For operational review only; does not prove root cause.";
        var buildings = result.BuildingReliability.Buildings;
        if (buildings.Count == 0)
            return;

        // Order IDs at each building: churn orders, replacement orders, and stuck orders (for sample list)
        var churnOrderIds = result.RescheduleBlockerPatterns.OrdersWithHighChurn.Select(c => c.OrderId).Distinct().ToList();
        List<(Guid OrderId, Guid BuildingId)> churnOrderToBuilding = new();
        if (churnOrderIds.Count > 0)
        {
            var rows = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CompanyId == companyId && churnOrderIds.Contains(o.Id))
                .Select(o => new { o.Id, o.BuildingId })
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            churnOrderToBuilding = rows.Select(x => (x.Id, x.BuildingId)).ToList();
        }

        var replacementOrderIds = await _context.OrderMaterialReplacements
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId && r.RecordedAt >= windowStart)
            .Select(r => r.OrderId)
            .Distinct()
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        List<(Guid OrderId, Guid BuildingId)> replacementOrderToBuilding = new();
        if (replacementOrderIds.Count > 0)
        {
            var rows = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CompanyId == companyId && replacementOrderIds.Contains(o.Id))
                .Select(o => new { o.Id, o.BuildingId })
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            replacementOrderToBuilding = rows.Select(x => (x.Id, x.BuildingId)).ToList();
        }

        var stuckOrderIds = result.CompletionPerformance.OrdersStuckLongerThanDays.Select(s => s.OrderId).Distinct().ToList();
        List<(Guid OrderId, Guid BuildingId)> stuckOrderToBuilding = new();
        if (stuckOrderIds.Count > 0)
        {
            var rows = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CompanyId == companyId && stuckOrderIds.Contains(o.Id))
                .Select(o => new { o.Id, o.BuildingId })
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            stuckOrderToBuilding = rows.Select(x => (x.Id, x.BuildingId)).ToList();
        }

        var buildingIds = buildings.Select(b => b.BuildingId).ToHashSet();
        var churnByBuilding = churnOrderToBuilding.Where(x => buildingIds.Contains(x.BuildingId)).GroupBy(x => x.BuildingId).ToDictionary(g => g.Key, g => g.Select(x => x.OrderId).ToList());
        var replacementByBuilding = replacementOrderToBuilding.Where(x => buildingIds.Contains(x.BuildingId)).GroupBy(x => x.BuildingId).ToDictionary(g => g.Key, g => g.Select(x => x.OrderId).ToList());
        var stuckByBuilding = stuckOrderToBuilding.Where(x => buildingIds.Contains(x.BuildingId)).GroupBy(x => x.BuildingId).ToDictionary(g => g.Key, g => g.Select(x => x.OrderId).ToList());

        const int MaxSampleOrdersPerCluster = 5;

        foreach (var b in buildings)
        {
            var hasHighRisk = b.Band == "HighRisk";
            var hasModerateRisk = b.Band == "ModerateRisk";
            var hasHighChurn = b.HighChurnOrderCount >= 1;
            var hasReplacementHeavy = b.OrdersWithReplacementsCount >= 1;
            var hasStuck = b.StuckOrderCount >= 1;

            // Cluster only when multiple signals align (at least 2 beyond the risk band)
            var signalCount = (hasHighChurn ? 1 : 0) + (hasReplacementHeavy ? 1 : 0) + (hasStuck ? 1 : 0);
            if (signalCount < 2)
                continue;

            var signalsPresent = new List<string>();
            if (hasHighRisk) signalsPresent.Add("High building reliability risk");
            else if (hasModerateRisk) signalsPresent.Add("Moderate building reliability risk");
            if (hasHighChurn) signalsPresent.Add("High-churn orders present");
            if (hasReplacementHeavy) signalsPresent.Add("Replacement-heavy orders present");
            if (hasStuck) signalsPresent.Add("Stuck orders present");

            // Merge sample orders: churn, then replacement, then stuck; deduplicate and cap at MaxSampleOrdersPerCluster
            var sampleOrderIds = new List<Guid>();
            var seen = new HashSet<Guid>();
            if (churnByBuilding.TryGetValue(b.BuildingId, out var churnOrders))
                foreach (var id in churnOrders.Take(MaxSampleOrdersPerCluster))
                    if (seen.Add(id)) sampleOrderIds.Add(id);
            if (sampleOrderIds.Count < MaxSampleOrdersPerCluster && replacementByBuilding.TryGetValue(b.BuildingId, out var replOrders))
                foreach (var id in replOrders)
                    if (seen.Add(id)) { sampleOrderIds.Add(id); if (sampleOrderIds.Count >= MaxSampleOrdersPerCluster) break; }
            if (sampleOrderIds.Count < MaxSampleOrdersPerCluster && stuckByBuilding.TryGetValue(b.BuildingId, out var stuckOrders))
                foreach (var id in stuckOrders)
                    if (seen.Add(id)) { sampleOrderIds.Add(id); if (sampleOrderIds.Count >= MaxSampleOrdersPerCluster) break; }
            sampleOrderIds = sampleOrderIds.Take(MaxSampleOrdersPerCluster).ToList();

            string classification = hasHighRisk ? "PossibleInfrastructureIssue" : "OperationalCluster";
            string interpretation = hasHighRisk
                ? "Multiple signals at this building; possible infrastructure or building coordination issue. Review access, site conditions, or device environment."
                : "Multiple operational signals at this building. Review for coordination, assignment mix, or process issues.";

            result.PatternClusters.Clusters.Add(new SiPatternClusterItemDto
            {
                BuildingId = b.BuildingId,
                BuildingName = b.BuildingName,
                SignalsPresent = signalsPresent,
                SampleOrderIds = sampleOrderIds,
                Interpretation = interpretation,
                Classification = classification,
                Limitations = "Cluster is based on aligned signals only; does not prove root cause. Use for prioritization and review."
            });
        }
    }
}
