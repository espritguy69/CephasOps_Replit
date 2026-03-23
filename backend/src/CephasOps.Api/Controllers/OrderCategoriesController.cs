using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// OrderCategory management endpoints
/// Previously known as InstallationTypesController but renamed for clarity.
/// OrderCategory represents service/technology categories (FTTH, FTTO, FTTR, FTTC).
/// </summary>
[ApiController]
[Route("api/order-categories")]
[Authorize]
public class OrderCategoriesController : ControllerBase
{
    private readonly IOrderCategoryService _orderCategoryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<OrderCategoriesController> _logger;

    public OrderCategoriesController(
        IOrderCategoryService orderCategoryService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<OrderCategoriesController> logger)
    {
        _orderCategoryService = orderCategoryService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    /// <summary>
    /// Get order categories list
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<OrderCategoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<OrderCategoryDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<OrderCategoryDto>>>> GetOrderCategories(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        Guid? departmentScope;
        try
        {
            departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return this.Error<List<OrderCategoryDto>>("You do not have access to this department", 403);
        }

        try
        {
            var orderCategories = await _orderCategoryService.GetOrderCategoriesAsync(companyId, departmentScope, isActive, cancellationToken);
            return this.Success(orderCategories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order categories");
            return this.InternalServerError<List<OrderCategoryDto>>($"Failed to get order categories: {ex.Message}");
        }
    }

    /// <summary>
    /// Get order category by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<OrderCategoryDto>>> GetOrderCategory(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var orderCategory = await _orderCategoryService.GetOrderCategoryByIdAsync(id, companyId, cancellationToken);
            if (orderCategory == null)
            {
                return this.NotFound<OrderCategoryDto>($"OrderCategory with ID {id} not found");
            }
            return this.Success(orderCategory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order category {OrderCategoryId}", id);
            return this.InternalServerError<OrderCategoryDto>($"Failed to get order category: {ex.Message}");
        }
    }

    /// <summary>
    /// Create new order category
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<OrderCategoryDto>>> CreateOrderCategory(
        [FromBody] CreateOrderCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var orderCategory = await _orderCategoryService.CreateOrderCategoryAsync(dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetOrderCategory), new { id = orderCategory.Id }, orderCategory, "Order category created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order category");
            return this.InternalServerError<OrderCategoryDto>($"Failed to create order category: {ex.Message}");
        }
    }

    /// <summary>
    /// Update order category
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<OrderCategoryDto>>> UpdateOrderCategory(
        Guid id,
        [FromBody] UpdateOrderCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var orderCategory = await _orderCategoryService.UpdateOrderCategoryAsync(id, dto, companyId, cancellationToken);
            return this.Success(orderCategory, "Order category updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<OrderCategoryDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order category {OrderCategoryId}", id);
            return this.InternalServerError<OrderCategoryDto>($"Failed to update order category: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete order category
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteOrderCategory(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            await _orderCategoryService.DeleteOrderCategoryAsync(id, companyId, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order category {OrderCategoryId}", id);
            return this.InternalServerError($"Failed to delete order category: {ex.Message}");
        }
    }
}

