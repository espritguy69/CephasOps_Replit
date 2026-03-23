using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Events.Ledger;

/// <summary>
/// Append-only ledger writer. Idempotent; replay-safe; no side effects.
/// Payload snapshots are validated (JSON and size) before write; invalid or oversized payloads are rejected or replaced with a placeholder.
/// Unique constraint violations on (SourceEventId, LedgerFamily) or (ReplayOperationId, LedgerFamily) are treated as success (no-op) to handle TOCTOU races.
/// </summary>
public sealed class LedgerWriter : ILedgerWriter
{
    private readonly ApplicationDbContext _context;
    private readonly ILedgerPayloadValidator _validator;
    private readonly ILogger<LedgerWriter> _logger;

    public LedgerWriter(
        ApplicationDbContext context,
        ILedgerPayloadValidator validator,
        ILogger<LedgerWriter> logger)
    {
        _context = context;
        _validator = validator;
        _logger = logger;
    }

    public async Task AppendFromEventAsync(
        Guid sourceEventId,
        string ledgerFamily,
        string eventType,
        DateTime occurredAtUtc,
        Guid? companyId,
        string? entityType,
        Guid? entityId,
        string? payloadSnapshot,
        string? correlationId,
        Guid? triggeredByUserId,
        string? orderingStrategyId,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var exists = await _context.LedgerEntries
            .AnyAsync(e => e.SourceEventId == sourceEventId && e.LedgerFamily == ledgerFamily, cancellationToken)
            .ConfigureAwait(false);
        if (exists)
            return;

        var result = _validator.Validate(payloadSnapshot, ledgerFamily, eventType);
        LogValidationResult(result, ledgerFamily, eventType, sourceEventId.ToString(), isReplay: false);
        var payloadToStore = result.PayloadToStore;

        _context.LedgerEntries.Add(new LedgerEntry
        {
            SourceEventId = sourceEventId,
            LedgerFamily = ledgerFamily,
            Category = category,
            CompanyId = companyId,
            EntityType = entityType,
            EntityId = entityId,
            EventType = eventType,
            OccurredAtUtc = occurredAtUtc,
            RecordedAtUtc = DateTime.UtcNow,
            PayloadSnapshot = payloadToStore,
            CorrelationId = correlationId,
            TriggeredByUserId = triggeredByUserId,
            OrderingStrategyId = orderingStrategyId
        });
        await SaveChangesAndHandleConflictAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AppendFromReplayOperationAsync(
        Guid replayOperationId,
        string ledgerFamily,
        string eventType,
        DateTime occurredAtUtc,
        Guid? companyId,
        string? payloadSnapshot,
        string? orderingStrategyId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _context.LedgerEntries
            .AnyAsync(e => e.ReplayOperationId == replayOperationId && e.LedgerFamily == ledgerFamily, cancellationToken)
            .ConfigureAwait(false);
        if (exists)
            return;

        var result = _validator.Validate(payloadSnapshot, ledgerFamily, eventType);
        LogValidationResult(result, ledgerFamily, eventType, replayOperationId.ToString(), isReplay: true);
        var payloadToStore = result.PayloadToStore;

        _context.LedgerEntries.Add(new LedgerEntry
        {
            ReplayOperationId = replayOperationId,
            LedgerFamily = ledgerFamily,
            CompanyId = companyId,
            EventType = eventType,
            OccurredAtUtc = occurredAtUtc,
            RecordedAtUtc = DateTime.UtcNow,
            PayloadSnapshot = payloadToStore,
            OrderingStrategyId = orderingStrategyId
        });
        await SaveChangesAndHandleConflictAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Saves changes; treats unique constraint violation (TOCTOU race) as idempotent success.
    /// </summary>
    private async Task SaveChangesAndHandleConflictAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            foreach (var entry in ex.Entries)
                entry.State = EntityState.Detached;
            _logger.LogDebug(
                "Ledger append conflict (concurrent insert for same idempotency key); treating as success. Message={Message}",
                ex.InnerException?.Message ?? ex.Message);
        }
    }

    /// <summary>
    /// True when the exception is a unique constraint violation (e.g. PostgreSQL 23505).
    /// </summary>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("23505", StringComparison.Ordinal) ||
               msg.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
    }

    private void LogValidationResult(
        LedgerPayloadValidationResult result,
        string ledgerFamily,
        string eventType,
        string sourceOrReplayId,
        bool isReplay)
    {
        if (result.WasRejected)
        {
            _logger.LogWarning(
                "Ledger payload validation failed: invalid payload rejected. LedgerFamily={LedgerFamily}, EventType={EventType}, SourceId={SourceId}, IsReplay={IsReplay}, Reason={Reason}, PayloadSizeBytes={Size}",
                ledgerFamily, eventType, sourceOrReplayId, isReplay, result.RejectionReason, result.OriginalSizeBytes);
        }
        else if (result.WasTruncated)
        {
            _logger.LogWarning(
                "Ledger payload exceeded max size; replaced with placeholder. LedgerFamily={LedgerFamily}, EventType={EventType}, SourceId={SourceId}, IsReplay={IsReplay}, OriginalSizeBytes={Size}",
                ledgerFamily, eventType, sourceOrReplayId, isReplay, result.OriginalSizeBytes);
        }
    }
}
