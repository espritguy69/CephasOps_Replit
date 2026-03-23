using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Api.DTOs;
using CephasOps.Application.Authorization;
using CephasOps.Application.Common.DTOs;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Common.Services;
using CephasOps.Application.Departments.Services;
using CephasOps.Application.Inventory.DTOs;
using CephasOps.Application.Inventory.Services;
using CephasOps.Application.Workflow.JobOrchestration;
using CephasOps.Domain.Authorization;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Inventory management endpoints
/// </summary>
[ApiController]
[Route("api/inventory")]
[Authorize(Policy = "Inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IStockLedgerService _stockLedgerService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICsvService _csvService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InventoryController> _logger;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly IFieldLevelSecurityFilter _fieldLevelSecurity;
    private readonly IJobExecutionEnqueuer _jobExecutionEnqueuer;

    public InventoryController(
        IInventoryService inventoryService,
        IStockLedgerService stockLedgerService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ICsvService csvService,
        ApplicationDbContext context,
        ILogger<InventoryController> logger,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        IFieldLevelSecurityFilter fieldLevelSecurity,
        IJobExecutionEnqueuer jobExecutionEnqueuer)
    {
        _inventoryService = inventoryService;
        _stockLedgerService = stockLedgerService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _csvService = csvService;
        _context = context;
        _logger = logger;
        _jobExecutionEnqueuer = jobExecutionEnqueuer;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _fieldLevelSecurity = fieldLevelSecurity;
    }

    /// <summary>
    /// Get materials list
    /// </summary>
    /// <param name="departmentId">Filter by department ID</param>
    /// <param name="category">Filter by category</param>
    /// <param name="search">Search term</param>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of materials</returns>
    [HttpGet("materials")]
    [RequirePermission(PermissionCatalog.InventoryView)]
    [ProducesResponseType(typeof(ApiResponse<List<MaterialDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<MaterialDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<MaterialDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<MaterialDto>>>> GetMaterials(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return this.Error<List<MaterialDto>>("Company context required", 401);
        }

        // Resolve and validate department scope (enforces department access)
        Guid? resolvedDepartmentId = null;
        try
        {
            resolvedDepartmentId = await _departmentAccessService.ResolveDepartmentScopeAsync(
                departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return this.Error<List<MaterialDto>>("You do not have access to this department", 403);
        }

        try
        {
            var materials = await _inventoryService.GetMaterialsAsync(companyId, resolvedDepartmentId, category, search, isActive, cancellationToken);
            await _fieldLevelSecurity.ApplyMaterialDtosAsync(materials, cancellationToken);
            return this.Success(materials);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting materials");
            return this.Error<List<MaterialDto>>($"Failed to get materials: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get material by barcode
    /// </summary>
    /// <param name="barcode">Barcode to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Material details if found</returns>
    [HttpGet("materials/by-barcode/{barcode}")]
    [RequirePermission(PermissionCatalog.InventoryView)]
    [ProducesResponseType(typeof(ApiResponse<MaterialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MaterialDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<MaterialDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<MaterialDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MaterialDto>>> GetMaterialByBarcode(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return this.Error<MaterialDto>("Company context required", 401);
        }

        try
        {
            var material = await _inventoryService.GetMaterialByBarcodeAsync(barcode, companyId, cancellationToken);
            if (material == null)
            {
                return this.NotFound<MaterialDto>($"Material with barcode '{barcode}' not found");
            }

            await _fieldLevelSecurity.ApplyMaterialDtoAsync(material, cancellationToken);
            return this.Success(material);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting material by barcode: {Barcode}", barcode);
            return this.Error<MaterialDto>($"Failed to get material by barcode: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get material by ID
    /// </summary>
    /// <param name="id">Material ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Material details</returns>
    [HttpGet("materials/{id}")]
    [RequirePermission(PermissionCatalog.InventoryView)]
    [ProducesResponseType(typeof(ApiResponse<MaterialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MaterialDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<MaterialDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<MaterialDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MaterialDto>>> GetMaterial(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return this.Error<MaterialDto>("Company context required", 401);
        }

        try
        {
            var material = await _inventoryService.GetMaterialByIdAsync(id, companyId, cancellationToken);
            if (material == null)
            {
                return this.NotFound<MaterialDto>($"Material with ID {id} not found");
            }

            await _fieldLevelSecurity.ApplyMaterialDtoAsync(material, cancellationToken);
            return this.Success(material);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting material: {MaterialId}", id);
            return this.Error<MaterialDto>($"Failed to get material: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create a new material
    /// </summary>
    /// <param name="dto">Material data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created material</returns>
    [HttpPost("materials")]
    [RequirePermission(PermissionCatalog.InventoryEdit)]
    [ProducesResponseType(typeof(ApiResponse<MaterialDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<MaterialDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MaterialDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<MaterialDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MaterialDto>>> CreateMaterial(
        [FromBody] CreateMaterialDto dto,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return this.Error<MaterialDto>("Company context required", 401);
        }

        try
        {
            var material = await _inventoryService.CreateMaterialAsync(dto, companyId, cancellationToken);
            await _fieldLevelSecurity.ApplyMaterialDtoAsync(material, cancellationToken);
            return this.StatusCode(201, ApiResponse<MaterialDto>.SuccessResponse(material, "Material created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating material");
            return this.Error<MaterialDto>($"Failed to create material: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update an existing material
    /// </summary>
    /// <param name="id">Material ID</param>
    /// <param name="dto">Updated material data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated material</returns>
    [HttpPut("materials/{id}")]
    [RequirePermission(PermissionCatalog.InventoryEdit)]
    [ProducesResponseType(typeof(ApiResponse<MaterialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MaterialDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<MaterialDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<MaterialDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MaterialDto>>> UpdateMaterial(
        Guid id,
        [FromBody] UpdateMaterialDto dto,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return this.Error<MaterialDto>("Company context required", 401);
        }

        try
        {
            var material = await _inventoryService.UpdateMaterialAsync(id, dto, companyId, cancellationToken);
            await _fieldLevelSecurity.ApplyMaterialDtoAsync(material, cancellationToken);
            return this.Success(material, "Material updated successfully");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<MaterialDto>($"Material with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating material: {MaterialId}", id);
            return this.Error<MaterialDto>($"Failed to update material: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete a material
    /// </summary>
    /// <param name="id">Material ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("materials/{id}")]
    [RequirePermission(PermissionCatalog.InventoryEdit)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteMaterial(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        }

        try
        {
            await _inventoryService.DeleteMaterialAsync(id, companyId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Material deleted successfully"));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"Material with ID {id} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting material: {MaterialId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete material: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get stock balances
    /// </summary>
    /// <param name="locationId">Filter by location</param>
    /// <param name="materialId">Filter by material</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of stock balances</returns>
    [HttpGet("stock")]
    [RequirePermission(PermissionCatalog.InventoryView)]
    [ProducesResponseType(typeof(ApiResponse<List<StockBalanceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<StockBalanceDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<StockBalanceDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<StockBalanceDto>>>> GetStockBalances(
        [FromQuery] Guid? locationId = null,
        [FromQuery] Guid? materialId = null,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return this.Error<List<StockBalanceDto>>("Company context required", 401);
        }

        try
        {
            var balances = await _inventoryService.GetStockBalancesAsync(companyId, locationId, materialId, cancellationToken);
            return this.Success(balances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock balances");
            return this.Error<List<StockBalanceDto>>($"Failed to get stock balances: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get stock locations
    /// </summary>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of stock locations</returns>
    [HttpGet("stock/locations")]
    [RequirePermission(PermissionCatalog.InventoryView)]
    [ProducesResponseType(typeof(ApiResponse<List<StockLocationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<StockLocationDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<StockLocationDto>>>> GetStockLocations(
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var locations = await _inventoryService.GetStockLocationsAsync(companyId, isActive, cancellationToken);
            return this.Success(locations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock locations");
            return this.Error<List<StockLocationDto>>($"Failed to get stock locations: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get stock movements
    /// </summary>
    /// <param name="materialId">Filter by material</param>
    /// <param name="locationId">Filter by location</param>
    /// <param name="movementType">Filter by movement type</param>
    /// <param name="fromDate">Filter from date</param>
    /// <param name="toDate">Filter to date</param>
    /// <param name="limit">Limit results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of stock movements</returns>
    [HttpGet("stock/movements")]
    [HttpGet("movements")]
    [RequirePermission(PermissionCatalog.InventoryView)]
    [ProducesResponseType(typeof(ApiResponse<List<StockMovementDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<StockMovementDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<StockMovementDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<StockMovementDto>>>> GetStockMovements(
        [FromQuery] Guid? materialId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] string? movementType = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return this.Error<List<StockMovementDto>>("Company context required", 401);
        }

        try
        {
            var movements = await _inventoryService.GetStockMovementsAsync(companyId, materialId, locationId, movementType, fromDate, toDate, cancellationToken);
            if (limit.HasValue && limit.Value > 0)
            {
                movements = movements.Take(limit.Value).ToList();
            }
            return this.Success(movements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock movements");
            return this.Error<List<StockMovementDto>>($"Failed to get stock movements: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create a stock movement (legacy). Quantities are ledger-only; prefer POST /api/inventory/receive, /transfer, /issue, /return for new code.
    /// </summary>
    /// <param name="dto">Stock movement data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created stock movement</returns>
    [Obsolete("Legacy write path. Prefer ledger endpoints: POST /api/inventory/receive, /transfer, /issue, /return. Ledger is the single source of truth for quantities.")]
    [HttpPost("stock/movements")]
    [HttpPost("movements")]
    [RequirePermission(PermissionCatalog.InventoryEdit)]
    [ProducesResponseType(typeof(ApiResponse<StockMovementDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<StockMovementDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<StockMovementDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<StockMovementDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<StockMovementDto>>> CreateStockMovement(
        [FromBody] CreateStockMovementDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Legacy write path invoked: POST CreateStockMovement. Prefer ledger endpoints (receive/transfer/issue/return). Ledger is the source of truth.");

        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if ((companyId == null && !_currentUserService.IsSuperAdmin) || userId == null)
        {
            return this.Error<StockMovementDto>("Company and user context required", 401);
        }

        try
        {
            var movement = await _inventoryService.CreateStockMovementAsync(dto, companyId, userId.Value, cancellationToken);
            return this.StatusCode(201, ApiResponse<StockMovementDto>.SuccessResponse(movement, "Stock movement created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stock movement");
            return this.Error<StockMovementDto>($"Failed to create stock movement: {ex.Message}", 500);
        }
    }

    // ============================================
    // Ledger-based inventory (Phase 1)
    // ============================================

    /// <summary>Receive stock into a location (ledger only; immutable).</summary>
    [HttpPost("receive")]
    [RequirePermission(PermissionCatalog.InventoryEdit)]
    [ProducesResponseType(typeof(ApiResponse<LedgerWriteResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<LedgerWriteResultDto>>> Receive(
        [FromBody] LedgerReceiveRequestDto dto,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, userId, resolvedDeptId) = await ResolveLedgerContextAsync(departmentId, cancellationToken);
        if (resolvedDeptId == null) return this.Error<LedgerWriteResultDto>("Department scope required", 403);
        if (userId == null) return this.Error<LedgerWriteResultDto>("User context required", 401);
        try
        {
            var result = await _stockLedgerService.ReceiveAsync(dto, companyId, resolvedDeptId, userId.Value, cancellationToken);
            return this.StatusCode(201, ApiResponse<LedgerWriteResultDto>.SuccessResponse(result, result.Message));
        }
        catch (UnauthorizedAccessException ex) { return this.Error<LedgerWriteResultDto>(ex.Message, 403); }
        catch (InvalidOperationException ex) { return this.Error<LedgerWriteResultDto>(ex.Message, 400); }
    }

    /// <summary>Transfer stock between locations (ledger only).</summary>
    [HttpPost("transfer")]
    [RequirePermission(PermissionCatalog.InventoryEdit)]
    [ProducesResponseType(typeof(ApiResponse<LedgerWriteResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<LedgerWriteResultDto>>> Transfer(
        [FromBody] LedgerTransferRequestDto dto,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, userId, resolvedDeptId) = await ResolveLedgerContextAsync(departmentId, cancellationToken);
        if (resolvedDeptId == null) return this.Error<LedgerWriteResultDto>("Department scope required", 403);
        if (userId == null) return this.Error<LedgerWriteResultDto>("User context required", 401);
        try
        {
            var result = await _stockLedgerService.TransferAsync(dto, companyId, resolvedDeptId, userId.Value, cancellationToken);
            return this.StatusCode(201, ApiResponse<LedgerWriteResultDto>.SuccessResponse(result, result.Message));
        }
        catch (UnauthorizedAccessException ex) { return this.Error<LedgerWriteResultDto>(ex.Message, 403); }
        catch (InvalidOperationException ex) { return this.Error<LedgerWriteResultDto>(ex.Message, 400); }
    }

    /// <summary>Allocate (reserve) stock for an order.</summary>
    [HttpPost("allocate")]
    [RequirePermission(PermissionCatalog.InventoryEdit)]
    [ProducesResponseType(typeof(ApiResponse<LedgerWriteResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<LedgerWriteResultDto>>> Allocate(
        [FromBody] LedgerAllocateRequestDto dto,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, userId, resolvedDeptId) = await ResolveLedgerContextAsync(departmentId, cancellationToken);
        if (resolvedDeptId == null) return this.Error<LedgerWriteResultDto>("Department scope required", 403);
        if (userId == null) return this.Error<LedgerWriteResultDto>("User context required", 401);
        try
        {
            var result = await _stockLedgerService.AllocateAsync(dto, companyId, resolvedDeptId, userId.Value, cancellationToken);
            return this.StatusCode(201, ApiResponse<LedgerWriteResultDto>.SuccessResponse(result, result.Message));
        }
        catch (UnauthorizedAccessException ex) { return this.Error<LedgerWriteResultDto>(ex.Message, 403); }
        catch (InvalidOperationException ex) { return this.Error<LedgerWriteResultDto>(ex.Message, 400); }
    }

    /// <summary>Issue stock to order (ledger only).</summary>
    [HttpPost("issue")]
    [RequirePermission(PermissionCatalog.InventoryEdit)]
    [ProducesResponseType(typeof(ApiResponse<LedgerWriteResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<LedgerWriteResultDto>>> Issue(
        [FromBody] LedgerIssueRequestDto dto,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, userId, resolvedDeptId) = await ResolveLedgerContextAsync(departmentId, cancellationToken);
        if (resolvedDeptId == null) return this.Error<LedgerWriteResultDto>("Department scope required", 403);
        if (userId == null) return this.Error<LedgerWriteResultDto>("User context required", 401);
        try
        {
            var result = await _stockLedgerService.IssueAsync(dto, companyId, resolvedDeptId, userId.Value, cancellationToken);
            return this.StatusCode(201, ApiResponse<LedgerWriteResultDto>.SuccessResponse(result, result.Message));
        }
        catch (UnauthorizedAccessException ex) { return this.Error<LedgerWriteResultDto>(ex.Message, 403); }
        catch (InvalidOperationException ex) { return this.Error<LedgerWriteResultDto>(ex.Message, 400); }
    }

    /// <summary>Return stock from order (ledger only).</summary>
    [HttpPost("return")]
    [RequirePermission(PermissionCatalog.InventoryEdit)]
    [ProducesResponseType(typeof(ApiResponse<LedgerWriteResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<LedgerWriteResultDto>>> Return(
        [FromBody] LedgerReturnRequestDto dto,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, userId, resolvedDeptId) = await ResolveLedgerContextAsync(departmentId, cancellationToken);
        if (resolvedDeptId == null) return this.Error<LedgerWriteResultDto>("Department scope required", 403);
        if (userId == null) return this.Error<LedgerWriteResultDto>("User context required", 401);
        try
        {
            var result = await _stockLedgerService.ReturnAsync(dto, companyId, resolvedDeptId, userId.Value, cancellationToken);
            return this.StatusCode(201, ApiResponse<LedgerWriteResultDto>.SuccessResponse(result, result.Message));
        }
        catch (UnauthorizedAccessException ex) { return this.Error<LedgerWriteResultDto>(ex.Message, 403); }
        catch (InvalidOperationException ex) { return this.Error<LedgerWriteResultDto>(ex.Message, 400); }
    }

    /// <summary>Get ledger entries (filterable, paged).</summary>
    [HttpGet("ledger")]
    [RequirePermission(PermissionCatalog.InventoryView)]
    [ProducesResponseType(typeof(ApiResponse<LedgerListResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<LedgerListResultDto>>> GetLedger(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? materialId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] Guid? orderId = null,
        [FromQuery] string? entryType = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var (companyId, _, resolvedDeptId) = await ResolveLedgerContextAsync(departmentId, cancellationToken);
        if (resolvedDeptId == null) return this.Error<LedgerListResultDto>("Department scope required", 403);
        try
        {
            var filter = new LedgerFilterDto { MaterialId = materialId, LocationId = locationId, OrderId = orderId, EntryType = entryType, FromDate = fromDate, ToDate = toDate, Page = page, PageSize = pageSize };
            var result = await _stockLedgerService.GetLedgerAsync(filter, companyId, resolvedDeptId, cancellationToken);
            return this.Success(result);
        }
        catch (UnauthorizedAccessException ex) { return this.Error<LedgerListResultDto>(ex.Message, 403); }
    }

    /// <summary>Get stock summary by location and serialised status (from ledger).</summary>
    [HttpGet("stock-summary")]
    [RequirePermission(PermissionCatalog.InventoryView)]
    [ProducesResponseType(typeof(ApiResponse<StockSummaryResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<StockSummaryResultDto>>> GetStockSummary(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] Guid? materialId = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, _, resolvedDeptId) = await ResolveLedgerContextAsync(departmentId, cancellationToken);
        if (resolvedDeptId == null) return this.Error<StockSummaryResultDto>("Department scope required", 403);
        try
        {
            var result = await _stockLedgerService.GetStockSummaryAsync(companyId, resolvedDeptId, locationId, materialId, cancellationToken);
            return this.Success(result);
        }
        catch (UnauthorizedAccessException ex) { return this.Error<StockSummaryResultDto>(ex.Message, 403); }
    }

    // ============================================
    // Reports - GET JSON (Phase 2.2.1)
    // ============================================

    /// <summary>Usage summary report (JSON). Department scope required.</summary>
    [HttpGet("reports/usage-summary")]
    [RequirePermission(PermissionCatalog.InventoryView)]
    [ProducesResponseType(typeof(ApiResponse<UsageSummaryReportResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<UsageSummaryReportResultDto>>> GetUsageSummaryReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] string? groupBy = null,
        [FromQuery] Guid? materialId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        if (toDate < fromDate)
            return StatusCode(400, ApiResponse.ErrorResponse("ToDate must be >= FromDate"));
        if ((toDate - fromDate).TotalDays > StockLedgerService.MaxUsageSummaryDateRangeDays)
            return StatusCode(400, ApiResponse.ErrorResponse($"Date range cannot exceed {StockLedgerService.MaxUsageSummaryDateRangeDays} days."));
        Guid? resolvedDeptId = null;
        try
        {
            resolvedDeptId = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse.ErrorResponse("You do not have access to this department"));
        }
        if (resolvedDeptId == null)
            return StatusCode(403, ApiResponse.ErrorResponse("Department scope required"));
        try
        {
            var result = await _stockLedgerService.GetUsageSummaryReportAsync(fromDate, toDate, groupBy, materialId, locationId, companyId, resolvedDeptId, page, pageSize, cancellationToken);
            return this.Success(result);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return StatusCode(400, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage summary report");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to get report: {ex.Message}"));
        }
    }

    /// <summary>Serial lifecycle report (JSON). Department scope required; at least one serial required.</summary>
    [HttpGet("reports/serial-lifecycle")]
    [RequirePermission(PermissionCatalog.InventoryView)]
    [ProducesResponseType(typeof(ApiResponse<SerialLifecycleReportResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<SerialLifecycleReportResultDto>>> GetSerialLifecycleReport(
        [FromQuery] string? serialNumber = null,
        [FromQuery] string? serialNumbers = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        var list = new List<string>();
        if (!string.IsNullOrWhiteSpace(serialNumber)) list.Add(serialNumber.Trim());
        if (!string.IsNullOrWhiteSpace(serialNumbers))
            list.AddRange(serialNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(s => s.Trim()).Where(s => s.Length > 0));
        list = list.Distinct().ToList();
        if (list.Count == 0)
            return StatusCode(400, ApiResponse.ErrorResponse("At least one serial number is required (serialNumber or serialNumbers query parameter)"));
        if (list.Count > 50)
            return StatusCode(400, ApiResponse.ErrorResponse("Maximum 50 serial numbers per request"));
        Guid? resolvedDeptId = null;
        try
        {
            resolvedDeptId = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse.ErrorResponse("You do not have access to this department"));
        }
        if (resolvedDeptId == null)
            return StatusCode(403, ApiResponse.ErrorResponse("Department scope required"));
        try
        {
            var result = await _stockLedgerService.GetSerialLifecycleReportAsync(list, companyId, resolvedDeptId, page, pageSize, cancellationToken);
            return this.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting serial lifecycle report");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to get report: {ex.Message}"));
        }
    }

    /// <summary>Stock-by-location history (JSON). Simplified: current snapshot as single period. Department scope required.</summary>
    [HttpGet("reports/stock-by-location-history")]
    [RequirePermission(PermissionCatalog.InventoryView)]
    [ProducesResponseType(typeof(ApiResponse<StockByLocationHistoryResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<StockByLocationHistoryResultDto>>> GetStockByLocationHistoryReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] string? snapshotType = "Daily",
        [FromQuery] Guid? materialId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        if (toDate < fromDate)
            return StatusCode(400, ApiResponse.ErrorResponse("ToDate must be >= FromDate"));
        Guid? resolvedDeptId = null;
        try
        {
            resolvedDeptId = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse.ErrorResponse("You do not have access to this department"));
        }
        if (resolvedDeptId == null)
            return StatusCode(403, ApiResponse.ErrorResponse("Department scope required"));
        try
        {
            var result = await _stockLedgerService.GetStockByLocationHistoryReportAsync(fromDate, toDate, snapshotType ?? "Daily", materialId, locationId, companyId, resolvedDeptId, page, pageSize, cancellationToken);
            return this.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock-by-location history report");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to get report: {ex.Message}"));
        }
    }

    // ============================================
    // Reports - Export (Phase 2.2.4)
    // ============================================

    /// <summary>Export usage-by-period report as CSV. Department scope required; enforced via DepartmentAccessService.</summary>
    [HttpGet("reports/usage-summary/export")]
    [RequirePermission(PermissionCatalog.InventoryView)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportUsageSummaryReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] string? groupBy = null,
        [FromQuery] Guid? materialId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        if (toDate < fromDate)
            return StatusCode(400, ApiResponse.ErrorResponse("ToDate must be >= FromDate"));
        if ((toDate - fromDate).TotalDays > StockLedgerService.MaxUsageSummaryDateRangeDays)
            return StatusCode(400, ApiResponse.ErrorResponse($"Date range cannot exceed {StockLedgerService.MaxUsageSummaryDateRangeDays} days."));
        Guid? resolvedDeptId = null;
        try
        {
            resolvedDeptId = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse.ErrorResponse("You do not have access to this department"));
        }
        if (resolvedDeptId == null)
            return StatusCode(403, ApiResponse.ErrorResponse("Department scope required"));

        try
        {
            var rows = await _stockLedgerService.GetUsageSummaryExportRowsAsync(fromDate, toDate, groupBy, materialId, locationId, companyId, resolvedDeptId, cancellationToken);
            var csvBytes = _csvService.ExportToCsvBytes(rows);
            var fileName = $"usage-summary-{fromDate:yyyy-MM-dd}-to-{toDate:yyyy-MM-dd}.csv";
            return File(csvBytes, "text/csv", fileName);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return StatusCode(400, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting usage summary report");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to export: {ex.Message}"));
        }
    }

    /// <summary>Export serial lifecycle report as CSV. Department scope required; at least one serial number required.</summary>
    [HttpGet("reports/serial-lifecycle/export")]
    [RequirePermission(PermissionCatalog.InventoryView)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportSerialLifecycleReport(
        [FromQuery] string? serialNumber = null,
        [FromQuery] string? serialNumbers = null,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));

        var list = new List<string>();
        if (!string.IsNullOrWhiteSpace(serialNumber)) list.Add(serialNumber.Trim());
        if (!string.IsNullOrWhiteSpace(serialNumbers))
            list.AddRange(serialNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(s => s.Trim()).Where(s => s.Length > 0));
        list = list.Distinct().ToList();
        if (list.Count == 0)
            return StatusCode(400, ApiResponse.ErrorResponse("At least one serial number is required (serialNumber or serialNumbers query parameter)"));
        if (list.Count > 50)
            return StatusCode(400, ApiResponse.ErrorResponse("Maximum 50 serial numbers per request"));

        Guid? resolvedDeptId = null;
        try
        {
            resolvedDeptId = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse.ErrorResponse("You do not have access to this department"));
        }
        if (resolvedDeptId == null)
            return StatusCode(403, ApiResponse.ErrorResponse("Department scope required"));

        try
        {
            var rows = await _stockLedgerService.GetSerialLifecycleExportRowsAsync(list, companyId, resolvedDeptId, cancellationToken);
            var csvBytes = _csvService.ExportToCsvBytes(rows);
            var fileName = $"serial-lifecycle-{DateTime.UtcNow:yyyy-MM-dd}.csv";
            return File(csvBytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting serial lifecycle report");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to export: {ex.Message}"));
        }
    }

    /// <summary>Schedule an inventory report export job (optional email delivery). Same RBAC as export; department scope required.</summary>
    [HttpPost("reports/export/schedule")]
    [RequirePermission(PermissionCatalog.InventoryEdit)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ScheduleReportExport(
        [FromBody] ScheduleInventoryReportExportRequest request,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        if (string.IsNullOrWhiteSpace(request?.ReportType))
            return StatusCode(400, ApiResponse.ErrorResponse("ReportType is required (UsageSummary or SerialLifecycle)"));
        var reportType = request.ReportType!.Trim();
        if (!string.Equals(reportType, "UsageSummary", StringComparison.OrdinalIgnoreCase) && !string.Equals(reportType, "SerialLifecycle", StringComparison.OrdinalIgnoreCase))
            return StatusCode(400, ApiResponse.ErrorResponse("ReportType must be UsageSummary or SerialLifecycle"));
        Guid? resolvedDeptId = null;
        try
        {
            resolvedDeptId = await _departmentAccessService.ResolveDepartmentScopeAsync(request.DepartmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse.ErrorResponse("You do not have access to this department"));
        }
        if (resolvedDeptId == null)
            return StatusCode(403, ApiResponse.ErrorResponse("Department scope required"));
        if (string.Equals(reportType, "UsageSummary", StringComparison.OrdinalIgnoreCase) && (!request.FromDate.HasValue || !request.ToDate.HasValue))
            return StatusCode(400, ApiResponse.ErrorResponse("FromDate and ToDate are required for UsageSummary"));
        if (string.Equals(reportType, "SerialLifecycle", StringComparison.OrdinalIgnoreCase) && (request.SerialNumbers == null || request.SerialNumbers.Count == 0))
            return StatusCode(400, ApiResponse.ErrorResponse("SerialNumbers is required for SerialLifecycle"));
        if (request.SerialNumbers != null && request.SerialNumbers.Count > 50)
            return StatusCode(400, ApiResponse.ErrorResponse("Maximum 50 serial numbers allowed"));

        var payload = new Dictionary<string, object?>
        {
            ["reportType"] = reportType,
            ["companyId"] = companyId?.ToString(),
            ["departmentId"] = resolvedDeptId.Value.ToString(),
        };
        if (string.Equals(reportType, "UsageSummary", StringComparison.OrdinalIgnoreCase))
        {
            payload["fromDate"] = request.FromDate!.Value.ToString("O");
            payload["toDate"] = request.ToDate!.Value.ToString("O");
            if (!string.IsNullOrWhiteSpace(request.GroupBy)) payload["groupBy"] = request.GroupBy.Trim();
            if (request.MaterialId.HasValue) payload["materialId"] = request.MaterialId.Value.ToString();
            if (request.LocationId.HasValue) payload["locationId"] = request.LocationId.Value.ToString();
        }
        else
        {
            payload["serialNumbers"] = string.Join(",", request.SerialNumbers!.Take(50));
        }
        if (!string.IsNullOrWhiteSpace(request.EmailTo)) payload["emailTo"] = request.EmailTo!.Trim();
        if (request.EmailAccountId.HasValue) payload["emailAccountId"] = request.EmailAccountId.Value.ToString();

        var payloadJson = JsonSerializer.Serialize(payload);
        var jobId = await _jobExecutionEnqueuer.EnqueueWithIdAsync("inventoryreportexport", payloadJson, companyId: companyId, cancellationToken: cancellationToken);
        return Accepted(ApiResponse.SuccessResponse(new { jobId }, "Report export job scheduled"));
    }

    private async Task<(Guid? CompanyId, Guid? UserId, Guid? ResolvedDepartmentId)> ResolveLedgerContextAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
            return (null, null, null);
        Guid? resolved = null;
        try
        {
            resolved = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException) { /* caller will return 403 */ }
        return (companyId ?? Guid.Empty, _currentUserService.UserId, resolved);
    }

    // ============================================
    // Materials - Import/Export
    // ============================================

    /// <summary>
    /// Export materials to CSV
    /// </summary>
    [HttpGet("materials/export")]
    [RequirePermission(PermissionCatalog.InventoryView)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportMaterials(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        }

        Guid? resolvedDepartmentId = null;
        try
        {
            resolvedDepartmentId = await _departmentAccessService.ResolveDepartmentScopeAsync(
                departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse.ErrorResponse("You do not have access to this department"));
        }

        try
        {
            var materials = await _inventoryService.GetMaterialsAsync(companyId, resolvedDepartmentId, category, null, isActive, cancellationToken);
            
            var csvData = materials.Select(m => new MaterialCsvDto
            {
                Code = m.ItemCode ?? string.Empty,
                Description = m.Description ?? string.Empty,
                CategoryName = m.Category ?? string.Empty,
                UnitOfMeasure = m.UnitOfMeasure ?? string.Empty,
                UnitCost = m.DefaultCost ?? 0,
                IsSerialised = m.IsSerialised,
                MinStockLevel = null,
                ReorderPoint = null,
                IsActive = m.IsActive
            }).ToList();

            var csvBytes = _csvService.ExportToCsvBytes(csvData);
            // File downloads don't use ApiResponse envelope - they return file content directly
            return File(csvBytes, "text/csv", $"materials-{DateTime.UtcNow:yyyy-MM-dd}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting materials");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to export materials: {ex.Message}"));
        }
    }

    /// <summary>
    /// Download materials CSV template
    /// </summary>
    [HttpGet("materials/template")]
    [RequirePermission(PermissionCatalog.InventoryView)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public IActionResult GetMaterialsTemplate()
    {
        try
        {
            var templateBytes = _csvService.GenerateTemplateBytes<MaterialCsvDto>();
            // File downloads don't use ApiResponse envelope - they return file content directly
            return File(templateBytes, "text/csv", "materials-template.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating materials template");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to generate template: {ex.Message}"));
        }
    }

    /// <summary>
    /// Import materials from CSV
    /// </summary>
    [HttpPost("materials/import")]
    [RequirePermission(PermissionCatalog.InventoryEdit)]
    [ProducesResponseType(typeof(ApiResponse<ImportResult<MaterialCsvDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ImportResult<MaterialCsvDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ImportResult<MaterialCsvDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ImportResult<MaterialCsvDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ImportResult<MaterialCsvDto>>>> ImportMaterials(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
        {
            return this.Error<ImportResult<MaterialCsvDto>>("Company context required", 401);
        }

        if (file == null || file.Length == 0)
        {
            return this.Error<ImportResult<MaterialCsvDto>>("No file uploaded", 400);
        }

        try
        {
            using var stream = file.OpenReadStream();
            var records = _csvService.ImportFromCsv<MaterialCsvDto>(stream);

            var result = new ImportResult<MaterialCsvDto>
            {
                TotalRows = records.Count
            };

            var (effectiveCompanyId, importErr) = this.RequireCompanyId(_tenantProvider);
            if (importErr != null) return importErr;
            var userId = _currentUserService.UserId ?? Guid.Empty;

            // Pre-load reference data for lookups
            var materialCategories = await _context.Set<Domain.Inventory.Entities.MaterialCategory>()
                .Where(c => c.CompanyId == effectiveCompanyId)
                .ToListAsync(cancellationToken);
            var categoryLookup = materialCategories.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

            // Get first active partner as default (materials require at least one partner)
            var defaultPartner = await _context.Partners
                .Where(p => p.IsActive && p.CompanyId == effectiveCompanyId)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (defaultPartner == null)
            {
                return this.Error<ImportResult<MaterialCsvDto>>(
                    "No active partner found. Materials require at least one partner. Please create a partner first.", 400);
            }

            foreach (var (record, index) in records.Select((r, i) => (r, i + 2)))
            {
                try
                {
                    // Resolve category by name
                    Guid? categoryId = null;
                    if (!string.IsNullOrWhiteSpace(record.CategoryName))
                    {
                        if (categoryLookup.TryGetValue(record.CategoryName.Trim(), out var category))
                        {
                            categoryId = category.Id;
                        }
                        else
                        {
                            _logger.LogWarning("Material category '{CategoryName}' not found for material '{Code}'. Skipping category assignment.", 
                                record.CategoryName, record.Code);
                        }
                    }

                    // Validate unit of measure (common values: PCS, M, KG, etc.)
                    var validUnits = new[] { "PCS", "M", "KG", "L", "SET", "UNIT", "BOX", "ROLL", "METER", "PIECE" };
                    var unitOfMeasure = record.UnitOfMeasure.Trim().ToUpper();
                    if (!validUnits.Contains(unitOfMeasure))
                    {
                        _logger.LogWarning("Unit of measure '{Unit}' may not be standard for material '{Code}'. Using as-is.", 
                            unitOfMeasure, record.Code);
                    }

                    // Map CSV DTO to CreateMaterialDto
                    var createDto = new CreateMaterialDto
                    {
                        ItemCode = record.Code.Trim(),
                        Description = record.Description.Trim(),
                        MaterialCategoryId = categoryId,
                        Category = string.IsNullOrWhiteSpace(record.CategoryName) ? null : record.CategoryName.Trim(),
                        UnitOfMeasure = unitOfMeasure,
                        DefaultCost = record.UnitCost,
                        IsSerialised = record.IsSerialised,
                        // Use default partner since CSV doesn't include partner info
                        PartnerIds = new List<Guid> { defaultPartner.Id }
                    };

                    // Create material
                    await _inventoryService.CreateMaterialAsync(createDto, companyId != Guid.Empty ? companyId : null, cancellationToken);
                    
                    result.SuccessCount++;
                    result.ImportedRecords.Add(record);
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = index,
                        Message = ex.Message
                    });
                    _logger.LogWarning(ex, "Error importing material at row {RowNumber}: {MaterialCode}", index, record.Code);
                }
            }

            return this.Success(result, "Materials import completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing materials");
            return this.Error<ImportResult<MaterialCsvDto>>($"Failed to import materials: {ex.Message}", 500);
        }
    }
}

