using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>Phase 14: Operational control plane - index of operator APIs.</summary>
[ApiController]
[Route("api/admin/control-plane")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class ControlPlaneController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public ControlPlaneController(ICurrentUserService currentUser, ITenantProvider tenantProvider)
    {
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    /// <summary>List operator capability groups and base paths for events, commands, jobs, integration, replay, trace, workers, observability.</summary>
    [HttpGet]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<ControlPlaneSummaryDto>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<ControlPlaneSummaryDto>> GetSummary()
    {
        var dto = new ControlPlaneSummaryDto
        {
            Capabilities = new List<ControlPlaneCapabilityDto>
            {
                new() { Name = "Operations overview", BasePath = "/api/admin/operations/overview", Description = "Job executions, event store, payout health, system health (last 24h), recent platform guard violations" },
                new() { Name = "SI operational insights", BasePath = "/api/admin/operations/si-insights", Description = "Service Installer field ops intelligence: completion performance, reschedule/blocker patterns, material replacements, assurance/rework, hotspots (company-scoped)" },
                new() { Name = "Event store", BasePath = "/api/event-store", Description = "Inspect events, lineage, ledger" },
                new() { Name = "Event replay", BasePath = "/api/event-store/replay", Description = "Replay operations" },
                new() { Name = "Rebuild", BasePath = "/api/event-store/rebuild", Description = "Rebuild operations" },
                new() { Name = "Command orchestration", BasePath = "/api/command-orchestration", Description = "Inspect commands, processing log" },
                new() { Name = "Background jobs", BasePath = "/api/background-jobs", Description = "Job runs, retry, enqueue" },
                new() { Name = "Job orchestration", BasePath = "/api/job-orchestration", Description = "Job definitions, execution" },
                new() { Name = "System workers", BasePath = "/api/system/workers", Description = "Worker instances" },
                new() { Name = "Integration", BasePath = "/api/integration", Description = "Connectors, outbound deliveries, inbound receipts, replay" },
                new() { Name = "Trace", BasePath = "/api/trace", Description = "Trace explorer by correlation/event/job" },
                new() { Name = "Operational trace", BasePath = "/api/operational-trace", Description = "Trace explorer alias" },
                new() { Name = "Observability", BasePath = "/api/observability", Description = "Events, insights; health at /health" },
                new() { Name = "Observability insights", BasePath = "/api/observability/insights", Description = "Field ops intelligence (OperationalInsights)" },
                new() { Name = "Tenants", BasePath = "/api/tenants", Description = "Tenant management (Phase 11)" },
                new() { Name = "Billing plans", BasePath = "/api/billing/plans", Description = "Subscription plans (Phase 12)" },
                new() { Name = "Tenant subscriptions", BasePath = "/api/billing/subscriptions", Description = "Tenant subscription management" }
            }
        };
        return this.Success(dto);
    }

    /// <summary>Tenant diagnostics: company id and links to event-store, integration, observability for the current (or specified) tenant.</summary>
    [HttpGet("tenant-diagnostics")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<TenantDiagnosticsDto>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<TenantDiagnosticsDto>> GetTenantDiagnostics([FromQuery] Guid? companyId = null)
    {
        var effectiveCompanyId = companyId ?? _tenantProvider.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
        {
            return this.Success(new TenantDiagnosticsDto
            {
                Message = "No company scope; specify ?companyId= for SuperAdmin or use a tenant-scoped user."
            });
        }
        var dto = new TenantDiagnosticsDto
        {
            CompanyId = effectiveCompanyId.Value,
            Links = new TenantDiagnosticsLinksDto
            {
                EventStore = $"/api/event-store?companyId={effectiveCompanyId.Value}",
                Integration = $"/api/integration?companyId={effectiveCompanyId.Value}",
                ObservabilityEvents = $"/api/observability/events?companyId={effectiveCompanyId.Value}",
                ObservabilityInsights = $"/api/observability/insights?companyId={effectiveCompanyId.Value}"
            }
        };
        return this.Success(dto);
    }
}

public class ControlPlaneSummaryDto
{
    public List<ControlPlaneCapabilityDto> Capabilities { get; set; } = new();
}

public class ControlPlaneCapabilityDto
{
    public string Name { get; set; } = string.Empty;
    public string BasePath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class TenantDiagnosticsDto
{
    public Guid? CompanyId { get; set; }
    public string? Message { get; set; }
    public TenantDiagnosticsLinksDto? Links { get; set; }
}

public class TenantDiagnosticsLinksDto
{
    public string EventStore { get; set; } = string.Empty;
    public string Integration { get; set; } = string.Empty;
    public string ObservabilityEvents { get; set; } = string.Empty;
    public string ObservabilityInsights { get; set; } = string.Empty;
}
