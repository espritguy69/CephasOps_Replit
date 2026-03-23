namespace CephasOps.Application.Rates;

/// <summary>
/// Result of a single run of missing payout snapshot detection and repair.
/// </summary>
public class MissingPayoutSnapshotRepairResult
{
    /// <summary>Number of snapshots created in this run.</summary>
    public int CreatedCount { get; init; }

    /// <summary>Number of completed orders that were ineligible (e.g. no payout resolution).</summary>
    public int SkippedCount { get; init; }

    /// <summary>Number of orders for which snapshot creation threw an error.</summary>
    public int ErrorCount { get; init; }

    /// <summary>Order IDs that failed with an error (for logging).</summary>
    public IReadOnlyList<Guid> ErrorOrderIds { get; init; } = Array.Empty<Guid>();

    /// <summary>Total completed orders without a snapshot that were considered in this run.</summary>
    public int TotalProcessed => CreatedCount + SkippedCount + ErrorCount;
}
