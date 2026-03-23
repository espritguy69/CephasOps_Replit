namespace CephasOps.Application.Orders.DTOs;

/// <summary>
/// Material collection check result
/// </summary>
public class MaterialCollectionCheckResultDto
{
    public Guid OrderId { get; set; }
    public Guid? ServiceInstallerId { get; set; }
    public bool RequiresCollection { get; set; }
    public List<MissingMaterialDto> MissingMaterials { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Missing material details
/// </summary>
public class MissingMaterialDto
{
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal RequiredQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal MissingQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = "pcs";
}

/// <summary>
/// Required material display DTO
/// </summary>
public class RequiredMaterialDisplayDto
{
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = "pcs";
    public bool IsSerialised { get; set; }
}

/// <summary>
/// Material pack for an order: required materials plus missing (to collect). Used by GET order material-pack API.
/// </summary>
public class MaterialPackDto
{
    public Guid OrderId { get; set; }
    public Guid? ServiceInstallerId { get; set; }
    public bool RequiresCollection { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<RequiredMaterialDisplayDto> RequiredMaterials { get; set; } = new();
    public List<MissingMaterialDto> MissingMaterials { get; set; } = new();
}

