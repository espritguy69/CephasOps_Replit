namespace CephasOps.Application.Integration;

/// <summary>
/// Runs retention cleanup for event platform tables: deletes old Processed/Delivered/completed rows in batches.
/// Options are read from config; override can be passed for testing.
/// </summary>
public interface IEventPlatformRetentionService
{
    /// <summary>
    /// Run one retention pass. Deletes eligible rows in batches per table. Returns counts per table.
    /// Idempotent and safe to call repeatedly.
    /// </summary>
    Task<EventPlatformRetentionResult> RunRetentionAsync(CancellationToken cancellationToken = default);
}

/// <summary>Result of one retention run: deleted counts per category.</summary>
public sealed class EventPlatformRetentionResult
{
    public int EventStoreDeleted { get; set; }
    public int EventProcessingLogDeleted { get; set; }
    public int OutboundDeliveriesDeleted { get; set; }
    public int InboundReceiptsDeleted { get; set; }
    public int ExternalIdempotencyDeleted { get; set; }
    public DateTime RunStartedAtUtc { get; set; }
    public DateTime RunCompletedAtUtc { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = new List<string>();

    public int TotalDeleted =>
        EventStoreDeleted + EventProcessingLogDeleted + OutboundDeliveriesDeleted + InboundReceiptsDeleted + ExternalIdempotencyDeleted;
}
