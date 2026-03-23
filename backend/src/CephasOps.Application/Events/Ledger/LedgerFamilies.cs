namespace CephasOps.Application.Events.Ledger;

/// <summary>Ledger family identifiers. Bounded set for operational event ledger.</summary>
public static class LedgerFamilies
{
    /// <summary>Workflow transition completed (from WorkflowTransitionCompletedEvent).</summary>
    public const string WorkflowTransition = "WorkflowTransition";

    /// <summary>Replay operation completed/failed/cancelled (from ReplayOperation completion).</summary>
    public const string ReplayOperationCompleted = "ReplayOperationCompleted";

    /// <summary>Order lifecycle status changes (from OrderStatusChangedEvent).</summary>
    public const string OrderLifecycle = "OrderLifecycle";
}
