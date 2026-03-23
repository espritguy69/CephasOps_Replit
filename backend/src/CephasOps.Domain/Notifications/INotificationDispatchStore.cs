using CephasOps.Domain.Notifications.Entities;

namespace CephasOps.Domain.Notifications;

/// <summary>
/// Persistence for notification delivery work (Phase 2 Notifications boundary).
/// Implementation in Infrastructure.
/// </summary>
public interface INotificationDispatchStore
{
    Task<bool> ExistsByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationDispatch dispatch, CancellationToken cancellationToken = default);
    /// <summary>Claim up to maxCount pending (or due retry) dispatches; sets Status=Processing and lease. Returns claimed rows.</summary>
    Task<IReadOnlyList<NotificationDispatch>> ClaimNextPendingBatchAsync(int maxCount, string? nodeId, DateTime? leaseExpiresAtUtc, CancellationToken cancellationToken = default);
    /// <summary>Mark dispatch as Sent or Failed/DeadLetter; clear lease.</summary>
    Task MarkProcessedAsync(Guid id, bool success, string? errorMessage, bool isNonRetryable, CancellationToken cancellationToken = default);
}
