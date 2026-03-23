using CephasOps.Application.Events.DTOs;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Operational replay policy: extends single-event policy with max window, max count, blocked companies, and destructive event types.
/// Default deny; operational replay is stricter than single-event retry.
/// </summary>
public class OperationalReplayPolicy : IOperationalReplayPolicy
{
    private const int DefaultMaxReplayWindowDays = 30;
    private const int DefaultMaxReplayCountPerRequest = 1000;

    private readonly IEventReplayPolicy _singleEventPolicy;

    private static readonly HashSet<string> DestructiveEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Add known destructive/non-idempotent types here. Single-event policy may allow some for retry; operational batch denies.
    };

    private static readonly HashSet<Guid> BlockedCompanyIds = new();
    // Populate via config if needed: e.g. high-risk or opt-out companies.

    public OperationalReplayPolicy(IEventReplayPolicy singleEventPolicy)
    {
        _singleEventPolicy = singleEventPolicy;
    }

    public int? MaxReplayWindowDays => DefaultMaxReplayWindowDays;
    public int? MaxReplayCountPerRequest => DefaultMaxReplayCountPerRequest;

    public OperationalReplayEligibility CheckEligibility(ReplayEligibilityInputDto entry, ReplayRequestDto request, DateTime utcNow)
    {
        if (entry == null)
            return new OperationalReplayEligibility { Eligible = false, BlockedReason = "Event is null." };

        if (!_singleEventPolicy.IsReplayAllowed(entry.EventType))
            return new OperationalReplayEligibility
            {
                Eligible = false,
                BlockedReason = $"Event type '{entry.EventType}' is not allowed for replay (single-event policy)."
            };

        if (IsDestructiveEventType(entry.EventType))
            return new OperationalReplayEligibility
            {
                Eligible = false,
                BlockedReason = $"Event type '{entry.EventType}' is blocked for operational replay (destructive)."
            };

        if (IsCompanyBlocked(entry.CompanyId))
            return new OperationalReplayEligibility
            {
                Eligible = false,
                BlockedReason = "Company is blocked from operational replay."
            };

        var windowDays = MaxReplayWindowDays ?? DefaultMaxReplayWindowDays;
        var cutoff = utcNow.AddDays(-windowDays);
        if (entry.OccurredAtUtc < cutoff)
            return new OperationalReplayEligibility
            {
                Eligible = false,
                BlockedReason = $"Event is older than replay window ({windowDays} days)."
            };

        return new OperationalReplayEligibility { Eligible = true };
    }

    public bool IsDestructiveEventType(string eventType)
    {
        if (string.IsNullOrEmpty(eventType)) return true;
        return DestructiveEventTypes.Contains(eventType.Trim());
    }

    public bool IsCompanyBlocked(Guid? companyId)
    {
        if (!companyId.HasValue) return false;
        return BlockedCompanyIds.Contains(companyId.Value);
    }
}
