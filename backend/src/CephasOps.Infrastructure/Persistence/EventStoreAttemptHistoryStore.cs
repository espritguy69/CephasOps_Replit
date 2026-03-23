using CephasOps.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Infrastructure.Persistence;

/// <summary>
/// Persists event dispatch attempt records to EventStoreAttemptHistory (Phase 7).
/// </summary>
public class EventStoreAttemptHistoryStore : IEventStoreAttemptHistoryStore
{
    private readonly ApplicationDbContext _context;

    public EventStoreAttemptHistoryStore(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task RecordAttemptAsync(EventStoreAttemptRecord record, CancellationToken cancellationToken = default)
    {
        var entity = new EventStoreAttemptHistory
        {
            EventId = record.EventId,
            EventType = record.EventType,
            CompanyId = record.CompanyId,
            HandlerName = record.HandlerName,
            AttemptNumber = record.AttemptNumber,
            Status = record.Status,
            StartedAtUtc = record.StartedAtUtc,
            FinishedAtUtc = record.FinishedAtUtc,
            DurationMs = record.DurationMs,
            ProcessingNodeId = record.ProcessingNodeId,
            ErrorType = record.ErrorType,
            ErrorMessage = record.ErrorMessage != null ? Truncate(record.ErrorMessage, 2000) : null,
            StackTraceSummary = record.StackTraceSummary != null ? Truncate(record.StackTraceSummary, 2000) : null,
            WasRetried = record.WasRetried,
            WasDeadLettered = record.WasDeadLettered
        };
        _context.Set<EventStoreAttemptHistory>().Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
