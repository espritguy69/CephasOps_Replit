using CephasOps.Application.Events.DTOs;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Central registry of replay targets. EventStore, Workflow, Projection are supported; Financial and Parser are documented but not fully implemented.
/// </summary>
public sealed class ReplayTargetRegistry : IReplayTargetRegistry
{
    private const string OrderingDesc = "OccurredAtUtc ASC, EventId ASC (event store has no sequence column; tie-breaker is EventId).";

    private static readonly IReadOnlyList<ReplayTargetDescriptorDto> Targets = new List<ReplayTargetDescriptorDto>
    {
        new()
        {
            Id = ReplayTargets.EventStore,
            DisplayName = "Event Store",
            Description = "Replay events from the event store; dispatches to all replay-eligible handlers with side-effect suppression.",
            Supported = true,
            OrderingStrategyId = OrderingStrategies.OccurredAtUtcAscendingEventIdAscending,
            OrderingStrategyDescription = OrderingDesc,
            OrderingGuaranteeLevel = OrderingGuaranteeLevels.StrongDeterministic,
            OrderingDegradedReason = null,
            SupportsPreview = true,
            SupportsApply = true,
            SupportsCheckpoint = true,
            IsReplaySafe = true,
            SupportedFilterNames = new List<string> { "CompanyId", "EventType", "Status", "FromOccurredAtUtc", "ToOccurredAtUtc", "EntityType", "EntityId", "CorrelationId", "MaxEvents" },
            Limitations = new List<string> { "Only policy-allowed event types are replayed; default deny." }
        },
        new()
        {
            Id = ReplayTargets.Workflow,
            DisplayName = "Workflow",
            Description = "Same as Event Store with workflow event focus; use EventType filter (e.g. WorkflowTransitionCompleted).",
            Supported = true,
            OrderingStrategyId = OrderingStrategies.OccurredAtUtcAscendingEventIdAscending,
            OrderingStrategyDescription = OrderingDesc,
            OrderingGuaranteeLevel = OrderingGuaranteeLevels.StrongDeterministic,
            OrderingDegradedReason = null,
            SupportsPreview = true,
            SupportsApply = true,
            SupportsCheckpoint = true,
            IsReplaySafe = true,
            SupportedFilterNames = new List<string> { "CompanyId", "EventType", "Status", "FromOccurredAtUtc", "ToOccurredAtUtc", "EntityType", "EntityId", "CorrelationId", "MaxEvents" },
            Limitations = new List<string> { "Use EventType filter to restrict to workflow events." }
        },
        new()
        {
            Id = ReplayTargets.Projection,
            DisplayName = "Projection",
            Description = "Replay for projection/read-model rebuild only; dispatches only to projection handlers (IProjectionEventHandler). Same event stream and ordering.",
            Supported = true,
            OrderingStrategyId = OrderingStrategies.OccurredAtUtcAscendingEventIdAscending,
            OrderingStrategyDescription = OrderingDesc,
            OrderingGuaranteeLevel = OrderingGuaranteeLevels.StrongDeterministic,
            OrderingDegradedReason = null,
            SupportsPreview = true,
            SupportsApply = true,
            SupportsCheckpoint = true,
            IsReplaySafe = true,
            SupportedFilterNames = new List<string> { "CompanyId", "EventType", "Status", "FromOccurredAtUtc", "ToOccurredAtUtc", "EntityType", "EntityId", "CorrelationId", "MaxEvents" },
            Limitations = new List<string> { "Only handlers implementing IProjectionEventHandler run; other handlers are skipped for this target." }
        },
        new()
        {
            Id = ReplayTargets.Financial,
            DisplayName = "Financial",
            Description = "Financial/payout replay; not implemented as a replay target in Phase 2. Use P&L Rebuild job or dedicated financial APIs.",
            Supported = false,
            OrderingStrategyId = null,
            OrderingStrategyDescription = null,
            OrderingGuaranteeLevel = null,
            OrderingDegradedReason = null,
            SupportsPreview = false,
            SupportsApply = false,
            SupportsCheckpoint = false,
            IsReplaySafe = false,
            SupportedFilterNames = new List<string>(),
            Limitations = new List<string> { "Use PnlRebuild job or RebuildPnlAsync for financial rebuilds; not wired as replay target." }
        },
        new()
        {
            Id = ReplayTargets.Parser,
            DisplayName = "Parser",
            Description = "Parser replay (reprocess attachments/sessions); not implemented as event-store replay. Use ParserReplayService APIs.",
            Supported = false,
            OrderingStrategyId = null,
            OrderingStrategyDescription = null,
            OrderingGuaranteeLevel = null,
            OrderingDegradedReason = null,
            SupportsPreview = false,
            SupportsApply = false,
            SupportsCheckpoint = false,
            IsReplaySafe = false,
            SupportedFilterNames = new List<string>(),
            Limitations = new List<string> { "Use ParserReplayService for attachment/session replay; not event-store based." }
        }
    };

    public IReadOnlyList<ReplayTargetDescriptorDto> GetAll() => Targets;

    public ReplayTargetDescriptorDto? GetById(string targetId)
    {
        if (string.IsNullOrEmpty(targetId)) return null;
        return Targets.FirstOrDefault(t => string.Equals(t.Id, targetId.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public bool IsSupported(string targetId)
    {
        var t = GetById(targetId);
        return t != null && t.Supported;
    }
}
