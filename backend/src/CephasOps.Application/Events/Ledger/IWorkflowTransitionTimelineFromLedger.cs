using CephasOps.Application.Events.Ledger.DTOs;

namespace CephasOps.Application.Events.Ledger;

/// <summary>
/// Ledger-derived projection: workflow transition timeline built from LedgerEntries (family WorkflowTransition).
/// Proves domain events → ledger → derived projection path. Read-only; replay-safe.
/// </summary>
public interface IWorkflowTransitionTimelineFromLedger
{
    Task<IReadOnlyList<WorkflowTransitionTimelineItemDto>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        Guid? companyId,
        DateTime? fromOccurredUtc,
        DateTime? toOccurredUtc,
        int limit,
        CancellationToken cancellationToken = default);
}
