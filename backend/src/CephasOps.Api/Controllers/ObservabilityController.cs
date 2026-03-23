using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Events;
using CephasOps.Application.Events.DTOs;
using CephasOps.Application.SaaS;
using CephasOps.Domain.Authorization;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Event platform observability: list and filter events for operators. Tenant-scoped for non–global admins.
/// </summary>
[ApiController]
[Route("api/observability")]
[Authorize(Policy = "Jobs")]
public class ObservabilityController : ControllerBase
{
    private const int MaxPageSize = 100;

    private const int MaxInsightsPageSize = 100;

    private readonly IEventStoreQueryService _queryService;
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;
    private readonly IFeatureFlagService _featureFlags;
    private readonly ILogger<ObservabilityController> _logger;

    public ObservabilityController(
        IEventStoreQueryService queryService,
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider,
        IFeatureFlagService featureFlags,
        ILogger<ObservabilityController> logger)
    {
        _queryService = queryService;
        _context = context;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
        _featureFlags = featureFlags;
        _logger = logger;
    }

    private Guid? ScopeCompanyId() => _currentUser.IsSuperAdmin ? null : _tenantProvider.CurrentTenantId;

    /// <summary>
    /// List events with optional filters. Supports filtering by CompanyId, EventType, date range, and Status.
    /// Events are traceable via EventId, CorrelationId, and related links (api/event-store/events/{id}/related-links).
    /// </summary>
    [HttpGet("events")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListEvents(
        [FromQuery] Guid? companyId,
        [FromQuery] string? eventType,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string? correlationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var scopeCompanyId = ScopeCompanyId();
        if (scopeCompanyId.HasValue && companyId.HasValue && companyId != scopeCompanyId)
            return this.Forbidden<object>("Company scope not allowed.");
        page = Math.Max(1, page);
        var filter = new EventStoreFilterDto
        {
            CompanyId = companyId,
            EventType = eventType,
            Status = status,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            CorrelationId = correlationId,
            Page = page,
            PageSize = Math.Clamp(pageSize, 1, MaxPageSize)
        };
        var (items, total) = await _queryService.GetEventsAsync(filter, scopeCompanyId, cancellationToken);
        return this.Success<object>(new { items, total, page = filter.Page, pageSize = filter.PageSize });
    }

    /// <summary>
    /// List operational insights (field ops intelligence) with optional filters. Tenant-scoped for non–global admins.
    /// </summary>
    [HttpGet("insights")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListInsights(
        [FromQuery] Guid? companyId,
        [FromQuery] string? type,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var scopeCompanyId = ScopeCompanyId();
        if (scopeCompanyId.HasValue && companyId.HasValue && companyId != scopeCompanyId)
            return this.Forbidden<object>("Company scope not allowed.");
        var effectiveCompanyId = scopeCompanyId ?? companyId;
        if (effectiveCompanyId.HasValue && effectiveCompanyId.Value != Guid.Empty)
        {
            var tenantId = await _context.Companies
                .AsNoTracking()
                .Where(c => c.Id == effectiveCompanyId.Value)
                .Select(c => c.TenantId)
                .FirstOrDefaultAsync(cancellationToken);
            if (tenantId.HasValue && tenantId.Value != Guid.Empty && !await _featureFlags.IsEnabledAsync(tenantId.Value, FeatureFlagKeys.OperationalInsights, cancellationToken))
                return this.Forbidden<object>("Operational insights are not enabled for this tenant.");
        }
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxInsightsPageSize);

        var query = _context.OperationalInsights.AsNoTracking();
        if (scopeCompanyId.HasValue)
            query = query.Where(o => o.CompanyId == scopeCompanyId.Value);
        else if (companyId.HasValue)
            query = query.Where(o => o.CompanyId == companyId.Value);
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(o => o.Type == type);
        if (fromUtc.HasValue)
            query = query.Where(o => o.OccurredAtUtc >= fromUtc.Value);
        if (toUtc.HasValue)
            query = query.Where(o => o.OccurredAtUtc <= toUtc.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new { o.Id, o.CompanyId, o.Type, o.PayloadJson, o.OccurredAtUtc, o.EntityType, o.EntityId })
            .ToListAsync(cancellationToken);
        return this.Success<object>(new { items, total, page, pageSize });
    }
}
