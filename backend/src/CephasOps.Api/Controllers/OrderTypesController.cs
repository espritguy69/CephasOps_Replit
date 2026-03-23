using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// OrderType management endpoints
/// </summary>
[ApiController]
[Route("api/order-types")]
[Authorize]
public class OrderTypesController : ControllerBase
{
    private readonly IOrderTypeService _orderTypeService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<OrderTypesController> _logger;

    public OrderTypesController(
        IOrderTypeService orderTypeService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<OrderTypesController> logger)
    {
        _orderTypeService = orderTypeService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    /// <summary>
    /// Get order types list. Use parentsOnly=true for parent types only (e.g. for Create Order dropdown).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<OrderTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<OrderTypeDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<OrderTypeDto>>>> GetOrderTypes(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool parentsOnly = false,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        // When parentsOnly=true (e.g. settings page, Create Order), return all parents for the company without department filter.
        Guid? departmentScope = null;
        if (!parentsOnly)
        {
            try
            {
                departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                return this.Error<List<OrderTypeDto>>("You do not have access to this department", 403);
            }
        }

        try
        {
            var orderTypes = await _orderTypeService.GetOrderTypesAsync(companyId, departmentScope, isActive, parentsOnly, cancellationToken);
            return this.Success(orderTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order types");
            return this.InternalServerError<List<OrderTypeDto>>($"Failed to get order types: {ex.Message}");
        }
    }

    /// <summary>
    /// Get subtypes of a parent order type. Use isActive=true for Create Order (active only); omit for settings (all).
    /// </summary>
    [HttpGet("{id}/subtypes")]
    [ProducesResponseType(typeof(ApiResponse<List<OrderTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<OrderTypeDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<OrderTypeDto>>>> GetSubtypes(
        Guid id,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var subtypes = await _orderTypeService.GetSubtypesAsync(id, companyId, isActive, cancellationToken);
            return this.Success(subtypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subtypes for order type {OrderTypeId}", id);
            return this.InternalServerError<List<OrderTypeDto>>($"Failed to get subtypes: {ex.Message}");
        }
    }

    /// <summary>
    /// Get order type by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<OrderTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderTypeDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<OrderTypeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<OrderTypeDto>>> GetOrderType(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var orderType = await _orderTypeService.GetOrderTypeByIdAsync(id, companyId, cancellationToken);
            if (orderType == null)
            {
                return this.NotFound<OrderTypeDto>($"OrderType with ID {id} not found");
            }
            return this.Success(orderType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order type {OrderTypeId}", id);
            return this.InternalServerError<OrderTypeDto>($"Failed to get order type: {ex.Message}");
        }
    }

    /// <summary>
    /// Create new order type
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderTypeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<OrderTypeDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<OrderTypeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<OrderTypeDto>>> CreateOrderType(
        [FromBody] CreateOrderTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var orderType = await _orderTypeService.CreateOrderTypeAsync(dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetOrderType), new { id = orderType.Id }, orderType, "Order type created successfully.");
        }
        catch (ArgumentException ex)
        {
            return this.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order type");
            return this.InternalServerError<OrderTypeDto>($"Failed to create order type: {ex.Message}");
        }
    }

    /// <summary>
    /// Update order type
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<OrderTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderTypeDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<OrderTypeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<OrderTypeDto>>> UpdateOrderType(
        Guid id,
        [FromBody] UpdateOrderTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var orderType = await _orderTypeService.UpdateOrderTypeAsync(id, dto, companyId, cancellationToken);
            return this.Success(orderType, "Order type updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<OrderTypeDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order type {OrderTypeId}", id);
            return this.InternalServerError<OrderTypeDto>($"Failed to update order type: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete order type
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteOrderType(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            await _orderTypeService.DeleteOrderTypeAsync(id, companyId, cancellationToken);
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
            _logger.LogError(ex, "Error deleting order type {OrderTypeId}", id);
            return this.InternalServerError($"Failed to delete order type: {ex.Message}");
        }
    }
}
