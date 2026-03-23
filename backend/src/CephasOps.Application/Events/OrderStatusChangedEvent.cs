using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>
/// Emitted when an order's status changes at a lifecycle transition point.
/// Currently published from the workflow engine when a workflow transition completes for an Order entity.
/// Enables order lifecycle ledger and timeline projections.
/// </summary>
public class OrderStatusChangedEvent : DomainEvent, IHasEntityContext
{
    /// <summary>Order that changed status.</summary>
    public Guid OrderId { get; set; }
    /// <summary>Status before the transition.</summary>
    public string? PreviousStatus { get; set; }
    /// <summary>Status after the transition.</summary>
    public string NewStatus { get; set; } = string.Empty;

    string? IHasEntityContext.EntityType => "Order";
    Guid? IHasEntityContext.EntityId => OrderId;

    public OrderStatusChangedEvent()
    {
        EventType = PlatformEventTypes.OrderStatusChanged;
        Version = "1";
        Source = "WorkflowEngine";
    }
}
