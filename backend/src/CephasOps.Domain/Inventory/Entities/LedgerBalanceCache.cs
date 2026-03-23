namespace CephasOps.Domain.Inventory.Entities;

/// <summary>
/// Derived balance cache for (MaterialId, LocationId). Strictly derived from ledger + allocations;
/// ledger remains the single source of truth. Updated on ledger writes (same transaction) and
/// repaired by reconciliation job when drift is detected.
/// </summary>
public class LedgerBalanceCache
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? CompanyId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid LocationId { get; set; }
    /// <summary>From Material.DepartmentId; used for department-scoped reads.</summary>
    public Guid DepartmentId { get; set; }

    /// <summary>SUM(ledger.Quantity) for this material/location.</summary>
    public decimal OnHand { get; set; }
    /// <summary>SUM(allocations.Quantity) where Status = Reserved.</summary>
    public decimal Reserved { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Optional: last ledger entry that contributed to this row (for auditing).</summary>
    public Guid? LastLedgerEntryId { get; set; }
}
