using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>
/// Emitted when an order transitions to Assigned. Drives installer task creation, material pack availability, and SLA kickoff.
/// Triggered only from WorkflowEngineService after successful Pending→Assigned (or equivalent) transition.
/// </summary>
public class OrderAssignedEvent : DomainEvent, IHasEntityContext
{
    public Guid OrderId { get; set; }
    public Guid? WorkflowJobId { get; set; }

    string? IHasEntityContext.EntityType => "Order";
    Guid? IHasEntityContext.EntityId => OrderId;

    public OrderAssignedEvent()
    {
        EventType = PlatformEventTypes.OrderAssigned;
        Version = "1";
        Source = "WorkflowEngine";
    }
}
