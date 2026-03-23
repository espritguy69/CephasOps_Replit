namespace CephasOps.Application.RMA.DTOs;

/// <summary>
/// RMA request DTO
/// </summary>
public class RmaRequestDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public Guid PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string? RmaNumber { get; set; }
    public DateTime RequestDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? MraDocumentId { get; set; }
    public List<RmaRequestItemDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// RMA request item DTO
/// </summary>
public class RmaRequestItemDto
{
    public Guid Id { get; set; }
    public Guid SerialisedItemId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public Guid? OriginalOrderId { get; set; }
    public string? Notes { get; set; }
    public string? Result { get; set; }
}

/// <summary>
/// Create RMA request DTO
/// </summary>
public class CreateRmaRequestDto
{
    public Guid PartnerId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<CreateRmaRequestItemDto> Items { get; set; } = new();
    public Guid? OrderId { get; set; }
}

/// <summary>
/// Create RMA request item DTO
/// </summary>
public class CreateRmaRequestItemDto
{
    public Guid SerialisedItemId { get; set; }
    public Guid? OriginalOrderId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Update RMA request DTO
/// </summary>
public class UpdateRmaRequestDto
{
    public string? Status { get; set; }
    public string? RmaNumber { get; set; }
    public Guid? MraDocumentId { get; set; }
}

