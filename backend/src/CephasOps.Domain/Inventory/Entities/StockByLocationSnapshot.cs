namespace CephasOps.Domain.Inventory.Entities;

/// <summary>
/// Point-in-time snapshot of stock by material/location for reporting (Phase 2.2.2).
/// Populated by background job (e.g. end-of-day). Used by stock-by-location history report.
/// </summary>
public class StockByLocationSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CompanyId { get; set; }
    /// <summary>Material's department; used for department-scoped report filtering.</summary>
    public Guid? DepartmentId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid LocationId { get; set; }

    /// <summary>Start of period (e.g. date for Daily, Monday for Weekly, first day for Monthly).</summary>
    public DateTime PeriodStart { get; set; }
    /// <summary>End of period (same as PeriodStart for Daily).</summary>
    public DateTime PeriodEnd { get; set; }
    /// <summary>Daily, Weekly, or Monthly.</summary>
    public string SnapshotType { get; set; } = "Daily";

    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
