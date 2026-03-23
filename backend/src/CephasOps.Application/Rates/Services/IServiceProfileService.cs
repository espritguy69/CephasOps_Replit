using CephasOps.Application.Rates.DTOs;

namespace CephasOps.Application.Rates.Services;

public interface IServiceProfileService
{
    Task<List<ServiceProfileDto>> ListAsync(Guid? companyId, ServiceProfileListFilter? filter, CancellationToken cancellationToken = default);
    Task<ServiceProfileDto?> GetByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<ServiceProfileDto> CreateAsync(CreateServiceProfileDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<ServiceProfileDto> UpdateAsync(Guid id, UpdateServiceProfileDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
}
