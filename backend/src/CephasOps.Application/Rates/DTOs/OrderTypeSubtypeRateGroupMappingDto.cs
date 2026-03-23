namespace CephasOps.Application.Rates.DTOs;

/// <summary>
/// Order type/subtype to rate group mapping DTO.
/// </summary>
public class OrderTypeSubtypeRateGroupMappingDto
{
    public Guid Id { get; set; }
    public Guid OrderTypeId { get; set; }
    public string? OrderTypeName { get; set; }
    public string? OrderTypeCode { get; set; }
    public Guid? OrderSubtypeId { get; set; }
    public string? OrderSubtypeName { get; set; }
    public string? OrderSubtypeCode { get; set; }
    public Guid RateGroupId { get; set; }
    public string? RateGroupName { get; set; }
    public string? RateGroupCode { get; set; }
    public Guid? CompanyId { get; set; }
}

/// <summary>
/// Assign a rate group to an order type (and optional subtype).
/// </summary>
public class AssignRateGroupToOrderTypeSubtypeDto
{
    public Guid OrderTypeId { get; set; }
    /// <summary>When null, the whole order type maps to the rate group (e.g. Activation).</summary>
    public Guid? OrderSubtypeId { get; set; }
    public Guid RateGroupId { get; set; }
}
