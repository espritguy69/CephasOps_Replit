using CephasOps.Domain.Companies.Enums;

namespace CephasOps.Application.Platform;

public class PlatformTenantListDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool TenantIsActive { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyCode { get; set; }
    public string? CompanyLegalName { get; set; }
    public CompanyStatus? CompanyStatus { get; set; }
    public string? SubscriptionStatus { get; set; }
    public DateTime? TrialEndsAtUtc { get; set; }
    public DateTime? NextBillingDateUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
