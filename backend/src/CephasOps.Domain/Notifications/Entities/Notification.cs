namespace CephasOps.Domain.Notifications.Entities;

using CephasOps.Domain.Common;

/// <summary>
/// Notification entity - user notifications
/// </summary>
public class Notification : CompanyScopedEntity
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty; // VipEmailReceived, OrderAssigned, etc.
    public string Priority { get; set; } = "Normal"; // Low, Normal, High, Critical
    public string Status { get; set; } = "Unread"; // Unread, Read, Archived
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
}

