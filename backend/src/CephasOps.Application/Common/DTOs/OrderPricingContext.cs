namespace CephasOps.Application.Common.DTOs;

/// <summary>
/// Full order-derived context used for pricing and workflow resolution.
/// Centralizes PartnerId, DepartmentId, OrderTypeId, OrderTypeCode (scope), ParentOrderTypeCode,
/// OrderCategoryId, InstallationMethodId, and PartnerGroupId as derivable from an Order.
/// Does not change how billing or rate engines select rows; only provides a single place for derivation.
/// </summary>
public class OrderPricingContext
{
    /// <summary>Partner ID from the order.</summary>
    public Guid? PartnerId { get; set; }

    /// <summary>Department ID from the order.</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Order type ID from the order.</summary>
    public Guid? OrderTypeId { get; set; }

    /// <summary>Order type code for scope/classification: parent's Code when subtype (e.g. MODIFICATION), else own Code (e.g. ACTIVATION).</summary>
    public string? OrderTypeCode { get; set; }

    /// <summary>Parent order type code when order type is a subtype; null when order type is a parent.</summary>
    public string? ParentOrderTypeCode { get; set; }

    /// <summary>Order category ID (FTTH, FTTO, etc.) from the order.</summary>
    public Guid? OrderCategoryId { get; set; }

    /// <summary>Installation method ID from the order.</summary>
    public Guid? InstallationMethodId { get; set; }

    /// <summary>Partner group ID from order.Partner.GroupId when Partner is loaded.</summary>
    public Guid? PartnerGroupId { get; set; }
}
