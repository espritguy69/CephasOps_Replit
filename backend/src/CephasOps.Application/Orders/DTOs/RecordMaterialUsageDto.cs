namespace CephasOps.Application.Orders.DTOs;

/// <summary>
/// DTO for recording material usage on an order
/// </summary>
public class RecordMaterialUsageDto
{
    /// <summary>
    /// Material ID (required)
    /// </summary>
    public Guid MaterialId { get; set; }

    /// <summary>
    /// Serial number (required for serialised materials, null for non-serialised)
    /// </summary>
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Serialised item ID (if material is already in system)
    /// </summary>
    public Guid? SerialisedItemId { get; set; }

    /// <summary>
    /// Quantity (required for non-serialised materials, should be 1 for serialised)
    /// </summary>
    public decimal Quantity { get; set; } = 1;

    /// <summary>
    /// Notes (optional)
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Response DTO for recorded material usage
/// </summary>
public class MaterialUsageRecordedDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public Guid? SerialisedItemId { get; set; }
    public string? SerialNumber { get; set; }
    public decimal Quantity { get; set; }
    public DateTime RecordedAt { get; set; }
}

