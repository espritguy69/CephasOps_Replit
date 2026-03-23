namespace CephasOps.Domain.Events;

/// <summary>
/// Append-only persistence record for a domain event. Used for audit, replay, and async processing.
/// Only processing metadata (Status, ProcessedAtUtc, RetryCount, LastError, LastErrorAtUtc, LastHandler) is updated after insert.
/// Phase 8: RootEventId, PartitionKey, ReplayId, SourceService, SourceModule, CapturedAtUtc, IdempotencyKey, TraceId, SpanId, Priority.
/// </summary>
public class EventStoreEntry
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    /// <summary>JSON payload; must not contain secrets. Stored as jsonb.</summary>
    public string Payload { get; set; } = "{}";
    public DateTime OccurredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public int RetryCount { get; set; }
    /// <summary>Pending | Processing | Processed | Failed | DeadLetter</summary>
    public string Status { get; set; } = "Pending";
    public string? CorrelationId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? TriggeredByUserId { get; set; }
    public string? Source { get; set; }
    /// <summary>Optional entity context (e.g. Order, Assurance) for indexing.</summary>
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    /// <summary>Last error message from handler failure (sanitized).</summary>
    public string? LastError { get; set; }
    public DateTime? LastErrorAtUtc { get; set; }
    /// <summary>Name of the handler that last failed (or last ran).</summary>
    public string? LastHandler { get; set; }
    /// <summary>Parent event when this event was spawned from another (e.g. child flow).</summary>
    public Guid? ParentEventId { get; set; }

    /// <summary>Event or command that caused this event (causation chain).</summary>
    public Guid? CausationId { get; set; }

    /// <summary>When to retry processing after failure (for Failed status). Null for Pending.</summary>
    public DateTime? NextRetryAtUtc { get; set; }

    /// <summary>When the event was claimed (Status set to Processing). Used for stuck-processing recovery.</summary>
    public DateTime? ProcessingStartedAtUtc { get; set; }

    /// <summary>Optional payload/contract version for version-safe handling.</summary>
    public string? PayloadVersion { get; set; }

    // --- Phase 7: Dispatcher ownership and lease ---
    /// <summary>Id of the node that last claimed this event (distributed dispatcher).</summary>
    public string? ProcessingNodeId { get; set; }
    /// <summary>When the processing lease expires; after this the event can be recovered by another node.</summary>
    public DateTime? ProcessingLeaseExpiresAtUtc { get; set; }
    /// <summary>When the event was last claimed (stamped at claim time).</summary>
    public DateTime? LastClaimedAtUtc { get; set; }
    /// <summary>Node id that last claimed (same as ProcessingNodeId when in Processing; retained for audit).</summary>
    public string? LastClaimedBy { get; set; }
    /// <summary>Classified error type for last failure (e.g. Validation, Deserialization, Transient).</summary>
    public string? LastErrorType { get; set; }

    // --- Phase 8: Platform event envelope ---
    /// <summary>Origin event of the full causality chain (for correlation trees).</summary>
    public Guid? RootEventId { get; set; }
    /// <summary>Partition key for ordering and concurrency (e.g. CompanyId, EntityId, CorrelationId).</summary>
    public string? PartitionKey { get; set; }
    /// <summary>When this event is a replay, id of the replay run (audit).</summary>
    public string? ReplayId { get; set; }
    /// <summary>Service that produced the event (e.g. CephasOps.Api).</summary>
    public string? SourceService { get; set; }
    /// <summary>Bounded context or module (e.g. Workflow, Orders).</summary>
    public string? SourceModule { get; set; }
    /// <summary>When the platform captured the event (e.g. at publish time).</summary>
    public DateTime? CapturedAtUtc { get; set; }
    /// <summary>Idempotency key for deduplication when present.</summary>
    public string? IdempotencyKey { get; set; }
    /// <summary>Distributed trace id.</summary>
    public string? TraceId { get; set; }
    /// <summary>Span id within the trace.</summary>
    public string? SpanId { get; set; }
    /// <summary>Priority or QoS (e.g. Normal, High). Used for backpressure and ordering hints.</summary>
    public string? Priority { get; set; }
}
