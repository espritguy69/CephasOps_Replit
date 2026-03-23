namespace CephasOps.Application.Pnl.DTOs;

/// <summary>
/// Per-order profitability result.
/// Profit = Revenue (BillingRatecard) - SI Payout (RateEngine) - MaterialCost (placeholder) - OtherCost (future).
/// </summary>
public class OrderProfitabilityDto
{
    public Guid OrderId { get; set; }
    /// <summary>Order service ID or identifier for display.</summary>
    public string? OrderNo { get; set; }
    public string? ServiceId { get; set; }
    public Guid? OrderTypeId { get; set; }
    public Guid? OrderCategoryId { get; set; }
    public Guid? InstallationMethodId { get; set; }

    /// <summary>Billing revenue from BillingRatecard (invoice line resolution).</summary>
    public decimal? RevenueAmount { get; set; }
    /// <summary>SI payout from RateEngineService.</summary>
    public decimal? PayoutAmount { get; set; }
    /// <summary>Material cost — placeholder; 0 until implemented.</summary>
    public decimal MaterialCostAmount { get; set; }
    /// <summary>Other direct cost — optional future extension.</summary>
    public decimal OtherCostAmount { get; set; }

    /// <summary>Profit = Revenue - Payout - MaterialCost - OtherCost. Null if revenue or payout unresolved.</summary>
    public decimal? ProfitAmount { get; set; }
    /// <summary>Margin % when RevenueAmount > 0 and profit is known.</summary>
    public decimal? MarginPercent { get; set; }

    public string? RevenueSource { get; set; }
    public string? PayoutSource { get; set; }

    /// <summary>RESOLVED | PARTIAL | UNRESOLVED.</summary>
    public string Status { get; set; } = "UNRESOLVED";
    /// <summary>Human-readable warning or reason when not fully resolved.</summary>
    public string? Warning { get; set; }
    public List<string> Messages { get; set; } = new();
    public List<string> ReasonCodes { get; set; } = new();
}

/// <summary>
/// Profitability status classification.
/// </summary>
public static class OrderProfitabilityStatus
{
    public const string Resolved = "RESOLVED";
    public const string Partial = "PARTIAL";
    public const string Unresolved = "UNRESOLVED";
}

/// <summary>
/// Reason codes when revenue or payout cannot be resolved.
/// </summary>
public static class OrderProfitabilityReasonCodes
{
    public const string OrderNotFound = "ORDER_NOT_FOUND";
    public const string OrderCategoryMissing = "ORDER_CATEGORY_MISSING";
    public const string NoBillingRatecardFound = "NO_BILLING_RATECARD_FOUND";
    public const string PartnerMissing = "PARTNER_MISSING";
    public const string InvalidOrderType = "INVALID_ORDER_TYPE";

    public const string NoSiRateFound = "NO_SI_RATE_FOUND";
    public const string SiLevelMissing = "SI_LEVEL_MISSING";
    public const string NoAssignedSi = "NO_ASSIGNED_SI";
    public const string PartnerGroupMissing = "PARTNER_GROUP_MISSING";
}

/// <summary>
/// Request for bulk order profitability calculation.
/// </summary>
public class BulkOrderProfitabilityRequest
{
    public List<Guid> OrderIds { get; set; } = new();
    public DateTime? ReferenceDate { get; set; }
}
