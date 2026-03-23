using CephasOps.Application.Inventory.DTOs;

namespace CephasOps.Application.Inventory.Services;

/// <summary>
/// Ledger-based inventory: immutable entries, balance derived from ledger, allocation prevents double-use.
/// </summary>
public interface IStockLedgerService
{
    Task<LedgerWriteResultDto> ReceiveAsync(LedgerReceiveRequestDto dto, Guid? companyId, Guid? departmentId, Guid userId, CancellationToken cancellationToken = default);
    Task<LedgerWriteResultDto> TransferAsync(LedgerTransferRequestDto dto, Guid? companyId, Guid? departmentId, Guid userId, CancellationToken cancellationToken = default);
    Task<LedgerWriteResultDto> AllocateAsync(LedgerAllocateRequestDto dto, Guid? companyId, Guid? departmentId, Guid userId, CancellationToken cancellationToken = default);
    Task<LedgerWriteResultDto> IssueAsync(LedgerIssueRequestDto dto, Guid? companyId, Guid? departmentId, Guid userId, CancellationToken cancellationToken = default);
    Task<LedgerWriteResultDto> ReturnAsync(LedgerReturnRequestDto dto, Guid? companyId, Guid? departmentId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a legacy stock movement as equivalent ledger entry(ies). Does not validate availability or department; does not update StockBalance.
    /// Caller is responsible for SaveChanges (same transaction as movement).
    /// </summary>
    /// <summary>For legacy write paths only. Creates ledger entries from legacy movement DTOs; does not update StockBalance.Quantity. Do not use for new features—use ReceiveAsync, TransferAsync, IssueAsync, ReturnAsync instead. Ledger is the single source of truth.</summary>
    Task RecordLegacyMovementAsync(CreateStockMovementDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default);

    Task<LedgerListResultDto> GetLedgerAsync(LedgerFilterDto filter, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default);
    Task<StockSummaryResultDto> GetStockSummaryAsync(Guid? companyId, Guid? departmentId, Guid? locationId, Guid? materialId, CancellationToken cancellationToken = default);

    /// <summary>Get ledger-derived balances (company-scoped, no department filter). Used by GET /api/inventory/stock and MaterialCollectionService.</summary>
    Task<List<LedgerDerivedBalanceDto>> GetLedgerDerivedBalancesAsync(Guid? companyId, Guid? locationId = null, Guid? materialId = null, CancellationToken cancellationToken = default);

    /// <summary>Export rows for usage-by-period report (CSV). Department-scoped; fromDate/toDate required.</summary>
    Task<List<UsageSummaryExportRowDto>> GetUsageSummaryExportRowsAsync(DateTime fromDate, DateTime toDate, string? groupBy, Guid? materialId, Guid? locationId, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default);

    /// <summary>Export rows for serial lifecycle report (CSV). Department-scoped; at least one serial required.</summary>
    Task<List<SerialLifecycleExportRowDto>> GetSerialLifecycleExportRowsAsync(IReadOnlyList<string> serialNumbers, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default);

    /// <summary>Usage summary report (JSON API). Department-scoped; paged.</summary>
    Task<UsageSummaryReportResultDto> GetUsageSummaryReportAsync(DateTime fromDate, DateTime toDate, string? groupBy, Guid? materialId, Guid? locationId, Guid? companyId, Guid? departmentId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Serial lifecycle report (JSON API). Department-scoped; at least one serial required.</summary>
    Task<SerialLifecycleReportResultDto> GetSerialLifecycleReportAsync(IReadOnlyList<string> serialNumbers, Guid? companyId, Guid? departmentId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Stock-by-location history (JSON API). Uses snapshots when available (Daily/Weekly/Monthly); otherwise current snapshot. Department-scoped.</summary>
    Task<StockByLocationHistoryResultDto> GetStockByLocationHistoryReportAsync(DateTime fromDate, DateTime toDate, string snapshotType, Guid? materialId, Guid? locationId, Guid? companyId, Guid? departmentId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Populate stock-by-location snapshots for a given period (Daily/Weekly/Monthly). Run via background job (e.g. end-of-day).</summary>
    Task EnsureSnapshotsForPeriodAsync(DateTime periodEndDate, string snapshotType, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>Reconcile LedgerBalanceCache with ledger + allocations; repair any drift. Run periodically (e.g. background job).</summary>
    Task ReconcileBalanceCacheAsync(CancellationToken cancellationToken = default);
}
