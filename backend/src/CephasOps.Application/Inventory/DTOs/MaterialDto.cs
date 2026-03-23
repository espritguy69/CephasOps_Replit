namespace CephasOps.Application.Inventory.DTOs;

/// <summary>
/// Material DTO
/// </summary>
public class MaterialDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Category ID (FK to MaterialCategory)
    /// </summary>
    public Guid? MaterialCategoryId { get; set; }
    /// <summary>
    /// Category name (from MaterialCategory)
    /// </summary>
    public string? MaterialCategoryName { get; set; }
    /// <summary>
    /// Category (legacy string field - kept for backward compatibility)
    /// </summary>
    public string? Category { get; set; }
    /// <summary>
    /// List of vertical IDs associated with this material
    /// </summary>
    public List<Guid> MaterialVerticalIds { get; set; } = new();
    /// <summary>
    /// List of vertical names associated with this material
    /// </summary>
    public List<string> MaterialVerticalNames { get; set; } = new();
    /// <summary>
    /// List of tag IDs associated with this material
    /// </summary>
    public List<Guid> MaterialTagIds { get; set; } = new();
    /// <summary>
    /// List of tag names associated with this material
    /// </summary>
    public List<string> MaterialTagNames { get; set; } = new();
    /// <summary>
    /// List of tag colors for UI display
    /// </summary>
    public List<string> MaterialTagColors { get; set; } = new();
    /// <summary>
    /// List of material attributes (key-value pairs)
    /// </summary>
    public List<MaterialAttributeDto> MaterialAttributes { get; set; } = new();
    public bool IsSerialised { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal? DefaultCost { get; set; }
    /// <summary>
    /// Partner ID (legacy - kept for backward compatibility)
    /// Will be set to the first partner in PartnerIds if available
    /// </summary>
    public Guid? PartnerId { get; set; }
    /// <summary>
    /// Partner name (legacy - kept for backward compatibility)
    /// Will be set to the first partner name in PartnerNames if available
    /// </summary>
    public string? PartnerName { get; set; }
    /// <summary>
    /// List of partner IDs associated with this material
    /// </summary>
    public List<Guid> PartnerIds { get; set; } = new();
    /// <summary>
    /// List of partner names associated with this material
    /// </summary>
    public List<string> PartnerNames { get; set; } = new();
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsActive { get; set; }
    public string? Barcode { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Material attribute DTO (key-value pair)
/// </summary>
public class MaterialAttributeDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DataType { get; set; } = "String";
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Create material request DTO
/// </summary>
public class CreateMaterialDto
{
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Category ID (FK to MaterialCategory)
    /// </summary>
    public Guid? MaterialCategoryId { get; set; }
    /// <summary>
    /// Category (legacy string field - kept for backward compatibility)
    /// </summary>
    public string? Category { get; set; }
    /// <summary>
    /// List of vertical IDs to associate with this material
    /// </summary>
    public List<Guid> MaterialVerticalIds { get; set; } = new();
    /// <summary>
    /// List of tag IDs to associate with this material
    /// </summary>
    public List<Guid> MaterialTagIds { get; set; } = new();
    /// <summary>
    /// List of material attributes (key-value pairs)
    /// </summary>
    public List<MaterialAttributeDto> MaterialAttributes { get; set; } = new();
    public bool IsSerialised { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal? DefaultCost { get; set; }
    /// <summary>
    /// Partner ID (legacy - kept for backward compatibility)
    /// If provided, will be added to PartnerIds
    /// </summary>
    public Guid? PartnerId { get; set; }
    /// <summary>
    /// List of partner IDs (source of truth)
    /// At least one partner is required
    /// </summary>
    public List<Guid> PartnerIds { get; set; } = new();
    public Guid? DepartmentId { get; set; }
    public string? Barcode { get; set; }
}

/// <summary>
/// Update material request DTO
/// </summary>
public class UpdateMaterialDto
{
    public string? ItemCode { get; set; }
    public string? Description { get; set; }
    /// <summary>
    /// Category ID (FK to MaterialCategory)
    /// </summary>
    public Guid? MaterialCategoryId { get; set; }
    /// <summary>
    /// Category (legacy string field - kept for backward compatibility)
    /// </summary>
    public string? Category { get; set; }
    /// <summary>
    /// List of vertical IDs to associate with this material (null = no change, empty list = remove all)
    /// </summary>
    public List<Guid>? MaterialVerticalIds { get; set; }
    /// <summary>
    /// List of tag IDs to associate with this material (null = no change, empty list = remove all)
    /// </summary>
    public List<Guid>? MaterialTagIds { get; set; }
    /// <summary>
    /// List of material attributes (key-value pairs) (null = no change, empty list = remove all)
    /// </summary>
    public List<MaterialAttributeDto>? MaterialAttributes { get; set; }
    public bool? IsSerialised { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal? DefaultCost { get; set; }
    /// <summary>
    /// Partner ID (legacy - kept for backward compatibility)
    /// If provided, will be added to PartnerIds
    /// </summary>
    public Guid? PartnerId { get; set; }
    /// <summary>
    /// List of partner IDs (source of truth)
    /// At least one partner is required
    /// </summary>
    public List<Guid>? PartnerIds { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool? IsActive { get; set; }
    public string? Barcode { get; set; }
}

/// <summary>
/// Stock balance DTO
/// </summary>
public class StockBalanceDto
{
    public Guid Id { get; set; }
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialDescription { get; set; } = string.Empty;
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

/// <summary>
/// Stock movement DTO
/// </summary>
public class StockMovementDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public Guid? ServiceInstallerId { get; set; }
    public Guid? PartnerId { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Create stock movement request DTO
/// </summary>
public class CreateStockMovementDto
{
    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public Guid? ServiceInstallerId { get; set; }
    public Guid? PartnerId { get; set; }
    public string? Remarks { get; set; }
    /// <summary>
    /// Serial number (required for serialised materials, ignored for non-serialised)
    /// </summary>
    public string? SerialNumber { get; set; }
}

/// <summary>
/// Stock location DTO
/// </summary>
public class StockLocationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}

