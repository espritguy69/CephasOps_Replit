using CephasOps.Domain.Common;

namespace CephasOps.Domain.Orders.Entities;

/// <summary>
/// OrderType entity - supports parent/subtype hierarchy.
/// Parents: ACTIVATION, MODIFICATION, ASSURANCE, VALUE_ADDED_SERVICE.
/// Subtypes: MODIFICATION->INDOOR/OUTDOOR, ASSURANCE->STANDARD/REPULL, VALUE_ADDED_SERVICE->UPGRADE/IAD/FIXED_IP.
/// Orders store a single leaf OrderTypeId (subtype id or parent id when no subtypes).
/// </summary>
public class OrderType : CompanyScopedEntity
{
    public Guid? DepartmentId { get; set; }
    /// <summary>Parent order type id; null for parent rows.</summary>
    public Guid? ParentOrderTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public OrderType? ParentOrderType { get; set; }
    public ICollection<OrderType> Children { get; set; } = new List<OrderType>();
}

