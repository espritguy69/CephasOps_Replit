using System.Collections.Concurrent;
using System.Text.Json;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Registry of event type name -> Type for deserializing stored events. Register known event types.
/// </summary>
public class EventTypeRegistry : IEventTypeRegistry
{
    private static readonly ConcurrentDictionary<string, Type> Types = new(StringComparer.OrdinalIgnoreCase);

    static EventTypeRegistry()
    {
        var workflowType = typeof(WorkflowTransitionCompletedEvent);
        var orderStatusType = typeof(OrderStatusChangedEvent);
        var orderAssignedType = typeof(OrderAssignedEvent);
        Register(PlatformEventTypes.WorkflowTransitionCompleted, workflowType);
        Register(PlatformEventTypes.LegacyWorkflowTransitionCompleted, workflowType);
        Register(PlatformEventTypes.OrderStatusChanged, orderStatusType);
        Register(PlatformEventTypes.LegacyOrderStatusChanged, orderStatusType);
        Register(PlatformEventTypes.OrderAssigned, orderAssignedType);
        Register(PlatformEventTypes.LegacyOrderAssigned, orderAssignedType);
        var orderCreatedType = typeof(OrderCreatedEvent);
        Register(PlatformEventTypes.OrderCreated, orderCreatedType);
        Register(PlatformEventTypes.LegacyOrderCreated, orderCreatedType);
        var orderCompletedType = typeof(OrderCompletedEvent);
        Register(PlatformEventTypes.OrderCompleted, orderCompletedType);
        Register(PlatformEventTypes.LegacyOrderCompleted, orderCompletedType);
        var invoiceGeneratedType = typeof(InvoiceGeneratedEvent);
        Register(PlatformEventTypes.InvoiceGenerated, invoiceGeneratedType);
        Register(PlatformEventTypes.LegacyInvoiceGenerated, invoiceGeneratedType);
        Register(PlatformEventTypes.MaterialIssued, typeof(MaterialIssuedEvent));
        Register(PlatformEventTypes.LegacyMaterialIssued, typeof(MaterialIssuedEvent));
        Register(PlatformEventTypes.MaterialReturned, typeof(MaterialReturnedEvent));
        Register(PlatformEventTypes.LegacyMaterialReturned, typeof(MaterialReturnedEvent));
        Register(PlatformEventTypes.PayrollCalculated, typeof(PayrollCalculatedEvent));
        Register(PlatformEventTypes.LegacyPayrollCalculated, typeof(PayrollCalculatedEvent));
        Register(PlatformEventTypes.JobStarted, typeof(JobStartedEvent));
        Register(PlatformEventTypes.JobCompleted, typeof(JobCompletedEvent));
        Register(PlatformEventTypes.JobFailed, typeof(JobFailedEvent));
    }

    public static void Register(string eventTypeName, Type type)
    {
        if (!typeof(CephasOps.Domain.Events.IDomainEvent).IsAssignableFrom(type))
            throw new ArgumentException("Type must implement IDomainEvent", nameof(type));
        Types[eventTypeName?.Trim() ?? ""] = type;
    }

    public Type? GetEventType(string eventTypeName)
    {
        if (string.IsNullOrEmpty(eventTypeName)) return null;
        return Types.TryGetValue(eventTypeName.Trim(), out var t) ? t : null;
    }

    /// <summary>Deserialize payload JSON to IDomainEvent using registered type, or null if type unknown.</summary>
    public CephasOps.Domain.Events.IDomainEvent? Deserialize(string eventTypeName, string payloadJson)
    {
        var type = GetEventType(eventTypeName);
        if (type == null) return null;
        try
        {
            var obj = JsonSerializer.Deserialize(payloadJson, type);
            return obj as CephasOps.Domain.Events.IDomainEvent;
        }
        catch
        {
            return null;
        }
    }
}
