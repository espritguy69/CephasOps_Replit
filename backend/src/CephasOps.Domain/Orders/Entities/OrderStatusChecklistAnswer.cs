using CephasOps.Domain.Common;

namespace CephasOps.Domain.Orders.Entities;

/// <summary>
/// Order status checklist answer entity - stores Yes/No answers for checklist items per order
/// </summary>
public class OrderStatusChecklistAnswer : CompanyScopedEntity
{
    /// <summary>
    /// Order ID this answer belongs to
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Checklist item ID this answer is for
    /// </summary>
    public Guid ChecklistItemId { get; set; }

    /// <summary>
    /// Answer value (true = Yes, false = No)
    /// </summary>
    public bool Answer { get; set; }

    /// <summary>
    /// Timestamp when the answer was provided
    /// </summary>
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who provided the answer
    /// </summary>
    public Guid AnsweredByUserId { get; set; }

    /// <summary>
    /// Optional remarks/notes
    /// </summary>
    public string? Remarks { get; set; }

    // Navigation properties
    /// <summary>
    /// Order this answer belongs to
    /// </summary>
    public Order? Order { get; set; }

    /// <summary>
    /// Checklist item this answer is for
    /// </summary>
    public OrderStatusChecklistItem? ChecklistItem { get; set; }
}

