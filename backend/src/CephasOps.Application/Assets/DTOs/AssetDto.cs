using CephasOps.Domain.Assets.Enums;

namespace CephasOps.Application.Assets.DTOs;

/// <summary>
/// Asset DTO
/// </summary>
public class AssetDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid AssetTypeId { get; set; }
    public string? AssetTypeName { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public string? ModelNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? Supplier { get; set; }
    public Guid? SupplierInvoiceId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? InServiceDate { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal SalvageValue { get; set; }
    public DepreciationMethod DepreciationMethod { get; set; }
    public string DepreciationMethodName => DepreciationMethod.ToString();
    public int UsefulLifeMonths { get; set; }
    public decimal CurrentBookValue { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public DateTime? LastDepreciationDate { get; set; }
    public AssetStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? Location { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public Guid? CostCentreId { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public string? InsurancePolicyNumber { get; set; }
    public DateTime? InsuranceExpiryDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public string? Notes { get; set; }
    public bool IsFullyDepreciated { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Calculated fields
    public int RemainingUsefulLifeMonths { get; set; }
    public decimal DepreciationPercent { get; set; }
}

/// <summary>
/// Create Asset request DTO
/// </summary>
public class CreateAssetDto
{
    public Guid AssetTypeId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public string? ModelNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? Supplier { get; set; }
    public Guid? SupplierInvoiceId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? InServiceDate { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal? SalvageValue { get; set; }
    public DepreciationMethod? DepreciationMethod { get; set; }
    public int? UsefulLifeMonths { get; set; }
    public string? Location { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? CostCentreId { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public string? InsurancePolicyNumber { get; set; }
    public DateTime? InsuranceExpiryDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Update Asset request DTO
/// </summary>
public class UpdateAssetDto
{
    public Guid? AssetTypeId { get; set; }
    public string? AssetTag { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public string? ModelNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? Supplier { get; set; }
    public DateTime? InServiceDate { get; set; }
    public decimal? SalvageValue { get; set; }
    public DepreciationMethod? DepreciationMethod { get; set; }
    public int? UsefulLifeMonths { get; set; }
    public AssetStatus? Status { get; set; }
    public string? Location { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? CostCentreId { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public string? InsurancePolicyNumber { get; set; }
    public DateTime? InsuranceExpiryDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Asset summary for dashboard
/// </summary>
public class AssetSummaryDto
{
    public int TotalAssets { get; set; }
    public int ActiveAssets { get; set; }
    public int DisposedAssets { get; set; }
    public int AssetsUnderMaintenance { get; set; }
    public decimal TotalPurchaseCost { get; set; }
    public decimal TotalCurrentBookValue { get; set; }
    public decimal TotalAccumulatedDepreciation { get; set; }
    public List<AssetsByTypeDto> AssetsByType { get; set; } = new();
}

/// <summary>
/// Assets grouped by type
/// </summary>
public class AssetsByTypeDto
{
    public Guid AssetTypeId { get; set; }
    public string AssetTypeName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalValue { get; set; }
}

