using CephasOps.Application.Events.DTOs;

namespace CephasOps.Application.Events;

/// <summary>
/// Bounded observability queries for the Event Bus: recent handler processing, processing by event, event detail with processing.
/// Admin-grade only; uses existing EventStore and EventProcessingLog data.
/// </summary>
public interface IEventBusObservabilityService
{
    /// <summary>Recent handler processing rows with optional filters. Ordered by StartedAtUtc descending.</summary>
    Task<(IReadOnlyList<EventProcessingLogItemDto> Items, int TotalCount)> GetRecentProcessingLogAsync(
        EventProcessingLogFilterDto filter,
        Guid? scopeCompanyId,
        CancellationToken cancellationToken = default);

    /// <summary>All processing log rows for a given event. Returns empty list if event not found or out of scope.</summary>
    Task<IReadOnlyList<EventProcessingLogItemDto>> GetProcessingLogByEventIdAsync(
        Guid eventId,
        Guid? scopeCompanyId,
        CancellationToken cancellationToken = default);

    /// <summary>Event detail plus its handler processing rows. Returns null if event not found or out of scope.</summary>
    Task<EventDetailWithProcessingDto?> GetEventDetailWithProcessingAsync(
        Guid eventId,
        Guid? scopeCompanyId,
        CancellationToken cancellationToken = default);
}
