using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>
/// Emitted when an order reaches OrderCompleted status (physical job done). Used for automation (e.g. GenerateInvoice) and analytics.
/// </summary>
public class OrderCompletedEvent : DomainEvent, IHasEntityContext
{
    public Guid OrderId { get; set; }
    public Guid? WorkflowJobId { get; set; }

    string? IHasEntityContext.EntityType => "Order";
    Guid? IHasEntityContext.EntityId => OrderId;

    public OrderCompletedEvent()
    {
        EventType = PlatformEventTypes.OrderCompleted;
        Version = "1";
        Source = "WorkflowEngine";
    }
}
