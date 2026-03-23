using CephasOps.Application.Notifications.DTOs;
using CephasOps.Application.Notifications.Services;
using CephasOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Notification endpoints
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get notifications for current user
    /// </summary>
    /// <param name="status">Filter by status (Unread, Read, Archived)</param>
    /// <param name="type">Filter by type</param>
    /// <param name="limit">Limit number of results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of notifications</returns>
    [HttpGet]
    [HttpGet("my")]
    [ProducesResponseType(typeof(ApiResponse<List<NotificationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<NotificationDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<NotificationDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetNotifications(
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized("User context required");
        }

        try
        {
            var notifications = await _notificationService.GetNotificationsAsync(
                userId.Value, _tenantProvider.CurrentTenantId, status, type, limit, cancellationToken);
            return this.Success(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications");
            return this.Error<List<NotificationDto>>($"Failed to get notifications: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get unread notification count for current user
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Unread count</returns>
    [HttpGet("my/unread-count")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized("User context required");
        }

        try
        {
            var notifications = await _notificationService.GetNotificationsAsync(
                userId.Value, _tenantProvider.CurrentTenantId, "Unread", null, null, cancellationToken);
            return this.Success(notifications?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting unread count, returning 0");
            return this.Success(0); // Return 0 instead of 500 error
        }
    }

    /// <summary>
    /// Mark all notifications as read for current user
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPut("my/read-all")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized("User context required");
        }

        try
        {
            var unreadNotifications = await _notificationService.GetNotificationsAsync(
                userId.Value, _tenantProvider.CurrentTenantId, "Unread", null, null, cancellationToken);

            foreach (var notification in unreadNotifications)
            {
                await _notificationService.MarkNotificationStatusAsync(
                    notification.Id,
                    new MarkNotificationStatusDto { IsRead = true },
                    userId.Value,
                    cancellationToken);
            }

            return this.Success("All notifications marked as read.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return this.InternalServerError($"Failed to mark all as read: {ex.Message}");
        }
    }

    /// <summary>
    /// Get notification by ID
    /// </summary>
    /// <param name="id">Notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Notification details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> GetNotification(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<NotificationDto>("User context required");
        }

        try
        {
            var notification = await _notificationService.GetNotificationByIdAsync(id, userId.Value, cancellationToken);
            if (notification == null)
            {
                return this.NotFound<NotificationDto>($"Notification with ID {id} not found");
            }

            return this.Success(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification: {NotificationId}", id);
            return this.InternalServerError<NotificationDto>($"Failed to get notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Mark notification as read or unread
    /// </summary>
    /// <param name="id">Notification ID</param>
    /// <param name="dto">Status update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated notification</returns>
    [HttpPost("{id}/read")]
    [HttpPut("{id}/read")]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> MarkNotificationRead(
        Guid id,
        [FromBody] MarkNotificationStatusDto? dto = null,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<NotificationDto>("User context required");
        }

        try
        {
            dto ??= new MarkNotificationStatusDto { IsRead = true };
            var notification = await _notificationService.MarkNotificationStatusAsync(id, dto, userId.Value, cancellationToken);
            return this.Success(notification, "Notification status updated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<NotificationDto>($"Notification with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification status: {NotificationId}", id);
            return this.InternalServerError<NotificationDto>($"Failed to update notification status: {ex.Message}");
        }
    }

    /// <summary>
    /// Archive a notification
    /// </summary>
    /// <param name="id">Notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated notification</returns>
    [HttpPut("{id}/archive")]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> ArchiveNotification(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<NotificationDto>("User context required");
        }

        try
        {
            var notification = await _notificationService.MarkNotificationStatusAsync(
                id,
                new MarkNotificationStatusDto { IsArchived = true },
                userId.Value,
                cancellationToken);
            return this.Success(notification, "Notification archived successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<NotificationDto>($"Notification with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving notification: {NotificationId}", id);
            return this.InternalServerError<NotificationDto>($"Failed to archive notification: {ex.Message}");
        }
    }
}

