namespace CephasOps.Application.Assets.DTOs;

/// <summary>
/// Asset Depreciation Entry DTO
/// </summary>
public class AssetDepreciationDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid AssetId { get; set; }
    public string? AssetName { get; set; }
    public string? AssetTag { get; set; }
    public string Period { get; set; } = string.Empty;
    public decimal DepreciationAmount { get; set; }
    public decimal OpeningBookValue { get; set; }
    public decimal ClosingBookValue { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public Guid? PnlTypeId { get; set; }
    public bool IsPosted { get; set; }
    public DateTime CalculatedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Depreciation schedule for an asset
/// </summary>
public class DepreciationScheduleDto
{
    public Guid AssetId { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public string AssetTag { get; set; } = string.Empty;
    public decimal PurchaseCost { get; set; }
    public decimal SalvageValue { get; set; }
    public decimal DepreciableAmount { get; set; }
    public int UsefulLifeMonths { get; set; }
    public decimal MonthlyDepreciation { get; set; }
    public List<DepreciationScheduleLineDto> Schedule { get; set; } = new();
}

/// <summary>
/// Single line in depreciation schedule
/// </summary>
public class DepreciationScheduleLineDto
{
    public string Period { get; set; } = string.Empty;
    public decimal OpeningBookValue { get; set; }
    public decimal DepreciationAmount { get; set; }
    public decimal ClosingBookValue { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public bool IsActual { get; set; } // true if already recorded, false if projected
}

/// <summary>
/// Request to run depreciation for a period
/// </summary>
public class RunDepreciationDto
{
    public string Period { get; set; } = string.Empty; // e.g., "2025-01"
    public Guid? AssetId { get; set; } // If null, run for all eligible assets
    public bool PostImmediately { get; set; } = false;
}

/// <summary>
/// Result of depreciation run
/// </summary>
public class DepreciationRunResultDto
{
    public string Period { get; set; } = string.Empty;
    public int AssetsProcessed { get; set; }
    public int EntriesCreated { get; set; }
    public decimal TotalDepreciation { get; set; }
    public List<string> Errors { get; set; } = new();
}

