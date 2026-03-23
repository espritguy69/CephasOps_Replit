namespace CephasOps.Application.SaaS;

/// <summary>
/// Well-known feature keys for use with IFeatureFlagService. Add to BillingPlanFeatures or TenantFeatureFlags as needed.
/// </summary>
public static class FeatureFlagKeys
{
    /// <summary>Automation engine (e.g. OrderCompleted → GenerateInvoice).</summary>
    public const string Automation = "Automation";

    /// <summary>Advanced reports and exports.</summary>
    public const string Reports = "Reports";

    /// <summary>Multi-department / multi-company scope.</summary>
    public const string MultiDepartment = "MultiDepartment";

    /// <summary>Operational insights (field ops intelligence) query and dashboards.</summary>
    public const string OperationalInsights = "OperationalInsights";

    /// <summary>Integration connectors and outbound delivery.</summary>
    public const string Integration = "Integration";

    /// <summary>Event replay and rebuild operations.</summary>
    public const string EventReplay = "EventReplay";
}
