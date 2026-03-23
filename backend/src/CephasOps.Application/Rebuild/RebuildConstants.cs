namespace CephasOps.Application.Rebuild;

/// <summary>Rebuild target identifiers. Bounded set for operational state rebuilder.</summary>
public static class RebuildTargetIds
{
    /// <summary>WorkflowTransitionHistory table rebuilt from Event Store (WorkflowTransitionCompleted events).</summary>
    public const string WorkflowTransitionHistoryEventStore = "WorkflowTransitionHistory.EventStore";

    /// <summary>WorkflowTransitionHistory table rebuilt from Event Ledger (WorkflowTransition family).</summary>
    public const string WorkflowTransitionHistoryLedger = "WorkflowTransitionHistory.Ledger";
}

/// <summary>Rebuild strategy: how target state is updated.</summary>
public static class RebuildStrategies
{
    /// <summary>Clear target state in scope, then repopulate from source. Removes rows not in source.</summary>
    public const string FullReplace = "FullReplace";

    /// <summary>Merge source into target; no delete. Idempotent upsert by key.</summary>
    public const string IdempotentUpsert = "IdempotentUpsert";

    /// <summary>Append only; no delete or update. For append-only targets.</summary>
    public const string BoundedAppend = "BoundedAppend";
}

/// <summary>Source of truth for a rebuild target.</summary>
public static class RebuildSourceOfTruth
{
    public const string EventStore = "EventStore";
    public const string EventLedger = "EventLedger";
}

/// <summary>Rebuild operation state.</summary>
public static class RebuildOperationStates
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string PartiallyCompleted = "PartiallyCompleted";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}
