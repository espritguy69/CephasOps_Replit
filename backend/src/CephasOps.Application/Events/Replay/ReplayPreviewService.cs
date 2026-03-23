using CephasOps.Application.Events.DTOs;
using CephasOps.Application.Events.Ledger;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Dry-run preview: evaluates eligibility for events matching the request and returns counts, sample, and blocked reasons. No handler execution.
/// </summary>
public class ReplayPreviewService : IReplayPreviewService
{
    private const int DefaultMaxEvaluate = 5000;
    private const int SampleSize = 20;

    private readonly IEventStoreQueryService _queryService;
    private readonly IOperationalReplayPolicy _policy;
    private readonly IReplayTargetRegistry _targetRegistry;
    private readonly ReplayMetrics _metrics;

    public ReplayPreviewService(
        IEventStoreQueryService queryService,
        IOperationalReplayPolicy policy,
        IReplayTargetRegistry targetRegistry,
        ReplayMetrics metrics)
    {
        _queryService = queryService;
        _policy = policy;
        _targetRegistry = targetRegistry;
        _metrics = metrics;
    }

    public async Task<ReplayPreviewResultDto> PreviewAsync(ReplayRequestDto request, Guid? scopeCompanyId, CancellationToken cancellationToken = default)
    {
        var targetId = request.ReplayTarget ?? ReplayTargets.EventStore;
        var descriptor = _targetRegistry.GetById(targetId);
        _metrics.RecordPreviewRequest(targetId);

        var maxToEvaluate = request.MaxEvents.HasValue ? Math.Min(request.MaxEvents.Value, DefaultMaxEvaluate) : DefaultMaxEvaluate;
        var safetyCutoff = ReplaySafetyWindow.GetCutoffUtc();
        var (items, totalMatched) = await _queryService.GetEventsForReplayAsync(request, scopeCompanyId, maxToEvaluate, resumeAfterEventId: null, resumeAfterOccurredAtUtc: null, safetyCutoffOccurredAtUtc: safetyCutoff, cancellationToken: cancellationToken);

        var utcNow = DateTime.UtcNow;
        var eligibleCount = 0;
        var blockedReasonsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var companiesSet = new HashSet<Guid?>();
        var eventTypesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entityTypesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            companiesSet.Add(item.CompanyId);
            eventTypesSet.Add(item.EventType);
            if (!string.IsNullOrEmpty(item.EntityType))
                entityTypesSet.Add(item.EntityType);

            var input = new ReplayEligibilityInputDto
            {
                EventId = item.EventId,
                EventType = item.EventType,
                CompanyId = item.CompanyId,
                OccurredAtUtc = item.OccurredAtUtc
            };
            var eligibility = _policy.CheckEligibility(input, request, utcNow);
            if (eligibility.Eligible)
                eligibleCount++;
            else if (!string.IsNullOrEmpty(eligibility.BlockedReason))
                blockedReasonsSet.Add(eligibility.BlockedReason);
        }

        var blockedCount = items.Count - eligibleCount;
        var sample = items.Take(SampleSize).ToList();

        var result = new ReplayPreviewResultDto
        {
            TotalMatched = totalMatched,
            EvaluatedCount = items.Count,
            EligibleCount = eligibleCount,
            BlockedCount = blockedCount,
            BlockedReasons = blockedReasonsSet.OrderBy(x => x).ToList(),
            SampleEvents = sample,
            CompaniesAffected = companiesSet.OrderBy(x => x?.ToString() ?? "").ToList(),
            EventTypesAffected = eventTypesSet.OrderBy(x => x).ToList(),
            ReplayTargetId = targetId,
            OrderingStrategyId = descriptor?.OrderingStrategyId ?? OrderingStrategies.OccurredAtUtcAscendingEventIdAscending,
            OrderingStrategyDescription = descriptor?.OrderingStrategyDescription ?? "OccurredAtUtc ASC, EventId ASC",
            OrderingGuaranteeLevel = descriptor?.OrderingGuaranteeLevel,
            OrderingDegradedReason = descriptor?.OrderingDegradedReason,
            EstimatedAffectedEntityTypes = entityTypesSet.OrderBy(x => x).ToList(),
            Limitations = descriptor?.Limitations?.ToList() ?? new List<string>(),
            SafetyWindowApplied = true,
            SafetyCutoffOccurredAtUtc = safetyCutoff,
            SafetyWindowMinutes = ReplaySafetyWindow.DefaultWindowMinutes
        };

        if (request.ToOccurredAtUtc.HasValue && request.ToOccurredAtUtc.Value > safetyCutoff)
            result.Limitations.Add($"Events with OccurredAtUtc after {safetyCutoff:O} are excluded by the replay safety window ({ReplaySafetyWindow.DefaultWindowMinutes} min). Execution will use the same cutoff.");

        // Projection-capable diff preview: only for Projection target; bounded and honest.
        if (string.Equals(targetId, ReplayTargets.Projection, StringComparison.OrdinalIgnoreCase))
        {
            var categories = new List<string>();
            if (eventTypesSet.Contains("WorkflowTransitionCompleted", StringComparer.OrdinalIgnoreCase))
                categories.Add("WorkflowTransitionHistory");
            result.AffectedProjectionCategories = categories;
            if (categories.Count > 0)
            {
                result.EstimatedChangedEntityCount = eligibleCount;
                result.ProjectionPreviewQuality = ProjectionPreviewQualities.Estimated;
                result.ProjectionPreviewUnavailableReason = null;
            }
            else
            {
                result.EstimatedChangedEntityCount = null;
                result.ProjectionPreviewQuality = ProjectionPreviewQualities.Unavailable;
                result.ProjectionPreviewUnavailableReason = "No projection handlers for the matched event types.";
            }
        }
        else
        {
            result.ProjectionPreviewQuality = ProjectionPreviewQualities.Unavailable;
            result.ProjectionPreviewUnavailableReason = "Not a projection target.";
        }

        // Ledger awareness: which ledger families may be written when replay runs (all supported targets run projection handlers, which include ledger handlers).
        var ledgerFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var et in eventTypesSet)
        {
            if (string.Equals(et, "WorkflowTransitionCompleted", StringComparison.OrdinalIgnoreCase))
                ledgerFamilies.Add(LedgerFamilies.WorkflowTransition);
            else if (string.Equals(et, "OrderStatusChanged", StringComparison.OrdinalIgnoreCase))
                ledgerFamilies.Add(LedgerFamilies.OrderLifecycle);
        }
        result.LedgerFamiliesAffected = ledgerFamilies.OrderBy(x => x).ToList();
        result.LedgerWritesExpected = result.LedgerFamiliesAffected.Count > 0;
        var projectionsImpacted = new List<string>();
        if (ledgerFamilies.Contains(LedgerFamilies.WorkflowTransition))
            projectionsImpacted.Add("WorkflowTransitionTimeline");
        if (ledgerFamilies.Contains(LedgerFamilies.OrderLifecycle))
            projectionsImpacted.Add("OrderTimeline");
        if (ledgerFamilies.Contains(LedgerFamilies.WorkflowTransition) || ledgerFamilies.Contains(LedgerFamilies.OrderLifecycle))
            projectionsImpacted.Add("UnifiedOrderHistory");
        result.LedgerDerivedProjectionsImpacted = projectionsImpacted.Distinct().OrderBy(x => x).ToList();
        if (result.LedgerFamiliesAffected.Count == 0 && eventTypesSet.Count > 0)
            result.LedgerPreviewUnavailableReason = "Matched event types are not mapped to any ledger family.";

        return result;
    }
}
