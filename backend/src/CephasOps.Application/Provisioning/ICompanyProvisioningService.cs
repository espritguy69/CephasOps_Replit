using CephasOps.Application.Provisioning.DTOs;

namespace CephasOps.Application.Provisioning;

/// <summary>Provisions a new tenant: company, subscription link, default departments, tenant admin user. Platform admin only.</summary>
public interface ICompanyProvisioningService
{
    /// <summary>Provisions a new tenant. Validates code/slug/email uniqueness. Runs in a single transaction where possible.</summary>
    Task<ProvisionTenantResultDto> ProvisionAsync(ProvisionTenantRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Returns whether the given company code is already in use.</summary>
    Task<bool> IsCompanyCodeInUseAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Returns whether the given slug is already in use.</summary>
    Task<bool> IsSlugInUseAsync(string slug, CancellationToken cancellationToken = default);
}
