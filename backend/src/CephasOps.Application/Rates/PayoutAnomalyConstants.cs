namespace CephasOps.Application.Rates;

/// <summary>
/// Anomaly type and severity constants for payout anomaly detection. Read-only monitoring only.
/// </summary>
public static class PayoutAnomalyTypes
{
    public const string HighPayoutVsPeer = "HighPayoutVsPeer";
    public const string ExcessiveCustomOverride = "ExcessiveCustomOverride";
    public const string ExcessiveLegacyFallback = "ExcessiveLegacyFallback";
    public const string RepeatedWarnings = "RepeatedWarnings";
    public const string ZeroPayout = "ZeroPayout";
    public const string NegativeMarginCluster = "NegativeMarginCluster";
    public const string InstallerDeviation = "InstallerDeviation";
}

/// <summary>
/// Severity for support/finance triage.
/// </summary>
public static class PayoutAnomalySeverity
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
}

/// <summary>
/// Review status for payout anomaly governance (operational metadata only).
/// </summary>
public static class PayoutAnomalyReviewStatus
{
    public const string Open = "Open";
    public const string Acknowledged = "Acknowledged";
    public const string Investigating = "Investigating";
    public const string Resolved = "Resolved";
    public const string FalsePositive = "FalsePositive";
}

/// <summary>
/// Alert delivery status for payout anomaly alerts (tracking only).
/// </summary>
public static class PayoutAnomalyAlertStatus
{
    public const string Sent = "Sent";
    public const string Failed = "Failed";
    public const string Pending = "Pending";
}

/// <summary>
/// Alert channel names for payout anomaly alerts.
/// </summary>
public static class PayoutAnomalyAlertChannel
{
    public const string Email = "Email";
    public const string Slack = "Slack";
    public const string Telegram = "Telegram";
}

/// <summary>
/// Default thresholds for anomaly rules. Documented for support; no automatic payroll action.
/// </summary>
public static class PayoutAnomalyThresholds
{
    /// <summary>High payout: flag when payout > (PeerAverage * this).</summary>
    public const double HighPayoutMultipleOfPeer = 2.0;

    /// <summary>Excessive custom overrides: max count per installer in 30 days.</summary>
    public const int ExcessiveCustomOverrideCount = 5;

    /// <summary>Excessive legacy fallback: max count per context (CompanyId+OrderTypeId) in 30 days.</summary>
    public const int ExcessiveLegacyFallbackCount = 10;

    /// <summary>Repeated warnings: max count per installer in 30 days.</summary>
    public const int RepeatedWarningsCount = 3;

    /// <summary>Negative margin cluster: max count per context in 30 days.</summary>
    public const int NegativeMarginClusterCount = 3;

    /// <summary>Installer deviation: flag when installer avg > (PeerAverage * (1 + this)). E.g. 0.5 = 50% above.</summary>
    public const double InstallerDeviationAbovePeerPercent = 0.5;

    /// <summary>Lookback days for time-windowed rules.</summary>
    public const int LookbackDays = 30;
}
