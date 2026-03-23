namespace CephasOps.Application.SIApp.DTOs;

/// <summary>
/// DTO for material return list item
/// </summary>
public class MaterialReturnDto
{
    public Guid Id { get; set; }
    public Guid? OrderId { get; set; }
    public string? OrderServiceId { get; set; }
    public string? SerialNumber { get; set; }
    public Guid? MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public DateTime ReturnedAt { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty; // "Faulty", "Returned", "RMA Created"
    public Guid? RmaRequestId { get; set; }
    public string? ReplacementReason { get; set; }
    public string ReturnType { get; set; } = string.Empty; // "Faulty", "Replacement", "NonSerialisedReplacement"
}

/// <summary>
/// DTO for material returns query filters
/// </summary>
public class MaterialReturnsQueryDto
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? MaterialId { get; set; }
    public string? Status { get; set; } // "all", "faulty", "returned", "rma"
    public string? ReturnType { get; set; } // "all", "faulty", "replacement", "nonserialised"
}

