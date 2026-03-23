using CephasOps.Domain.Common;
using CephasOps.Domain.Inventory.Enums;

namespace CephasOps.Domain.Inventory.Entities;

/// <summary>
/// Links stock to an order: reservation and lifecycle (Reserved → Issued → Returned/Cancelled).
/// Prevents double-use of the same stock.
/// </summary>
public class StockAllocation : CompanyScopedEntity
{
    /// <summary>Material</summary>
    public Guid MaterialId { get; set; }

    /// <summary>Serialised item (if serialised material)</summary>
    public Guid? SerialisedItemId { get; set; }

    /// <summary>Location where stock is reserved/held</summary>
    public Guid LocationId { get; set; }

    /// <summary>Quantity reserved/allocated</summary>
    public decimal Quantity { get; set; }

    /// <summary>Order this allocation is for</summary>
    public Guid OrderId { get; set; }

    /// <summary>Reserved, Issued, Returned, Cancelled</summary>
    public StockAllocationStatus Status { get; set; }

    /// <summary>Ledger entry that created the reservation (if any)</summary>
    public Guid? LedgerEntryIdReserved { get; set; }

    /// <summary>Ledger entry that issued (if any)</summary>
    public Guid? LedgerEntryIdIssued { get; set; }

    /// <summary>Ledger entry that returned (if any)</summary>
    public Guid? LedgerEntryIdReturned { get; set; }

    /// <summary>User who created the allocation</summary>
    public Guid CreatedByUserId { get; set; }

    // Navigation
    public Material? Material { get; set; }
    public SerialisedItem? SerialisedItem { get; set; }
    public StockLocation? Location { get; set; }
    public StockLedgerEntry? LedgerEntryReserved { get; set; }
    public StockLedgerEntry? LedgerEntryIssued { get; set; }
    public StockLedgerEntry? LedgerEntryReturned { get; set; }
}
