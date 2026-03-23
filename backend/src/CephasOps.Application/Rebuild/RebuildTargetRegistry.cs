namespace CephasOps.Application.Rebuild;

/// <summary>
/// Central registry of rebuildable operational state targets. Phase 1: WorkflowTransitionHistory from Event Store and Ledger.
/// </summary>
public sealed class RebuildTargetRegistry : IRebuildTargetRegistry
{
    private static readonly IReadOnlyList<RebuildTargetDescriptor> Targets = new List<RebuildTargetDescriptor>
    {
        new()
        {
            Id = RebuildTargetIds.WorkflowTransitionHistoryEventStore,
            DisplayName = "Workflow transition history (from Event Store)",
            Description = "Rebuild WorkflowTransitionHistory table from canonical Event Store (WorkflowTransitionCompleted events). Deterministic; replay-safe. Supports checkpoint/resume.",
            SourceOfTruth = RebuildSourceOfTruth.EventStore,
            RebuildStrategy = RebuildStrategies.FullReplace,
            ScopeRuleNames = new[] { "CompanyId", "FromOccurredAtUtc", "ToOccurredAtUtc" },
            OrderingGuarantee = "OccurredAtUtc ASC, EventId ASC",
            IsFullRebuild = true,
            SupportsPreview = true,
            SupportsResume = true,
            Limitations = new List<string> { "Only WorkflowTransitionCompleted events are used." }
        },
        new()
        {
            Id = RebuildTargetIds.WorkflowTransitionHistoryLedger,
            DisplayName = "Workflow transition history (from Event Ledger)",
            Description = "Rebuild WorkflowTransitionHistory table from Event Ledger (WorkflowTransition family). Use when Ledger is source of record or Event Store is unavailable.",
            SourceOfTruth = RebuildSourceOfTruth.EventLedger,
            RebuildStrategy = RebuildStrategies.FullReplace,
            ScopeRuleNames = new[] { "CompanyId", "FromOccurredAtUtc", "ToOccurredAtUtc" },
            OrderingGuarantee = "OccurredAtUtc ASC, Id ASC",
            IsFullRebuild = true,
            SupportsPreview = true,
            SupportsResume = false,
            Limitations = new List<string> { "Ledger must already contain WorkflowTransition entries; no Event Store read. No checkpoint/resume." }
        }
    };

    public IReadOnlyList<RebuildTargetDescriptor> GetAll() => Targets;

    public RebuildTargetDescriptor? GetById(string targetId)
    {
        if (string.IsNullOrEmpty(targetId)) return null;
        return Targets.FirstOrDefault(t => string.Equals(t.Id, targetId.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public bool IsSupported(string targetId)
    {
        var t = GetById(targetId);
        return t != null;
    }
}
