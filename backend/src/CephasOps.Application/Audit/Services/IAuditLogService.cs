using CephasOps.Application.Audit.DTOs;

namespace CephasOps.Application.Audit.Services;

/// <summary>
/// Service for writing and reading audit log entries (who did what, when).
/// Per LOGGING_AND_AUDIT_MODULE.md.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Append an audit log entry. Fire-and-forget safe; does not throw on failure.
    /// </summary>
    /// <param name="companyId">Company context (null for system-wide).</param>
    /// <param name="userId">User who performed the action (null if System).</param>
    /// <param name="entityType">e.g. Order, Invoice, GlobalSetting.</param>
    /// <param name="entityId">Target entity ID.</param>
    /// <param name="action">Created, Updated, Deleted, StatusChanged, Login, Logout.</param>
    /// <param name="fieldChangesJson">Optional JSON array of { field, oldValue, newValue }.</param>
    /// <param name="channel">AdminWeb, SIApp, Api, BackgroundJob.</param>
    /// <param name="ipAddress">Optional client IP.</param>
    /// <param name="metadataJson">Optional extra context (JSON).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogAuditAsync(
        Guid? companyId,
        Guid? userId,
        string entityType,
        Guid entityId,
        string action,
        string? fieldChangesJson = null,
        string channel = "Api",
        string? ipAddress = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit log entries with optional filters. Used by admin/support APIs.
    /// </summary>
    Task<List<AuditLogDto>> GetAuditLogsAsync(
        Guid? companyId = null,
        string? entityType = null,
        Guid? entityId = null,
        Guid? userId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? action = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get security (auth) activity entries with user email, paginated. EntityType=Auth only. v1.4 Phase 1.
    /// </summary>
    Task<(List<SecurityActivityEntryDto> Items, int TotalCount)> GetSecurityActivityAsync(
        Guid? userId = null,
        string? action = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get auth events in a time window for anomaly detection. EntityType=Auth only, no paging. v1.4 Phase 2.
    /// </summary>
    Task<List<SecurityActivityEntryDto>> GetAuthEventsForDetectionAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        Guid? userId = null,
        int maxCount = 5000,
        CancellationToken cancellationToken = default);
}
