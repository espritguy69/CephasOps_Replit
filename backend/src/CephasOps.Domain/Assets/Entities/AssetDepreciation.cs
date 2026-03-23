using CephasOps.Domain.Common;

namespace CephasOps.Domain.Assets.Entities;

/// <summary>
/// Asset depreciation entry entity (monthly/periodic depreciation records)
/// </summary>
public class AssetDepreciation : CompanyScopedEntity
{
    /// <summary>
    /// Asset ID
    /// </summary>
    public Guid AssetId { get; set; }

    /// <summary>
    /// Period for this depreciation (e.g., "2025-01")
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Depreciation amount for this period
    /// </summary>
    public decimal DepreciationAmount { get; set; }

    /// <summary>
    /// Book value at start of period
    /// </summary>
    public decimal OpeningBookValue { get; set; }

    /// <summary>
    /// Book value at end of period (after depreciation)
    /// </summary>
    public decimal ClosingBookValue { get; set; }

    /// <summary>
    /// Accumulated depreciation at end of period
    /// </summary>
    public decimal AccumulatedDepreciation { get; set; }

    /// <summary>
    /// P&amp;L Type ID for the depreciation expense
    /// </summary>
    public Guid? PnlTypeId { get; set; }

    /// <summary>
    /// Whether this entry has been posted to accounting
    /// </summary>
    public bool IsPosted { get; set; }

    /// <summary>
    /// Date when this depreciation was calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties

    /// <summary>
    /// Asset this depreciation is for
    /// </summary>
    public Asset? Asset { get; set; }
}

