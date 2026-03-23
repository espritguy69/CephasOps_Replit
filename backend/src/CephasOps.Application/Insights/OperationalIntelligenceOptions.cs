namespace CephasOps.Application.Insights;

/// <summary>Configurable thresholds for operational intelligence rules. All queries are read-only and tenant-scoped.</summary>
public class OperationalIntelligenceOptions
{
    public const string SectionName = "OperationalIntelligence";

    /// <summary>Hours without status update after which an assigned order is considered stuck.</summary>
    public int StuckOrderThresholdHours { get; set; } = 4;

    /// <summary>Percentage of stuck threshold elapsed with no activity to flag "likely stuck soon".</summary>
    public double LikelyStuckSoonPercentOfThreshold { get; set; } = 0.75;

    /// <summary>Number of material replacements on an order to flag as replacement-heavy.</summary>
    public int ReplacementHeavyPerOrderThreshold { get; set; } = 2;

    /// <summary>Replacements in last N days for installer/building to flag.</summary>
    public int ReplacementLookbackDays { get; set; } = 30;

    /// <summary>Replacements count on installer's orders in lookback to flag installer.</summary>
    public int InstallerReplacementCountThreshold { get; set; } = 5;

    /// <summary>Blockers on installer's orders in lookback window to flag.</summary>
    public int InstallerBlockerCountThreshold { get; set; } = 3;

    /// <summary>Last N assigned orders to compute installer issue ratio.</summary>
    public int InstallerPeerWindowSize { get; set; } = 20;

    /// <summary>Issue ratio (blockers + replacements) above this vs peer baseline to flag installer.</summary>
    public double InstallerIssueRatioThreshold { get; set; } = 0.25;

    /// <summary>Reschedule count on order to flag as reschedule-heavy.</summary>
    public int RescheduleHeavyThreshold { get; set; } = 2;

    /// <summary>Blockers on order to flag as blocker-accumulation.</summary>
    public int OrderBlockerCountThreshold { get; set; } = 2;

    /// <summary>Hours with no order activity (last status log) to flag silent order.</summary>
    public int SilentOrderThresholdHours { get; set; } = 6;

    /// <summary>Recent orders at same building with issues to flag building.</summary>
    public int BuildingRecurrenceThreshold { get; set; } = 3;

    /// <summary>Replacements at same building in lookback to flag building.</summary>
    public int BuildingReplacementThreshold { get; set; } = 4;

    /// <summary>Tenant: stuck orders count to raise tenant risk.</summary>
    public int TenantStuckOrdersAnomalyThreshold { get; set; } = 5;

    /// <summary>Tenant: replacement ratio (replacements / completed orders) in month to flag.</summary>
    public double TenantAbnormalReplacementRatioThreshold { get; set; } = 0.15;

    /// <summary>SLA: percent of SLA duration elapsed to flag "nearing breach".</summary>
    public double SlaNearingBreachPercent { get; set; } = 0.85;

    /// <summary>Max orders to return per list (orders-at-risk, installers-at-risk, buildings-at-risk).</summary>
    public int MaxResultsPerList { get; set; } = 50;
}
