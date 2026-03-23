namespace CephasOps.Application.Notifications;

/// <summary>
/// Runs notification retention: archive old Read/Unread notifications, then hard-delete archived notifications past delete window (Phase 7).
/// Owned by the Notifications boundary; replaces legacy notificationretention BackgroundJob.
/// </summary>
public interface INotificationRetentionService
{
    /// <summary>
    /// Archive notifications older than archiveDays (Read/Unread → Archived), then hard-delete Archived notifications older than deleteDays.
    /// When companyId is null, applies to all notifications (global); when set, restricts to that company (tenant-safe).
    /// </summary>
    /// <param name="archiveDays">Archive Read/Unread notifications older than this many days (default 90).</param>
    /// <param name="deleteDays">Hard-delete Archived notifications older than this many days (default 365).</param>
    /// <param name="companyId">Optional. When null, run globally; when set, scope to this company.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Counts: archived, deleted.</returns>
    Task<NotificationRetentionResult> RunRetentionAsync(
        int archiveDays = 90,
        int deleteDays = 365,
        Guid? companyId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Result of a retention run.</summary>
public class NotificationRetentionResult
{
    public int ArchivedCount { get; set; }
    public int DeletedCount { get; set; }
}
