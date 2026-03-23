using CephasOps.Domain.Common;

namespace CephasOps.Domain.Pnl.Entities;

/// <summary>
/// Persisted financial alert for an order. One row per alert (order can have multiple).
/// Used for dashboard history and GET /api/financial-alerts.
/// </summary>
public class OrderFinancialAlert : CompanyScopedEntity
{
    public Guid OrderId { get; set; }
    public string AlertCode { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal? RevenueAmount { get; set; }
    public decimal? PayoutAmount { get; set; }
    public decimal? ProfitAmount { get; set; }
    public decimal? MarginPercent { get; set; }
    /// <summary>When true, alert is still considered active (e.g. for dashboard). Set false when resolved.</summary>
    public bool IsActive { get; set; } = true;
}
