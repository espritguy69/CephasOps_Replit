using CephasOps.Domain.Common;
using CephasOps.Domain.Inventory.Enums;

namespace CephasOps.Domain.Inventory.Entities;

/// <summary>
/// Immutable ledger entry for stock. All quantity changes flow through this table.
/// Balance is derived by summing entries per material and location.
/// </summary>
public class StockLedgerEntry : CompanyScopedEntity
{
    /// <summary>Event type (RECEIVE, TRANSFER, ALLOCATE, ISSUE, RETURN, ADJUST, SCRAP)</summary>
    public StockLedgerEntryType EntryType { get; set; }

    /// <summary>Material</summary>
    public Guid MaterialId { get; set; }

    /// <summary>Primary location (for single-location events: RECEIVE, ISSUE, ADJUST, SCRAP)</summary>
    public Guid LocationId { get; set; }

    /// <summary>Quantity: positive = in, negative = out. For TRANSFER, one row with From+To or two rows.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Source location (for TRANSFER)</summary>
    public Guid? FromLocationId { get; set; }

    /// <summary>Destination location (for TRANSFER)</summary>
    public Guid? ToLocationId { get; set; }

    /// <summary>Order this entry is tied to (if any)</summary>
    public Guid? OrderId { get; set; }

    /// <summary>Serialised item (if entry is for a serial)</summary>
    public Guid? SerialisedItemId { get; set; }

    /// <summary>Allocation this entry is part of (if any)</summary>
    public Guid? AllocationId { get; set; }

    /// <summary>Reference type (e.g. DO, PO, Manual)</summary>
    public string? ReferenceType { get; set; }

    /// <summary>External reference id</summary>
    public string? ReferenceId { get; set; }

    /// <summary>User who created the entry</summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>Optional remarks</summary>
    public string? Remarks { get; set; }

    // Navigation
    public Material? Material { get; set; }
    public StockLocation? Location { get; set; }
    public StockLocation? FromLocation { get; set; }
    public StockLocation? ToLocation { get; set; }
    public SerialisedItem? SerialisedItem { get; set; }
    public StockAllocation? Allocation { get; set; }
}
