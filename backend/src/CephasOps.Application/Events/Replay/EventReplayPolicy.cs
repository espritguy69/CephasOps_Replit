using CephasOps.Application.Events;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Default replay policy: allow replay for safe, idempotent event types only.
/// WorkflowTransitionCompleted is allowed (handlers are logging/notification; idempotent).
/// </summary>
public class EventReplayPolicy : IEventReplayPolicy
{
    private static readonly HashSet<string> AllowedForReplay = new(StringComparer.OrdinalIgnoreCase)
    {
        "WorkflowTransitionCompleted", // Handlers are log/audit; idempotent
        PlatformEventTypes.WorkflowTransitionCompleted,
        "OrderStatusChanged", // OrderLifecycle ledger; idempotent by (SourceEventId, Family)
        PlatformEventTypes.OrderStatusChanged,
        "OrderAssigned",
        PlatformEventTypes.OrderAssigned,
        "OrderCreated", // Forwarding/projections; idempotent
        PlatformEventTypes.OrderCreated,
        "OrderCompleted",
        PlatformEventTypes.OrderCompleted,
        "InvoiceGenerated",
        PlatformEventTypes.InvoiceGenerated,
        "MaterialIssued",
        PlatformEventTypes.MaterialIssued,
        "MaterialReturned",
        PlatformEventTypes.MaterialReturned,
        "PayrollCalculated",
        PlatformEventTypes.PayrollCalculated
    };

    private static readonly HashSet<string> BlockedForReplay = new(StringComparer.OrdinalIgnoreCase)
    {
        // Add destructive or non-idempotent types here when they exist
    };

    public bool IsReplayAllowed(string eventType)
    {
        if (string.IsNullOrEmpty(eventType)) return false;
        return AllowedForReplay.Contains(eventType.Trim());
    }

    public bool IsReplayBlocked(string eventType)
    {
        if (string.IsNullOrEmpty(eventType)) return true;
        return BlockedForReplay.Contains(eventType.Trim()) || !AllowedForReplay.Contains(eventType.Trim());
    }
}
