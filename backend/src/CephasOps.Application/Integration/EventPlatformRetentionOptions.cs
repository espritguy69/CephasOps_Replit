namespace CephasOps.Application.Integration;

/// <summary>
/// Configurable retention windows (in days) for event platform cleanup. Section: "EventPlatformRetention".
/// Only rows older than the configured days are eligible for deletion. Set to 0 to disable cleanup for that category.
/// </summary>
public sealed class EventPlatformRetentionOptions
{
    public const string SectionName = "EventPlatformRetention";

    /// <summary>Retention days for EventStore rows in Processed or DeadLetter status. Default 90. 0 = skip.</summary>
    public int EventStoreProcessedAndDeadLetterDays { get; set; } = 90;

    /// <summary>Retention days for EventProcessingLog rows in Completed state. Default 90. 0 = skip.</summary>
    public int EventProcessingLogCompletedDays { get; set; } = 90;

    /// <summary>Retention days for OutboundIntegrationDeliveries in Delivered status. Default 60. 0 = skip.</summary>
    public int OutboundDeliveredDays { get; set; } = 60;

    /// <summary>Retention days for InboundWebhookReceipts in Processed status. Default 90. 0 = skip.</summary>
    public int InboundProcessedDays { get; set; } = 90;

    /// <summary>Retention days for ExternalIdempotencyRecords that are completed. Default 7 (senders rarely retry after 7 days). 0 = skip.</summary>
    public int ExternalIdempotencyCompletedDays { get; set; } = 7;

    /// <summary>Max rows to delete per table per run (batch size). Default 1000. Prevents long locks.</summary>
    public int MaxDeletesPerTablePerRun { get; set; } = 1000;

    /// <summary>When true, retention worker runs. Default true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Interval in seconds between retention runs. Default 86400 (24 hours).</summary>
    public int RunIntervalSeconds { get; set; } = 86400;
}
