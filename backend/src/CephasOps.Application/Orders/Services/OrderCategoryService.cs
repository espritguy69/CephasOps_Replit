using CephasOps.Application.Orders.DTOs;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Orders.Services;

/// <summary>
/// OrderCategory service implementation
/// Previously known as InstallationTypeService but renamed for clarity.
/// </summary>
public class OrderCategoryService : IOrderCategoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderCategoryService> _logger;

    public OrderCategoryService(ApplicationDbContext context, ILogger<OrderCategoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<OrderCategoryDto>> GetOrderCategoriesAsync(Guid? companyId, Guid? departmentId = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<OrderCategoryDto>();

        var query = _context.OrderCategories.Where(oc => oc.CompanyId == effectiveCompanyId.Value);

        if (departmentId.HasValue)
        {
            query = query.Where(oc => oc.DepartmentId == departmentId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(oc => oc.IsActive == isActive.Value);
        }

        var orderCategories = await query
            .OrderBy(oc => oc.DisplayOrder)
            .ThenBy(oc => oc.Name)
            .ToListAsync(cancellationToken);

        return orderCategories.Select(MapToDto).ToList();
    }

    public async Task<OrderCategoryDto?> GetOrderCategoryByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return null;

        var orderCategory = await _context.OrderCategories
            .Where(oc => oc.Id == id && oc.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return orderCategory != null ? MapToDto(orderCategory) : null;
    }

    public async Task<OrderCategoryDto> CreateOrderCategoryAsync(CreateOrderCategoryDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to create an order category.");

        var orderCategory = new OrderCategory
        {
            Id = Guid.NewGuid(),
            CompanyId = effectiveCompanyId.Value,
            DepartmentId = dto.DepartmentId,
            Name = dto.Name,
            Code = dto.Code,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.OrderCategories.Add(orderCategory);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("OrderCategory created: {OrderCategoryId}, Name: {Name}", orderCategory.Id, orderCategory.Name);

        return MapToDto(orderCategory);
    }

    public async Task<OrderCategoryDto> UpdateOrderCategoryAsync(Guid id, UpdateOrderCategoryDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to update an order category.");

        var orderCategory = await _context.OrderCategories
            .Where(oc => oc.Id == id && oc.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (orderCategory == null)
        {
            throw new KeyNotFoundException($"OrderCategory with ID {id} not found");
        }

        if (dto.DepartmentId.HasValue) orderCategory.DepartmentId = dto.DepartmentId;
        if (!string.IsNullOrEmpty(dto.Name)) orderCategory.Name = dto.Name;
        if (!string.IsNullOrEmpty(dto.Code)) orderCategory.Code = dto.Code;
        if (dto.Description != null) orderCategory.Description = dto.Description;
        if (dto.IsActive.HasValue) orderCategory.IsActive = dto.IsActive.Value;
        if (dto.DisplayOrder.HasValue) orderCategory.DisplayOrder = dto.DisplayOrder.Value;
        orderCategory.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("OrderCategory updated: {OrderCategoryId}", id);

        return MapToDto(orderCategory);
    }

    public async Task DeleteOrderCategoryAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to delete an order category.");

        var orderCategory = await _context.OrderCategories
            .Where(oc => oc.Id == id && oc.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (orderCategory == null)
        {
            throw new KeyNotFoundException($"OrderCategory with ID {id} not found");
        }

        // Check if any orders are using this order category
        var hasOrders = await _context.Orders.AnyAsync(o => o.OrderCategoryId == id, cancellationToken);
        if (hasOrders)
        {
            throw new InvalidOperationException($"Cannot delete OrderCategory {id} because it is being used by orders");
        }

        _context.OrderCategories.Remove(orderCategory);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("OrderCategory deleted: {OrderCategoryId}", id);
    }

    private static OrderCategoryDto MapToDto(OrderCategory orderCategory)
    {
        return new OrderCategoryDto
        {
            Id = orderCategory.Id,
            CompanyId = orderCategory.CompanyId,
            DepartmentId = orderCategory.DepartmentId,
            Name = orderCategory.Name,
            Code = orderCategory.Code,
            Description = orderCategory.Description,
            IsActive = orderCategory.IsActive,
            DisplayOrder = orderCategory.DisplayOrder,
            CreatedAt = orderCategory.CreatedAt,
            UpdatedAt = orderCategory.UpdatedAt
        };
    }
}

