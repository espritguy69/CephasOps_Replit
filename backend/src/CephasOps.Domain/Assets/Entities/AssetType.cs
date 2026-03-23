using CephasOps.Domain.Common;
using CephasOps.Domain.Assets.Enums;

namespace CephasOps.Domain.Assets.Entities;

/// <summary>
/// Asset type/category entity (e.g., Vehicle, Computer, Furniture)
/// </summary>
public class AssetType : CompanyScopedEntity
{
    /// <summary>
    /// Name of the asset type
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique code for the asset type (e.g., "VEH", "COMP", "FURN")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Description of this asset type
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Default depreciation method for assets of this type
    /// </summary>
    public DepreciationMethod DefaultDepreciationMethod { get; set; } = DepreciationMethod.StraightLine;

    /// <summary>
    /// Default useful life in months for assets of this type
    /// </summary>
    public int DefaultUsefulLifeMonths { get; set; } = 60; // 5 years default

    /// <summary>
    /// Default salvage value percentage (e.g., 10 means 10% of original value)
    /// </summary>
    public decimal DefaultSalvageValuePercent { get; set; } = 10;

    /// <summary>
    /// P&amp;L Type ID for depreciation expenses (link to expense category)
    /// </summary>
    public Guid? DepreciationPnlTypeId { get; set; }

    /// <summary>
    /// Whether this asset type is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sort order for display
    /// </summary>
    public int SortOrder { get; set; }

    // Navigation properties

    /// <summary>
    /// Assets of this type
    /// </summary>
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}

