namespace CephasOps.Application.Buildings.DTOs;

/// <summary>
/// DTO for building default material
/// </summary>
public class BuildingDefaultMaterialDto
{
    public Guid Id { get; set; }
    public Guid BuildingId { get; set; }
    public Guid OrderTypeId { get; set; }
    public string? OrderTypeName { get; set; }
    public string? OrderTypeCode { get; set; }
    public Guid MaterialId { get; set; }
    public string? MaterialCode { get; set; }
    public string? MaterialDescription { get; set; }
    public string? MaterialUnitOfMeasure { get; set; }
    public decimal DefaultQuantity { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating building default material
/// </summary>
public class CreateBuildingDefaultMaterialDto
{
    public Guid OrderTypeId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal DefaultQuantity { get; set; } = 1;
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for updating building default material
/// </summary>
public class UpdateBuildingDefaultMaterialDto
{
    public decimal? DefaultQuantity { get; set; }
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Summary of default materials for dashboard
/// </summary>
public class BuildingDefaultMaterialsSummaryDto
{
    public int BuildingsWithMaterials { get; set; }
    public int JobTypesConfigured { get; set; }
    public int TotalMaterialItems { get; set; }
    public decimal AvgItemsPerBuilding { get; set; }
    public List<MaterialUsageSummaryDto> MostUsedMaterials { get; set; } = new();
    public StockImpactSummaryDto StockImpact { get; set; } = new();
}

/// <summary>
/// Material usage summary for dashboard
/// </summary>
public class MaterialUsageSummaryDto
{
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialDescription { get; set; } = string.Empty;
    public int BuildingCount { get; set; }
}

/// <summary>
/// Stock impact summary for dashboard
/// </summary>
public class StockImpactSummaryDto
{
    public int OrdersCompleted { get; set; }
    public int MaterialsConsumed { get; set; }
    public decimal TotalValue { get; set; }
}

