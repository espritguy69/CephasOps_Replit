using CephasOps.Application.Events.DTOs;

namespace CephasOps.Application.Events;

/// <summary>
/// Query and list events from the event store. Company-scoped when user is not global admin.
/// </summary>
public interface IEventStoreQueryService
{
    Task<(IReadOnlyList<EventStoreListItemDto> Items, int TotalCount)> GetEventsAsync(EventStoreFilterDto filter, Guid? scopeCompanyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get events matching replay request filters for preview or execution. Returns a batch of list items and total count (no payload).
    /// When resumeAfterEventId and resumeAfterOccurredAtUtc are set, only events strictly after that cursor (in OccurredAtUtc ASC, EventId ASC order) are returned.
    /// When safetyCutoffOccurredAtUtc is set, only events with OccurredAtUtc &lt;= that value are returned (replay safety window).
    /// </summary>
    Task<(IReadOnlyList<EventStoreListItemDto> Items, int TotalCount)> GetEventsForReplayAsync(
        ReplayRequestDto request,
        Guid? scopeCompanyId,
        int? maxEvents,
        Guid? resumeAfterEventId = null,
        DateTime? resumeAfterOccurredAtUtc = null,
        DateTime? safetyCutoffOccurredAtUtc = null,
        CancellationToken cancellationToken = default);

    Task<EventStoreDetailDto?> GetByEventIdAsync(Guid eventId, Guid? scopeCompanyId, CancellationToken cancellationToken = default);
    Task<EventStoreDashboardDto> GetDashboardAsync(DateTime? fromUtc, DateTime? toUtc, Guid? scopeCompanyId, CancellationToken cancellationToken = default);
    Task<EventStoreRelatedLinksDto?> GetRelatedLinksAsync(Guid eventId, Guid? scopeCompanyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current counts (pending, failed, dead-letter) and oldest pending event time for metrics and health.
    /// When scopeCompanyId is null, returns global counts; otherwise scoped to that company.
    /// </summary>
    Task<EventStoreCountsSnapshot> GetEventStoreCountsAsync(Guid? scopeCompanyId, CancellationToken cancellationToken = default);

    /// <summary>Get execution attempt history for an event (Phase 7). Returns empty list if event not found or not in scope.</summary>
    Task<IReadOnlyList<EventStoreAttemptHistoryItemDto>> GetAttemptHistoryByEventIdAsync(Guid eventId, Guid? scopeCompanyId, CancellationToken cancellationToken = default);
}
