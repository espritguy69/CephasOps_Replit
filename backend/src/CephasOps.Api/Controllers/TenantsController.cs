using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Tenants.DTOs;
using CephasOps.Application.Tenants.Services;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Tenant management for multi-tenant SaaS (Phase 11). Admin only.
/// </summary>
[ApiController]
[Route("api/tenants")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(ITenantService tenantService, ILogger<TenantsController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>List tenants with optional active filter.</summary>
    [HttpGet]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<List<TenantDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TenantDto>>>> List(
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var list = await _tenantService.ListAsync(isActive, cancellationToken);
        return this.Success(list);
    }

    /// <summary>Get tenant by ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantService.GetByIdAsync(id, cancellationToken);
        if (tenant == null)
            return this.NotFound<TenantDto>($"Tenant {id} not found");
        return this.Success(tenant);
    }

    /// <summary>Get tenant by slug.</summary>
    [HttpGet("by-slug/{slug}")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantDto>>> GetBySlug(string slug, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantService.GetBySlugAsync(slug, cancellationToken);
        if (tenant == null)
            return this.NotFound<TenantDto>($"Tenant with slug '{slug}' not found");
        return this.Success(tenant);
    }

    /// <summary>Create a tenant.</summary>
    [HttpPost]
    [RequirePermission(PermissionCatalog.AdminTenantsEdit)]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TenantDto>>> Create(
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Slug))
            return BadRequest(ApiResponse<TenantDto>.ErrorResponse("Name and Slug are required."));
        var tenant = await _tenantService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, ApiResponse<TenantDto>.SuccessResponse(tenant, "Tenant created."));
    }

    /// <summary>Update a tenant.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionCatalog.AdminTenantsEdit)]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantDto>>> Update(
        Guid id,
        [FromBody] UpdateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantService.UpdateAsync(id, request, cancellationToken);
        if (tenant == null)
            return this.NotFound<TenantDto>($"Tenant {id} not found");
        return this.Success(tenant);
    }
}
