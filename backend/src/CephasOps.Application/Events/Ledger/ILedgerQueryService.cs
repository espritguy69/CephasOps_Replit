using CephasOps.Application.Events.Ledger.DTOs;

namespace CephasOps.Application.Events.Ledger;

/// <summary>Query ledger entries with filters. Read-only; no side effects.</summary>
public interface ILedgerQueryService
{
    Task<(IReadOnlyList<LedgerEntryDto> Items, int Total)> ListAsync(
        Guid? companyId,
        string? entityType,
        Guid? entityId,
        string? ledgerFamily,
        DateTime? fromOccurredUtc,
        DateTime? toOccurredUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<LedgerEntryDto?> GetByIdAsync(Guid id, Guid? scopeCompanyId, CancellationToken cancellationToken = default);
}
