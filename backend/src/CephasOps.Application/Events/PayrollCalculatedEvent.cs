using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>
/// Emitted when a payroll run is created/calculated (PayrollService.CreatePayrollRunAsync).
/// </summary>
public class PayrollCalculatedEvent : DomainEvent, IHasEntityContext
{
    public Guid PayrollRunId { get; set; }
    public Guid PayrollPeriodId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Draft";

    string? IHasEntityContext.EntityType => "PayrollRun";
    Guid? IHasEntityContext.EntityId => PayrollRunId;

    public PayrollCalculatedEvent()
    {
        EventType = PlatformEventTypes.PayrollCalculated;
        Version = "1";
        Source = "Payroll";
    }
}
