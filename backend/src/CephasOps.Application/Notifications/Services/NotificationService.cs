using CephasOps.Application.Notifications.DTOs;
using CephasOps.Domain.Notifications;
using CephasOps.Domain.Notifications.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Notifications.Services;

/// <summary>
/// Notification service implementation. When DeliveryChannels includes Email, enqueues send via NotificationDispatch (Phase 6).
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationDispatchStore _dispatchStore;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext context,
        INotificationDispatchStore dispatchStore,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _dispatchStore = dispatchStore;
        _logger = logger;
    }

    public async Task<List<NotificationDto>> GetNotificationsAsync(
        Guid userId,
        Guid? companyId = null,
        string? status = null,
        string? type = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use LINQ query instead of raw SQL to avoid dynamic type issues
            var query = _context.Notifications.Where(n => n.UserId == userId);

            if (companyId.HasValue)
            {
                query = query.Where(n => n.CompanyId == null || n.CompanyId == companyId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(n => n.Status == status);
            }

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(n => n.Type == type);
            }

            query = query.OrderByDescending(n => n.CreatedAt);

            if (limit.HasValue)
            {
                query = query.Take(limit.Value);
            }

            var notifications = await query.ToListAsync(cancellationToken);

            if (notifications == null || notifications.Count == 0)
            {
                return new List<NotificationDto>();
            }

            return notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                CompanyId = n.CompanyId,
                UserId = n.UserId,
                Type = n.Type ?? string.Empty,
                Priority = n.Priority ?? "Normal",
                Status = n.Status ?? "Unread",
                Title = n.Title ?? string.Empty,
                Message = n.Message ?? string.Empty,
                ActionUrl = n.ActionUrl,
                ActionText = n.ActionText,
                RelatedEntityId = n.RelatedEntityId,
                RelatedEntityType = n.RelatedEntityType,
                MetadataJson = n.MetadataJson,
                ReadAt = n.ReadAt,
                ArchivedAt = n.ArchivedAt,
                ReadByUserId = n.ReadByUserId,
                ExpiresAt = n.ExpiresAt,
                DeliveryChannels = n.DeliveryChannels,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching notifications for user {UserId}, returning empty list", userId);
            // Return empty list if table doesn't exist or query fails
            return new List<NotificationDto>();
        }
    }

    public async Task<NotificationDto?> GetNotificationByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .Where(n => n.Id == id && n.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (notification == null) return null;

        return new NotificationDto
        {
            Id = notification.Id,
            CompanyId = notification.CompanyId,
            UserId = notification.UserId,
            Type = notification.Type ?? string.Empty,
            Priority = notification.Priority ?? "Normal",
            Status = notification.Status ?? "Unread",
            Title = notification.Title ?? string.Empty,
            Message = notification.Message ?? string.Empty,
            ActionUrl = notification.ActionUrl,
            ActionText = notification.ActionText,
            RelatedEntityId = notification.RelatedEntityId,
            RelatedEntityType = notification.RelatedEntityType,
            MetadataJson = notification.MetadataJson,
            ReadAt = notification.ReadAt,
            ArchivedAt = notification.ArchivedAt,
            ReadByUserId = notification.ReadByUserId,
            ExpiresAt = notification.ExpiresAt,
            DeliveryChannels = notification.DeliveryChannels,
            CreatedAt = notification.CreatedAt,
            UpdatedAt = notification.UpdatedAt
        };
    }

    public async Task<NotificationDto> MarkNotificationStatusAsync(Guid id, MarkNotificationStatusDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .Where(n => n.Id == id && n.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (notification == null)
        {
            throw new KeyNotFoundException($"Notification with ID {id} not found");
        }

        var now = DateTime.UtcNow;
        
        if (dto.IsArchived)
        {
            notification.Status = "Archived";
            notification.ArchivedAt = now;
        }
        else
        {
            notification.Status = dto.IsRead ? "Read" : "Unread";
            if (dto.IsRead)
            {
                notification.ReadAt = now;
                notification.ReadByUserId = userId;
            }
        }
        
        notification.UpdatedAt = now;
        
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Notification status updated: {NotificationId}, Status: {Status}, User: {UserId}", id, notification.Status, userId);

        return new NotificationDto
        {
            Id = notification.Id,
            CompanyId = notification.CompanyId,
            UserId = notification.UserId,
            Type = notification.Type ?? string.Empty,
            Priority = notification.Priority ?? "Normal",
            Status = notification.Status ?? "Unread",
            Title = notification.Title ?? string.Empty,
            Message = notification.Message ?? string.Empty,
            ActionUrl = notification.ActionUrl,
            ActionText = notification.ActionText,
            RelatedEntityId = notification.RelatedEntityId,
            RelatedEntityType = notification.RelatedEntityType,
            MetadataJson = notification.MetadataJson,
            ReadAt = notification.ReadAt,
            ArchivedAt = notification.ArchivedAt,
            ReadByUserId = notification.ReadByUserId,
            ExpiresAt = notification.ExpiresAt,
            DeliveryChannels = notification.DeliveryChannels,
            CreatedAt = notification.CreatedAt,
            UpdatedAt = notification.UpdatedAt
        };
    }

    public async Task<List<Guid>> ResolveUsersByRoleAsync(string roleName, Guid? companyId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return new List<Guid>();
        }

        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required for ResolveUsersByRoleAsync.");
        TenantSafetyGuard.AssertTenantContext();
        // Multi-tenant SaaS — CompanyId filter required.
        var query = _context.UserRoles
            .Include(ur => ur.Role)
            .Include(ur => ur.User)
            .Where(ur => ur.Role != null && ur.Role.Name == roleName)
            .Where(ur => ur.User != null && ur.User.IsActive && ur.User.CompanyId == companyId.Value);

        var userIds = await query
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Resolved {Count} users for role {RoleName}", userIds.Count, roleName);
        return userIds;
    }

    public async Task<List<Guid>> ResolveUsersByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        var userIds = await _context.DepartmentMemberships
            .Include(dm => dm.Department)
            .Where(dm => dm.DepartmentId == departmentId)
            .Where(dm => dm.Department != null && dm.Department.IsActive)
            .Select(dm => dm.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Resolved {Count} users for department {DepartmentId}", userIds.Count, departmentId);
        return userIds;
    }

    public async Task<List<Guid>> GetDefaultVipRecipientsAsync(Guid? companyId, CancellationToken cancellationToken = default)
    {
        // Check for setting: "notification.vip.defaultRecipients"
        var settingKey = "notification.vip.defaultRecipients";
        
        var setting = await _context.GlobalSettings
            .Where(gs => gs.Key == settingKey)
            .FirstOrDefaultAsync(cancellationToken);

        if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
        {
            _logger.LogWarning("No default VIP recipients configured");
            return new List<Guid>();
        }

        // Parse comma-separated GUIDs from setting value
        var userIds = setting.Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => Guid.TryParse(s, out _))
            .Select(s => Guid.Parse(s))
            .ToList();

        _logger.LogInformation("Resolved {Count} default VIP recipients from settings", userIds.Count);
        return userIds;
    }

    public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default)
    {
        var companyId = dto.CompanyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("CompanyId is required to create a notification. Set it on the request or ensure tenant scope is set.");

        var notification = new CephasOps.Domain.Notifications.Entities.Notification
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            UserId = dto.UserId,
            Type = dto.Type,
            Priority = dto.Priority ?? "Normal",
            Status = "Unread",
            Title = dto.Title,
            Message = dto.Message,
            ActionUrl = dto.ActionUrl,
            ActionText = dto.ActionText,
            RelatedEntityId = dto.RelatedEntityId,
            RelatedEntityType = dto.RelatedEntityType,
            MetadataJson = dto.MetadataJson,
            DeliveryChannels = dto.DeliveryChannels ?? "InApp",
            ExpiresAt = dto.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Notification created: {NotificationId} for user {UserId}", notification.Id, dto.UserId);

        // Phase 6: request email delivery via NotificationDispatch when DeliveryChannels includes Email
        if (dto.DeliveryChannels?.Contains("Email", StringComparison.OrdinalIgnoreCase) == true)
        {
            await RequestEmailDispatchForNotificationAsync(notification, cancellationToken);
        }

        return new NotificationDto
        {
            Id = notification.Id,
            CompanyId = notification.CompanyId,
            UserId = notification.UserId,
            Type = notification.Type ?? string.Empty,
            Priority = notification.Priority ?? "Normal",
            Status = notification.Status ?? "Unread",
            Title = notification.Title ?? string.Empty,
            Message = notification.Message ?? string.Empty,
            ActionUrl = notification.ActionUrl,
            ActionText = notification.ActionText,
            RelatedEntityId = notification.RelatedEntityId,
            RelatedEntityType = notification.RelatedEntityType,
            MetadataJson = notification.MetadataJson,
            ReadAt = notification.ReadAt,
            ArchivedAt = notification.ArchivedAt,
            ReadByUserId = notification.ReadByUserId,
            ExpiresAt = notification.ExpiresAt,
            DeliveryChannels = notification.DeliveryChannels,
            CreatedAt = notification.CreatedAt,
            UpdatedAt = notification.UpdatedAt
        };
    }

    /// <summary>
    /// Enqueue email delivery for an in-app notification via NotificationDispatch (Phase 6). Idempotent by notification id.
    /// </summary>
    private async Task RequestEmailDispatchForNotificationAsync(
        Notification notification,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == notification.UserId)
            .Select(u => new { u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            _logger.LogWarning("Notification {NotificationId}: user {UserId} has no email; skipping email dispatch", notification.Id, notification.UserId);
            return;
        }

        // Tenant-safe: include CompanyId so the same logical key in different tenants does not collide
        var idempotencyKey = (notification.CompanyId.HasValue && notification.CompanyId.Value != Guid.Empty)
            ? $"{notification.CompanyId.Value:N}:{notification.Id}:Email"
            : $"{notification.Id}:Email";
        if (await _dispatchStore.ExistsByIdempotencyKeyAsync(idempotencyKey, cancellationToken))
        {
            _logger.LogDebug("Notification {NotificationId}: email dispatch already requested (idempotency)", notification.Id);
            return;
        }

        var payloadJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["subject"] = notification.Title ?? "(No subject)",
            ["body"] = notification.Message ?? string.Empty
        });

        var dispatch = new NotificationDispatch
        {
            CompanyId = notification.CompanyId,
            Channel = "Email",
            Target = user.Email.Trim(),
            TemplateKey = null,
            PayloadJson = payloadJson,
            Status = "Pending",
            MaxAttempts = 5,
            IdempotencyKey = idempotencyKey,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dispatchStore.AddAsync(dispatch, cancellationToken);
        _logger.LogInformation("Enqueued email dispatch for notification {NotificationId} to {Email}, dispatch {DispatchId}", notification.Id, user.Email, dispatch.Id);
    }
}
