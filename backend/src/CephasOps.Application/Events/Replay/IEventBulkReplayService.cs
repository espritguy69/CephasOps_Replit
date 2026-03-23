using CephasOps.Application.Events.DTOs;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Bulk operator actions for event store: replay dead-letter, replay failed, reset stuck, cancel pending (Phase 7).
/// All actions support filters and dry-run. Requires JobsAdmin or stricter.
/// </summary>
public interface IEventBulkReplayService
{
    /// <summary>Replay (requeue to Pending) dead-letter events matching the filter. Returns count affected.</summary>
    Task<BulkActionResult> ReplayDeadLetterByFilterAsync(EventStoreFilterDto filter, Guid? scopeCompanyId, Guid? initiatedByUserId, bool dryRun, CancellationToken cancellationToken = default);

    /// <summary>Requeue failed events (where retry is due) to Pending by filter. Returns count affected.</summary>
    Task<BulkActionResult> ReplayFailedByFilterAsync(EventStoreFilterDto filter, Guid? scopeCompanyId, Guid? initiatedByUserId, bool dryRun, CancellationToken cancellationToken = default);

    /// <summary>Reset stuck Processing events matching the filter to Failed with NextRetryAtUtc = now. Returns count affected.</summary>
    Task<BulkActionResult> ResetStuckByFilterAsync(EventStoreFilterDto filter, Guid? scopeCompanyId, bool dryRun, CancellationToken cancellationToken = default);

    /// <summary>Cancel pending events by filter (set status to Cancelled or remove from queue for incident control). Returns count affected.</summary>
    Task<BulkActionResult> CancelPendingByFilterAsync(EventStoreFilterDto filter, Guid? scopeCompanyId, Guid? initiatedByUserId, bool dryRun, CancellationToken cancellationToken = default);
}

public class BulkActionResult
{
    public bool Success { get; set; }
    public int CountAffected { get; set; }
    public string? ErrorMessage { get; set; }
    public bool DryRun { get; set; }
}
