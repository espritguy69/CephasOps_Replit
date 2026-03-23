using CephasOps.Application.Orders.DTOs;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Orders.Services;

/// <summary>
/// PATTERN: Service class for business logic
/// 
/// Key conventions:
/// - Implement interface (IOrderService)
/// - Inject DbContext and ILogger
/// - Use async/await with CancellationToken
/// - Apply department filtering in queries
/// - Use DTOs for input/output (not domain entities)
/// </summary>
public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        ApplicationDbContext context,
        ILogger<OrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// PATTERN: List query with filters
    /// 
    /// - Apply filters conditionally
    /// - Apply department filtering for data isolation
    /// - Use AsNoTracking() for read-only queries
    /// - Map to DTOs before returning
    /// </summary>
    public async Task<List<OrderDto>> GetOrdersAsync(
        Guid? companyId,
        Guid? departmentId = null,
        string? status = null,
        Guid? partnerId = null,
        Guid? assignedSiId = null,
        Guid? buildingId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        // Start with base query
        var query = _context.Orders.AsNoTracking();

        // IMPORTANT: Apply department filter for data isolation
        if (departmentId.HasValue)
        {
            query = query.Where(o => o.DepartmentId == departmentId.Value);
        }

        // Apply optional filters
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (partnerId.HasValue)
        {
            query = query.Where(o => o.PartnerId == partnerId.Value);
        }

        if (assignedSiId.HasValue)
        {
            query = query.Where(o => o.AssignedSiId == assignedSiId.Value);
        }

        if (buildingId.HasValue)
        {
            query = query.Where(o => o.BuildingId == buildingId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.AppointmentDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => o.AppointmentDate <= toDate.Value);
        }

        // Execute query and map to DTOs
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return orders.Select(MapToDto).ToList();
    }

    /// <summary>
    /// PATTERN: Get single entity by ID
    /// 
    /// - Apply department filtering
    /// - Return null if not found (let controller handle 404)
    /// </summary>
    public async Task<OrderDto?> GetOrderByIdAsync(
        Guid id,
        Guid? companyId,
        Guid? departmentId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.AsNoTracking().Where(o => o.Id == id);

        if (departmentId.HasValue)
        {
            query = query.Where(o => o.DepartmentId == departmentId.Value);
        }

        var order = await query.FirstOrDefaultAsync(cancellationToken);

        return order != null ? MapToDto(order) : null;
    }

    /// <summary>
    /// PATTERN: Create entity
    /// 
    /// - Resolve department from OrderType settings first
    /// - Fall back to Building's department
    /// - Create entity and save
    /// - Return created DTO
    /// </summary>
    public async Task<OrderDto> CreateOrderAsync(
        CreateOrderDto dto,
        Guid companyId,
        Guid userId,
        Guid? departmentId,
        CancellationToken cancellationToken = default)
    {
        // IMPORTANT: Resolve department from OrderType settings first
        var orderType = await _context.OrderTypes
            .Where(ot => ot.Id == dto.OrderTypeId)
            .FirstOrDefaultAsync(cancellationToken);

        if (orderType == null)
        {
            throw new InvalidOperationException($"OrderType with ID {dto.OrderTypeId} not found");
        }

        // Priority: OrderType's DepartmentId > explicit departmentId > Building's department
        Guid? resolvedDepartmentId = null;
        
        if (orderType.DepartmentId.HasValue)
        {
            resolvedDepartmentId = orderType.DepartmentId;
            _logger.LogInformation(
                "Using DepartmentId from OrderType: {OrderTypeName} ({OrderTypeCode}), DepartmentId: {DepartmentId}", 
                orderType.Name, orderType.Code, resolvedDepartmentId);
        }
        else if (departmentId.HasValue || dto.DepartmentId.HasValue)
        {
            resolvedDepartmentId = departmentId ?? dto.DepartmentId;
        }
        else if (dto.BuildingId != Guid.Empty)
        {
            // Fall back to building's department
            resolvedDepartmentId = await ResolveDepartmentFromBuildingAsync(dto.BuildingId, cancellationToken);
        }

        // Create the order
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            DepartmentId = resolvedDepartmentId,
            OrderTypeId = dto.OrderTypeId,
            // ... map other properties from dto
            Status = "Pending",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order created: {OrderId}, Department: {DepartmentId}", order.Id, resolvedDepartmentId);

        return MapToDto(order);
    }

    /// <summary>
    /// PATTERN: Helper method to resolve department from building
    /// </summary>
    private async Task<Guid?> ResolveDepartmentFromBuildingAsync(Guid buildingId, CancellationToken cancellationToken)
    {
        var building = await _context.Buildings
            .Where(b => b.Id == buildingId)
            .Select(b => new { b.DepartmentId })
            .FirstOrDefaultAsync(cancellationToken);

        return building?.DepartmentId;
    }

    /// <summary>
    /// PATTERN: Map entity to DTO
    /// 
    /// - Keep mapping logic centralized
    /// - Use static method for simple mappings
    /// - Consider using Mapster for complex mappings
    /// </summary>
    private static OrderDto MapToDto(Order o)
    {
        return new OrderDto
        {
            Id = o.Id,
            CompanyId = o.CompanyId,
            DepartmentId = o.DepartmentId,
            PartnerId = o.PartnerId,
            OrderTypeId = o.OrderTypeId,
            ServiceId = o.ServiceId,
            Status = o.Status,
            CustomerName = o.CustomerName,
            CustomerPhone = o.CustomerPhone,
            BuildingId = o.BuildingId,
            BuildingName = o.BuildingName,
            AppointmentDate = o.AppointmentDate,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt
        };
    }
}

/// <summary>
/// PATTERN: Service interface
/// 
/// - Define all public methods
/// - Use async Task<T> with CancellationToken
/// - Document parameters
/// </summary>
public interface IOrderService
{
    Task<List<OrderDto>> GetOrdersAsync(
        Guid? companyId,
        Guid? departmentId = null,
        string? status = null,
        Guid? partnerId = null,
        Guid? assignedSiId = null,
        Guid? buildingId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    Task<OrderDto?> GetOrderByIdAsync(Guid id, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default);
    
    Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, Guid companyId, Guid userId, Guid? departmentId, CancellationToken cancellationToken = default);
}

