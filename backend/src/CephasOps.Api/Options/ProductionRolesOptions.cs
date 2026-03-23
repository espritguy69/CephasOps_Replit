namespace CephasOps.Api.Options;

/// <summary>Production infrastructure: role-based enablement of hosted services. Use to run API-only or worker-only nodes.</summary>
public class ProductionRolesOptions
{
    public const string SectionName = "ProductionRoles";

    /// <summary>When false, hosted services are not registered; use with API-only replicas. Default true (all-in-one).</summary>
    public bool RunJobWorkers { get; set; } = true;
    /// <summary>When false, Guardian (anomaly, drift, performance) is not run. Default true.</summary>
    public bool RunGuardian { get; set; } = true;
    /// <summary>When false, storage lifecycle tier transitions are not run. Default true.</summary>
    public bool RunStorageLifecycle { get; set; } = true;
    /// <summary>When false, tenant metrics aggregation (daily/monthly) is not run. Default true.</summary>
    public bool RunMetricsAggregation { get; set; } = true;
    /// <summary>When false, job execution watchdog (stuck job reset) is not run. Default true.</summary>
    public bool RunWatchdog { get; set; } = true;
    /// <summary>When false, schedulers (email ingest, stock snapshot, ledger, PnL, SLA, payout, etc.) are not run. Default true.</summary>
    public bool RunSchedulers { get; set; } = true;
    /// <summary>When false, event store dispatcher and event bus metrics are not run. Default true.</summary>
    public bool RunEventDispatcher { get; set; } = true;
    /// <summary>When false, notification dispatch and retention workers are not run. Default true.</summary>
    public bool RunNotificationWorkers { get; set; } = true;
    /// <summary>When false, outbound integration retry and event platform retention workers are not run. Default true.</summary>
    public bool RunIntegrationWorkers { get; set; } = true;
    /// <summary>When false, email cleanup hosted service is not run. Default true.</summary>
    public bool RunEmailCleanup { get; set; } = true;
}
