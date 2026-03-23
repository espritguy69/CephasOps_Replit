using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>
/// Emitted when material is returned (e.g. RMA or stock return). Register emission where returns are persisted.
/// </summary>
public class MaterialReturnedEvent : DomainEvent, IHasEntityContext
{
    public Guid? OrderId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public string? SerialNumber { get; set; }
    public string? ReturnReason { get; set; }

    string? IHasEntityContext.EntityType => "MaterialReturn";
    Guid? IHasEntityContext.EntityId => OrderId;

    public MaterialReturnedEvent()
    {
        EventType = PlatformEventTypes.MaterialReturned;
        Version = "1";
        Source = "Inventory";
    }
}
