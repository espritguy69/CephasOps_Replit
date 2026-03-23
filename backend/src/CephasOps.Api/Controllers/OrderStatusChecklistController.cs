using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Order status checklist management endpoints
/// </summary>
[ApiController]
[Route("api/order-statuses/{statusCode}/checklist")]
[Authorize]
public class OrderStatusChecklistController : ControllerBase
{
    private readonly IOrderStatusChecklistService _checklistService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<OrderStatusChecklistController> _logger;

    public OrderStatusChecklistController(
        IOrderStatusChecklistService checklistService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<OrderStatusChecklistController> logger)
    {
        _checklistService = checklistService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all checklist items for a status
    /// </summary>
    [HttpGet("items")]
    [ProducesResponseType(typeof(ApiResponse<List<OrderStatusChecklistItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<OrderStatusChecklistItemDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<OrderStatusChecklistItemDto>>>> GetChecklistItems(
        string statusCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await _checklistService.GetChecklistItemsByStatusAsync(statusCode, cancellationToken);
            return this.Success(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting checklist items for status {StatusCode}", statusCode);
            return this.Error<List<OrderStatusChecklistItemDto>>($"Error retrieving checklist items: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create a new checklist item
    /// </summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(ApiResponse<OrderStatusChecklistItemDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<OrderStatusChecklistItemDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<OrderStatusChecklistItemDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<OrderStatusChecklistItemDto>>> CreateChecklistItem(
        string statusCode,
        [FromBody] CreateOrderStatusChecklistItemDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure status code matches
            dto.StatusCode = statusCode;

            var userId = _currentUserService.UserId;
            var companyId = _tenantProvider.CurrentTenantId;

            var item = await _checklistService.CreateChecklistItemAsync(dto, companyId, userId, cancellationToken);
            return this.StatusCode(201, ApiResponse<OrderStatusChecklistItemDto>.SuccessResponse(item, "Checklist item created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation creating checklist item for status {StatusCode}", statusCode);
            return this.Error<OrderStatusChecklistItemDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checklist item for status {StatusCode}", statusCode);
            return this.Error<OrderStatusChecklistItemDto>($"Error creating checklist item: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update a checklist item
    /// </summary>
    [HttpPut("items/{itemId}")]
    [ProducesResponseType(typeof(ApiResponse<OrderStatusChecklistItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderStatusChecklistItemDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<OrderStatusChecklistItemDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<OrderStatusChecklistItemDto>>> UpdateChecklistItem(
        string statusCode,
        Guid itemId,
        [FromBody] UpdateOrderStatusChecklistItemDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var item = await _checklistService.UpdateChecklistItemAsync(itemId, dto, userId, cancellationToken);
            return this.Success(item, "Checklist item updated successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Checklist item {ItemId} not found", itemId);
            return this.NotFound<OrderStatusChecklistItemDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating checklist item {ItemId}", itemId);
            return this.Error<OrderStatusChecklistItemDto>($"Error updating checklist item: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete a checklist item
    /// </summary>
    [HttpDelete("items/{itemId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteChecklistItem(
        string statusCode,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId;
            await _checklistService.DeleteChecklistItemAsync(itemId, userId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Checklist item deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error deleting checklist item {ItemId}: {Message}", itemId, ex.Message);
            
            if (ex.Message.Contains("not found"))
            {
                return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
            }
            
            return StatusCode(400, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting checklist item {ItemId}", itemId);
            return StatusCode(500, ApiResponse.ErrorResponse($"Error deleting checklist item: {ex.Message}"));
        }
    }
}

/// <summary>
/// Order checklist answers endpoints
/// </summary>
[ApiController]
[Route("api/orders/{orderId}/checklist")]
[Authorize]
public class OrderChecklistAnswersController : ControllerBase
{
    private readonly IOrderStatusChecklistService _checklistService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<OrderChecklistAnswersController> _logger;

    public OrderChecklistAnswersController(
        IOrderStatusChecklistService checklistService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<OrderChecklistAnswersController> logger)
    {
        _checklistService = checklistService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get checklist items with answers for an order
    /// </summary>
    [HttpGet("{statusCode}")]
    [ProducesResponseType(typeof(ApiResponse<List<OrderStatusChecklistWithAnswersDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<OrderStatusChecklistWithAnswersDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<OrderStatusChecklistWithAnswersDto>>>> GetChecklistWithAnswers(
        Guid orderId,
        string statusCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await _checklistService.GetChecklistWithAnswersAsync(orderId, statusCode, cancellationToken);
            return this.Success(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting checklist with answers for order {OrderId}, status {StatusCode}", orderId, statusCode);
            return this.Error<List<OrderStatusChecklistWithAnswersDto>>($"Error retrieving checklist: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Submit checklist answers
    /// </summary>
    [HttpPost("answers")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> SubmitAnswers(
        Guid orderId,
        [FromBody] SubmitOrderStatusChecklistAnswersDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return StatusCode(401, ApiResponse.ErrorResponse("User not authenticated"));
            }

            await _checklistService.SubmitChecklistAnswersAsync(orderId, dto, userId.Value, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Checklist answers submitted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation submitting answers for order {OrderId}", orderId);
            return StatusCode(400, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting checklist answers for order {OrderId}", orderId);
            return StatusCode(500, ApiResponse.ErrorResponse($"Error submitting answers: {ex.Message}"));
        }
    }

    /// <summary>
    /// Validate checklist completion for a status
    /// </summary>
    [HttpGet("validate/{statusCode}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> ValidateChecklist(
        Guid orderId,
        string statusCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isValid = await _checklistService.ValidateChecklistCompletionAsync(orderId, statusCode, cancellationToken);
            var errors = isValid 
                ? new List<string>() 
                : await _checklistService.GetChecklistValidationErrorsAsync(orderId, statusCode, cancellationToken);

            return this.Success(new
            {
                isValid,
                errors
            } as object);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating checklist for order {OrderId}, status {StatusCode}", orderId, statusCode);
            return StatusCode(500, ApiResponse.ErrorResponse($"Error validating checklist: {ex.Message}"));
        }
    }

    /// <summary>
    /// Reorder checklist items
    /// </summary>
    [HttpPost("items/reorder")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> ReorderChecklistItems(
        string statusCode,
        [FromBody] Dictionary<Guid, int> itemOrderMap,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId;
            await _checklistService.ReorderChecklistItemsAsync(statusCode, itemOrderMap, userId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Checklist items reordered successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering checklist items for status {StatusCode}", statusCode);
            return StatusCode(500, ApiResponse.ErrorResponse($"Error reordering checklist items: {ex.Message}"));
        }
    }

    /// <summary>
    /// Bulk update checklist items
    /// </summary>
    [HttpPost("items/bulk-update")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> BulkUpdateChecklistItems(
        string statusCode,
        [FromBody] BulkUpdateChecklistItemsDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId;
            await _checklistService.BulkUpdateChecklistItemsAsync(
                statusCode,
                dto.ItemIds,
                dto.UpdateDto,
                userId,
                cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Checklist items bulk updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating checklist items for status {StatusCode}", statusCode);
            return StatusCode(500, ApiResponse.ErrorResponse($"Error bulk updating checklist items: {ex.Message}"));
        }
    }

    /// <summary>
    /// Copy checklist from another status
    /// </summary>
    [HttpPost("copy-from/{sourceStatusCode}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> CopyChecklistFromStatus(
        string statusCode,
        string sourceStatusCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var companyId = _tenantProvider.CurrentTenantId;
            await _checklistService.CopyChecklistFromStatusAsync(
                sourceStatusCode,
                statusCode,
                companyId,
                userId,
                cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Checklist copied successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation copying checklist from {SourceStatusCode} to {StatusCode}", sourceStatusCode, statusCode);
            return StatusCode(400, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying checklist from {SourceStatusCode} to {StatusCode}", sourceStatusCode, statusCode);
            return StatusCode(500, ApiResponse.ErrorResponse($"Error copying checklist: {ex.Message}"));
        }
    }
}

/// <summary>
/// DTO for bulk update operation
/// </summary>
public class BulkUpdateChecklistItemsDto
{
    public List<Guid> ItemIds { get; set; } = new();
    public UpdateOrderStatusChecklistItemDto UpdateDto { get; set; } = new();
}

