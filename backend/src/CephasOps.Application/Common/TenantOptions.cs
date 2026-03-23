namespace CephasOps.Application.Common;

/// <summary>
/// Configuration for tenant resolution (SaaS multi-tenant).
/// When JWT has no company_id, this default is used (e.g. legacy Cephas company).
/// </summary>
public class TenantOptions
{
    public const string SectionName = "Tenant";

    /// <summary>
    /// Default company (tenant) id when request has no company context.
    /// Set to the default tenant company id (e.g. Cephas) after running AddMultiTenantArchitecture migration.
    /// </summary>
    public Guid? DefaultCompanyId { get; set; }
}
