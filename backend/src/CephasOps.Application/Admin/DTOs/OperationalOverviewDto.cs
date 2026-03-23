namespace CephasOps.Application.Admin.DTOs;

/// <summary>
/// Compact operational overview for internal monitoring: job executions, event store, payout health, and system health.
/// Not BI or customer-facing; used by operators to see platform guard and workflow failure signals.
/// </summary>
public class OperationalOverviewDto
{
    public DateTime GeneratedAtUtc { get; set; }
    /// <summary>Start of the time window used for event-store metrics (e.g. last 24h).</summary>
    public DateTime WindowStartUtc { get; set; }
    /// <summary>End of the time window for event-store metrics.</summary>
    public DateTime WindowEndUtc { get; set; }

    /// <summary>Job execution queue: pending, running, failed (retry scheduled), dead-letter, succeeded.</summary>
    public OperationalJobExecutionsDto JobExecutions { get; set; } = new();

    /// <summary>Event store (bus) in the window: counts and top failing types/companies.</summary>
    public OperationalEventStoreDto EventStore { get; set; } = new();

    /// <summary>Payout/snapshot health and anomaly summary; latest repair run.</summary>
    public OperationalPayoutHealthDto PayoutHealth { get; set; } = new();

    /// <summary>System health: database and background job runner status.</summary>
    public OperationalSystemHealthDto SystemHealth { get; set; } = new();

    /// <summary>Recent platform guard violations (in-memory; process restart clears). Read-only observability.</summary>
    public OperationalGuardViolationsDto GuardViolations { get; set; } = new();
}

/// <summary>Recent platform guard violations for operations overview. Safe identifiers only.</summary>
public class OperationalGuardViolationsDto
{
    /// <summary>Total violations currently in the in-memory buffer.</summary>
    public int TotalRecorded { get; set; }
    /// <summary>Count by guard name (e.g. TenantSafetyGuard, FinancialIsolationGuard).</summary>
    public List<OperationalGuardCountDto> ByGuard { get; set; } = new();
    /// <summary>Most recent violations, newest first (e.g. last 50).</summary>
    public List<OperationalGuardViolationItemDto> Recent { get; set; } = new();
}

public class OperationalGuardCountDto
{
    public string GuardName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class OperationalGuardViolationItemDto
{
    public DateTime OccurredAtUtc { get; set; }
    public string GuardName { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public Guid? EventId { get; set; }
}

/// <summary>Job execution summary for operations overview.</summary>
public class OperationalJobExecutionsDto
{
    public int PendingCount { get; set; }
    public int RunningCount { get; set; }
    public int FailedRetryScheduledCount { get; set; }
    public int DeadLetterCount { get; set; }
    public int SucceededCount { get; set; }
}

/// <summary>Event store metrics for the overview window.</summary>
public class OperationalEventStoreDto
{
    public int EventsInWindow { get; set; }
    public int ProcessedCount { get; set; }
    public int FailedCount { get; set; }
    public int DeadLetterCount { get; set; }
    public double ProcessedPercent { get; set; }
    public double FailedPercent { get; set; }
    /// <summary>Top failing event types (up to 5).</summary>
    public List<OperationalEventTypeCountDto> TopFailingEventTypes { get; set; } = new();
    /// <summary>Top failing companies by failed+dead-letter count (up to 5).</summary>
    public List<OperationalCompanyEventCountDto> TopFailingCompanies { get; set; } = new();
}

public class OperationalEventTypeCountDto
{
    public string EventType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class OperationalCompanyEventCountDto
{
    public Guid? CompanyId { get; set; }
    public int FailedCount { get; set; }
    public int DeadLetterCount { get; set; }
}

/// <summary>Payout/snapshot health and anomaly summary for operations.</summary>
public class OperationalPayoutHealthDto
{
    public int CompletedWithSnapshot { get; set; }
    public int CompletedMissingSnapshot { get; set; }
    public decimal CoveragePercent { get; set; }
    public int LegacyFallbackCount { get; set; }
    public int ZeroPayoutCount { get; set; }
    public int NegativeMarginCount { get; set; }
    /// <summary>Latest repair run if any (id, started at, error count, trigger).</summary>
    public OperationalRepairRunDto? LatestRepairRun { get; set; }
}

public class OperationalRepairRunDto
{
    public Guid Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalProcessed { get; set; }
    public int CreatedCount { get; set; }
    public int ErrorCount { get; set; }
    public string TriggerSource { get; set; } = string.Empty;
}

/// <summary>Compact system health for operations overview.</summary>
public class OperationalSystemHealthDto
{
    public bool DatabaseConnected { get; set; }
    public string BackgroundJobRunnerStatus { get; set; } = "Unknown";
}
