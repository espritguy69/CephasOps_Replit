using CephasOps.Application.Rates.DTOs;

namespace CephasOps.Application.Rates.Services;

public interface IOrderCategoryServiceProfileService
{
    Task<List<OrderCategoryServiceProfileDto>> ListAsync(Guid? companyId, OrderCategoryServiceProfileListFilter? filter, CancellationToken cancellationToken = default);
    Task<OrderCategoryServiceProfileDto?> GetByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<OrderCategoryServiceProfileDto> CreateAsync(CreateOrderCategoryServiceProfileDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
}
