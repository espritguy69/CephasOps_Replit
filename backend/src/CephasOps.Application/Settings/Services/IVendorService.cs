using CephasOps.Application.Settings.DTOs;

namespace CephasOps.Application.Settings.Services;

public interface IVendorService
{
    Task<List<VendorDto>> GetAllAsync(Guid companyId, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<VendorDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VendorDto?> GetByCodeAsync(Guid companyId, string code, CancellationToken cancellationToken = default);
    Task<VendorDto> CreateAsync(Guid companyId, CreateVendorDto dto, CancellationToken cancellationToken = default);
    Task<VendorDto?> UpdateAsync(Guid id, UpdateVendorDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

