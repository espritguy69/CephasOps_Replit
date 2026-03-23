using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>
/// Emitted when an order is created (manual or from parsed draft). Drives downstream consumers (e.g. integrations, analytics).
/// </summary>
public class OrderCreatedEvent : DomainEvent, IHasEntityContext
{
    public Guid OrderId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? BuildingId { get; set; }
    public string? SourceSystem { get; set; }

    string? IHasEntityContext.EntityType => "Order";
    Guid? IHasEntityContext.EntityId => OrderId;

    public OrderCreatedEvent()
    {
        EventType = PlatformEventTypes.OrderCreated;
        Version = "1";
        Source = "Orders";
    }
}
