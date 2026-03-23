using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Parser.DTOs;

namespace CephasOps.Application.Orders.Services;

/// <summary>
/// Order service interface
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Get orders with filtering
    /// </summary>
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

    /// <summary>
    /// Get orders with filtering, keyword search, and pagination (for Reports Hub and paged API).
    /// </summary>
    Task<OrderListResultDto> GetOrdersPagedAsync(
        Guid? companyId,
        Guid? departmentId = null,
        string? status = null,
        Guid? partnerId = null,
        Guid? assignedSiId = null,
        Guid? buildingId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? keyword = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get order by ID
    /// </summary>
    Task<OrderDto?> GetOrderByIdAsync(Guid id, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new order
    /// </summary>
    Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, Guid companyId, Guid userId, Guid? departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing order
    /// </summary>
    Task<OrderDto> UpdateOrderAsync(Guid id, UpdateOrderDto dto, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an order
    /// </summary>
    Task DeleteOrderAsync(Guid id, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Change order status (via workflow engine)
    /// </summary>
    Task<OrderDto> ChangeOrderStatusAsync(Guid id, ChangeOrderStatusDto dto, Guid? companyId, Guid? departmentId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create an order from a parsed order draft
    /// </summary>
    Task<CreateOrderFromDraftResult> CreateFromParsedDraftAsync(CreateOrderFromDraftDto dto, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns names of parsed materials that do not match any Material (ItemCode/Description) for the company.
    /// Used by parser draft DTO to show unmatched-material warning without duplicating resolution logic.
    /// </summary>
    Task<List<string>> GetUnmatchedParsedMaterialNamesAsync(Guid companyId, List<ParsedDraftMaterialDto> materials, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an order with the given service ID already exists
    /// </summary>
    Task<bool> ExistsByServiceIdAsync(string serviceId, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an order with the given service ID and ticket ID already exists (for assurance)
    /// </summary>
    Task<bool> ExistsByServiceIdAndTicketIdAsync(string serviceId, string ticketId, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a note to an order (appends to existing notes)
    /// </summary>
    Task<OrderDto> AddOrderNoteAsync(Guid id, string note, string userName, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get status change history for an order
    /// </summary>
    Task<List<OrderStatusLogDto>> GetOrderStatusLogsAsync(Guid orderId, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get reschedule history for an order
    /// </summary>
    Task<List<OrderRescheduleDto>> GetOrderReschedulesAsync(Guid orderId, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default);
}
