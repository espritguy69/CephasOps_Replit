namespace CephasOps.Api.Options;

/// <summary>SaaS scaling: per-tenant API rate limit options. Plan overrides keyed by billing plan code (e.g. Trial, Standard, Enterprise).</summary>
public class TenantRateLimitOptions
{
    public const string SectionName = "SaaS:TenantRateLimit";

    /// <summary>Enable per-tenant rate limiting.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Default requests per minute when plan has no override.</summary>
    public int RequestsPerMinute { get; set; } = 100;

    /// <summary>Default requests per hour when plan has no override.</summary>
    public int RequestsPerHour { get; set; } = 1000;

    /// <summary>Per-plan overrides: key = plan code (e.g. "Trial", "Standard", "Enterprise"), value = requests per minute.</summary>
    public Dictionary<string, int> Plans { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Trial"] = 50,
        ["Standard"] = 100,
        ["Enterprise"] = 500
    };

    /// <summary>Get limit per minute for a plan code; falls back to RequestsPerMinute if plan not in Plans.</summary>
    public int GetRequestsPerMinuteForPlan(string? planCode)
    {
        if (string.IsNullOrEmpty(planCode)) return RequestsPerMinute;
        return Plans.TryGetValue(planCode, out var limit) ? limit : RequestsPerMinute;
    }
}
