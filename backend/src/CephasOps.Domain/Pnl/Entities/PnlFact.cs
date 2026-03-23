using CephasOps.Domain.Common;

namespace CephasOps.Domain.Pnl.Entities;

/// <summary>
/// P&amp;L fact entity (aggregated P&amp;L data)
/// </summary>
public class PnlFact : CompanyScopedEntity
{
    /// <summary>
    /// P&amp;L period ID
    /// </summary>
    public Guid? PnlPeriodId { get; set; }

    /// <summary>
    /// Partner ID (nullable)
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// Vertical (ISP, Barbershop, Travel)
    /// </summary>
    public string? Vertical { get; set; }

    /// <summary>
    /// Cost centre ID (nullable)
    /// </summary>
    public Guid? CostCentreId { get; set; }

    /// <summary>
    /// Period (e.g., "2025-01")
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Order type (Activation, Assurance, etc.)
    /// </summary>
    public string? OrderType { get; set; }

    /// <summary>
    /// Revenue amount
    /// </summary>
    public decimal RevenueAmount { get; set; }

    /// <summary>
    /// Direct material cost
    /// </summary>
    public decimal DirectMaterialCost { get; set; }

    /// <summary>
    /// Direct labour cost
    /// </summary>
    public decimal DirectLabourCost { get; set; }

    /// <summary>
    /// Indirect cost
    /// </summary>
    public decimal IndirectCost { get; set; }

    /// <summary>
    /// Gross profit (Revenue - DirectMaterialCost - DirectLabourCost)
    /// </summary>
    public decimal GrossProfit { get; set; }

    /// <summary>
    /// Net profit (GrossProfit - IndirectCost)
    /// </summary>
    public decimal NetProfit { get; set; }

    /// <summary>
    /// Jobs count
    /// </summary>
    public int JobsCount { get; set; }

    /// <summary>
    /// Orders completed count
    /// </summary>
    public int OrdersCompletedCount { get; set; }

    /// <summary>
    /// Reschedules count
    /// </summary>
    public int ReschedulesCount { get; set; }

    /// <summary>
    /// Assurance jobs count
    /// </summary>
    public int AssuranceJobsCount { get; set; }

    /// <summary>
    /// Last recalculated timestamp
    /// </summary>
    public DateTime? LastRecalculatedAt { get; set; }

    // Navigation properties
    public PnlPeriod? PnlPeriod { get; set; }
}

