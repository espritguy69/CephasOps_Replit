using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Companies.DTOs;
using CephasOps.Application.Companies.Services;
using CephasOps.Application.Platform;
using CephasOps.Application.Provisioning;
using CephasOps.Application.Provisioning.DTOs;
using CephasOps.Domain.Authorization;
using CephasOps.Domain.Companies.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>Platform admin only: tenant provisioning, list, diagnostics, suspend/resume.</summary>
[ApiController]
[Route("api/platform/tenants")]
[Authorize(Roles = "SuperAdmin")]
public class TenantProvisioningController : ControllerBase
{
    private readonly ICompanyProvisioningService _provisioningService;
    private readonly ICompanyService _companyService;
    private readonly IPlatformAdminService _platformAdminService;
    private readonly ILogger<TenantProvisioningController> _logger;

    public TenantProvisioningController(
        ICompanyProvisioningService provisioningService,
        ICompanyService companyService,
        IPlatformAdminService platformAdminService,
        ILogger<TenantProvisioningController> logger)
    {
        _provisioningService = provisioningService;
        _companyService = companyService;
        _platformAdminService = platformAdminService;
        _logger = logger;
    }

    /// <summary>List tenants with company and subscription summary. SuperAdmin only.</summary>
    [HttpGet]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<List<PlatformTenantListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<PlatformTenantListDto>>>> ListTenants(
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var list = await _platformAdminService.ListTenantsAsync(search, skip, take, cancellationToken);
        return this.Success(list);
    }

    /// <summary>Get tenant diagnostics (users, orders, subscription). SuperAdmin only.</summary>
    [HttpGet("{tenantId:guid}/diagnostics")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<PlatformTenantDiagnosticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PlatformTenantDiagnosticsDto>>> GetTenantDiagnostics(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var diagnostics = await _platformAdminService.GetTenantDiagnosticsAsync(tenantId, cancellationToken);
        if (diagnostics == null)
            return this.NotFound<PlatformTenantDiagnosticsDto>($"Tenant {tenantId} not found");
        return this.Success(diagnostics);
    }

    /// <summary>Suspend a tenant (set company status to Suspended). SuperAdmin only.</summary>
    [HttpPost("{tenantId:guid}/suspend")]
    [RequirePermission(PermissionCatalog.AdminTenantsEdit)]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> SuspendTenant(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var companyId = await _platformAdminService.GetCompanyIdByTenantIdAsync(tenantId, cancellationToken);
        if (!companyId.HasValue)
            return this.NotFound<CompanyDto>($"No company found for tenant {tenantId}");
        var company = await _companyService.SetCompanyStatusAsync(companyId.Value, CompanyStatus.Suspended, cancellationToken);
        return this.Success(company!, "Tenant suspended.");
    }

    /// <summary>Resume a tenant (set company status to Active). SuperAdmin only.</summary>
    [HttpPost("{tenantId:guid}/resume")]
    [RequirePermission(PermissionCatalog.AdminTenantsEdit)]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> ResumeTenant(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var companyId = await _platformAdminService.GetCompanyIdByTenantIdAsync(tenantId, cancellationToken);
        if (!companyId.HasValue)
            return this.NotFound<CompanyDto>($"No company found for tenant {tenantId}");
        var company = await _companyService.SetCompanyStatusAsync(companyId.Value, CompanyStatus.Active, cancellationToken);
        return this.Success(company!, "Tenant resumed.");
    }

    /// <summary>Provision a new tenant (company, default departments, tenant admin user).</summary>
    [HttpPost("provision")]
    [RequirePermission(PermissionCatalog.AdminTenantsEdit)]
    [ProducesResponseType(typeof(ApiResponse<ProvisionTenantResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProvisionTenantResultDto>>> Provision(
        [FromBody] ProvisionTenantRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _provisioningService.ProvisionAsync(request, cancellationToken);
            return StatusCode(201, ApiResponse<ProvisionTenantResultDto>.SuccessResponse(result, "Tenant provisioned successfully."));
        }
        catch (ArgumentException ex)
        {
            return this.Error<ProvisionTenantResultDto>(ex.Message, 400);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Provisioning conflict");
            return StatusCode(409, ApiResponse<ProvisionTenantResultDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>Check if a company code is available.</summary>
    [HttpGet("check-code")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> CheckCode([FromQuery] string code, CancellationToken cancellationToken = default)
    {
        var inUse = await _provisioningService.IsCompanyCodeInUseAsync(code ?? "", cancellationToken);
        return this.Success<object>(new CheckCodeResult { Code = code?.Trim() ?? "", InUse = inUse });
    }

    /// <summary>Check if a slug is available.</summary>
    [HttpGet("check-slug")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> CheckSlug([FromQuery] string slug, CancellationToken cancellationToken = default)
    {
        var inUse = await _provisioningService.IsSlugInUseAsync(slug ?? "", cancellationToken);
        return this.Success<object>(new CheckSlugResult { Slug = slug?.Trim() ?? "", InUse = inUse });
    }

    /// <summary>Set company (tenant) lifecycle status: Active, Suspended, Disabled, Trial, etc.</summary>
    [HttpPatch("companies/{companyId:guid}/status")]
    [RequirePermission(PermissionCatalog.AdminTenantsEdit)]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> SetCompanyStatus(
        Guid companyId,
        [FromBody] SetCompanyStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<CompanyStatus>(request.Status, ignoreCase: true, out var status))
            return this.Error<CompanyDto>("Invalid status. Use: Active, Suspended, Disabled, Trial, Archived, PendingProvisioning", 400);
        var company = await _companyService.SetCompanyStatusAsync(companyId, status, cancellationToken);
        if (company == null)
            return this.NotFound<CompanyDto>($"Company {companyId} not found");
        return this.Success(company, "Company status updated.");
    }

    /// <summary>Get tenant subscription (current active/trialing or latest). SuperAdmin only.</summary>
    [HttpGet("{tenantId:guid}/subscription")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> GetTenantSubscription(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var sub = await _platformAdminService.GetTenantSubscriptionAsync(tenantId, cancellationToken);
        if (sub == null)
            return this.NotFound<object>($"No subscription found for tenant {tenantId}");
        return this.Success<object>(sub);
    }

    /// <summary>Update tenant subscription (plan, status, trial end, limits). SuperAdmin only.</summary>
    [HttpPatch("{tenantId:guid}/subscription")]
    [RequirePermission(PermissionCatalog.AdminTenantsEdit)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateTenantSubscription(
        Guid tenantId,
        [FromBody] PlatformTenantSubscriptionUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            return this.Error<object>("Request body required", 400);
        try
        {
            var sub = await _platformAdminService.UpdateTenantSubscriptionAsync(tenantId, request, cancellationToken);
            if (sub == null)
                return this.NotFound<object>($"No subscription found for tenant {tenantId}");
            return this.Success<object>(sub, "Subscription updated.");
        }
        catch (ArgumentException ex)
        {
            return this.Error<object>(ex.Message, 400);
        }
    }
}

/// <summary>Request body for setting company lifecycle status.</summary>
public class SetCompanyStatusRequest
{
    public string Status { get; set; } = "Active";
}

/// <summary>Result of check-code availability.</summary>
public class CheckCodeResult
{
    public string Code { get; set; } = string.Empty;
    public bool InUse { get; set; }
}

/// <summary>Result of check-slug availability.</summary>
public class CheckSlugResult
{
    public string Slug { get; set; } = string.Empty;
    public bool InUse { get; set; }
}
