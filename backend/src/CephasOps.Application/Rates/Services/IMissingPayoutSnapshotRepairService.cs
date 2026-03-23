using CephasOps.Application.Rates;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Detects completed orders that have no payout snapshot and creates them via IOrderPayoutSnapshotService.
/// Safe to run multiple times (idempotent): only orders with status Completed/OrderCompleted and no snapshot are processed.
/// </summary>
public interface IMissingPayoutSnapshotRepairService
{
    /// <summary>
    /// Find orders with status Completed or OrderCompleted that have no OrderPayoutSnapshot,
    /// then call CreateSnapshotForOrderIfEligibleAsync for each. Logs and returns counts.
    /// </summary>
    Task<MissingPayoutSnapshotRepairResult> DetectMissingPayoutSnapshotsAsync(CancellationToken cancellationToken = default);
}
