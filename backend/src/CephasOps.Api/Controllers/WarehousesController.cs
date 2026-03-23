using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Departments.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/warehouses")]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _service;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ICurrentUserService _currentUserService;

    public WarehousesController(
        IWarehouseService service,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ICurrentUserService currentUserService)
    {
        _service = service;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<WarehouseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<WarehouseDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<WarehouseDto>>>> GetAll(
        [FromQuery] Guid? companyId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (departmentId.HasValue || _departmentRequestContext.DepartmentId.HasValue)
        {
            try
            {
                await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                return this.Error<List<WarehouseDto>>("You do not have access to this department", 403);
            }
        }

        try
        {
            var currentTenantId = _tenantProvider.CurrentTenantId;
            Guid? effectiveCompanyId;
            if (companyId.HasValue && companyId.Value != Guid.Empty)
            {
                if (currentTenantId != companyId.Value && !_currentUserService.IsSuperAdmin)
                    return this.Forbidden<List<WarehouseDto>>("You cannot access another tenant's warehouses.");
                effectiveCompanyId = companyId;
            }
            else
                effectiveCompanyId = currentTenantId;
            if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
                return this.Forbidden<List<WarehouseDto>>("Company context is required for this operation.");
            var items = await _service.GetAllAsync(effectiveCompanyId.Value, isActive);
            return this.Success(items);
        }
        catch (Exception ex)
        {
            return this.Error<List<WarehouseDto>>($"Failed to get warehouses: {ex.Message}", 500);
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
                return this.NotFound<WarehouseDto>($"Warehouse with ID {id} not found");
            var currentTenantId = _tenantProvider.CurrentTenantId;
            if (!_currentUserService.IsSuperAdmin && currentTenantId.HasValue && currentTenantId.Value != Guid.Empty && item.CompanyId != currentTenantId.Value)
                return this.NotFound<WarehouseDto>($"Warehouse with ID {id} not found");
            return this.Success(item);
        }
        catch (Exception ex)
        {
            return this.Error<WarehouseDto>($"Failed to get warehouse: {ex.Message}", 500);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> Create([FromQuery] Guid? companyId, [FromBody] WarehouseDto dto)
    {
        var currentTenantId = _tenantProvider.CurrentTenantId;
        Guid effectiveCompanyId;
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            if (!_currentUserService.IsSuperAdmin && (!currentTenantId.HasValue || currentTenantId.Value != companyId.Value))
                return this.Forbidden<WarehouseDto>("You cannot create a warehouse for another tenant.");
            effectiveCompanyId = companyId.Value;
        }
        else
        {
            if (!currentTenantId.HasValue || currentTenantId.Value == Guid.Empty)
                return this.Forbidden<WarehouseDto>("Company context is required for this operation.");
            effectiveCompanyId = currentTenantId.Value;
        }
        try
        {
            var item = await _service.CreateAsync(effectiveCompanyId, dto);
            return this.StatusCode(201, ApiResponse<WarehouseDto>.SuccessResponse(item, "Warehouse created successfully"));
        }
        catch (Exception ex)
        {
            return this.Error<WarehouseDto>($"Failed to create warehouse: {ex.Message}", 500);
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> Update(Guid id, [FromBody] WarehouseDto dto)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing == null)
            return this.NotFound<WarehouseDto>($"Warehouse with ID {id} not found");
        var currentTenantId = _tenantProvider.CurrentTenantId;
        if (!_currentUserService.IsSuperAdmin && currentTenantId.HasValue && currentTenantId.Value != Guid.Empty && (existing.CompanyId == null || existing.CompanyId != currentTenantId.Value))
            return this.NotFound<WarehouseDto>($"Warehouse with ID {id} not found");
        try
        {
            var item = await _service.UpdateAsync(id, dto);
            return this.Success(item, "Warehouse updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<WarehouseDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.Error<WarehouseDto>($"Failed to update warehouse: {ex.Message}", 500);
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing == null)
            return StatusCode(404, ApiResponse.ErrorResponse($"Warehouse with ID {id} not found"));
        var currentTenantId = _tenantProvider.CurrentTenantId;
        if (!_currentUserService.IsSuperAdmin && currentTenantId.HasValue && currentTenantId.Value != Guid.Empty && (existing.CompanyId == null || existing.CompanyId != currentTenantId.Value))
            return StatusCode(404, ApiResponse.ErrorResponse($"Warehouse with ID {id} not found"));
        try
        {
            await _service.DeleteAsync(id);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Warehouse deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete warehouse: {ex.Message}"));
        }
    }
}

