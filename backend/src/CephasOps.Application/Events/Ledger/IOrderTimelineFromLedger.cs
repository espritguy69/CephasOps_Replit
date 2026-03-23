using CephasOps.Application.Events.Ledger.DTOs;

namespace CephasOps.Application.Events.Ledger;

/// <summary>
/// Ledger-derived projection: order timeline from OrderLifecycle ledger entries.
/// Proves order lifecycle event → ledger → order timeline path. Read-only; replay-safe.
/// </summary>
public interface IOrderTimelineFromLedger
{
    Task<IReadOnlyList<OrderTimelineItemDto>> GetByOrderIdAsync(
        Guid orderId,
        Guid? companyId,
        DateTime? fromOccurredUtc,
        DateTime? toOccurredUtc,
        int limit,
        CancellationToken cancellationToken = default);
}
