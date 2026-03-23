using CephasOps.Application.Settings.DTOs;

namespace CephasOps.Application.Settings.Services;

public interface IPaymentTermService
{
    Task<List<PaymentTermDto>> GetAllAsync(Guid companyId, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<PaymentTermDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentTermDto?> GetByCodeAsync(Guid companyId, string code, CancellationToken cancellationToken = default);
    Task<PaymentTermDto> CreateAsync(Guid companyId, CreatePaymentTermDto dto, CancellationToken cancellationToken = default);
    Task<PaymentTermDto?> UpdateAsync(Guid id, UpdatePaymentTermDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

