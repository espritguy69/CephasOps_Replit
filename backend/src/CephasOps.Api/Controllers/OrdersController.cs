using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Authorization;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Common.Services;
using CephasOps.Application.Departments.Services;
using CephasOps.Application.Events;
using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Pnl.DTOs;
using CephasOps.Application.Workflow;
using CephasOps.Application.Pnl.Services;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
using CephasOps.Domain.Authorization;
using CephasOps.Domain.Common.Services;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Order management endpoints
/// </summary>
[ApiController]
[Route("api/orders")]
[Authorize(Policy = "Orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IOrderProfitabilityService _orderProfitabilityService;
    private readonly IOrderPayoutSnapshotService _orderPayoutSnapshotService;
    private readonly IOrderProfitAlertService _orderProfitAlertService;
    private readonly MaterialCollectionService _materialCollectionService;
    private readonly OrderMaterialUsageService _orderMaterialUsageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly IEncryptionService _encryptionService;
    private readonly ApplicationDbContext _context;
    private readonly IFieldLevelSecurityFilter _fieldLevelSecurity;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderService orderService,
        IOrderProfitabilityService orderProfitabilityService,
        IOrderPayoutSnapshotService orderPayoutSnapshotService,
        IOrderProfitAlertService orderProfitAlertService,
        MaterialCollectionService materialCollectionService,
        OrderMaterialUsageService orderMaterialUsageService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        IEncryptionService encryptionService,
        ApplicationDbContext context,
        IFieldLevelSecurityFilter fieldLevelSecurity,
        IEventBus eventBus,
        ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _orderProfitabilityService = orderProfitabilityService;
        _orderPayoutSnapshotService = orderPayoutSnapshotService;
        _orderProfitAlertService = orderProfitAlertService;
        _materialCollectionService = materialCollectionService;
        _orderMaterialUsageService = orderMaterialUsageService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _encryptionService = encryptionService;
        _context = context;
        _fieldLevelSecurity = fieldLevelSecurity;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// Get orders with filtering
    /// </summary>
    /// <param name="status">Filter by status</param>
    /// <param name="partnerId">Filter by partner</param>
    /// <param name="assignedSiId">Filter by assigned SI</param>
    /// <param name="buildingId">Filter by building</param>
    /// <param name="fromDate">Filter from date</param>
    /// <param name="toDate">Filter to date</param>
    /// <param name="departmentId">Optional department scope</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of orders</returns>
    [HttpGet]
    [RequirePermission(PermissionCatalog.OrdersView)]
    [ProducesResponseType(typeof(ApiResponse<List<OrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<OrderDto>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<List<OrderDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetOrders(
        [FromQuery] string? status = null,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] Guid? assignedSiId = null,
        [FromQuery] Guid? buildingId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        // SaaS: scope orders list by current tenant (JWT or X-Company-Id for SuperAdmin)
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var orders = await _orderService.GetOrdersAsync(
                companyId, departmentScope, status, partnerId, assignedSiId, buildingId, fromDate, toDate, cancellationToken);
            await _fieldLevelSecurity.ApplyOrderDtosAsync(orders, cancellationToken);
            return this.Success(orders);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when listing orders");
            return this.Error<List<OrderDto>>(ex.Message, 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders");
            return this.Error<List<OrderDto>>($"Failed to get orders: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get orders with filtering, keyword search, and pagination (for Orders List and Reports Hub).
    /// </summary>
    /// <param name="keyword">Search by service ID, ticket ID, customer, building, etc.</param>
    /// <param name="page">1-based page number</param>
    /// <param name="pageSize">Page size (default 50)</param>
    /// <param name="status">Filter by status</param>
    /// <param name="partnerId">Filter by partner</param>
    /// <param name="assignedSiId">Filter by assigned SI</param>
    /// <param name="buildingId">Filter by building</param>
    /// <param name="fromDate">Filter from date</param>
    /// <param name="toDate">Filter to date</param>
    /// <param name="departmentId">Optional department scope</param>
    /// <param name="includeProfitability">When true, populates RevenueAmount, PayoutAmount, ProfitAmount on each order (requires company context)</param>
    /// <param name="includeFinancialAlerts">When true, populates HasFinancialAlert, HighestAlertSeverity, AlertCount on each order (computed from profitability)</param>
    /// <param name="includeFinancialAlertsSummary">When true, populates HasFinancialAlert, HighestAlertSeverity, ActiveAlertCount from persisted active alerts (lightweight, for badges)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of orders</returns>
    [HttpGet("paged")]
    [RequirePermission(PermissionCatalog.OrdersView)]
    [ProducesResponseType(typeof(ApiResponse<OrderListResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderListResultDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<OrderListResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<OrderListResultDto>>> GetOrdersPaged(
        [FromQuery] string? keyword = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] Guid? assignedSiId = null,
        [FromQuery] Guid? buildingId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool includeProfitability = false,
        [FromQuery] bool includeFinancialAlerts = false,
        [FromQuery] bool includeFinancialAlertsSummary = false,
        CancellationToken cancellationToken = default)
    {
        // SaaS: always scope paged list by current tenant; profitability/alert flags only control enrichment
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var paged = await _orderService.GetOrdersPagedAsync(
                companyId, departmentScope, status, partnerId, assignedSiId, buildingId, fromDate, toDate,
                string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim(), page, pageSize, cancellationToken);

            if (includeProfitability && companyId.HasValue && paged.Items.Count > 0)
            {
                var orderIds = paged.Items.Select(o => o.Id).ToList();
                var profList = await _orderProfitabilityService.CalculateOrdersProfitabilityAsync(orderIds, companyId.Value, null, cancellationToken);
                var profByOrder = profList.ToDictionary(p => p.OrderId);
                foreach (var order in paged.Items)
                {
                    if (profByOrder.TryGetValue(order.Id, out var prof))
                    {
                        order.RevenueAmount = prof.RevenueAmount;
                        order.PayoutAmount = prof.PayoutAmount;
                        order.ProfitAmount = prof.ProfitAmount;
                    }
                }
            }

            if (includeFinancialAlerts && companyId.HasValue && paged.Items.Count > 0)
            {
                var orderIds = paged.Items.Select(o => o.Id).ToList();
                var alertsList = await _orderProfitAlertService.EvaluateOrdersAlertsAsync(orderIds, companyId.Value, null, cancellationToken);
                var alertsByOrder = alertsList.ToDictionary(a => a.OrderId);
                foreach (var order in paged.Items)
                {
                    if (alertsByOrder.TryGetValue(order.Id, out var ar))
                    {
                        order.HasFinancialAlert = ar.AlertCount > 0;
                        order.HighestAlertSeverity = ar.HighestSeverity;
                        order.AlertCount = ar.AlertCount;
                    }
                }
            }

            if (includeFinancialAlertsSummary && companyId.HasValue && paged.Items.Count > 0)
            {
                var orderIds = paged.Items.Select(o => o.Id).ToList();
                var summaries = await _orderProfitAlertService.GetOrderFinancialAlertSummariesAsync(companyId.Value, orderIds, cancellationToken);
                var summaryByOrder = summaries.ToDictionary(s => s.OrderId);
                foreach (var order in paged.Items)
                {
                    if (summaryByOrder.TryGetValue(order.Id, out var sum))
                    {
                        order.HasFinancialAlert = sum.ActiveAlertCount > 0;
                        order.HighestAlertSeverity = sum.HighestAlertSeverity;
                        order.ActiveAlertCount = sum.ActiveAlertCount;
                        order.AlertCount = sum.ActiveAlertCount;
                    }
                }
            }

            await _fieldLevelSecurity.ApplyOrderDtosAsync(paged.Items, cancellationToken);
            return this.Success(paged);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when listing orders (paged)");
            return this.Error<OrderListResultDto>(ex.Message, 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders (paged)");
            return this.Error<OrderListResultDto>($"Failed to get orders: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="departmentId">Optional department scope</param>
    /// <param name="includeProfitability">When true, populates RevenueAmount, PayoutAmount, ProfitAmount on the order from BillingRatecard and RateEngine</param>
    /// <param name="includeFinancialAlerts">When true, populates HasFinancialAlert, HighestAlertSeverity, AlertCount from profitability-based alerts (computed)</param>
    /// <param name="includeFinancialAlertsSummary">When true, populates HasFinancialAlert, HighestAlertSeverity, ActiveAlertCount from persisted active alerts (lightweight)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order details</returns>
    [HttpGet("{id}")]
    [RequirePermission(PermissionCatalog.OrdersView)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrder(
        Guid id,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool includeProfitability = false,
        [FromQuery] bool includeFinancialAlerts = false,
        [FromQuery] bool includeFinancialAlertsSummary = false,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var order = await _orderService.GetOrderByIdAsync(id, companyId, departmentScope, cancellationToken);
            if (order == null)
            {
                return NotFound($"Order with ID {id} not found");
            }

            if (includeProfitability && companyId.HasValue)
            {
                var prof = await _orderProfitabilityService.CalculateOrderProfitabilityAsync(id, companyId.Value, null, cancellationToken);
                if (prof != null)
                {
                    order.RevenueAmount = prof.RevenueAmount;
                    order.PayoutAmount = prof.PayoutAmount;
                    order.ProfitAmount = prof.ProfitAmount;
                }
            }

            if (includeFinancialAlerts && companyId.HasValue)
            {
                var alertsResult = await _orderProfitAlertService.EvaluateOrderAlertsAsync(id, companyId.Value, null, cancellationToken);
                order.HasFinancialAlert = alertsResult.AlertCount > 0;
                order.HighestAlertSeverity = alertsResult.HighestSeverity;
                order.AlertCount = alertsResult.AlertCount;
            }

            if (includeFinancialAlertsSummary && companyId.HasValue)
            {
                var summaries = await _orderProfitAlertService.GetOrderFinancialAlertSummariesAsync(companyId.Value, new[] { id }, cancellationToken);
                var sum = summaries.FirstOrDefault(s => s.OrderId == id);
                if (sum != null)
                {
                    order.HasFinancialAlert = sum.ActiveAlertCount > 0;
                    order.HighestAlertSeverity = sum.HighestAlertSeverity;
                    order.ActiveAlertCount = sum.ActiveAlertCount;
                    order.AlertCount = sum.ActiveAlertCount;
                }
            }

            await _fieldLevelSecurity.ApplyOrderDtoAsync(order, cancellationToken);
            return this.Success(order);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when reading order {OrderId}", id);
            return this.Error<OrderDto>(ex.Message, 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order: {OrderId}", id);
            return this.Error<OrderDto>($"Failed to get order: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get per-order profitability for a single order (revenue from BillingRatecard, payout from RateEngine).
    /// </summary>
    [HttpGet("{id}/profitability")]
    [RequirePermission(PermissionCatalog.OrdersView)]
    [ProducesResponseType(typeof(ApiResponse<OrderProfitabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<OrderProfitabilityDto>>> GetOrderProfitability(
        Guid id,
        [FromQuery] DateTime? referenceDate = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }
        if (companyId == null)
        {
            return this.Error<OrderProfitabilityDto>("Company context required for profitability", 401);
        }

        try
        {
            var result = await _orderProfitabilityService.CalculateOrderProfitabilityAsync(id, companyId.Value, referenceDate, cancellationToken);
            if (result == null)
            {
                return this.NotFound<OrderProfitabilityDto>("Order not found");
            }
            return this.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating profitability for order {OrderId}", id);
            return this.Error<OrderProfitabilityDto>($"Failed to calculate profitability: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get installer payout breakdown for an order (base rate, modifiers, trace). Read-only; uses existing rate resolution.
    /// </summary>
    [HttpGet("{id}/payout-breakdown")]
    [ProducesResponseType(typeof(ApiResponse<GponRateResolutionResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<GponRateResolutionResult>>> GetOrderPayoutBreakdown(
        Guid id,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] DateTime? referenceDate = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var order = await _orderService.GetOrderByIdAsync(id, companyId, departmentScope, cancellationToken);
            if (order == null)
            {
                return this.NotFound<GponRateResolutionResult>("Order not found");
            }
            var result = await _orderProfitabilityService.GetOrderPayoutBreakdownAsync(id, companyId, referenceDate, cancellationToken);
            if (result == null)
            {
                return this.NotFound<GponRateResolutionResult>("Order not found or payout could not be resolved");
            }
            return this.Success(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when reading payout breakdown for order {OrderId}", id);
            return this.Error<GponRateResolutionResult>(ex.Message, 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payout breakdown for order {OrderId}", id);
            return this.Error<GponRateResolutionResult>($"Failed to get payout breakdown: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get payout for order: snapshot if exists (immutable), else resolve live. Returns same result shape plus source (Snapshot | Live).
    /// </summary>
    [HttpGet("{id}/payout-snapshot")]
    [RequirePermission(PermissionCatalog.OrdersView)]
    [ProducesResponseType(typeof(ApiResponse<OrderPayoutSnapshotResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<OrderPayoutSnapshotResponseDto>>> GetOrderPayoutSnapshot(
        Guid id,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] DateTime? referenceDate = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var order = await _orderService.GetOrderByIdAsync(id, companyId, departmentScope, cancellationToken);
            if (order == null)
            {
                return this.NotFound<OrderPayoutSnapshotResponseDto>("Order not found");
            }
            var response = await _orderPayoutSnapshotService.GetPayoutWithSnapshotOrLiveAsync(id, companyId, referenceDate, cancellationToken);
            return this.Success(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when reading payout snapshot for order {OrderId}", id);
            return this.Error<OrderPayoutSnapshotResponseDto>(ex.Message, 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payout snapshot for order {OrderId}", id);
            return this.Error<OrderPayoutSnapshotResponseDto>($"Failed to get payout: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Calculate profitability for multiple orders (bulk).
    /// </summary>
    [HttpPost("profitability/bulk")]
    [RequirePermission(PermissionCatalog.OrdersEdit)]
    [ProducesResponseType(typeof(ApiResponse<List<OrderProfitabilityDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<OrderProfitabilityDto>>>> GetOrdersProfitabilityBulk(
        [FromBody] BulkOrderProfitabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }
        if (companyId == null)
        {
            return this.Error<List<OrderProfitabilityDto>>("Company context required for profitability", 401);
        }
        if (request?.OrderIds == null || request.OrderIds.Count == 0)
        {
            return this.Error<List<OrderProfitabilityDto>>("OrderIds are required", 400);
        }

        try
        {
            var results = await _orderProfitabilityService.CalculateOrdersProfitabilityAsync(
                request.OrderIds,
                companyId.Value,
                request.ReferenceDate,
                cancellationToken);
            return this.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating bulk order profitability");
            return this.Error<List<OrderProfitabilityDto>>($"Failed to calculate profitability: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get financial alerts for a single order (computed from profitability).
    /// </summary>
    [HttpGet("{id}/financial-alerts")]
    [RequirePermission(PermissionCatalog.OrdersView)]
    [ProducesResponseType(typeof(ApiResponse<OrderFinancialAlertsResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<OrderFinancialAlertsResultDto>>> GetOrderFinancialAlerts(
        Guid id,
        [FromQuery] DateTime? referenceDate = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }
        if (companyId == null)
        {
            return this.Error<OrderFinancialAlertsResultDto>("Company context required for financial alerts", 401);
        }

        try
        {
            var result = await _orderProfitAlertService.EvaluateOrderAlertsAsync(id, companyId.Value, referenceDate, cancellationToken);
            return this.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating financial alerts for order {OrderId}", id);
            return this.Error<OrderFinancialAlertsResultDto>($"Failed to evaluate financial alerts: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Evaluate financial alerts for multiple orders (bulk).
    /// </summary>
    [HttpPost("financial-alerts/bulk")]
    [RequirePermission(PermissionCatalog.OrdersEdit)]
    [ProducesResponseType(typeof(ApiResponse<List<OrderFinancialAlertsResultDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<OrderFinancialAlertsResultDto>>>> GetOrdersFinancialAlertsBulk(
        [FromBody] BulkOrderFinancialAlertsRequest request,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }
        if (companyId == null)
        {
            return this.Error<List<OrderFinancialAlertsResultDto>>("Company context required for financial alerts", 401);
        }
        if (request?.OrderIds == null || request.OrderIds.Count == 0)
        {
            return this.Error<List<OrderFinancialAlertsResultDto>>("OrderIds are required", 400);
        }

        try
        {
            var results = await _orderProfitAlertService.EvaluateOrdersAlertsAsync(
                request.OrderIds,
                companyId.Value,
                request.ReferenceDate,
                cancellationToken);
            return this.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating bulk financial alerts");
            return this.Error<List<OrderFinancialAlertsResultDto>>($"Failed to evaluate financial alerts: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Evaluate financial alerts for an order and persist them (replacing existing). Notifies when Critical alerts exist.
    /// </summary>
    [HttpPost("{id}/financial-alerts/evaluate-and-save")]
    [RequirePermission(PermissionCatalog.OrdersEdit)]
    [ProducesResponseType(typeof(ApiResponse<OrderFinancialAlertsResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<OrderFinancialAlertsResultDto>>> EvaluateAndSaveOrderFinancialAlerts(
        Guid id,
        [FromQuery] DateTime? referenceDate = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }
        if (companyId == null)
        {
            return this.Error<OrderFinancialAlertsResultDto>("Company context required for financial alerts", 401);
        }

        try
        {
            var result = await _orderProfitAlertService.EvaluateAndSaveOrderAlertsAsync(id, companyId.Value, referenceDate, cancellationToken);
            return this.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating and saving financial alerts for order {OrderId}", id);
            return this.Error<OrderFinancialAlertsResultDto>($"Failed to evaluate and save financial alerts: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get ONU password for an order (decrypted)
    /// Only accessible to authorized users (Admin, Manager, or assigned SI)
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="departmentId">Optional department scope</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Decrypted ONU password</returns>
    [HttpGet("{id}/onu-password")]
    [RequirePermission(PermissionCatalog.OrdersView)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<string>>> GetOnuPassword(
        Guid id,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        var userRoles = _currentUserService.Roles ?? new List<string>();
        var siId = _currentUserService.ServiceInstallerId;

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var order = await _orderService.GetOrderByIdAsync(id, companyId, departmentScope, cancellationToken);
            
            if (order == null)
            {
                return this.NotFound<string>("Order not found");
            }

            // Check authorization: Admin, Manager, or assigned SI can view ONU password
            var isAuthorized = userRoles.Contains("Admin") || 
                              userRoles.Contains("Manager") || 
                              (siId.HasValue && order.AssignedSiId == siId.Value);

            if (!isAuthorized)
            {
                _logger.LogWarning("Unauthorized access attempt to ONU password for order {OrderId} by user {UserId}", id, userId);
                return this.Error<string>("You do not have permission to view ONU passwords", 403);
            }

            // Get order entity to access encrypted password
            var query = _context.Orders.Where(o => o.Id == id);
            if (companyId.HasValue)
            {
                query = query.Where(o => o.CompanyId == companyId.Value);
            }
            if (departmentScope.HasValue)
            {
                query = query.Where(o => o.DepartmentId == departmentScope.Value);
            }
            
            var orderEntity = await query.FirstOrDefaultAsync(cancellationToken);
            
            if (orderEntity == null)
            {
                return this.NotFound<string>("Order not found");
            }

            if (string.IsNullOrWhiteSpace(orderEntity.OnuPasswordEncrypted))
            {
                return this.Success<string>(string.Empty, "No ONU password stored for this order");
            }

            var decryptedPassword = _encryptionService.Decrypt(orderEntity.OnuPasswordEncrypted);
            return this.Success(decryptedPassword, "ONU password retrieved successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when reading ONU password for order {OrderId}", id);
            return this.Error<string>(ex.Message, 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ONU password for order: {OrderId}", id);
            return this.Error<string>($"Failed to get ONU password: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    /// <param name="dto">Order data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created order</returns>
    [HttpPost]
    [RequirePermission(PermissionCatalog.OrdersEdit)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrder(
        [FromBody] CreateOrderDto dto,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can create orders, but still needs a companyId (can be from context or query param)
        // Regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        
        if (companyId == null || userId == null)
        {
            return Unauthorized("Company and user context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(dto.DepartmentId, cancellationToken);
            var order = await _orderService.CreateOrderAsync(dto, companyId.Value, userId.Value, departmentScope, cancellationToken);
            var orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = order.Id,
                CompanyId = order.CompanyId,
                PartnerId = order.PartnerId,
                BuildingId = order.BuildingId,
                SourceSystem = order.SourceSystem,
                TriggeredByUserId = userId
            };
            await _eventBus.PublishAsync(orderCreatedEvent, cancellationToken);
            return this.StatusCode(201, ApiResponse<OrderDto>.SuccessResponse(order, "Order created successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when creating order");
            return this.Error<OrderDto>(ex.Message, 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return this.Error<OrderDto>($"Failed to create order: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update an existing order
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="dto">Updated order data</param>
    /// <param name="departmentId">Optional department scope</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated order</returns>
    [HttpPut("{id}")]
    [RequirePermission(PermissionCatalog.OrdersEdit)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<OrderDto>>> UpdateOrder(
        Guid id,
        [FromBody] UpdateOrderDto dto,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var order = await _orderService.UpdateOrderAsync(id, dto, companyId, departmentScope, cancellationToken);
            return this.Success(order, "Order updated successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when updating order {OrderId}", id);
            return this.Error<OrderDto>(ex.Message, 403);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Order with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order: {OrderId}", id);
            return this.Error<OrderDto>($"Failed to update order: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete an order
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="departmentId">Optional department scope</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [RequirePermission(PermissionCatalog.OrdersEdit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrder(
        Guid id,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            await _orderService.DeleteOrderAsync(id, companyId, departmentScope, cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when deleting order {OrderId}", id);
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Order with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order: {OrderId}", id);
            return StatusCode(500, $"Failed to delete order: {ex.Message}");
        }
    }

    /// <summary>
    /// Change order status (via workflow engine)
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="dto">Status change data</param>
    /// <param name="departmentId">Optional department scope</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated order</returns>
    [HttpPost("{id}/status")]
    [RequirePermission(PermissionCatalog.OrdersEdit)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<OrderDto>>> ChangeOrderStatus(
        Guid id,
        [FromBody] ChangeOrderStatusDto dto,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if ((companyId == null && !_currentUserService.IsSuperAdmin) || userId == null)
        {
            return Unauthorized("Company and user context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var order = await _orderService.ChangeOrderStatusAsync(id, dto, companyId, departmentScope, userId.Value, cancellationToken);
            return this.Success(order, "Order status changed successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when changing order status {OrderId}", id);
            return this.Error<OrderDto>(ex.Message, 403);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Order with ID {id} not found");
        }
        catch (InvalidWorkflowTransitionException ex)
        {
            _logger.LogWarning(ex, "Invalid workflow transition when changing order status: {OrderId}", id);
            return this.Error<OrderDto>(ex.Message, 400);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when changing order status: {OrderId}", id);
            return this.Error<OrderDto>($"Invalid status change: {ex.Message}", 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing order status: {OrderId}", id);
            return this.Error<OrderDto>($"Failed to change order status: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Add a note to an order
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="dto">Note data</param>
    /// <param name="departmentId">Optional department scope</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated order with new note</returns>
    [HttpPost("{id}/notes")]
    [RequirePermission(PermissionCatalog.OrdersEdit)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OrderDto>>> AddOrderNote(
        Guid id,
        [FromBody] AddOrderNoteDto dto,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        var userName = _currentUserService.Email ?? "Unknown";

        if ((companyId == null && !_currentUserService.IsSuperAdmin) || userId == null)
        {
            return Unauthorized("Company and user context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var order = await _orderService.AddOrderNoteAsync(id, dto.Note, userName, companyId, departmentScope, cancellationToken);
            return this.Success<OrderDto>(order, "Note added successfully.");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when adding note to order {OrderId}", id);
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Order with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding note to order: {OrderId}", id);
            return StatusCode(500, $"Failed to add note: {ex.Message}");
        }
    }

    /// <summary>
    /// Get order status change history
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="departmentId">Optional department scope</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of status log entries</returns>
    [HttpGet("{id}/status-logs")]
    [RequirePermission(PermissionCatalog.OrdersView)]
    [ProducesResponseType(typeof(ApiResponse<List<OrderStatusLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<OrderStatusLogDto>>>> GetOrderStatusLogs(
        Guid id,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var logs = await _orderService.GetOrderStatusLogsAsync(id, companyId, departmentScope, cancellationToken);
            return this.Success<List<OrderStatusLogDto>>(logs);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when getting status logs for order {OrderId}", id);
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<List<OrderStatusLogDto>>($"Order with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status logs for order: {OrderId}", id);
            return this.InternalServerError<List<OrderStatusLogDto>>($"Failed to get status logs: {ex.Message}");
        }
    }

    /// <summary>
    /// Get order reschedule history
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="departmentId">Optional department scope</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of reschedule records</returns>
    [HttpGet("{id}/reschedules")]
    [RequirePermission(PermissionCatalog.OrdersView)]
    [ProducesResponseType(typeof(ApiResponse<List<OrderRescheduleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<OrderRescheduleDto>>>> GetOrderReschedules(
        Guid id,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return Unauthorized("Company context required");
        }

        try
        {
            var departmentScope = await ResolveDepartmentScopeAsync(departmentId, cancellationToken);
            var reschedules = await _orderService.GetOrderReschedulesAsync(id, companyId, departmentScope, cancellationToken);
            return this.Success<List<OrderRescheduleDto>>(reschedules);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when getting reschedules for order {OrderId}", id);
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<List<OrderRescheduleDto>>($"Order with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reschedules for order: {OrderId}", id);
            return this.InternalServerError<List<OrderRescheduleDto>>($"Failed to get reschedules: {ex.Message}");
        }
    }

    /// <summary>
    /// Get allowed blocker reasons for a given order status.
    /// Per ORDER_LIFECYCLE.md:
    /// - Pre-Customer blockers (status = Assigned or OnTheWay): building access, network issues, etc.
    /// - Post-Customer blockers (status = MetCustomer): customer rejection, technical issues, etc.
    /// </summary>
    /// <param name="status">Current order status</param>
    /// <returns>Allowed blocker reasons and context</returns>
    [HttpGet("blocker-reasons")]
    [RequirePermission(PermissionCatalog.OrdersView)]
    [ProducesResponseType(typeof(ApiResponse<BlockerReasonsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse<BlockerReasonsResponse>> GetBlockerReasons([FromQuery] string status)
    {
        if (string.IsNullOrEmpty(status))
        {
            return this.BadRequest<BlockerReasonsResponse>("Status parameter is required");
        }

        var canSetBlocker = Domain.Orders.Enums.OrderStatus.CanSetBlocker(status);
        if (!canSetBlocker)
        {
            var response = new BlockerReasonsResponse
            {
                CanSetBlocker = false,
                BlockerContext = null,
                AllowedReasons = new List<string>(),
                Message = $"Blocker cannot be set from status '{status}'. " +
                         $"Blocker can only be set from: {string.Join(", ", Domain.Orders.Enums.OrderStatus.PreCustomerBlockerAllowedStatuses.Concat(Domain.Orders.Enums.OrderStatus.PostCustomerBlockerAllowedStatuses))}"
            };
            return this.Success<BlockerReasonsResponse>(response);
        }

        var isPreCustomer = Domain.Orders.Enums.OrderStatus.IsPreCustomerBlockerContext(status);
        var blockerContext = isPreCustomer ? "PreCustomer" : "PostCustomer";
        var allowedReasons = isPreCustomer 
            ? Domain.Orders.Enums.BlockerReason.PreCustomerReasons.ToList()
            : Domain.Orders.Enums.BlockerReason.PostCustomerReasons.ToList();

        var response2 = new BlockerReasonsResponse
        {
            CanSetBlocker = true,
            BlockerContext = blockerContext,
            AllowedReasons = allowedReasons,
            Message = isPreCustomer 
                ? "Pre-Customer blocker: SI has not met customer yet (status is Assigned or OnTheWay)"
                : "Post-Customer blocker: SI has met/spoken to customer (status is MetCustomer)"
        };
        return this.Success(response2);
    }

    /// <summary>
    /// Check material collection requirements for an order
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Material collection check result</returns>
    [HttpGet("{id}/materials/collection-check")]
    [RequirePermission(PermissionCatalog.OrdersView)]
    [ProducesResponseType(typeof(MaterialCollectionCheckResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MaterialCollectionCheckResultDto>>> CheckMaterialCollection(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var result = await _materialCollectionService.CheckMaterialCollectionAsync(
                id,
                companyId,
                cancellationToken);
            return this.Success<MaterialCollectionCheckResultDto>(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order {OrderId} not found for material collection check", id);
            return this.NotFound<MaterialCollectionCheckResultDto>($"Order not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking material collection for order {OrderId}", id);
            return this.InternalServerError<MaterialCollectionCheckResultDto>($"Failed to check material collection: {ex.Message}");
        }
    }

    /// <summary>
    /// Get material pack for an order: required materials plus missing (to collect). Uses same logic as material collection check.
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Material pack (required and missing materials)</returns>
    [HttpGet("{id}/material-pack")]
    [RequirePermission(PermissionCatalog.OrdersView)]
    [ProducesResponseType(typeof(ApiResponse<MaterialPackDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MaterialPackDto>>> GetMaterialPack(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var result = await _materialCollectionService.GetMaterialPackAsync(id, companyId, cancellationToken);
            return this.Success<MaterialPackDto>(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order {OrderId} not found for material pack", id);
            return this.NotFound<MaterialPackDto>($"Order not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting material pack for order {OrderId}", id);
            return this.InternalServerError<MaterialPackDto>($"Failed to get material pack: {ex.Message}");
        }
    }

    /// <summary>
    /// Get required materials for an order
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of required materials</returns>
    [HttpGet("{id}/materials/required")]
    [RequirePermission(PermissionCatalog.OrdersView)]
    [ProducesResponseType(typeof(ApiResponse<List<RequiredMaterialDisplayDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<RequiredMaterialDisplayDto>>>> GetRequiredMaterials(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var materials = await _materialCollectionService.GetRequiredMaterialsForOrderAsync(
                id,
                companyId,
                cancellationToken);
            return this.Success<List<RequiredMaterialDisplayDto>>(materials);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order {OrderId} not found for required materials", id);
            return this.NotFound<List<RequiredMaterialDisplayDto>>($"Order not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting required materials for order {OrderId}", id);
            return this.InternalServerError<List<RequiredMaterialDisplayDto>>($"Failed to get required materials: {ex.Message}");
        }
    }

    /// <summary>
    /// Record material usage for an order
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="dto">Material usage data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recorded material usage</returns>
    [HttpPost("{id}/materials/usage")]
    [RequirePermission(PermissionCatalog.OrdersEdit)]
    [ProducesResponseType(typeof(ApiResponse<MaterialUsageRecordedDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MaterialUsageRecordedDto>>> RecordMaterialUsage(
        Guid id,
        [FromBody] RecordMaterialUsageDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var siId = _currentUserService.ServiceInstallerId;
        var userId = _currentUserService.UserId;

        try
        {
            var result = await _orderMaterialUsageService.RecordMaterialUsageAsync(
                id,
                dto.MaterialId,
                dto.SerialNumber,
                dto.Quantity,
                companyId,
                siId,
                userId,
                dto.Notes,
                cancellationToken);

            return this.Success<MaterialUsageRecordedDto>(result, "Material usage recorded successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order or material not found for material usage recording");
            return this.NotFound<MaterialUsageRecordedDto>($"Order or material not found: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for material usage recording");
            return this.BadRequest<MaterialUsageRecordedDto>($"Invalid request: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for material usage recording");
            return this.BadRequest<MaterialUsageRecordedDto>($"Invalid operation: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording material usage for order {OrderId}", id);
            return this.InternalServerError<MaterialUsageRecordedDto>($"Failed to record material usage: {ex.Message}");
        }
    }

    /// <summary>
    /// Get material usage for an order
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of material usage records</returns>
    [HttpGet("{id}/materials/usage")]
    [RequirePermission(PermissionCatalog.OrdersView)]
    [ProducesResponseType(typeof(ApiResponse<List<MaterialUsageRecordedDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<MaterialUsageRecordedDto>>>> GetMaterialUsage(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var usage = await _orderMaterialUsageService.GetMaterialUsageAsync(id, cancellationToken);
            return this.Success<List<MaterialUsageRecordedDto>>(usage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting material usage for order {OrderId}", id);
            return this.InternalServerError<List<MaterialUsageRecordedDto>>($"Failed to get material usage: {ex.Message}");
        }
    }

    private Task<Guid?> ResolveDepartmentScopeAsync(Guid? requestedDepartmentId, CancellationToken cancellationToken)
    {
        var departmentFromRequest = requestedDepartmentId ?? _departmentRequestContext.DepartmentId;
        return _departmentAccessService.ResolveDepartmentScopeAsync(departmentFromRequest, cancellationToken);
    }
}

/// <summary>
/// Response for blocker reasons endpoint
/// </summary>
public class BlockerReasonsResponse
{
    public bool CanSetBlocker { get; set; }
    public string? BlockerContext { get; set; }
    public List<string> AllowedReasons { get; set; } = new();
    public string? Message { get; set; }
}

/// <summary>
/// DTO for adding a note to an order
/// </summary>
public class AddOrderNoteDto
{
    public string Note { get; set; } = string.Empty;
}

