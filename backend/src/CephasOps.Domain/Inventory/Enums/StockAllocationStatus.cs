namespace CephasOps.Domain.Inventory.Enums;

/// <summary>
/// Lifecycle status of a stock allocation (reservation) for an order.
/// </summary>
public enum StockAllocationStatus
{
    /// <summary>Reserved for order, not yet issued</summary>
    Reserved = 0,

    /// <summary>Issued (e.g. to SI or order)</summary>
    Issued = 1,

    /// <summary>Returned</summary>
    Returned = 2,

    /// <summary>Cancelled (reservation released)</summary>
    Cancelled = 3
}
