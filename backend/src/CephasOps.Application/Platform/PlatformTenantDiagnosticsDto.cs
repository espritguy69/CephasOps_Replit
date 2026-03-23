namespace CephasOps.Application.Platform;

public class PlatformTenantDiagnosticsDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string? CompanyCode { get; set; }
    public string? CompanyStatus { get; set; }
    public int UserCount { get; set; }
    public int OrderCount { get; set; }
    public string? SubscriptionStatus { get; set; }
    public DateTime? TrialEndsAtUtc { get; set; }
    public DateTime? NextBillingDateUtc { get; set; }
}
