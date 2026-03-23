using CephasOps.Application.Companies.DTOs;

namespace CephasOps.Application.Companies.Services;

/// <summary>
/// Partner service interface
/// </summary>
public interface IPartnerService
{
    Task<List<PartnerDto>> GetPartnersAsync(Guid? companyId, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<PartnerDto?> GetPartnerByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<PartnerDto> CreatePartnerAsync(CreatePartnerDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<PartnerDto> UpdatePartnerAsync(Guid id, UpdatePartnerDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeletePartnerAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
}

