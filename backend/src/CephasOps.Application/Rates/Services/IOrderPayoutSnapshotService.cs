using CephasOps.Application.Rates.DTOs;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Creates and reads immutable payout snapshots for orders.
/// Snapshot is created when order reaches a completed state; once saved it is not updated.
/// </summary>
public interface IOrderPayoutSnapshotService
{
    /// <summary>
    /// Create a payout snapshot for the order if eligible (no existing snapshot, resolution succeeded).
    /// Called when order status becomes OrderCompleted or Completed. Idempotent: skips if snapshot already exists.
    /// </summary>
    /// <param name="orderId">Order to create snapshot for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="provenance">How this snapshot is being created (NormalFlow, RepairJob, etc.). Defaults to NormalFlow.</param>
    Task CreateSnapshotForOrderIfEligibleAsync(Guid orderId, CancellationToken cancellationToken = default, string? provenance = null);

    /// <summary>
    /// Get snapshot by order ID, or null if none exists.
    /// </summary>
    Task<OrderPayoutSnapshotDto?> GetSnapshotByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payout for order: snapshot if exists (as resolution result shape + source), else resolve live.
    /// </summary>
    Task<OrderPayoutSnapshotResponseDto> GetPayoutWithSnapshotOrLiveAsync(Guid orderId, Guid? companyId, DateTime? referenceDate, CancellationToken cancellationToken = default);
}
