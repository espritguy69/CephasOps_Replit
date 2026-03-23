namespace CephasOps.Application.Rates.DTOs;

/// <summary>
/// Read-only payload for the Payout Health Dashboard. No payout logic; reporting only.
/// </summary>
public class PayoutHealthDashboardDto
{
    public PayoutSnapshotHealthDto SnapshotHealth { get; init; } = new();
    public PayoutAnomalySummaryDto AnomalySummary { get; init; } = new();
    public IReadOnlyList<TopUnusualPayoutRowDto> TopUnusualPayouts { get; init; } = Array.Empty<TopUnusualPayoutRowDto>();
    public IReadOnlyList<RecentSnapshotRowDto> RecentSnapshots { get; init; } = Array.Empty<RecentSnapshotRowDto>();
    /// <summary>Most recent repair run (if any).</summary>
    public RepairRunSummaryDto? LatestRepairRun { get; init; }
    /// <summary>Recent repair runs for the table (e.g. last 10).</summary>
    public IReadOnlyList<RepairRunSummaryDto> RecentRepairRuns { get; init; } = Array.Empty<RepairRunSummaryDto>();
}

/// <summary>
/// Snapshot coverage: completed orders with vs without snapshot, and provenance breakdown.
/// </summary>
public class PayoutSnapshotHealthDto
{
    /// <summary>Number of orders with status Completed or OrderCompleted that have a snapshot.</summary>
    public int CompletedWithSnapshot { get; init; }

    /// <summary>Number of orders with status Completed or OrderCompleted that have no snapshot.</summary>
    public int CompletedMissingSnapshot { get; init; }

    /// <summary>Total completed orders (with + missing snapshot).</summary>
    public int TotalCompleted => CompletedWithSnapshot + CompletedMissingSnapshot;

    /// <summary>Coverage percentage (0–100). 100 when TotalCompleted is 0.</summary>
    public decimal CoveragePercent => TotalCompleted == 0 ? 100 : (CompletedWithSnapshot * 100m) / TotalCompleted;

    /// <summary>Snapshots created during normal completion flow (OrderService.ChangeOrderStatusAsync).</summary>
    public int NormalFlowCount { get; init; }

    /// <summary>Snapshots created by the repair job (scheduler or manual).</summary>
    public int RepairJobCount { get; init; }

    /// <summary>Snapshots with unknown provenance (pre-provenance or backfill).</summary>
    public int UnknownProvenanceCount { get; init; }

    /// <summary>Snapshots from Backfill batch.</summary>
    public int BackfillCount { get; init; }

    /// <summary>Snapshots from ManualBackfill.</summary>
    public int ManualBackfillCount { get; init; }
}

/// <summary>
/// One repair run record for dashboard (latest or recent list).
/// </summary>
public class RepairRunSummaryDto
{
    public Guid Id { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int TotalProcessed { get; init; }
    public int CreatedCount { get; init; }
    public int SkippedCount { get; init; }
    public int ErrorCount { get; init; }
    public string TriggerSource { get; init; } = "";
    public string? Notes { get; init; }
}

/// <summary>
/// Counts for payout anomaly visibility (legacy, custom override, warnings, zero payout, negative margin).
/// </summary>
public class PayoutAnomalySummaryDto
{
    /// <summary>Snapshots using legacy fallback (PayoutPath = Legacy).</summary>
    public int LegacyFallbackCount { get; init; }

    /// <summary>Snapshots using custom override (PayoutPath = CustomOverride).</summary>
    public int CustomOverrideCount { get; init; }

    /// <summary>Snapshots that have at least one resolution warning in ResolutionTraceJson.</summary>
    public int OrdersWithWarningsCount { get; init; }

    /// <summary>Completed orders with a snapshot where FinalPayout is zero.</summary>
    public int ZeroPayoutCount { get; init; }

    /// <summary>Orders with snapshot and P&amp;L detail where profit is negative (when PnlDetailPerOrder data exists).</summary>
    public int NegativeMarginCount { get; init; }
}

/// <summary>
/// One row for "top unusual payouts": payout above average for the same rate group/path.
/// </summary>
public class TopUnusualPayoutRowDto
{
    public Guid OrderId { get; init; }
    public decimal FinalPayout { get; init; }
    public string Currency { get; init; } = "MYR";
    public string? PayoutPath { get; init; }
    public Guid? RateGroupId { get; init; }
    public decimal GroupAveragePayout { get; init; }
    public double MultipleOfAverage { get; init; }
    public DateTime CalculatedAt { get; init; }
}

/// <summary>
/// One row for recent snapshots list.
/// </summary>
public class RecentSnapshotRowDto
{
    public Guid OrderId { get; init; }
    public decimal FinalPayout { get; init; }
    public string Currency { get; init; } = "MYR";
    public string? PayoutPath { get; init; }
    public DateTime CalculatedAt { get; init; }
}
