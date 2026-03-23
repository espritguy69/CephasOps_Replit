using CephasOps.Application.Events.DTOs;
using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Events;

/// <summary>
/// Bounded observability queries for the Event Bus. Uses EventStore and EventProcessingLog only.
/// </summary>
public sealed class EventBusObservabilityService : IEventBusObservabilityService
{
    private const int MaxPageSize = 100;

    private readonly ApplicationDbContext _context;
    private readonly IEventStoreQueryService _eventStoreQuery;

    public EventBusObservabilityService(ApplicationDbContext context, IEventStoreQueryService eventStoreQuery)
    {
        _context = context;
        _eventStoreQuery = eventStoreQuery;
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<EventProcessingLogItemDto> Items, int TotalCount)> GetRecentProcessingLogAsync(
        EventProcessingLogFilterDto filter,
        Guid? scopeCompanyId,
        CancellationToken cancellationToken = default)
    {
        var q = _context.EventProcessingLog.AsNoTracking().AsQueryable();

        if (scopeCompanyId.HasValue)
        {
            var allowedEventIds = _context.EventStore.AsNoTracking()
                .Where(e => e.CompanyId == scopeCompanyId.Value)
                .Select(e => e.EventId);
            q = q.Where(p => allowedEventIds.Contains(p.EventId));
        }

        if (filter.FailedOnly)
            q = q.Where(p => p.State == EventProcessingLog.States.Failed);
        if (filter.EventId.HasValue)
            q = q.Where(p => p.EventId == filter.EventId.Value);
        if (filter.ReplayOperationId.HasValue)
            q = q.Where(p => p.ReplayOperationId == filter.ReplayOperationId.Value);
        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
            q = q.Where(p => p.CorrelationId != null && p.CorrelationId.Contains(filter.CorrelationId!));

        var total = await q.CountAsync(cancellationToken).ConfigureAwait(false);
        var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);
        var skip = (Math.Max(1, filter.Page) - 1) * pageSize;

        var items = await q
            .OrderByDescending(p => p.StartedAtUtc)
            .Skip(skip)
            .Take(pageSize)
            .Select(p => new EventProcessingLogItemDto
            {
                Id = p.Id,
                EventId = p.EventId,
                HandlerName = p.HandlerName,
                State = p.State,
                StartedAtUtc = p.StartedAtUtc,
                CompletedAtUtc = p.CompletedAtUtc,
                AttemptCount = p.AttemptCount,
                Error = p.Error,
                ReplayOperationId = p.ReplayOperationId,
                CorrelationId = p.CorrelationId
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items, total);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventProcessingLogItemDto>> GetProcessingLogByEventIdAsync(
        Guid eventId,
        Guid? scopeCompanyId,
        CancellationToken cancellationToken = default)
    {
        var eventEntry = await _eventStoreQuery.GetByEventIdAsync(eventId, scopeCompanyId, cancellationToken).ConfigureAwait(false);
        if (eventEntry == null)
            return Array.Empty<EventProcessingLogItemDto>();

        var list = await _context.EventProcessingLog.AsNoTracking()
            .Where(p => p.EventId == eventId)
            .OrderBy(p => p.StartedAtUtc)
            .Select(p => new EventProcessingLogItemDto
            {
                Id = p.Id,
                EventId = p.EventId,
                HandlerName = p.HandlerName,
                State = p.State,
                StartedAtUtc = p.StartedAtUtc,
                CompletedAtUtc = p.CompletedAtUtc,
                AttemptCount = p.AttemptCount,
                Error = p.Error,
                ReplayOperationId = p.ReplayOperationId,
                CorrelationId = p.CorrelationId
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return list;
    }

    /// <inheritdoc />
    public async Task<EventDetailWithProcessingDto?> GetEventDetailWithProcessingAsync(
        Guid eventId,
        Guid? scopeCompanyId,
        CancellationToken cancellationToken = default)
    {
        var eventDetail = await _eventStoreQuery.GetByEventIdAsync(eventId, scopeCompanyId, cancellationToken).ConfigureAwait(false);
        if (eventDetail == null)
            return null;

        var processingLogs = await GetProcessingLogByEventIdAsync(eventId, scopeCompanyId, cancellationToken).ConfigureAwait(false);
        return new EventDetailWithProcessingDto
        {
            Event = eventDetail,
            ProcessingLogs = processingLogs.ToList()
        };
    }
}
