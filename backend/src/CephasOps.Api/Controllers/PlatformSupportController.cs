using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Auth.DTOs;
using CephasOps.Application.Auth.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Platform;
using CephasOps.Domain.Authorization;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Api.Controllers;

/// <summary>Platform support: tenant diagnostics, logs hint, impersonation, retry jobs. SuperAdmin only. Actions are audit-logged.</summary>
[ApiController]
[Route("api/platform/support")]
[Authorize(Roles = "SuperAdmin")]
public class PlatformSupportController : ControllerBase
{
    private readonly IPlatformAdminService _platformAdminService;
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PlatformSupportController> _logger;

    public PlatformSupportController(
        IPlatformAdminService platformAdminService,
        IAuthService authService,
        ICurrentUserService currentUserService,
        ApplicationDbContext context,
        ILogger<PlatformSupportController> logger)
    {
        _platformAdminService = platformAdminService;
        _authService = authService;
        _currentUserService = currentUserService;
        _context = context;
        _logger = logger;
    }

    /// <summary>Impersonate a tenant admin. Returns JWT for the first Admin/SuperAdmin user of the tenant. SuperAdmin only; audit-logged.</summary>
    [HttpPost("impersonate")]
    [RequirePermission(PermissionCatalog.AdminTenantsEdit)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Impersonate(
        [FromBody] ImpersonateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request?.TenantId == null)
            return this.Error<LoginResponseDto>("TenantId is required", 400);

        var requestedByUserId = _currentUserService.UserId;
        if (!requestedByUserId.HasValue)
            return this.Unauthorized<LoginResponseDto>("Current user required");

        var companyId = await _platformAdminService.GetCompanyIdByTenantIdAsync(request.TenantId.Value, cancellationToken);
        if (!companyId.HasValue)
            return this.NotFound<LoginResponseDto>($"Tenant {request.TenantId} not found");

        var adminUser = await _context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.CompanyId == companyId && u.IsActive)
            .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
            .Join(_context.Roles, x => x.ur.RoleId, r => r.Id, (x, r) => new { x.u, r.Name })
            .Where(x => x.Name == "Admin" || x.Name == "SuperAdmin")
            .Select(x => x.u)
            .FirstOrDefaultAsync(cancellationToken);

        if (adminUser == null)
            return this.NotFound<LoginResponseDto>($"No admin user found for tenant {request.TenantId}");

        try
        {
            var loginResponse = await _authService.CreateTokenForImpersonationAsync(adminUser.Id, requestedByUserId.Value, cancellationToken);
            return this.Success(loginResponse, "Impersonation token issued. Use AccessToken as Bearer for subsequent requests.");
        }
        catch (InvalidOperationException ex)
        {
            return this.Error<LoginResponseDto>(ex.Message, 400);
        }
    }

    /// <summary>Get tenant diagnostics (users, orders, subscription). Use for support triage.</summary>
    [HttpGet("tenants/{tenantId:guid}/diagnostics")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> GetTenantDiagnostics(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var diagnostics = await _platformAdminService.GetTenantDiagnosticsAsync(tenantId, cancellationToken);
        if (diagnostics == null)
            return this.NotFound<object>($"Tenant {tenantId} not found");
        return this.Success<object>(diagnostics);
    }

    /// <summary>Get hint for viewing tenant logs. Query your log aggregation (e.g. Seq, Application Insights) filtered by TenantId.</summary>
    [HttpGet("tenants/{tenantId:guid}/logs")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> GetTenantLogsHint(Guid tenantId)
    {
        _logger.LogInformation("Support: logs hint requested for TenantId={TenantId}", tenantId);
        return this.Success<object>(new
        {
            tenantId,
            message = "Query your log aggregation (e.g. Seq, Application Insights) filtered by TenantId. Structured logs include TenantId and CompanyId.",
            filterExample = $"TenantId = \"{tenantId}\""
        });
    }

    /// <summary>Retry a failed or dead-letter job for a tenant. Job must belong to the tenant. Resets status to Pending and schedules immediate run.</summary>
    [HttpPost("tenants/{tenantId:guid}/jobs/{jobExecutionId:guid}/retry")]
    [RequirePermission(PermissionCatalog.AdminTenantsEdit)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> RetryTenantJob(
        Guid tenantId,
        Guid jobExecutionId,
        CancellationToken cancellationToken = default)
    {
        var companyIds = await _context.Companies
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        if (companyIds.Count == 0)
            return this.NotFound<object>($"Tenant {tenantId} not found");

        var job = await _context.Set<JobExecution>()
            .FirstOrDefaultAsync(j => j.Id == jobExecutionId && j.CompanyId != null && companyIds.Contains(j.CompanyId.Value), cancellationToken);
        if (job == null)
            return this.NotFound<object>($"Job {jobExecutionId} not found for tenant {tenantId}");

        job.Status = "Pending";
        job.ProcessingNodeId = null;
        job.ProcessingLeaseExpiresAtUtc = null;
        job.ClaimedAtUtc = null;
        job.NextRunAtUtc = DateTime.UtcNow;
        job.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Support: job {JobId} retried by admin for TenantId={TenantId}", jobExecutionId, tenantId);
        return this.Success<object>(new { jobExecutionId, status = "Pending", nextRunAtUtc = job.NextRunAtUtc }, "Job scheduled for retry.");
    }
}

/// <summary>Request body for POST /api/platform/support/impersonate.</summary>
public class ImpersonateRequest
{
    public Guid? TenantId { get; set; }
}
