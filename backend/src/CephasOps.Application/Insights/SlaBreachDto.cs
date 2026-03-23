namespace CephasOps.Application.Insights;

/// <summary>SLA breach state for an order. NoSla = no due time; OnTrack = due in future and not nearing; NearingBreach = due within threshold; Breached = past due.</summary>
public static class SlaBreachState
{
    public const string NoSla = "NoSla";
    public const string OnTrack = "OnTrack";
    public const string NearingBreach = "NearingBreach";
    public const string Breached = "Breached";
}

/// <summary>Single order with SLA state, due/overdue minutes, and explainable reason.</summary>
public class SlaBreachOrderItemDto
{
    public Guid OrderId { get; set; }
    public string? OrderRef { get; set; }
    public Guid CompanyId { get; set; }
    public string? CurrentStatus { get; set; }
    public Guid? AssignedSiId { get; set; }
    public DateTime? KpiDueAt { get; set; }
    public DateTime NowUtc { get; set; }
    /// <summary>When due in future: minutes until due. When overdue: negative (e.g. -95). Null when NoSla.</summary>
    public int? MinutesToDueOrOverdue { get; set; }
    public string BreachState { get; set; } = SlaBreachState.NoSla;
    public string Severity { get; set; } = "Info";
    public string Explanation { get; set; } = string.Empty;
    public Guid? RelatedSlaProfileId { get; set; }
    public string? RelatedSlaProfileName { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public bool HasBlocker { get; set; }
    public bool HasReplacement { get; set; }
    public bool HasReschedule { get; set; }
}

/// <summary>Counts per breach state for tenant or platform.</summary>
public class SlaBreachDistributionDto
{
    public int OnTrackCount { get; set; }
    public int NearingBreachCount { get; set; }
    public int BreachedCount { get; set; }
    public int NoSlaCount { get; set; }
}

/// <summary>Tenant-scoped SLA summary: distribution and generated time.</summary>
public class SlaBreachSummaryDto
{
    public SlaBreachDistributionDto Distribution { get; set; } = new();
    public DateTime GeneratedAtUtc { get; set; }
}
