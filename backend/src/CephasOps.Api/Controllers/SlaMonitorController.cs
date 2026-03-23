using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Sla.DTOs;
using CephasOps.Domain.Authorization;
using CephasOps.Domain.Sla.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Api.Controllers;

/// <summary>
/// SLA Intelligence: breaches, dashboard, and rules. Company-scoped for non–SuperAdmin. Follows admin/Jobs pattern.
/// </summary>
[ApiController]
[Route("api/sla")]
[Authorize(Policy = "Jobs")]
public class SlaMonitorController : ControllerBase
{
    public const int MaxPageSize = 100;

    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SlaMonitorController> _logger;

    public SlaMonitorController(ApplicationDbContext context, ICurrentUserService currentUser, ITenantProvider tenantProvider, ILogger<SlaMonitorController> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private IQueryable<SlaBreach> ApplyCompanyFilter(IQueryable<SlaBreach> query)
    {
        if (_currentUser.IsSuperAdmin) return query;
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId.HasValue) return query.Where(b => b.CompanyId == companyId.Value);
        return query;
    }

    private IQueryable<SlaRule> ApplyCompanyFilterRules(IQueryable<SlaRule> query)
    {
        if (_currentUser.IsSuperAdmin) return query;
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId.HasValue) return query.Where(r => r.CompanyId == companyId.Value);
        return query;
    }

    /// <summary>List SLA breaches with optional filters.</summary>
    [HttpGet("breaches")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetBreaches(
        [FromQuery] Guid? companyId,
        [FromQuery] string? targetType,
        [FromQuery] string? severity,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var q = ApplyCompanyFilter(_context.SlaBreaches.AsNoTracking());
        if (companyId.HasValue) q = q.Where(b => b.CompanyId == companyId.Value);
        if (!string.IsNullOrEmpty(targetType)) q = q.Where(b => b.TargetType == targetType);
        if (!string.IsNullOrEmpty(severity)) q = q.Where(b => b.Severity == severity);
        if (!string.IsNullOrEmpty(status)) q = q.Where(b => b.Status == status);
        if (fromUtc.HasValue) q = q.Where(b => b.DetectedAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) q = q.Where(b => b.DetectedAtUtc <= toUtc.Value);

        var total = await q.CountAsync(cancellationToken);
        var size = Math.Clamp(pageSize, 1, MaxPageSize);
        var items = await q.OrderByDescending(b => b.DetectedAtUtc)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(b => new SlaBreachDto
            {
                Id = b.Id,
                CompanyId = b.CompanyId,
                RuleId = b.RuleId,
                TargetType = b.TargetType,
                TargetId = b.TargetId,
                CorrelationId = b.CorrelationId,
                DetectedAtUtc = b.DetectedAtUtc,
                DurationSeconds = b.DurationSeconds,
                Severity = b.Severity,
                Status = b.Status,
                Title = b.Title,
                AcknowledgedAtUtc = b.AcknowledgedAtUtc,
                ResolvedAtUtc = b.ResolvedAtUtc
            })
            .ToListAsync(cancellationToken);

        return this.Success<object>(new { items, total, page, pageSize = size });
    }

    /// <summary>SLA dashboard: open/critical counts, average resolution time, most common breached targets.</summary>
    [HttpGet("dashboard")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<SlaDashboardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SlaDashboardDto>>> GetDashboard(CancellationToken cancellationToken = default)
    {
        var q = ApplyCompanyFilter(_context.SlaBreaches.AsNoTracking());

        var openCount = await q.Where(b => b.Status == "Open").CountAsync(cancellationToken);
        var criticalCount = await q.Where(b => b.Severity == "Critical" && b.Status == "Open").CountAsync(cancellationToken);

        var resolved = await q.Where(b => b.Status == "Resolved" && b.ResolvedAtUtc != null).ToListAsync(cancellationToken);
        double? avgResolutionHours = null;
        if (resolved.Count > 0)
        {
            var totalHours = resolved.Sum(b => (b.ResolvedAtUtc!.Value - b.DetectedAtUtc).TotalHours);
            avgResolutionHours = Math.Round(totalHours / resolved.Count, 2);
        }

        var byTarget = await q.Where(b => b.Status == "Open")
            .GroupBy(b => new { b.TargetType, b.Title })
            .Select(g => new SlaBreachSummaryByTargetDto
            {
                TargetType = g.Key.TargetType,
                TargetName = g.Key.Title ?? g.Key.TargetType,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(cancellationToken);

        var dashboard = new SlaDashboardDto
        {
            OpenBreachesCount = openCount,
            CriticalBreachesCount = criticalCount,
            AverageResolutionTimeHours = avgResolutionHours,
            MostCommonBreachedTargets = byTarget
        };
        return this.Success(dashboard);
    }

    /// <summary>List SLA rules (company-scoped).</summary>
    [HttpGet("rules")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetRules(
        [FromQuery] bool? enabled,
        [FromQuery] string? ruleType,
        CancellationToken cancellationToken = default)
    {
        var q = ApplyCompanyFilterRules(_context.SlaRules.AsNoTracking()).Where(r => !r.IsDeleted);
        if (enabled.HasValue) q = q.Where(r => r.Enabled == enabled.Value);
        if (!string.IsNullOrEmpty(ruleType)) q = q.Where(r => r.RuleType == ruleType);

        var items = await q.OrderBy(r => r.RuleType).ThenBy(r => r.TargetName)
            .Select(r => new SlaRuleDto
            {
                Id = r.Id,
                CompanyId = r.CompanyId,
                RuleType = r.RuleType,
                TargetType = r.TargetType,
                TargetName = r.TargetName,
                MaxDurationSeconds = r.MaxDurationSeconds,
                WarningThresholdSeconds = r.WarningThresholdSeconds,
                EscalationThresholdSeconds = r.EscalationThresholdSeconds,
                Enabled = r.Enabled,
                CreatedAtUtc = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return this.Success<object>(new { items });
    }

    /// <summary>Create an SLA rule.</summary>
    [HttpPost("rules")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<SlaRuleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SlaRuleDto>>> CreateRule([FromBody] CreateSlaRuleDto dto, CancellationToken cancellationToken = default)
    {
        var companyId = dto.CompanyId;
        if (!_currentUser.IsSuperAdmin && companyId != _tenantProvider.CurrentTenantId)
            return this.BadRequest("Company scope not allowed.");
        if (string.IsNullOrWhiteSpace(dto.RuleType) || string.IsNullOrWhiteSpace(dto.TargetType) || string.IsNullOrWhiteSpace(dto.TargetName))
            return this.BadRequest("RuleType, TargetType, and TargetName are required.");

        var rule = new SlaRule
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            RuleType = dto.RuleType.Trim(),
            TargetType = dto.TargetType.Trim(),
            TargetName = dto.TargetName.Trim(),
            MaxDurationSeconds = dto.MaxDurationSeconds,
            WarningThresholdSeconds = dto.WarningThresholdSeconds,
            EscalationThresholdSeconds = dto.EscalationThresholdSeconds,
            Enabled = dto.Enabled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.SlaRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);

        var result = new SlaRuleDto
        {
            Id = rule.Id,
            CompanyId = rule.CompanyId,
            RuleType = rule.RuleType,
            TargetType = rule.TargetType,
            TargetName = rule.TargetName,
            MaxDurationSeconds = rule.MaxDurationSeconds,
            WarningThresholdSeconds = rule.WarningThresholdSeconds,
            EscalationThresholdSeconds = rule.EscalationThresholdSeconds,
            Enabled = rule.Enabled,
            CreatedAtUtc = rule.CreatedAt
        };
        return this.CreatedAtAction(nameof(GetRules), new { }, result, "SLA rule created.");
    }

    /// <summary>Update an SLA rule.</summary>
    [HttpPut("rules/{id:guid}")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<SlaRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SlaRuleDto>>> UpdateRule(Guid id, [FromBody] UpdateSlaRuleDto dto, CancellationToken cancellationToken = default)
    {
        var rule = await ApplyCompanyFilterRules(_context.SlaRules).Where(r => !r.IsDeleted).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (rule == null) return NotFound();

        if (dto.RuleType != null) rule.RuleType = dto.RuleType.Trim();
        if (dto.TargetType != null) rule.TargetType = dto.TargetType.Trim();
        if (dto.TargetName != null) rule.TargetName = dto.TargetName.Trim();
        if (dto.MaxDurationSeconds.HasValue) rule.MaxDurationSeconds = dto.MaxDurationSeconds.Value;
        if (dto.WarningThresholdSeconds.HasValue) rule.WarningThresholdSeconds = dto.WarningThresholdSeconds;
        if (dto.EscalationThresholdSeconds.HasValue) rule.EscalationThresholdSeconds = dto.EscalationThresholdSeconds;
        if (dto.Enabled.HasValue) rule.Enabled = dto.Enabled.Value;
        rule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var result = new SlaRuleDto
        {
            Id = rule.Id,
            CompanyId = rule.CompanyId,
            RuleType = rule.RuleType,
            TargetType = rule.TargetType,
            TargetName = rule.TargetName,
            MaxDurationSeconds = rule.MaxDurationSeconds,
            WarningThresholdSeconds = rule.WarningThresholdSeconds,
            EscalationThresholdSeconds = rule.EscalationThresholdSeconds,
            Enabled = rule.Enabled,
            CreatedAtUtc = rule.CreatedAt
        };
        return this.Success(result);
    }

    /// <summary>Update breach status (Acknowledge or Resolve).</summary>
    [HttpPatch("breaches/{id:guid}")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBreachStatus(Guid id, [FromBody] UpdateSlaBreachStatusDto dto, CancellationToken cancellationToken = default)
    {
        var breach = await ApplyCompanyFilter(_context.SlaBreaches).FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (breach == null) return NotFound();

        if (string.Equals(dto.Status, "Acknowledged", StringComparison.OrdinalIgnoreCase))
        {
            breach.Status = "Acknowledged";
            breach.AcknowledgedAtUtc = DateTime.UtcNow;
        }
        else if (string.Equals(dto.Status, "Resolved", StringComparison.OrdinalIgnoreCase))
        {
            breach.Status = "Resolved";
            breach.ResolvedAtUtc = DateTime.UtcNow;
            breach.ResolvedByUserId = dto.ResolvedByUserId;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public class UpdateSlaBreachStatusDto
{
    public string Status { get; set; } = string.Empty; // Acknowledged | Resolved
    public Guid? ResolvedByUserId { get; set; }
}
