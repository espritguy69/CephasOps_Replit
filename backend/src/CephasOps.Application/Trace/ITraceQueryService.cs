using CephasOps.Application.Trace.DTOs;

namespace CephasOps.Application.Trace;

/// <summary>
/// Options for timeline query (e.g. pagination).
/// </summary>
public class TraceQueryOptions
{
    /// <summary>Max items to return (default no limit). When set, TotalCount is set to full count before limit.</summary>
    public int? Limit { get; set; }
}

/// <summary>
/// Assembles an operational trace timeline from WorkflowJob, EventStore, and JobRun.
/// Lookup by CorrelationId, EventId, JobRunId, WorkflowJobId, or EntityType+EntityId. Company-scoped.
/// </summary>
public interface ITraceQueryService
{
    /// <summary>Get timeline for all records sharing this correlation ID.</summary>
    Task<TraceTimelineDto> GetByCorrelationIdAsync(string correlationId, Guid? scopeCompanyId, TraceQueryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Get timeline for this event and all related records (same correlation).</summary>
    Task<TraceTimelineDto?> GetByEventIdAsync(Guid eventId, Guid? scopeCompanyId, TraceQueryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Get timeline for this job run and all related records (same correlation/event).</summary>
    Task<TraceTimelineDto?> GetByJobRunIdAsync(Guid jobRunId, Guid? scopeCompanyId, TraceQueryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Get timeline for this workflow job and all related records (same correlation).</summary>
    Task<TraceTimelineDto?> GetByWorkflowJobIdAsync(Guid workflowJobId, Guid? scopeCompanyId, TraceQueryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Get timeline for all records related to this entity (EntityType + EntityId).</summary>
    Task<TraceTimelineDto> GetByEntityAsync(string entityType, Guid entityId, Guid? scopeCompanyId, TraceQueryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Get minimal trace metrics for a time window (failure counts, chains with failures). Company-scoped.</summary>
    Task<TraceMetricsDto> GetMetricsAsync(DateTime fromUtc, DateTime toUtc, Guid? scopeCompanyId, CancellationToken cancellationToken = default);
}
