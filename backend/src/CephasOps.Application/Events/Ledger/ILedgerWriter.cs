using CephasOps.Domain.Events;

namespace CephasOps.Application.Events.Ledger;

/// <summary>
/// Append-only ledger writer. Idempotent by (SourceEventId, LedgerFamily) or (ReplayOperationId, LedgerFamily).
/// Replay-safe; no side effects beyond writing to LedgerEntries.
/// </summary>
public interface ILedgerWriter
{
    /// <summary>
    /// Append a ledger entry from a source event. Idempotent: no-op if an entry for (sourceEventId, family) already exists.
    /// </summary>
    Task AppendFromEventAsync(
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
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Append a ledger entry from a completed replay operation. Idempotent: no-op if an entry for (replayOperationId, family) already exists.
    /// </summary>
    Task AppendFromReplayOperationAsync(
        Guid replayOperationId,
        string ledgerFamily,
        string eventType,
        DateTime occurredAtUtc,
        Guid? companyId,
        string? payloadSnapshot,
        string? orderingStrategyId,
        CancellationToken cancellationToken = default);
}
