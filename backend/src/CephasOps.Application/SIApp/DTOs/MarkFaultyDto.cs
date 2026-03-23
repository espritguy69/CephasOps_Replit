namespace CephasOps.Application.SIApp.DTOs;

/// <summary>
/// DTO for marking a device as faulty
/// </summary>
public class MarkFaultyDto
{
    /// <summary>
    /// Serial number of the faulty device
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Reason for marking as faulty
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Optional notes
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Response DTO for marking device as faulty
/// </summary>
public class MarkFaultyResponseDto
{
    public Guid SerialisedItemId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public Guid? StockMovementId { get; set; }
    public Guid? RmaRequestId { get; set; }
    public string Message { get; set; } = string.Empty;
}

