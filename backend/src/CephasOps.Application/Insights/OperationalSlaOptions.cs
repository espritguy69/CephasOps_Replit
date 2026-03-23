namespace CephasOps.Application.Insights;

/// <summary>Configurable thresholds for SLA breach engine. Order-level breach uses Order.KpiDueAt as authoritative due time.</summary>
public class OperationalSlaOptions
{
    public const string SectionName = "OperationalSla";

    /// <summary>Order is "nearing breach" when due within this many minutes (default 30).</summary>
    public int NearingBreachMinutes { get; set; } = 30;

    /// <summary>Order is "breached" when current time is past KpiDueAt. Severity: Warning when overdue &lt; this; Critical when overdue >= this (default 120 minutes).</summary>
    public int BreachedCriticalOverdueMinutes { get; set; } = 120;

    /// <summary>Max orders to return in orders-at-risk list (default 100).</summary>
    public int MaxOrdersAtRisk { get; set; } = 100;
}
