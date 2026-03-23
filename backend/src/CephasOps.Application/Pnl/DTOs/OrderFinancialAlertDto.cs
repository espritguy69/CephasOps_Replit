namespace CephasOps.Application.Pnl.DTOs;

/// <summary>
/// A single financial alert for an order. Computed from profitability result; not persisted by default.
/// </summary>
public class OrderFinancialAlertDto
{
    public Guid OrderId { get; set; }
    public string AlertCode { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal? RevenueAmount { get; set; }
    public decimal? PayoutAmount { get; set; }
    public decimal? ProfitAmount { get; set; }
    public decimal? MarginPercent { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Result of evaluating financial alerts for one order.
/// </summary>
public class OrderFinancialAlertsResultDto
{
    public Guid OrderId { get; set; }
    public List<OrderFinancialAlertDto> Alerts { get; set; } = new();
    public int AlertCount => Alerts.Count;
    public string? HighestSeverity { get; set; }
}

/// <summary>
/// Alert codes for order financial alerts.
/// </summary>
public static class OrderFinancialAlertCodes
{
    public const string NegativeProfit = "NEGATIVE_PROFIT";
    public const string PayoutExceedsRevenue = "PAYOUT_EXCEEDS_REVENUE";
    public const string LowMargin = "LOW_MARGIN";
    public const string NoBillingRateFound = "NO_BILLING_RATE_FOUND";
    public const string NoPayoutRateFound = "NO_PAYOUT_RATE_FOUND";
    public const string OrderCategoryMissing = "ORDER_CATEGORY_MISSING";
    public const string InstallationMethodMissing = "INSTALLATION_METHOD_MISSING";
    public const string OrderTypeInactive = "ORDER_TYPE_INACTIVE";
    public const string ProfitabilityUnresolved = "PROFITABILITY_UNRESOLVED";
}

/// <summary>
/// Severity levels for financial alerts.
/// </summary>
public static class OrderFinancialAlertSeverity
{
    public const string Info = "Info";
    public const string Warning = "Warning";
    public const string Critical = "Critical";
}

/// <summary>
/// Request for bulk financial alerts evaluation.
/// </summary>
public class BulkOrderFinancialAlertsRequest
{
    public List<Guid> OrderIds { get; set; } = new();
    public DateTime? ReferenceDate { get; set; }
}

/// <summary>
/// A persisted financial alert (from store). Includes Id and IsActive for dashboard/API.
/// </summary>
public class PersistedOrderFinancialAlertDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string AlertCode { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal? RevenueAmount { get; set; }
    public decimal? PayoutAmount { get; set; }
    public decimal? ProfitAmount { get; set; }
    public decimal? MarginPercent { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Query parameters for listing persisted financial alerts.
/// </summary>
public class ListOrderFinancialAlertsQuery
{
    public Guid CompanyId { get; set; }
    public Guid? OrderId { get; set; }
    public string? Severity { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public bool ActiveOnly { get; set; } = true;
}

/// <summary>
/// Lightweight per-order summary from persisted active alerts (for order list/detail enrichment).
/// Severity priority: Critical &gt; Warning &gt; Info.
/// </summary>
public class OrderFinancialAlertSummaryDto
{
    public Guid OrderId { get; set; }
    public int ActiveAlertCount { get; set; }
    public string? HighestAlertSeverity { get; set; }
}
