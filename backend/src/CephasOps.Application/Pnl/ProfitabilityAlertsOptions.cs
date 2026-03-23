namespace CephasOps.Application.Pnl;

/// <summary>
/// Configuration for profitability / financial alerts. Bind from "ProfitabilityAlerts" in appsettings.
/// </summary>
public class ProfitabilityAlertsOptions
{
    public const string SectionName = "ProfitabilityAlerts";

    /// <summary>
    /// Margin percent below which LOW_MARGIN alert is raised. Default 10.
    /// </summary>
    public decimal LowMarginThresholdPercent { get; set; } = 10;
}
