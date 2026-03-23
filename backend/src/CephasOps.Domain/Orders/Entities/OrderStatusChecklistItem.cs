using CephasOps.Domain.Common;

namespace CephasOps.Domain.Orders.Entities;

/// <summary>
/// Order status checklist item entity - defines process steps and sub-steps for a status
/// </summary>
public class OrderStatusChecklistItem : CompanyScopedEntity
{
    /// <summary>
    /// Status code this checklist item belongs to (e.g., "Assigned", "MetCustomer")
    /// </summary>
    public string StatusCode { get; set; } = string.Empty;

    /// <summary>
    /// Parent checklist item ID (null for main steps, set for sub-steps)
    /// </summary>
    public Guid? ParentChecklistItemId { get; set; }

    /// <summary>
    /// Step name/title
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Display order (for sorting main steps and sub-steps)
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// Whether this step is required before status transition
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Whether this item is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User ID who created this item
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// User ID who last updated this item
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }

    // Navigation properties
    /// <summary>
    /// Parent checklist item (if this is a sub-step)
    /// </summary>
    public OrderStatusChecklistItem? Parent { get; set; }

    /// <summary>
    /// Child sub-steps (if this is a main step)
    /// </summary>
    public ICollection<OrderStatusChecklistItem> SubSteps { get; set; } = new List<OrderStatusChecklistItem>();

    /// <summary>
    /// Answers provided for this checklist item
    /// </summary>
    public ICollection<OrderStatusChecklistAnswer> Answers { get; set; } = new List<OrderStatusChecklistAnswer>();
}

