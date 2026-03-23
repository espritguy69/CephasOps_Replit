using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Events;

/// <summary>
/// Idempotency guard for event handlers using EventProcessingLog. Ensures at most one successful completion per (EventId, HandlerName).
/// Concurrency-safe: claim is done via insert-or-update; only one worker can have State=Processing at a time per (EventId, HandlerName).
/// </summary>
public class EventProcessingLogStore : IEventProcessingLogStore
{
    /// <summary>If a row is in Processing longer than this, allow reclaim (treat as stale/crashed).</summary>
    public static readonly TimeSpan StaleProcessingThreshold = TimeSpan.FromMinutes(15);

    private readonly ApplicationDbContext _context;
    private readonly ILogger<EventProcessingLogStore> _logger;

    public EventProcessingLogStore(ApplicationDbContext context, ILogger<EventProcessingLogStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> TryClaimAsync(
        Guid eventId,
        string handlerName,
        Guid? replayOperationId,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var staleCutoff = now - StaleProcessingThreshold;

        var existing = await _context.EventProcessingLog
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == eventId && e.HandlerName == handlerName, cancellationToken)
            .ConfigureAwait(false);

        if (existing != null)
        {
            if (existing.State == EventProcessingLog.States.Completed)
            {
                _logger.LogDebug(
                    "Event handler already completed, skipping. EventId={EventId}, HandlerName={HandlerName}",
                    eventId, handlerName);
                return false;
            }
            if (existing.State == EventProcessingLog.States.Processing && existing.StartedAtUtc > staleCutoff)
            {
                _logger.LogDebug(
                    "Event handler already in progress (not stale), skipping. EventId={EventId}, HandlerName={HandlerName}, StartedAt={StartedAt}",
                    eventId, handlerName, existing.StartedAtUtc);
                return false;
            }
            // Failed or stale Processing: update to claim
            var rows = await _context.EventProcessingLog
                .Where(e => e.EventId == eventId && e.HandlerName == handlerName
                    && (e.State == EventProcessingLog.States.Failed
                        || (e.State == EventProcessingLog.States.Processing && e.StartedAtUtc <= staleCutoff)))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.State, EventProcessingLog.States.Processing)
                    .SetProperty(e => e.AttemptCount, e => e.AttemptCount + 1)
                    .SetProperty(e => e.StartedAtUtc, now)
                    .SetProperty(e => e.ReplayOperationId, replayOperationId)
                    .SetProperty(e => e.CorrelationId, correlationId)
                    .SetProperty(e => e.CompletedAtUtc, (DateTime?)null)
                    .SetProperty(e => e.Error, (string?)null),
                    cancellationToken)
                .ConfigureAwait(false);
            if (rows == 0)
            {
                _logger.LogDebug(
                    "Could not claim event handler (concurrent claim or already completed). EventId={EventId}, HandlerName={HandlerName}",
                    eventId, handlerName);
                return false;
            }
            _logger.LogInformation(
                "Event handler claim (retry/stale). EventId={EventId}, HandlerName={HandlerName}, ReplayOperationId={ReplayOperationId}",
                eventId, handlerName, replayOperationId);
            return true;
        }

        try
        {
            _context.EventProcessingLog.Add(new EventProcessingLog
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                HandlerName = handlerName,
                ReplayOperationId = replayOperationId,
                CorrelationId = correlationId,
                State = EventProcessingLog.States.Processing,
                StartedAtUtc = now,
                AttemptCount = 1
            });
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(
                "Event handler claim (first attempt). EventId={EventId}, HandlerName={HandlerName}, ReplayOperationId={ReplayOperationId}",
                eventId, handlerName, replayOperationId);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _logger.LogDebug(ex,
                "Event handler claim failed (concurrent insert). EventId={EventId}, HandlerName={HandlerName}",
                eventId, handlerName);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task MarkCompletedAsync(Guid eventId, string handlerName, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var rows = await _context.EventProcessingLog
            .Where(e => e.EventId == eventId && e.HandlerName == handlerName && e.State == EventProcessingLog.States.Processing)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.State, EventProcessingLog.States.Completed)
                .SetProperty(e => e.CompletedAtUtc, now)
                .SetProperty(e => e.Error, (string?)null),
                cancellationToken)
            .ConfigureAwait(false);
        if (rows > 0)
            _logger.LogInformation("Event handler completed. EventId={EventId}, HandlerName={HandlerName}", eventId, handlerName);
    }

    /// <inheritdoc />
    public async Task MarkFailedAsync(Guid eventId, string handlerName, string? errorMessage, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var error = errorMessage != null ? Truncate(errorMessage, 2000) : null;
        var rows = await _context.EventProcessingLog
            .Where(e => e.EventId == eventId && e.HandlerName == handlerName && e.State == EventProcessingLog.States.Processing)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.State, EventProcessingLog.States.Failed)
                .SetProperty(e => e.CompletedAtUtc, now)
                .SetProperty(e => e.Error, error),
                cancellationToken)
            .ConfigureAwait(false);
        if (rows > 0)
            _logger.LogWarning("Event handler failed. EventId={EventId}, HandlerName={HandlerName}, Error={Error}", eventId, handlerName, error);
    }

    /// <inheritdoc />
    public async Task<bool> IsCompletedAsync(Guid eventId, string handlerName, CancellationToken cancellationToken = default)
    {
        return await _context.EventProcessingLog
            .AsNoTracking()
            .AnyAsync(e => e.EventId == eventId && e.HandlerName == handlerName && e.State == EventProcessingLog.States.Completed, cancellationToken)
            .ConfigureAwait(false);
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        while (inner != null)
        {
            var msg = inner.Message;
            if (msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) ||
                (inner.GetType().Name.Contains("Npgsql") && msg.Contains("23505")))
                return true;
            inner = inner.InnerException;
        }
        return false;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
