namespace CephasOps.Application.Inventory.DTOs;

/// <summary>Request to receive stock into a location.</summary>
public class LedgerReceiveRequestDto
{
    public Guid MaterialId { get; set; }
    public Guid LocationId { get; set; }
    public decimal Quantity { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
    public string? Remarks { get; set; }
    /// <summary>Required for serialised materials; must be unique.</summary>
    public string? SerialNumber { get; set; }
}

/// <summary>Request to transfer stock between locations.</summary>
public class LedgerTransferRequestDto
{
    public Guid MaterialId { get; set; }
    public Guid FromLocationId { get; set; }
    public Guid ToLocationId { get; set; }
    public decimal Quantity { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>Request to reserve/allocate stock for an order.</summary>
public class LedgerAllocateRequestDto
{
    public Guid OrderId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid LocationId { get; set; }
    public decimal Quantity { get; set; }
    public string? Remarks { get; set; }
    /// <summary>For serialised material: specific serial to reserve.</summary>
    public string? SerialNumber { get; set; }
}

/// <summary>Request to issue stock (to order/SI).</summary>
public class LedgerIssueRequestDto
{
    public Guid OrderId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid LocationId { get; set; }
    public decimal Quantity { get; set; }
    public string? Remarks { get; set; }
    /// <summary>For serialised material: serial to issue.</summary>
    public string? SerialNumber { get; set; }
    /// <summary>If set, issue fulfils this allocation.</summary>
    public Guid? AllocationId { get; set; }
}

/// <summary>Request to return stock from order.</summary>
public class LedgerReturnRequestDto
{
    public Guid OrderId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid LocationId { get; set; }
    public decimal Quantity { get; set; }
    public string? Remarks { get; set; }
    public string? SerialNumber { get; set; }
    /// <summary>If set, return closes this allocation.</summary>
    public Guid? AllocationId { get; set; }
}

/// <summary>Single ledger entry in response.</summary>
public class LedgerEntryDto
{
    public Guid Id { get; set; }
    public string EntryType { get; set; } = string.Empty; // Receive, Transfer, Allocate, Issue, Return, Adjust, Scrap
    public Guid MaterialId { get; set; }
    public string? MaterialCode { get; set; }
    public Guid LocationId { get; set; }
    public string? LocationName { get; set; }
    public decimal Quantity { get; set; }
    public Guid? FromLocationId { get; set; }
    public string? FromLocationName { get; set; }
    public Guid? ToLocationId { get; set; }
    public string? ToLocationName { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? SerialisedItemId { get; set; }
    public string? SerialNumber { get; set; }
    public Guid? AllocationId { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
}

/// <summary>Filter for GET ledger.</summary>
public class LedgerFilterDto
{
    public Guid? MaterialId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? OrderId { get; set; }
    public string? EntryType { get; set; } // Receive, Transfer, etc.
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>Paged ledger response.</summary>
public class LedgerListResultDto
{
    public List<LedgerEntryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>Stock at a location (derived from ledger).</summary>
public class StockByLocationDto
{
    public Guid MaterialId { get; set; }
    public string? MaterialCode { get; set; }
    public string? MaterialDescription { get; set; }
    public Guid LocationId { get; set; }
    public string? LocationName { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal QuantityAvailable => QuantityOnHand - QuantityReserved;
    public bool IsSerialised { get; set; }
}

/// <summary>Serialised item status for stock summary.</summary>
public class SerialisedStatusDto
{
    public Guid SerialisedItemId { get; set; }
    public Guid MaterialId { get; set; }
    public string? MaterialCode { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public Guid? CurrentLocationId { get; set; }
    public string? CurrentLocationName { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? LastOrderId { get; set; }
}

/// <summary>Stock summary: by location + serialised status.</summary>
public class StockSummaryResultDto
{
    public List<StockByLocationDto> ByLocation { get; set; } = new();
    public List<SerialisedStatusDto> SerialisedItems { get; set; } = new();
}

/// <summary>Ledger-derived balance (company-scoped). Used by GET /api/inventory/stock and MaterialCollectionService.</summary>
public class LedgerDerivedBalanceDto
{
    public Guid MaterialId { get; set; }
    public Guid LocationId { get; set; }
    public decimal OnHand { get; set; }
    public decimal Reserved { get; set; }
}

/// <summary>Response for a single ledger write (receive/transfer/allocate/issue/return).</summary>
public class LedgerWriteResultDto
{
    public Guid? LedgerEntryId { get; set; }
    public Guid? AllocationId { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

// --- Phase 2.2.4 report export (CSV-friendly flat rows) ---

/// <summary>One row for usage-by-period CSV export (optionally grouped by material/location/department).</summary>
public class UsageSummaryExportRowDto
{
    public string KeyId { get; set; } = string.Empty;
    public string? KeyName { get; set; }
    public decimal Received { get; set; }
    public decimal Transferred { get; set; }
    public decimal Issued { get; set; }
    public decimal Returned { get; set; }
    public decimal Adjusted { get; set; }
    public decimal Scrapped { get; set; }
}

/// <summary>One row per serial lifecycle event for CSV export.</summary>
public class SerialLifecycleExportRowDto
{
    public string SerialNumber { get; set; } = string.Empty;
    public string? MaterialCode { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? LocationName { get; set; }
    public string? OrderReference { get; set; }
    public string CreatedAtUtc { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

// --- Phase 2.2.1 JSON report API (contract-aligned with frontend) ---

/// <summary>Usage summary row for JSON report.</summary>
public class UsageSummaryRowDto
{
    public string KeyId { get; set; } = string.Empty;
    public string? KeyName { get; set; }
    public decimal Received { get; set; }
    public decimal Transferred { get; set; }
    public decimal Issued { get; set; }
    public decimal Returned { get; set; }
    public decimal Adjusted { get; set; }
    public decimal Scrapped { get; set; }
}

/// <summary>Usage summary report result (JSON API).</summary>
public class UsageSummaryReportResultDto
{
    public string FromDate { get; set; } = string.Empty;
    public string ToDate { get; set; } = string.Empty;
    public string? GroupBy { get; set; }
    public UsageSummaryTotalsDto? Totals { get; set; }
    public List<UsageSummaryRowDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>Usage summary totals.</summary>
public class UsageSummaryTotalsDto
{
    public decimal Received { get; set; }
    public decimal Transferred { get; set; }
    public decimal Issued { get; set; }
    public decimal Returned { get; set; }
    public decimal Adjusted { get; set; }
    public decimal Scrapped { get; set; }
}

/// <summary>Serial lifecycle event (JSON API).</summary>
public class SerialLifecycleEventDto
{
    public string LedgerEntryId { get; set; } = string.Empty;
    public string EntryType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? FromLocationId { get; set; }
    public string? ToLocationId { get; set; }
    public string? OrderId { get; set; }
    public string? OrderReference { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string? ReferenceType { get; set; }
}

/// <summary>Serial lifecycle per serial (JSON API).</summary>
public class SerialLifecycleDto
{
    public string SerialNumber { get; set; } = string.Empty;
    public string MaterialId { get; set; } = string.Empty;
    public string? MaterialCode { get; set; }
    public string? SerialisedItemId { get; set; }
    public List<SerialLifecycleEventDto> Events { get; set; } = new();
}

/// <summary>Serial lifecycle report result (JSON API).</summary>
public class SerialLifecycleReportResultDto
{
    public List<string> SerialsQueried { get; set; } = new();
    public List<SerialLifecycleDto> SerialLifecycles { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>Stock-by-location history row (JSON API).</summary>
public class StockByLocationHistoryRowDto
{
    public string PeriodStart { get; set; } = string.Empty;
    public string PeriodEnd { get; set; } = string.Empty;
    public string MaterialId { get; set; } = string.Empty;
    public string? MaterialCode { get; set; }
    public string LocationId { get; set; } = string.Empty;
    public string? LocationName { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
}

/// <summary>Stock-by-location history result (JSON API). Simplified: current snapshot as single period when no snapshot table.</summary>
public class StockByLocationHistoryResultDto
{
    public string FromDate { get; set; } = string.Empty;
    public string ToDate { get; set; } = string.Empty;
    public string SnapshotType { get; set; } = string.Empty;
    public List<StockByLocationHistoryRowDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
