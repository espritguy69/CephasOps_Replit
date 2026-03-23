using CephasOps.Application.Events.Ledger.DTOs;

namespace CephasOps.Application.Events.Ledger;

/// <summary>
/// Unified operational history for an order: merges WorkflowTransition and OrderLifecycle ledger entries into one ordered timeline. Read-only; replay-safe.
/// </summary>
public interface IUnifiedOrderHistoryFromLedger
{
    Task<IReadOnlyList<UnifiedOrderHistoryItemDto>> GetByOrderIdAsync(
        Guid orderId,
        Guid? companyId,
        DateTime? fromOccurredUtc,
        DateTime? toOccurredUtc,
        int limit,
        CancellationToken cancellationToken = default);
}
