namespace CephasOps.Application.Rates;

/// <summary>
/// Configuration for payout anomaly alerting (email and future channels). Read-only alerting only.
/// </summary>
public class PayoutAnomalyAlertOptions
{
    public const string SectionName = "PayoutAnomalyAlert";

    /// <summary>Email account ID to use for sending alert emails. If not set, first active account is used.</summary>
    public Guid? EmailAccountId { get; set; }

    /// <summary>Default recipient emails (comma-separated) when not overridden by API request.</summary>
    public string DefaultRecipientEmails { get; set; } = "";

    /// <summary>Hours within which we do not re-alert the same anomaly on the same channel (duplicate prevention).</summary>
    public int DuplicatePreventionHours { get; set; } = 24;

    /// <summary>Include repeated Medium-severity anomalies (same type/context) in alert run when true.</summary>
    public bool IncludeMediumRepeated { get; set; } = false;

    // --- Scheduled runs ---

    /// <summary>When true, background scheduler runs anomaly alerting on IntervalHours.</summary>
    public bool SchedulerEnabled { get; set; } = false;

    /// <summary>Interval in hours between scheduled alert runs (e.g. 6 or 24).</summary>
    public int SchedulerIntervalHours { get; set; } = 6;

    /// <summary>Hours after last alert with no review action (open/unacknowledged) to consider anomaly "stale".</summary>
    public int StaleThresholdHours { get; set; } = 24;
}
