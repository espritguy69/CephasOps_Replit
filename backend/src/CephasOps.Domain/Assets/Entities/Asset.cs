using CephasOps.Domain.Common;
using CephasOps.Domain.Assets.Enums;

namespace CephasOps.Domain.Assets.Entities;

/// <summary>
/// Asset entity representing a company asset (vehicle, computer, equipment, etc.)
/// </summary>
public class Asset : CompanyScopedEntity
{
    /// <summary>
    /// Asset type ID
    /// </summary>
    public Guid AssetTypeId { get; set; }

    /// <summary>
    /// Asset tag/code (unique identifier for the asset)
    /// </summary>
    public string AssetTag { get; set; } = string.Empty;

    /// <summary>
    /// Name/description of the asset
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Serial number (if applicable)
    /// </summary>
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Model number
    /// </summary>
    public string? ModelNumber { get; set; }

    /// <summary>
    /// Manufacturer/brand
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Supplier/vendor name
    /// </summary>
    public string? Supplier { get; set; }

    /// <summary>
    /// Supplier invoice reference
    /// </summary>
    public Guid? SupplierInvoiceId { get; set; }

    /// <summary>
    /// Purchase date
    /// </summary>
    public DateTime PurchaseDate { get; set; }

    /// <summary>
    /// Date when asset was put into service
    /// </summary>
    public DateTime? InServiceDate { get; set; }

    /// <summary>
    /// Original purchase cost
    /// </summary>
    public decimal PurchaseCost { get; set; }

    /// <summary>
    /// Salvage/residual value at end of useful life
    /// </summary>
    public decimal SalvageValue { get; set; }

    /// <summary>
    /// Depreciation method for this asset
    /// </summary>
    public DepreciationMethod DepreciationMethod { get; set; } = DepreciationMethod.StraightLine;

    /// <summary>
    /// Useful life in months
    /// </summary>
    public int UsefulLifeMonths { get; set; } = 60;

    /// <summary>
    /// Current book value (purchase cost minus accumulated depreciation)
    /// </summary>
    public decimal CurrentBookValue { get; set; }

    /// <summary>
    /// Accumulated depreciation to date
    /// </summary>
    public decimal AccumulatedDepreciation { get; set; }

    /// <summary>
    /// Last depreciation calculation date
    /// </summary>
    public DateTime? LastDepreciationDate { get; set; }

    /// <summary>
    /// Current status of the asset
    /// </summary>
    public AssetStatus Status { get; set; } = AssetStatus.Active;

    /// <summary>
    /// Location/department where asset is assigned
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Department ID if assigned to a department
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// User ID if assigned to a specific person
    /// </summary>
    public Guid? AssignedToUserId { get; set; }

    /// <summary>
    /// Cost centre ID for accounting purposes
    /// </summary>
    public Guid? CostCentreId { get; set; }

    /// <summary>
    /// Warranty expiry date
    /// </summary>
    public DateTime? WarrantyExpiryDate { get; set; }

    /// <summary>
    /// Insurance policy number
    /// </summary>
    public string? InsurancePolicyNumber { get; set; }

    /// <summary>
    /// Insurance expiry date
    /// </summary>
    public DateTime? InsuranceExpiryDate { get; set; }

    /// <summary>
    /// Next scheduled maintenance date
    /// </summary>
    public DateTime? NextMaintenanceDate { get; set; }

    /// <summary>
    /// Notes about the asset
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether asset is fully depreciated
    /// </summary>
    public bool IsFullyDepreciated { get; set; }

    // Navigation properties

    /// <summary>
    /// Asset type
    /// </summary>
    public AssetType? AssetType { get; set; }

    /// <summary>
    /// Maintenance records for this asset
    /// </summary>
    public ICollection<AssetMaintenance> MaintenanceRecords { get; set; } = new List<AssetMaintenance>();

    /// <summary>
    /// Depreciation entries for this asset
    /// </summary>
    public ICollection<AssetDepreciation> DepreciationEntries { get; set; } = new List<AssetDepreciation>();

    /// <summary>
    /// Disposal record (if disposed)
    /// </summary>
    public AssetDisposal? Disposal { get; set; }
}

