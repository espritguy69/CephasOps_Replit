using CephasOps.Application.Events.DTOs;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Policy for batch/operational replay. Stricter than single-event replay: default deny, max window, max count, blocked companies, destructive types.
/// </summary>
public interface IOperationalReplayPolicy
{
    /// <summary>
    /// Check if an event is eligible for operational (batch) replay. Uses single-event policy and adds window, company, and destructive checks.
    /// </summary>
    OperationalReplayEligibility CheckEligibility(ReplayEligibilityInputDto entry, ReplayRequestDto request, DateTime utcNow);

    /// <summary>
    /// Maximum age of events (days from utcNow) allowed for operational replay. Null = use default (e.g. 30).
    /// </summary>
    int? MaxReplayWindowDays { get; }

    /// <summary>
    /// Maximum number of events allowed per replay request. Null = use default (e.g. 1000).
    /// </summary>
    int? MaxReplayCountPerRequest { get; }

    /// <summary>
    /// True if the event type is considered destructive (never allowed for operational replay).
    /// </summary>
    bool IsDestructiveEventType(string eventType);

    /// <summary>
    /// True if the company is blocked from operational replay (e.g. high-risk tenants).
    /// </summary>
    bool IsCompanyBlocked(Guid? companyId);
}

/// <summary>
/// Result of an eligibility check for one event.
/// </summary>
public class OperationalReplayEligibility
{
    public bool Eligible { get; set; }
    public string? BlockedReason { get; set; }
}
