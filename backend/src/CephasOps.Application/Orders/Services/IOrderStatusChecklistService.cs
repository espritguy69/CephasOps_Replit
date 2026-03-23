using CephasOps.Application.Orders.DTOs;

namespace CephasOps.Application.Orders.Services;

/// <summary>
/// Service interface for managing order status checklist items and answers
/// </summary>
public interface IOrderStatusChecklistService
{
    /// <summary>
    /// Get all checklist items for a status (with sub-steps nested)
    /// </summary>
    Task<List<OrderStatusChecklistItemDto>> GetChecklistItemsByStatusAsync(
        string statusCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get checklist items with answers for a specific order
    /// </summary>
    Task<List<OrderStatusChecklistWithAnswersDto>> GetChecklistWithAnswersAsync(
        Guid orderId,
        string statusCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new checklist item
    /// </summary>
    Task<OrderStatusChecklistItemDto> CreateChecklistItemAsync(
        CreateOrderStatusChecklistItemDto dto,
        Guid? companyId,
        Guid? userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update a checklist item
    /// </summary>
    Task<OrderStatusChecklistItemDto> UpdateChecklistItemAsync(
        Guid id,
        UpdateOrderStatusChecklistItemDto dto,
        Guid? userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a checklist item (soft delete)
    /// </summary>
    Task DeleteChecklistItemAsync(
        Guid id,
        Guid? userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit answers for checklist items
    /// </summary>
    Task SubmitChecklistAnswersAsync(
        Guid orderId,
        SubmitOrderStatusChecklistAnswersDto dto,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that all required checklist items are completed for a status
    /// </summary>
    Task<bool> ValidateChecklistCompletionAsync(
        Guid orderId,
        string statusCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get validation errors for incomplete checklist items
    /// </summary>
    Task<List<string>> GetChecklistValidationErrorsAsync(
        Guid orderId,
        string statusCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorder checklist items (update OrderIndex for multiple items)
    /// </summary>
    Task ReorderChecklistItemsAsync(
        string statusCode,
        Dictionary<Guid, int> itemOrderMap,
        Guid? userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk update checklist items (activate/deactivate, set required, etc.)
    /// </summary>
    Task BulkUpdateChecklistItemsAsync(
        string statusCode,
        List<Guid> itemIds,
        UpdateOrderStatusChecklistItemDto updateDto,
        Guid? userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copy checklist items from one status to another
    /// </summary>
    Task CopyChecklistFromStatusAsync(
        string sourceStatusCode,
        string targetStatusCode,
        Guid? companyId,
        Guid? userId,
        CancellationToken cancellationToken = default);
}

