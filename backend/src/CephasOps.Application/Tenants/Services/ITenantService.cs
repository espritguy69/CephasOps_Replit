using CephasOps.Application.Tenants.DTOs;

namespace CephasOps.Application.Tenants.Services;

/// <summary>
/// Tenant management service (Phase 11).
/// </summary>
public interface ITenantService
{
    Task<List<TenantDto>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<TenantDto> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);
    Task<TenantDto?> UpdateAsync(Guid id, UpdateTenantRequest request, CancellationToken cancellationToken = default);
}
