using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Notifications.Services;

/// <summary>
/// Runs notification retention inside the Notifications boundary (Phase 7). Archive then hard-delete by age.
/// When companyId is null, runs platform-wide (all tenants) and uses TenantSafetyGuard.EnterPlatformBypass; when companyId is set, runs in tenant scope.
/// </summary>
public class NotificationRetentionService : INotificationRetentionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationRetentionService> _logger;

    public NotificationRetentionService(
        ApplicationDbContext context,
        ILogger<NotificationRetentionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<NotificationRetentionResult> RunRetentionAsync(
        int archiveDays = 90,
        int deleteDays = 365,
        Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        if (archiveDays < 1) archiveDays = 90;
        if (deleteDays < 1) deleteDays = 365;

        return await TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(companyId, async (ct) =>
        {
            var archiveCutoff = DateTime.UtcNow.AddDays(-archiveDays);
            var deleteCutoff = DateTime.UtcNow.AddDays(-deleteDays);

            var query = _context.Notifications.AsQueryable();
            if (companyId.HasValue)
                query = query.Where(n => n.CompanyId == companyId.Value);

            // Step 1: Archive Read/Unread older than archiveCutoff
            var toArchive = await query
                .Where(n => n.Status != "Archived"
                    && n.CreatedAt < archiveCutoff
                    && (n.Status == "Read" || n.Status == "Unread"))
                .ToListAsync(ct);

            var now = DateTime.UtcNow;
            foreach (var n in toArchive)
            {
                n.Status = "Archived";
                n.ArchivedAt = now;
                n.UpdatedAt = now;
            }

            var archivedCount = toArchive.Count;
            if (archivedCount > 0)
                _logger.LogInformation("Notification retention: archived {Count} notifications (older than {Days} days, companyId: {CompanyId})", archivedCount, archiveDays, companyId?.ToString() ?? "global");

            // Step 2: Hard-delete Archived older than deleteCutoff
            var deleteQuery = _context.Notifications.AsQueryable();
            if (companyId.HasValue)
                deleteQuery = deleteQuery.Where(n => n.CompanyId == companyId.Value);

            var toDelete = await deleteQuery
                .Where(n => n.Status == "Archived" && n.ArchivedAt.HasValue && n.ArchivedAt.Value < deleteCutoff)
                .ToListAsync(ct);

            var deletedCount = toDelete.Count;
            if (deletedCount > 0)
            {
                _context.Notifications.RemoveRange(toDelete);
                _logger.LogInformation("Notification retention: deleted {Count} archived notifications (older than {Days} days, companyId: {CompanyId})", deletedCount, deleteDays, companyId?.ToString() ?? "global");
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Notification retention completed: archived={Archived}, deleted={Deleted}", archivedCount, deletedCount);
            return new NotificationRetentionResult { ArchivedCount = archivedCount, DeletedCount = deletedCount };
        }, cancellationToken);
    }
}
