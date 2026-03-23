using System.Text.Json;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Domain.Orders.Enums;
using CephasOps.Domain.Rates;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Read-only dashboard service. Uses OrderPayoutSnapshots, Orders, and PnlDetailPerOrder for reporting only.
/// No payout calculation or snapshot creation logic.
/// </summary>
public class PayoutHealthDashboardService : IPayoutHealthDashboardService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ApplicationDbContext _context;
    private readonly ILogger<PayoutHealthDashboardService> _logger;

    public PayoutHealthDashboardService(
        ApplicationDbContext context,
        ILogger<PayoutHealthDashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PayoutHealthDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var snapshotHealth = await GetSnapshotHealthAsync(cancellationToken);
        var anomalySummary = await GetAnomalySummaryAsync(cancellationToken);
        var topUnusual = await GetTopUnusualPayoutsAsync(cancellationToken);
        var recentSnapshots = await GetRecentSnapshotsAsync(cancellationToken);
        var latestRepairRun = await GetLatestRepairRunAsync(cancellationToken);
        var recentRepairRuns = await GetRecentRepairRunsAsync(10, cancellationToken);

        return new PayoutHealthDashboardDto
        {
            SnapshotHealth = snapshotHealth,
            AnomalySummary = anomalySummary,
            TopUnusualPayouts = topUnusual,
            RecentSnapshots = recentSnapshots,
            LatestRepairRun = latestRepairRun,
            RecentRepairRuns = recentRepairRuns
        };
    }

    private async Task<PayoutSnapshotHealthDto> GetSnapshotHealthAsync(CancellationToken cancellationToken)
    {
        var totalCompleted = await _context.Orders
            .AsNoTracking()
            .CountAsync(o =>
                o.Status == OrderStatus.Completed || o.Status == OrderStatus.OrderCompleted,
                cancellationToken);

        var completedOrderIds = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.OrderCompleted)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        var withSnapshot = completedOrderIds.Count == 0
            ? 0
            : await _context.OrderPayoutSnapshots
                .AsNoTracking()
                .CountAsync(s => completedOrderIds.Contains(s.OrderId), cancellationToken);

        var provenanceCounts = await _context.OrderPayoutSnapshots
            .AsNoTracking()
            .GroupBy(s => s.Provenance ?? SnapshotProvenance.Unknown)
            .Select(g => new { Provenance = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int GetCount(string p) => provenanceCounts.FirstOrDefault(x => x.Provenance == p)?.Count ?? 0;

        return new PayoutSnapshotHealthDto
        {
            CompletedWithSnapshot = withSnapshot,
            CompletedMissingSnapshot = totalCompleted - withSnapshot,
            NormalFlowCount = GetCount(SnapshotProvenance.NormalFlow),
            RepairJobCount = GetCount(SnapshotProvenance.RepairJob),
            UnknownProvenanceCount = GetCount(SnapshotProvenance.Unknown),
            BackfillCount = GetCount(SnapshotProvenance.Backfill),
            ManualBackfillCount = GetCount(SnapshotProvenance.ManualBackfill)
        };
    }

    private async Task<RepairRunSummaryDto?> GetLatestRepairRunAsync(CancellationToken cancellationToken)
    {
        var run = await _context.PayoutSnapshotRepairRuns
            .AsNoTracking()
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return run == null ? null : MapToRepairRunSummary(run);
    }

    private async Task<IReadOnlyList<RepairRunSummaryDto>> GetRecentRepairRunsAsync(int take, CancellationToken cancellationToken)
    {
        var list = await _context.PayoutSnapshotRepairRuns
            .AsNoTracking()
            .OrderByDescending(r => r.StartedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
        return list.Select(MapToRepairRunSummary).ToList();
    }

    private static RepairRunSummaryDto MapToRepairRunSummary(PayoutSnapshotRepairRun r)
    {
        return new RepairRunSummaryDto
        {
            Id = r.Id,
            StartedAt = r.StartedAt,
            CompletedAt = r.CompletedAt,
            TotalProcessed = r.TotalProcessed,
            CreatedCount = r.CreatedCount,
            SkippedCount = r.SkippedCount,
            ErrorCount = r.ErrorCount,
            TriggerSource = r.TriggerSource ?? "",
            Notes = r.Notes
        };
    }

    private async Task<PayoutAnomalySummaryDto> GetAnomalySummaryAsync(CancellationToken cancellationToken)
    {
        var snapshots = await _context.OrderPayoutSnapshots
            .AsNoTracking()
            .Select(s => new
            {
                s.Id,
                s.OrderId,
                s.PayoutPath,
                s.FinalPayout,
                s.ResolutionTraceJson
            })
            .ToListAsync(cancellationToken);

        var legacyCount = snapshots.Count(s => s.PayoutPath == "Legacy");
        var customOverrideCount = snapshots.Count(s => s.PayoutPath == "CustomOverride");
        var zeroPayoutCount = snapshots.Count(s => s.FinalPayout == 0);

        var withWarningsCount = 0;
        foreach (var s in snapshots)
        {
            if (string.IsNullOrEmpty(s.ResolutionTraceJson)) continue;
            try
            {
                using var doc = JsonDocument.Parse(s.ResolutionTraceJson);
                if (doc.RootElement.TryGetProperty("warnings", out var arr) && arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0)
                    withWarningsCount++;
            }
            catch
            {
                // ignore parse errors
            }
        }

        var snapshotOrderIds = snapshots.Select(s => s.OrderId).Distinct().ToList();
        var negativeMarginCount = 0;
        if (snapshotOrderIds.Count > 0)
        {
            negativeMarginCount = await _context.PnlDetailPerOrders
                .AsNoTracking()
                .CountAsync(p => snapshotOrderIds.Contains(p.OrderId) && p.ProfitForOrder < 0, cancellationToken);
        }

        return new PayoutAnomalySummaryDto
        {
            LegacyFallbackCount = legacyCount,
            CustomOverrideCount = customOverrideCount,
            OrdersWithWarningsCount = withWarningsCount,
            ZeroPayoutCount = zeroPayoutCount,
            NegativeMarginCount = negativeMarginCount
        };
    }

    private async Task<IReadOnlyList<TopUnusualPayoutRowDto>> GetTopUnusualPayoutsAsync(CancellationToken cancellationToken)
    {
        var list = await _context.OrderPayoutSnapshots
            .AsNoTracking()
            .Select(s => new
            {
                s.OrderId,
                s.FinalPayout,
                s.Currency,
                s.PayoutPath,
                s.RateGroupId,
                s.CalculatedAt
            })
            .ToListAsync(cancellationToken);

        if (list.Count == 0) return Array.Empty<TopUnusualPayoutRowDto>();

        var groupKey = (Guid? rg, string? path) => (rg ?? Guid.Empty, path ?? "");
        var byGroup = list
            .GroupBy(s => groupKey(s.RateGroupId, s.PayoutPath))
            .ToDictionary(g => g.Key, g => g.ToList());

        var groupAvg = byGroup.ToDictionary(kv => kv.Key, kv => kv.Value.Average(x => (double)x.FinalPayout));

        var unusual = list
            .Where(s =>
            {
                var key = groupKey(s.RateGroupId, s.PayoutPath);
                var avg = groupAvg.GetValueOrDefault(key, 0);
                if (avg <= 0) return false;
                return (double)s.FinalPayout > 2 * avg;
            })
            .OrderByDescending(s => s.FinalPayout)
            .Take(20)
            .Select(s =>
            {
                var key = groupKey(s.RateGroupId, s.PayoutPath);
                var avg = groupAvg.GetValueOrDefault(key, 0);
                return new TopUnusualPayoutRowDto
                {
                    OrderId = s.OrderId,
                    FinalPayout = s.FinalPayout,
                    Currency = s.Currency ?? "MYR",
                    PayoutPath = s.PayoutPath,
                    RateGroupId = s.RateGroupId,
                    GroupAveragePayout = (decimal)avg,
                    MultipleOfAverage = avg > 0 ? (double)s.FinalPayout / avg : 0,
                    CalculatedAt = s.CalculatedAt
                };
            })
            .ToList();

        return unusual;
    }

    private async Task<IReadOnlyList<RecentSnapshotRowDto>> GetRecentSnapshotsAsync(CancellationToken cancellationToken)
    {
        var list = await _context.OrderPayoutSnapshots
            .AsNoTracking()
            .OrderByDescending(s => s.CalculatedAt)
            .Take(20)
            .Select(s => new RecentSnapshotRowDto
            {
                OrderId = s.OrderId,
                FinalPayout = s.FinalPayout,
                Currency = s.Currency ?? "MYR",
                PayoutPath = s.PayoutPath,
                CalculatedAt = s.CalculatedAt
            })
            .ToListAsync(cancellationToken);

        return list;
    }
}
