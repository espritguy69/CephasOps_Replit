using CephasOps.Application.Notifications.DTOs;

namespace CephasOps.Application.Notifications.Services;

/// <summary>
/// Notification service interface
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Get notifications for current user
    /// </summary>
    Task<List<NotificationDto>> GetNotificationsAsync(
        Guid userId,
        Guid? companyId = null,
        string? status = null,
        string? type = null,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification by ID
    /// </summary>
    Task<NotificationDto?> GetNotificationByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark notification as read or unread
    /// </summary>
    Task<NotificationDto> MarkNotificationStatusAsync(Guid id, MarkNotificationStatusDto dto, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve user IDs by role name
    /// </summary>
    Task<List<Guid>> ResolveUsersByRoleAsync(string roleName, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve user IDs by department ID
    /// </summary>
    Task<List<Guid>> ResolveUsersByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get default VIP recipient user IDs from settings
    /// </summary>
    Task<List<Guid>> GetDefaultVipRecipientsAsync(Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create notification for a user
    /// </summary>
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default);
}

