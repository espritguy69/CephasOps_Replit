namespace CephasOps.Application.Notifications.DTOs;

/// <summary>
/// Notification DTO
/// </summary>
public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public Guid? ReadByUserId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? DeliveryChannels { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Mark notification as read/unread/archived request DTO
/// </summary>
public class MarkNotificationStatusDto
{
    public bool IsRead { get; set; }
    public bool IsArchived { get; set; }
}

/// <summary>
/// Create notification request DTO
/// </summary>
public class CreateNotificationDto
{
    public Guid? CompanyId { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Priority { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? MetadataJson { get; set; }
    public string? DeliveryChannels { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

