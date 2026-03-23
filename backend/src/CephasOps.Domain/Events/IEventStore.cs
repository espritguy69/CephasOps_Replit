namespace CephasOps.Domain.Events;

/// <summary>
/// Append-only store for domain events. Persist before dispatch for audit and retry.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Append an event to the store. Append-only; no update or delete.
    /// When envelope is provided, platform envelope fields (PartitionKey, RootEventId, SourceService, etc.) are persisted (Phase 8).
    /// </summary>
    Task AppendAsync(IDomainEvent domainEvent, EventStoreEnvelopeMetadata? envelope = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add an event to the current DbContext without saving. Caller must call SaveChangesAsync in the same transaction as business data (outbox pattern).
    /// When envelope is provided, platform envelope fields are set (Phase 8).
    /// </summary>
    void AppendInCurrentTransaction(IDomainEvent domainEvent, EventStoreEnvelopeMetadata? envelope = null);

    /// <summary>
    /// Claim a batch of pending or due-retry events for processing. Uses FOR UPDATE SKIP LOCKED for concurrency safety. Returns entries already set to Processing.
    /// When nodeId and leaseExpiresAtUtc are provided, stamps ownership and lease expiry on claimed rows (Phase 7).
    /// </summary>
    Task<IReadOnlyList<EventStoreEntry>> ClaimNextPendingBatchAsync(int maxCount, int maxRetriesBeforeDeadLetter, CancellationToken cancellationToken = default, string? nodeId = null, DateTime? leaseExpiresAtUtc = null);

    /// <summary>
    /// Mark an event as processing started (Status = Processing). Optional; call before dispatching handlers.
    /// </summary>
    Task MarkAsProcessingAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark an event as processed (or failed) and update ProcessedAtUtc / RetryCount / Status / LastError / LastErrorAtUtc / LastHandler.
    /// When success, clears lease fields (ProcessingNodeId, ProcessingLeaseExpiresAtUtc, etc.). When failure, optional errorType and isNonRetryable:
    /// if isNonRetryable is true, status is set to DeadLetter immediately (poison). Returns a result for observability (metrics); null if event not found.
    /// </summary>
    Task<EventStoreMarkProcessedResult?> MarkProcessedAsync(Guid eventId, bool success, string? errorMessage = null, string? lastHandler = null, string? errorType = null, bool isNonRetryable = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single event by id for replay and async processing. Returns null if not found.
    /// </summary>
    Task<EventStoreEntry?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset events stuck in Processing to Failed with NextRetryAtUtc = now so they can be re-claimed.
    /// Considers lease expiry (ProcessingLeaseExpiresAtUtc &lt; now) and/or ProcessingStartedAtUtc older than timeout.
    /// Safe after crash/termination; preserves retry and dead-letter semantics.
    /// </summary>
    Task<int> ResetStuckProcessingAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset a DeadLetter event to Pending so the dispatcher can pick it up again. RetryCount is unchanged.
    /// Returns true if the event was DeadLetter and was reset; false if not found or not DeadLetter.
    /// </summary>
    Task<bool> ResetDeadLetterToPendingAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk reset DeadLetter events matching the filter to Pending. Returns count updated (Phase 7).
    /// </summary>
    Task<int> BulkResetDeadLetterToPendingAsync(EventStoreBulkFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk reset Failed events (with due retry) matching the filter to Pending. Returns count updated (Phase 7).
    /// </summary>
    Task<int> BulkResetFailedToPendingAsync(EventStoreBulkFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk reset stuck Processing events matching the filter to Failed with NextRetryAtUtc = now. Returns count updated (Phase 7).
    /// </summary>
    Task<int> BulkResetStuckAsync(EventStoreBulkFilter filter, TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk cancel Pending events matching the filter (set Status = Cancelled). Returns count updated (Phase 7).
    /// </summary>
    Task<int> BulkCancelPendingAsync(EventStoreBulkFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Count events matching the bulk filter and status (for dry-run).</summary>
    Task<int> CountByBulkFilterAsync(EventStoreBulkFilter filter, string status, CancellationToken cancellationToken = default);

    /// <summary>Count Failed events with NextRetryAtUtc &lt;= now matching the filter (for dry-run).</summary>
    Task<int> CountFailedDueForRetryByFilterAsync(EventStoreBulkFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Count stuck Processing events matching the filter (for dry-run).</summary>
    Task<int> CountStuckByFilterAsync(EventStoreBulkFilter filter, TimeSpan timeout, CancellationToken cancellationToken = default);
}

/// <summary>Filter for bulk operations (Phase 7). Bounded filters only.</summary>
public class EventStoreBulkFilter
{
    public Guid? CompanyId { get; set; }
    public string? EventType { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int? RetryCountMin { get; set; }
    public int? RetryCountMax { get; set; }
    /// <summary>Max events to affect per bulk call. Default 1000.</summary>
    public int MaxCount { get; set; } = 1000;
}
