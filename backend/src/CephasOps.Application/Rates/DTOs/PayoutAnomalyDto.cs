namespace CephasOps.Application.Rates.DTOs;

/// <summary>
/// Single anomaly record for support/finance review. Read-only.
/// Id is a stable fingerprint used for governance (acknowledge, assign, resolve, comment).
/// </summary>
public record PayoutAnomalyDto
{
    /// <summary>Stable fingerprint id for this anomaly; use in governance API (acknowledge, assign, etc.).</summary>
    public string Id { get; init; } = "";
    public string AnomalyType { get; init; } = "";
    public string Severity { get; init; } = "Medium";
    public Guid? OrderId { get; init; }
    public Guid? InstallerId { get; init; }
    public string? InstallerName { get; init; }
    public Guid? PayoutSnapshotId { get; init; }
    public decimal? PayoutAmount { get; init; }
    public decimal? BaselineAmount { get; init; }
    public double? DeviationPercent { get; init; }
    public string? PayoutPath { get; init; }
    public Guid? RateGroupId { get; init; }
    public Guid? ServiceProfileId { get; init; }
    public Guid? OrderCategoryId { get; init; }
    public Guid? InstallationMethodId { get; init; }
    public Guid? OrderTypeId { get; init; }
    public Guid? CompanyId { get; init; }
    public DateTime DetectedAt { get; init; }
    public string Reason { get; init; } = "";
    public int? WarningCount { get; init; }
    public int? CustomOverrideCount { get; init; }
    public int? LegacyFallbackCount { get; init; }
    public int? NegativeMarginCount { get; init; }

    /// <summary>Governance: review status if a review exists for this anomaly (Open, Acknowledged, Investigating, Resolved, FalsePositive).</summary>
    public string? ReviewStatus { get; init; }
    /// <summary>Governance: user id assigned to this anomaly's review.</summary>
    public Guid? AssignedToUserId { get; init; }
    /// <summary>Governance: display name of assigned user.</summary>
    public string? AssignedToUserName { get; init; }

    /// <summary>Alerting: whether this anomaly has been sent to an alert channel (e.g. email).</summary>
    public bool Alerted { get; init; }
    /// <summary>Alerting: UTC time of the most recent successful alert for this anomaly.</summary>
    public DateTime? LastAlertedAt { get; init; }
    /// <summary>Response tracking: UTC time of last review action (acknowledge, assign, resolve, comment) if any.</summary>
    public DateTime? LastActionAt { get; init; }
}

/// <summary>
/// Summary counts by anomaly type and severity for anomaly detection dashboard cards.
/// </summary>
public class PayoutAnomalyDetectionSummaryDto
{
    public int HighPayoutVsPeerCount { get; init; }
    public int ExcessiveCustomOverrideCount { get; init; }
    public int ExcessiveLegacyFallbackCount { get; init; }
    public int RepeatedWarningsCount { get; init; }
    public int ZeroPayoutCount { get; init; }
    public int NegativeMarginClusterCount { get; init; }
    public int InstallerDeviationCount { get; init; }
    public int TotalCount { get; init; }
    public int HighSeverityCount { get; init; }
    public int MediumSeverityCount { get; init; }
    public int LowSeverityCount { get; init; }
}

/// <summary>
/// Top cluster row (e.g. installers with most custom override anomalies, contexts with most legacy).
/// </summary>
public class PayoutAnomalyClusterDto
{
    public string ClusterKind { get; init; } = ""; // e.g. "CustomOverrideByInstaller", "LegacyFallbackByContext"
    public string Label { get; init; } = "";       // e.g. installer name or "OrderType X"
    public Guid? EntityId { get; init; }           // InstallerId or null for context
    public string? ContextKey { get; init; }       // e.g. "CompanyId|OrderTypeId"
    public int AnomalyCount { get; init; }
    public int? ExtraCount { get; init; }          // e.g. custom override count in period
}

/// <summary>
/// Filter for anomaly list and summary.
/// </summary>
public class PayoutAnomalyFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? InstallerId { get; set; }
    public string? AnomalyType { get; set; }
    public string? Severity { get; set; }
    public string? PayoutPath { get; set; }
    public Guid? CompanyId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Paged result for anomaly list.
/// </summary>
public class PayoutAnomalyListResultDto
{
    public IReadOnlyList<PayoutAnomalyDto> Items { get; init; } = Array.Empty<PayoutAnomalyDto>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

// --- Anomaly governance (review) ---

/// <summary>
/// Single review record for an anomaly. Operational metadata only.
/// </summary>
public class PayoutAnomalyReviewDto
{
    public Guid Id { get; init; }
    public string AnomalyFingerprintId { get; init; } = "";
    public string AnomalyType { get; init; } = "";
    public Guid? OrderId { get; init; }
    public Guid? InstallerId { get; init; }
    public Guid? PayoutSnapshotId { get; init; }
    public string Severity { get; init; } = "";
    public DateTime DetectedAt { get; init; }
    public string Status { get; init; } = "Open";
    public Guid? AssignedToUserId { get; init; }
    public string? AssignedToUserName { get; init; }
    public string? NotesJson { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Dashboard summary for anomaly governance.
/// </summary>
public class PayoutAnomalyReviewSummaryDto
{
    public int OpenCount { get; init; }
    public int InvestigatingCount { get; init; }
    public int ResolvedTodayCount { get; init; }
}

/// <summary>
/// Request to assign an anomaly review to a user.
/// </summary>
public class AssignAnomalyRequestDto
{
    public Guid? AssignedToUserId { get; set; }
}

/// <summary>
/// Request to add a comment to an anomaly review.
/// </summary>
public class AddAnomalyCommentRequestDto
{
    public string Text { get; set; } = "";
}
