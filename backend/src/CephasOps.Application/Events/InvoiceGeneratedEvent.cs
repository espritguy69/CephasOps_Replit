using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>
/// Emitted when an invoice is created. Used for analytics and integration.
/// </summary>
public class InvoiceGeneratedEvent : DomainEvent, IHasEntityContext
{
    public Guid InvoiceId { get; set; }
    public Guid? PartnerId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Draft";

    string? IHasEntityContext.EntityType => "Invoice";
    Guid? IHasEntityContext.EntityId => InvoiceId;

    public InvoiceGeneratedEvent()
    {
        EventType = PlatformEventTypes.InvoiceGenerated;
        Version = "1";
        Source = "Billing";
    }
}
