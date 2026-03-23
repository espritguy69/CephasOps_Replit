namespace CephasOps.Application.Trace.DTOs;

/// <summary>
/// Single item on the operational trace timeline. Derived from WorkflowJob, EventStore, or JobRun.
/// </summary>
public class TraceTimelineItemDto
{
    /// <summary>When the activity occurred (UTC).</summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>Correlation ID linking workflow, events, and job runs.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Company scope.</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>Kind of activity: WorkflowTransitionRequested, WorkflowTransitionStarted, WorkflowTransitionCompleted, EventEmitted, EventProcessed, EventHandlerStarted, EventHandlerCompleted, BackgroundJobStarted, BackgroundJobCompleted.</summary>
    public string ItemType { get; set; } = string.Empty;

    /// <summary>Status (e.g. Pending, Running, Succeeded, Failed, Processed).</summary>
    public string? Status { get; set; }

    /// <summary>Source (e.g. WorkflowEngine, EventBus, Scheduler).</summary>
    public string? Source { get; set; }

    /// <summary>Entity type (e.g. Order, Invoice).</summary>
    public string? EntityType { get; set; }

    /// <summary>Entity ID.</summary>
    public Guid? EntityId { get; set; }

    /// <summary>Short title for the timeline.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional summary or error message.</summary>
    public string? Summary { get; set; }

    /// <summary>Same as Summary; alias for API/runbook (detail or error message).</summary>
    public string? DetailSummary { get; set; }

    /// <summary>Primary ID of the related record (EventId, JobRunId, or WorkflowJobId).</summary>
    public Guid? RelatedId { get; set; }

    /// <summary>Kind of RelatedId: Event, JobRun, WorkflowJob.</summary>
    public string? RelatedIdKind { get; set; }

    /// <summary>Parent related ID (e.g. ParentJobRunId, ParentEventId).</summary>
    public Guid? ParentRelatedId { get; set; }

    /// <summary>Actor/user ID when available (InitiatedByUserId, TriggeredByUserId).</summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>Handler name when applicable (e.g. event handler that ran or failed).</summary>
    public string? HandlerName { get; set; }
}

/// <summary>
/// Full trace timeline for a given lookup (by correlation, event, job run, workflow job, or entity).
/// </summary>
public class TraceTimelineDto
{
    /// <summary>Identifier that was used for the lookup (e.g. correlation ID, event ID).</summary>
    public string LookupKind { get; set; } = string.Empty;

    /// <summary>Value used (e.g. correlation ID string, or GUID).</summary>
    public string? LookupValue { get; set; }

    /// <summary>Timeline items in chronological order (oldest first).</summary>
    public List<TraceTimelineItemDto> Items { get; set; } = new();

    /// <summary>Total count of items (when pagination is applied).</summary>
    public int? TotalCount { get; set; }

    /// <summary>Current page (1-based) when pagination is used.</summary>
    public int? Page { get; set; }

    /// <summary>Page size when pagination is used.</summary>
    public int? PageSize { get; set; }
}
