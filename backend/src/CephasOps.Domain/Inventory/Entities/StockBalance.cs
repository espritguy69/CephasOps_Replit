using CephasOps.Domain.Common;

namespace CephasOps.Domain.Inventory.Entities;

/// <summary>
/// Stock balance entity (quantity at a location).
/// Phase 2.1.3+: Quantity is NOT the source of truth for stock levels. Use StockLedgerEntry and GET /api/inventory/stock-summary for authoritative quantities. Do not write to Quantity in new or legacy flows.
/// </summary>
public class StockBalance : CompanyScopedEntity
{
    /// <summary>
    /// Material ID
    /// </summary>
    public Guid MaterialId { get; set; }

    /// <summary>
    /// Stock location ID
    /// </summary>
    public Guid StockLocationId { get; set; }

    /// <summary>
    /// Quantity at location. NOT the source of truth—ledger (StockLedgerEntry) is. Read-only for reporting; do not mutate in write paths.
    /// </summary>
    public decimal Quantity { get; set; }

    // Navigation properties
    public Material? Material { get; set; }
    public StockLocation? StockLocation { get; set; }
}

