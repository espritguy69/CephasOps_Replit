namespace CephasOps.Domain.Inventory.Enums;

/// <summary>
/// Immutable ledger event types for stock.
/// </summary>
public enum StockLedgerEntryType
{
    /// <summary>Goods received (e.g. from supplier)</summary>
    Receive = 0,

    /// <summary>Transfer between locations</summary>
    Transfer = 1,

    /// <summary>Reservation/allocation to order</summary>
    Allocate = 2,

    /// <summary>Issue to order / service installer</summary>
    Issue = 3,

    /// <summary>Return from order / customer</summary>
    Return = 4,

    /// <summary>Quantity adjustment (e.g. count correction)</summary>
    Adjust = 5,

    /// <summary>Scrap / write-off</summary>
    Scrap = 6
}
