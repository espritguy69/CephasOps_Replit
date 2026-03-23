using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// PATTERN: Controller-based API endpoints for CephasOps
/// 
/// Key conventions:
/// - Use [ApiController] and [Route("api/[resource]")]
/// - Inject services via constructor
/// - Use ICurrentUserService for user context
/// - Use IDepartmentRequestContext for department filtering
/// - Return ActionResult<T> with proper status codes
/// - Handle exceptions with try/catch and return appropriate errors
/// </summary>
[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderService orderService,
        ICurrentUserService currentUserService,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _currentUserService = currentUserService;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    /// <summary>
    /// PATTERN: List endpoint with filters and department scoping
    /// 
    /// - Accept filters via [FromQuery]
    /// - Always resolve department scope for data isolation
    /// - Return List<T> or paged result
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OrderDto>>> GetOrders(
        [FromQuery] string? status = null,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] Guid? assignedSiId = null,
        [FromQuery] Guid? buildingId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        // Single-company mode: companyId is null (no company filtering)
        var companyId = (Guid?)null;

        try
        {
            // IMPORTANT: Always resolve department scope for data isolation
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            
            var orders = await _orderService.GetOrdersAsync(
                companyId, 
                departmentScope, 
                status, 
                partnerId, 
                assignedSiId, 
                buildingId, 
                fromDate, 
                toDate, 
                cancellationToken);
            
            return Ok(orders);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when listing orders");
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders");
            return StatusCode(500, new { error = "Failed to get orders", message = ex.Message });
        }
    }

    /// <summary>
    /// PATTERN: Get single entity by ID
    /// 
    /// - Use route parameter {id:guid}
    /// - Return 404 if not found
    /// - Apply department scoping
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrder(
        Guid id,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _currentUserService.CompanyId;

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var order = await _orderService.GetOrderByIdAsync(id, companyId, departmentScope, cancellationToken);
            
            if (order == null)
            {
                return NotFound($"Order with ID {id} not found");
            }

            return Ok(order);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when reading order {OrderId}", id);
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order: {OrderId}", id);
            return StatusCode(500, new { error = "Failed to get order", message = ex.Message });
        }
    }

    /// <summary>
    /// PATTERN: Create endpoint
    /// 
    /// - Accept DTO via [FromBody]
    /// - Validate user context (userId, companyId)
    /// - Return CreatedAtAction with location header
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> CreateOrder(
        [FromBody] CreateOrderDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _currentUserService.CompanyId;
        var userId = _currentUserService.UserId;
        
        if (companyId == null || userId == null)
        {
            return Unauthorized("Company and user context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(dto.DepartmentId, cancellationToken);
            var order = await _orderService.CreateOrderAsync(dto, companyId.Value, userId.Value, departmentScope, cancellationToken);
            
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when creating order");
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, new { error = "Failed to create order", message = ex.Message });
        }
    }

    /// <summary>
    /// PATTERN: Update endpoint
    /// 
    /// - Use route parameter for ID
    /// - Accept DTO via [FromBody]
    /// - Return 404 if not found
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> UpdateOrder(
        Guid id,
        [FromBody] UpdateOrderDto dto,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _currentUserService.CompanyId;

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var order = await _orderService.UpdateOrderAsync(id, dto, companyId, departmentScope, cancellationToken);
            return Ok(order);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Order with ID {id} not found");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when updating order {OrderId}", id);
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order: {OrderId}", id);
            return StatusCode(500, new { error = "Failed to update order", message = ex.Message });
        }
    }

    /// <summary>
    /// PATTERN: Delete endpoint
    /// 
    /// - Return 204 NoContent on success
    /// - Return 404 if not found
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrder(
        Guid id,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _currentUserService.CompanyId;

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            await _orderService.DeleteOrderAsync(id, companyId, departmentScope, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Order with ID {id} not found");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when deleting order {OrderId}", id);
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order: {OrderId}", id);
            return StatusCode(500, new { error = "Failed to delete order", message = ex.Message });
        }
    }

    /// <summary>
    /// PATTERN: Department scope resolution
    /// 
    /// - Priority: Query param > Request header > User's default
    /// - Use IDepartmentAccessService for validation
    /// </summary>
    private Task<Guid?> ResolveDepartmentScopeAsync(Guid? requestedDepartmentId, CancellationToken cancellationToken)
    {
        var departmentFromRequest = requestedDepartmentId ?? _departmentRequestContext.DepartmentId;
        return _departmentAccessService.ResolveDepartmentScopeAsync(departmentFromRequest, cancellationToken);
    }
}

