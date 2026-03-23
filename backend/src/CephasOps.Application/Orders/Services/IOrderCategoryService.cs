using CephasOps.Application.Orders.DTOs;

namespace CephasOps.Application.Orders.Services;

/// <summary>
/// OrderCategory service interface
/// Previously known as IInstallationTypeService but renamed for clarity.
/// </summary>
public interface IOrderCategoryService
{
    Task<List<OrderCategoryDto>> GetOrderCategoriesAsync(Guid? companyId, Guid? departmentId = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<OrderCategoryDto?> GetOrderCategoryByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<OrderCategoryDto> CreateOrderCategoryAsync(CreateOrderCategoryDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<OrderCategoryDto> UpdateOrderCategoryAsync(Guid id, UpdateOrderCategoryDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteOrderCategoryAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
}

