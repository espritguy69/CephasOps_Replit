using CephasOps.Application.Orders.DTOs;

namespace CephasOps.Application.Orders.Services;

/// <summary>
/// OrderType service interface (parent/subtype hierarchy)
/// </summary>
public interface IOrderTypeService
{
    Task<List<OrderTypeDto>> GetOrderTypesAsync(Guid? companyId, Guid? departmentId = null, bool? isActive = null, bool parentsOnly = false, CancellationToken cancellationToken = default);
    Task<List<OrderTypeDto>> GetSubtypesAsync(Guid parentId, Guid? companyId, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<OrderTypeDto?> GetOrderTypeByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<OrderTypeDto?> GetOrderTypeByCodeAsync(Guid? companyId, Guid? departmentId, string code, Guid? parentOrderTypeId, CancellationToken cancellationToken = default);
    Task<OrderTypeDto> CreateOrderTypeAsync(CreateOrderTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<OrderTypeDto> UpdateOrderTypeAsync(Guid id, UpdateOrderTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteOrderTypeAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
}

