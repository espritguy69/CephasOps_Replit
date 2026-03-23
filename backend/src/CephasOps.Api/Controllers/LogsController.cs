using CephasOps.Application.Audit.DTOs;
using CephasOps.Application.Audit.Services;
using CephasOps.Application.Auth.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Read-only API for audit log (who did what, when). Per LOGGING_AND_AUDIT_MODULE.md.
/// Security activity (auth events) restricted to SuperAdmin/Admin. v1.4 Phase 1. Security alerts v1.4 Phase 2.
/// Audit log list is tenant-scoped: non-SuperAdmin only see their company's entries.
/// </summary>
[ApiController]
[Route("api/logs")]
[Authorize]
public class LogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ISecurityAnomalyDetectionService _securityAnomalyDetectionService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<LogsController> _logger;

    public LogsController(
        IAuditLogService auditLogService,
        ISecurityAnomalyDetectionService securityAnomalyDetectionService,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUserService,
        ILogger<LogsController> logger)
    {
        _auditLogService = auditLogService;
        _securityAnomalyDetectionService = securityAnomalyDetectionService;
        _tenantProvider = tenantProvider;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get audit log entries with optional filters (companyId, entityType, entityId, userId, dateFrom, dateTo, action).
    /// Non-SuperAdmin: companyId is forced to current tenant; requesting another company returns 403.
    /// </summary>
    /// <param name="companyId">Filter by company (tenant-scoped for non-SuperAdmin).</param>
    /// <param name="entityType">Filter by entity type (e.g. Order, Invoice).</param>
    /// <param name="entityId">Filter by entity ID.</param>
    /// <param name="userId">Filter by user who performed the action.</param>
    /// <param name="dateFrom">From timestamp (UTC).</param>
    /// <param name="dateTo">To timestamp (UTC).</param>
    /// <param name="action">Filter by action (e.g. StatusChanged, Created).</param>
    /// <param name="limit">Max number of entries (default 100, max 500).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit log entries.</returns>
    [HttpGet("audit")]
    [ProducesResponseType(typeof(ApiResponse<List<AuditLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<AuditLogDto>>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<AuditLogDto>>>> GetAuditLogs(
        [FromQuery] Guid? companyId = null,
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? action = null,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var scopeCompanyId = _tenantProvider.CurrentTenantId;
        if (!_currentUserService.IsSuperAdmin)
        {
            if (!scopeCompanyId.HasValue || scopeCompanyId.Value == Guid.Empty)
                return this.Forbidden<List<AuditLogDto>>("Company context is required.");
            if (companyId.HasValue && companyId.Value != scopeCompanyId)
                return this.Forbidden<List<AuditLogDto>>("Company scope not allowed.");
            companyId = scopeCompanyId;
        }

        var list = await _auditLogService.GetAuditLogsAsync(
            companyId,
            entityType,
            entityId,
            userId,
            dateFrom,
            dateTo,
            action,
            limit,
            cancellationToken);

        return this.Success(list, "Audit log entries.");
    }

    /// <summary>
    /// Get security (auth) activity with user email, paginated. SuperAdmin/Admin only. v1.4 Phase 1.
    /// </summary>
    [HttpGet("security-activity")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetSecurityActivity(
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _auditLogService.GetSecurityActivityAsync(
            userId, action, dateFrom, dateTo, page, pageSize, cancellationToken);
        return this.Success<object>(new { items, totalCount }, "Security activity.");
    }

    /// <summary>
    /// Get security alerts (suspicious activity) from anomaly detection. SuperAdmin/Admin only. v1.4 Phase 2.
    /// </summary>
    [HttpGet("security-alerts")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [RequirePermission(PermissionCatalog.AdminSecurityView)]
    [ProducesResponseType(typeof(ApiResponse<List<SecurityAlertDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<SecurityAlertDto>>>> GetSecurityAlerts(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? alertType = null,
        CancellationToken cancellationToken = default)
    {
        var alerts = await _securityAnomalyDetectionService.DetectAsync(
            dateFrom, dateTo, userId, alertType, cancellationToken);
        return this.Success(alerts, "Security alerts.");
    }
}
