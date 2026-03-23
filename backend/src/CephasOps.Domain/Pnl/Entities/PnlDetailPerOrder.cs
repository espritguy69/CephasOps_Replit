using CephasOps.Domain.Common;

namespace CephasOps.Domain.Pnl.Entities;

/// <summary>
/// P&amp;L detail per order entity (granular order-level P&amp;L).
/// Per PNL_MODULE.md section 6.2 - for detailed per-order profitability analysis.
/// </summary>
public class PnlDetailPerOrder : CompanyScopedEntity
{
    /// <summary>
    /// Order ID - links to the Order entity
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Partner ID - for partner profitability drill-down
    /// </summary>
    public Guid PartnerId { get; set; }

    /// <summary>
    /// Department ID - for department-level analysis
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Period (e.g., "2025-01") - for time-based aggregation
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Order type code (e.g., "ACTIVATION", "ASSURANCE", "MODIFICATION_OUTDOOR")
    /// </summary>
    public string OrderType { get; set; } = string.Empty;

    /// <summary>
    /// Order category code (e.g., "FTTH", "FTTO", "FTTR", "FTTC") - represents the service/technology category
    /// Previously known as InstallationType.
    /// </summary>
    public string? OrderCategory { get; set; }

    /// <summary>
    /// Installation method code (e.g., "PRELAID", "NON_PRELAID", "RDF_POLE") - represents site condition
    /// </summary>
    public string? InstallationMethod { get; set; }

    /// <summary>
    /// Revenue amount from invoice for this order
    /// </summary>
    public decimal RevenueAmount { get; set; }

    /// <summary>
    /// Material cost (sum of materials used in this order)
    /// </summary>
    public decimal MaterialCost { get; set; }

    /// <summary>
    /// Labour cost (SI payment for this order from Payroll)
    /// </summary>
    public decimal LabourCost { get; set; }

    /// <summary>
    /// Overhead allocated to this order (based on allocation strategy)
    /// </summary>
    public decimal OverheadAllocated { get; set; }

    /// <summary>
    /// Gross profit = Revenue - MaterialCost - LabourCost
    /// </summary>
    public decimal GrossProfit { get; set; }

    /// <summary>
    /// Net profit = GrossProfit - OverheadAllocated
    /// </summary>
    public decimal ProfitForOrder { get; set; }

    /// <summary>
    /// KPI result for this order (OnTime, Late, Exceeded, Rework)
    /// </summary>
    public string? KpiResult { get; set; }

    /// <summary>
    /// Number of reschedules for this order
    /// </summary>
    public int RescheduleCount { get; set; }

    /// <summary>
    /// Service Installer ID who completed the order (for SI profitability analysis)
    /// </summary>
    public Guid? ServiceInstallerId { get; set; }

    /// <summary>
    /// Rate source used for revenue calculation (e.g., "GponPartnerJobRate", "BillingRatecard")
    /// </summary>
    public string? RevenueRateSource { get; set; }

    /// <summary>
    /// Rate source used for labour cost calculation (e.g., "GponSiJobRate", "GponSiCustomRate")
    /// </summary>
    public string? LabourRateSource { get; set; }

    /// <summary>
    /// Order completion date (for period assignment)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When this P&amp;L record was calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Notes or flags for data quality issues
    /// </summary>
    public string? DataQualityNotes { get; set; }
}

