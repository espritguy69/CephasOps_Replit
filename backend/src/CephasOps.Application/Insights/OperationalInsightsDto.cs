namespace CephasOps.Application.Insights;

/// <summary>Platform Health Dashboard — platform admin only.</summary>
public class PlatformHealthDto
{
    public int ActiveTenants { get; set; }
    public int OrdersToday { get; set; }
    public double CompletionRate { get; set; }
    public double? AvgCompletionTimeHours { get; set; }
    public int FailedOrders { get; set; }
    public IReadOnlyList<TenantHealthDistributionItemDto> TenantHealthDistribution { get; set; } = new List<TenantHealthDistributionItemDto>();
    /// <summary>SRE: events processed (last 24h).</summary>
    public int EventsProcessed { get; set; }
    /// <summary>SRE: event processing failures (last 24h).</summary>
    public int EventFailures { get; set; }
    /// <summary>SRE: events in retry queue (Failed with NextRetryAtUtc or Pending).</summary>
    public int RetryQueueSize { get; set; }
    /// <summary>SRE: oldest pending event age in seconds (null if no pending).</summary>
    public double? EventLagSeconds { get; set; }
}

public class TenantHealthDistributionItemDto
{
    public string Status { get; set; } = string.Empty; // Healthy, Warning, Critical
    public int Count { get; set; }
}

/// <summary>Tenant Performance Dashboard — tenant scoped.</summary>
public class TenantPerformanceDto
{
    public int OrdersThisMonth { get; set; }
    public double CompletionRate { get; set; }
    public double? AvgInstallTimeHours { get; set; }
    public int ActiveInstallers { get; set; }
    public int DeviceReplacements { get; set; }
    /// <summary>Orders completed within SLA (no breach in period).</summary>
    public int OrdersCompletedWithinSla { get; set; }
    /// <summary>Orders that breached SLA in the period.</summary>
    public int OrdersBreachedSla { get; set; }
    /// <summary>Average installer response time (assignment to first SI action) in hours.</summary>
    public double? InstallerResponseTimeHours { get; set; }
}

/// <summary>Operations Control Dashboard — tenant scoped.</summary>
public class OperationsControlDto
{
    public int OrdersAssignedToday { get; set; }
    public int OrdersCompletedToday { get; set; }
    public int InstallersActive { get; set; }
    public int StuckOrders { get; set; }
    public IReadOnlyList<StuckOrderItemDto> StuckOrdersList { get; set; } = new List<StuckOrderItemDto>();
    public int Exceptions { get; set; }
    /// <summary>Average install time (assignment to completion) in hours for recent completed orders.</summary>
    public double? AvgInstallTimeHours { get; set; }
    /// <summary>Orders completed within SLA today.</summary>
    public int OrdersCompletedWithinSlaToday { get; set; }
    /// <summary>Orders that breached SLA today.</summary>
    public int OrdersBreachedSlaToday { get; set; }
}

public class StuckOrderItemDto
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? AssignedSiId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

/// <summary>Financial Overview Dashboard — tenant scoped.</summary>
public class FinancialOverviewDto
{
    public decimal RevenueToday { get; set; }
    public decimal RevenueMonth { get; set; }
    public decimal InstallerPayouts { get; set; }
    public decimal? ProfitMarginPercent { get; set; }
    public decimal PendingPayouts { get; set; }
}

/// <summary>Risk and Quality Dashboard — tenant scoped.</summary>
public class RiskQualityDto
{
    public int CustomerComplaints { get; set; }
    public int DeviceFailures { get; set; }
    public int RescheduledOrders { get; set; }
    public double? InstallerRatingAverage { get; set; }
    public int RepeatCustomerIssues { get; set; }
}
