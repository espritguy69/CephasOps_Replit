using CephasOps.Application.Settings.DTOs;

namespace CephasOps.Application.Settings.Services;

public interface ITaxCodeService
{
    Task<List<TaxCodeDto>> GetAllAsync(Guid companyId, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<TaxCodeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaxCodeDto?> GetByCodeAsync(Guid companyId, string code, CancellationToken cancellationToken = default);
    Task<TaxCodeDto> CreateAsync(Guid companyId, CreateTaxCodeDto dto, CancellationToken cancellationToken = default);
    Task<TaxCodeDto?> UpdateAsync(Guid id, UpdateTaxCodeDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

