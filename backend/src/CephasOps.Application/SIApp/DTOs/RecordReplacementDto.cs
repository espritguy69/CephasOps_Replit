namespace CephasOps.Application.SIApp.DTOs;

/// <summary>
/// DTO for recording material replacement (Assurance orders)
/// </summary>
public class RecordReplacementDto
{
    /// <summary>
    /// Old (faulty) device serial number
    /// </summary>
    public string OldSerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// New (replacement) device serial number
    /// </summary>
    public string NewSerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Replacement reason (e.g., "Faulty ONU", "LOSi", "Customer request")
    /// </summary>
    public string ReplacementReason { get; set; } = string.Empty;

    /// <summary>
    /// Optional notes
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Response DTO for recording replacement
/// </summary>
public class RecordReplacementResponseDto
{
    public Guid ReplacementId { get; set; }
    public string OldSerialNumber { get; set; } = string.Empty;
    public string NewSerialNumber { get; set; } = string.Empty;
    public Guid? OldSerialisedItemId { get; set; }
    public Guid? NewSerialisedItemId { get; set; }
    public Guid? RmaRequestId { get; set; }
    public string Message { get; set; } = string.Empty;
}

