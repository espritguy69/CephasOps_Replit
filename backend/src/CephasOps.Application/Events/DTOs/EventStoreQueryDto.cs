namespace CephasOps.Application.Events.DTOs;

/// <summary>Filter for querying EventStore.</summary>
public class EventStoreFilterDto
{
    public Guid? CompanyId { get; set; }
    public string? EventType { get; set; }
    public string? Status { get; set; }
    public string? CorrelationId { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    /// <summary>Minimum retry count (inclusive).</summary>
    public int? RetryCountMin { get; set; }
    /// <summary>Maximum retry count (inclusive).</summary>
    public int? RetryCountMax { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>Single event row for list responses.</summary>
public class EventStoreListItemDto
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public int RetryCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public Guid? CompanyId { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? LastError { get; set; }
    public string? LastHandler { get; set; }
    public Guid? ParentEventId { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }
    // Phase 7
    public string? ProcessingNodeId { get; set; }
    public DateTime? ProcessingLeaseExpiresAtUtc { get; set; }
    public DateTime? LastClaimedAtUtc { get; set; }
    public string? LastClaimedBy { get; set; }
    public string? LastErrorType { get; set; }
    // Phase 8
    public Guid? RootEventId { get; set; }
    public string? PartitionKey { get; set; }
    public string? ReplayId { get; set; }
    public string? SourceService { get; set; }
    public string? SourceModule { get; set; }
    public DateTime? CapturedAtUtc { get; set; }
}

/// <summary>Current event store counts and oldest pending age for metrics and health.</summary>
public class EventStoreCountsSnapshot
{
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
    public int DeadLetterCount { get; set; }
    public DateTime? OldestPendingCreatedAtUtc { get; set; }
    public double OldestPendingEventAgeSeconds => OldestPendingCreatedAtUtc.HasValue
        ? (DateTime.UtcNow - OldestPendingCreatedAtUtc.Value).TotalSeconds
        : 0;
    /// <summary>Events currently in Processing (Phase 7).</summary>
    public int ProcessingCount { get; set; }
    /// <summary>Processing events with expired lease (Phase 7).</summary>
    public int ExpiredLeasesCount { get; set; }
}

/// <summary>Full event detail including payload.</summary>
public class EventStoreDetailDto : EventStoreListItemDto
{
    public string Payload { get; set; } = "{}";
    public Guid? TriggeredByUserId { get; set; }
    public string? Source { get; set; }
    public DateTime? LastErrorAtUtc { get; set; }
}

/// <summary>Single attempt record for execution history (Phase 7).</summary>
public class EventStoreAttemptHistoryItemDto
{
    public long Id { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string HandlerName { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public int? DurationMs { get; set; }
    public string? ProcessingNodeId { get; set; }
    public string? ErrorType { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StackTraceSummary { get; set; }
    public bool WasRetried { get; set; }
    public bool WasDeadLettered { get; set; }
}

/// <summary>Dashboard metrics for Event Bus.</summary>
public class EventStoreDashboardDto
{
    public int EventsToday { get; set; }
    public int ProcessedCount { get; set; }
    public int FailedCount { get; set; }
    public int DeadLetterCount { get; set; }
    public double ProcessedPercent { get; set; }
    public double FailedPercent { get; set; }
    public int TotalRetryCount { get; set; }
    public List<EventTypeCountDto> TopFailingEventTypes { get; set; } = new();
    public List<CompanyEventCountDto> TopFailingCompanies { get; set; } = new();
}

public class EventTypeCountDto
{
    public string EventType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class CompanyEventCountDto
{
    public Guid? CompanyId { get; set; }
    public int FailedCount { get; set; }
    public int DeadLetterCount { get; set; }
}

/// <summary>Related JobRuns (same EventId or CorrelationId) for traceability.</summary>
public class EventStoreRelatedJobRunDto
{
    public Guid Id { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public Guid? EventId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>Related WorkflowJobs (same CorrelationId) for traceability.</summary>
public class EventStoreRelatedWorkflowJobDto
{
    public Guid Id { get; set; }
    public string? CorrelationId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? CurrentStatus { get; set; }
    public string? TargetStatus { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Links for event detail: JobRuns and WorkflowJobs sharing EventId or CorrelationId.</summary>
public class EventStoreRelatedLinksDto
{
    public List<EventStoreRelatedJobRunDto> JobRuns { get; set; } = new();
    public List<EventStoreRelatedWorkflowJobDto> WorkflowJobs { get; set; } = new();
}

/// <summary>Filter for event bus observability: handler processing list.</summary>
public class EventProcessingLogFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    /// <summary>When true, only rows with State = Failed.</summary>
    public bool FailedOnly { get; set; }
    public Guid? EventId { get; set; }
    public Guid? ReplayOperationId { get; set; }
    public string? CorrelationId { get; set; }
}

/// <summary>Single handler processing row for observability list.</summary>
public class EventProcessingLogItemDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string HandlerName { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? Error { get; set; }
    public Guid? ReplayOperationId { get; set; }
    public string? CorrelationId { get; set; }
}

/// <summary>Event detail plus related handler processing rows for observability.</summary>
public class EventDetailWithProcessingDto
{
    public EventStoreDetailDto Event { get; set; } = null!;
    public List<EventProcessingLogItemDto> ProcessingLogs { get; set; } = new();
}
