using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Orders.Services;
using OrderCategoryDto = CephasOps.Application.Orders.DTOs.OrderCategoryDto;
using CreateOrderCategoryDto = CephasOps.Application.Orders.DTOs.CreateOrderCategoryDto;
using UpdateOrderCategoryDto = CephasOps.Application.Orders.DTOs.UpdateOrderCategoryDto;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// InstallationType management endpoints
/// </summary>
[ApiController]
[Route("api/installation-types")]
[Authorize]
public class InstallationTypesController : ControllerBase
{
    private readonly IOrderCategoryService _orderCategoryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<InstallationTypesController> _logger;

    public InstallationTypesController(
        IOrderCategoryService orderCategoryService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<InstallationTypesController> logger)
    {
        _orderCategoryService = orderCategoryService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    /// <summary>
    /// Get installation types list
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<OrderCategoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<OrderCategoryDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<OrderCategoryDto>>>> GetInstallationTypes(
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
            var installationTypes = await _orderCategoryService.GetOrderCategoriesAsync(companyId, departmentScope, isActive, cancellationToken);
            return this.Success(installationTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting installation types");
            return this.InternalServerError<List<OrderCategoryDto>>($"Failed to get installation types: {ex.Message}");
        }
    }

    /// <summary>
    /// Get installation type by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<OrderCategoryDto>>> GetInstallationType(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var installationType = await _orderCategoryService.GetOrderCategoryByIdAsync(id, companyId, cancellationToken);
            if (installationType == null)
            {
                return this.NotFound<OrderCategoryDto>($"InstallationType with ID {id} not found");
            }
            return this.Success(installationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting installation type {InstallationTypeId}", id);
            return this.InternalServerError<OrderCategoryDto>($"Failed to get installation type: {ex.Message}");
        }
    }

    /// <summary>
    /// Create new installation type
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<OrderCategoryDto>>> CreateInstallationType(
        [FromBody] CreateOrderCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var installationType = await _orderCategoryService.CreateOrderCategoryAsync(dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetInstallationType), new { id = installationType.Id }, installationType, "Installation type created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating installation type");
            return this.InternalServerError<OrderCategoryDto>($"Failed to create installation type: {ex.Message}");
        }
    }

    /// <summary>
    /// Update installation type
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<OrderCategoryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<OrderCategoryDto>>> UpdateInstallationType(
        Guid id,
        [FromBody] UpdateOrderCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var installationType = await _orderCategoryService.UpdateOrderCategoryAsync(id, dto, companyId, cancellationToken);
            return this.Success(installationType, "Installation type updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<OrderCategoryDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating installation type {InstallationTypeId}", id);
            return this.InternalServerError<OrderCategoryDto>($"Failed to update installation type: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete installation type
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteInstallationType(
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
            _logger.LogError(ex, "Error deleting installation type {InstallationTypeId}", id);
            return this.InternalServerError($"Failed to delete installation type: {ex.Message}");
        }
    }
}
