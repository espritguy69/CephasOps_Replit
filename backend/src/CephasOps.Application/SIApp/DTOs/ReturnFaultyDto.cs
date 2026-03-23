namespace CephasOps.Application.SIApp.DTOs;

/// <summary>
/// DTO for returning faulty material (standalone)
/// </summary>
public class ReturnFaultyDto
{
    /// <summary>
    /// Serial number (for serialised materials)
    /// </summary>
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Material ID (for non-serialised materials)
    /// </summary>
    public Guid? MaterialId { get; set; }

    /// <summary>
    /// Quantity (for non-serialised materials)
    /// </summary>
    public decimal? Quantity { get; set; }

    /// <summary>
    /// Optional order ID to link this return to an order
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// Reason for return
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Optional notes
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for recording non-serialised replacement
/// </summary>
public class RecordNonSerialisedReplacementDto
{
    /// <summary>
    /// Material ID
    /// </summary>
    public Guid MaterialId { get; set; }

    /// <summary>
    /// Quantity replaced
    /// </summary>
    public decimal QuantityReplaced { get; set; }

    /// <summary>
    /// Replacement reason
    /// </summary>
    public string ReplacementReason { get; set; } = string.Empty;

    /// <summary>
    /// Optional remark
    /// </summary>
    public string? Remark { get; set; }
}

