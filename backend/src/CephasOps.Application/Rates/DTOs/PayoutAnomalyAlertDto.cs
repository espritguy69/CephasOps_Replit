namespace CephasOps.Application.Rates.DTOs;

/// <summary>
/// Request to run payout anomaly alerts (e.g. from cron or manual trigger).
/// </summary>
public class RunPayoutAnomalyAlertsRequestDto
{
    /// <summary>Recipient emails for this run. If empty, options default or configured list is used.</summary>
    public List<string> RecipientEmails { get; set; } = new();

    /// <summary>Include repeated Medium-severity anomalies in this run.</summary>
    public bool? IncludeMediumRepeated { get; set; }
}

/// <summary>
/// Result of a single alert run.
/// </summary>
public class RunPayoutAnomalyAlertsResultDto
{
    public int AnomaliesConsidered { get; set; }
    public int AnomaliesAlerted { get; set; }
    public int AlertsSent { get; set; }
    public int AlertsFailed { get; set; }
    /// <summary>Anomalies skipped due to duplicate prevention (already alerted in window).</summary>
    public int SkippedCount { get; set; }
    public List<string> ChannelsUsed { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Alert response summary: counts of alerted anomalies by review status and stale count. Read-only.
/// </summary>
public class AlertResponseSummaryDto
{
    public int AlertedOpen { get; init; }
    public int AlertedAcknowledged { get; init; }
    public int AlertedInvestigating { get; init; }
    public int AlertedResolved { get; init; }
    public int AlertedFalsePositive { get; init; }
    public int StaleCount { get; init; }
    /// <summary>Average minutes from last alert to first review action, for anomalies that have been acted on.</summary>
    public double? AverageTimeToFirstActionMinutes { get; init; }
}

/// <summary>
/// Single alert run history record for UI and auditing.
/// </summary>
public class PayoutAnomalyAlertRunDto
{
    public Guid Id { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int EvaluatedCount { get; init; }
    public int SentCount { get; init; }
    public int SkippedCount { get; init; }
    public int ErrorCount { get; init; }
    public string TriggerSource { get; init; } = "";
}
