namespace CephasOps.Application.Pnl.DTOs;

/// <summary>
/// P&amp;L summary DTO
/// </summary>
public class PnlSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalDirectMaterialCost { get; set; }
    public decimal TotalDirectLabourCost { get; set; }
    public decimal TotalIndirectCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal NetProfit { get; set; }
    public int TotalJobs { get; set; }
    public int TotalOrdersCompleted { get; set; }
    public List<PnlFactDto> Facts { get; set; } = new();
}

/// <summary>
/// P&amp;L fact DTO
/// </summary>
public class PnlFactDto
{
    public Guid Id { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public string? Vertical { get; set; }
    public Guid? CostCentreId { get; set; }
    public string? CostCentreName { get; set; }
    public string Period { get; set; } = string.Empty;
    public string? OrderType { get; set; }
    public decimal RevenueAmount { get; set; }
    public decimal DirectMaterialCost { get; set; }
    public decimal DirectLabourCost { get; set; }
    public decimal IndirectCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal NetProfit { get; set; }
    public int JobsCount { get; set; }
    public int OrdersCompletedCount { get; set; }
}

/// <summary>
/// P&amp;L order detail DTO
/// </summary>
public class PnlOrderDetailDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string OrderUniqueId { get; set; } = string.Empty;
    public Guid PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public decimal RevenueAmount { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LabourCost { get; set; }
    public decimal OverheadAllocated { get; set; }
    public decimal ProfitForOrder { get; set; }
}

/// <summary>
/// P&amp;L detail per order DTO - enhanced per-order profitability with all fields
/// </summary>
public class PnlDetailPerOrderDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public Guid OrderId { get; set; }
    public Guid PartnerId { get; set; }
    public string? PartnerName { get; set; }
    /// <summary>Display-only: Partner.Code + "-" + OrderCategory.Code (e.g. TIME-FTTH). Not persisted.</summary>
    public string? DerivedPartnerCategoryLabel { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string Period { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string? OrderTypeName { get; set; }
    public string? OrderCategory { get; set; }
    public string? InstallationMethod { get; set; }
    public decimal RevenueAmount { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LabourCost { get; set; }
    public decimal OverheadAllocated { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal ProfitForOrder { get; set; }
    public string? KpiResult { get; set; }
    public int RescheduleCount { get; set; }
    public Guid? ServiceInstallerId { get; set; }
    public string? ServiceInstallerName { get; set; }
    public string? RevenueRateSource { get; set; }
    public string? LabourRateSource { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CalculatedAt { get; set; }
    public string? DataQualityNotes { get; set; }
    public string? OrderNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? BuildingName { get; set; }
    public string? AddressLine1 { get; set; }
}

/// <summary>
/// P&amp;L period DTO
/// </summary>
public class PnlPeriodDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Period { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LastRecalculatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Overhead entry DTO
/// </summary>
public class OverheadEntryDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public Guid CostCentreId { get; set; }
    public string CostCentreName { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? AllocationBasis { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Create overhead entry request DTO
/// </summary>
public class CreateOverheadEntryDto
{
    public Guid CostCentreId { get; set; }
    public string Period { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? AllocationBasis { get; set; }
}

