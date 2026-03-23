using CephasOps.Domain.Assets.Enums;

namespace CephasOps.Application.Assets.DTOs;

/// <summary>
/// Asset Type DTO
/// </summary>
public class AssetTypeDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DepreciationMethod DefaultDepreciationMethod { get; set; }
    public string DefaultDepreciationMethodName => DefaultDepreciationMethod.ToString();
    public int DefaultUsefulLifeMonths { get; set; }
    public decimal DefaultSalvageValuePercent { get; set; }
    public Guid? DepreciationPnlTypeId { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int AssetCount { get; set; }
}

/// <summary>
/// Create Asset Type request DTO
/// </summary>
public class CreateAssetTypeDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DepreciationMethod DefaultDepreciationMethod { get; set; } = DepreciationMethod.StraightLine;
    public int DefaultUsefulLifeMonths { get; set; } = 60;
    public decimal DefaultSalvageValuePercent { get; set; } = 10;
    public Guid? DepreciationPnlTypeId { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

/// <summary>
/// Update Asset Type request DTO
/// </summary>
public class UpdateAssetTypeDto
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public DepreciationMethod? DefaultDepreciationMethod { get; set; }
    public int? DefaultUsefulLifeMonths { get; set; }
    public decimal? DefaultSalvageValuePercent { get; set; }
    public Guid? DepreciationPnlTypeId { get; set; }
    public bool? IsActive { get; set; }
    public int? SortOrder { get; set; }
}

