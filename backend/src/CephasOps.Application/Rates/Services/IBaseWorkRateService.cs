using CephasOps.Application.Rates.DTOs;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Base work rate CRUD and list (Phase 2 — no impact on payout resolution).
/// </summary>
public interface IBaseWorkRateService
{
    Task<List<BaseWorkRateDto>> ListAsync(Guid? companyId, BaseWorkRateListFilter? filter, CancellationToken cancellationToken = default);
    Task<BaseWorkRateDto?> GetByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<BaseWorkRateDto> CreateAsync(CreateBaseWorkRateDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<BaseWorkRateDto> UpdateAsync(Guid id, UpdateBaseWorkRateDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
}
