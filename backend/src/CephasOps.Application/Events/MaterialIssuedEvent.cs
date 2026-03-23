using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>
/// Emitted when material is issued/recorded for an order (e.g. OrderMaterialUsageService.RecordMaterialUsageAsync).
/// </summary>
public class MaterialIssuedEvent : DomainEvent, IHasEntityContext
{
    public Guid OrderId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid? UsageId { get; set; }
    public decimal Quantity { get; set; }
    public string? SerialNumber { get; set; }

    string? IHasEntityContext.EntityType => "OrderMaterialUsage";
    Guid? IHasEntityContext.EntityId => UsageId ?? OrderId;

    public MaterialIssuedEvent()
    {
        EventType = PlatformEventTypes.MaterialIssued;
        Version = "1";
        Source = "Inventory";
    }
}
