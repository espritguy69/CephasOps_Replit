namespace CephasOps.Domain.Notifications.Entities;

using CephasOps.Domain.Common;

/// <summary>
/// Notification setting entity - user/company notification preferences
/// </summary>
public class NotificationSetting : CompanyScopedEntity
{
    public Guid? UserId { get; set; } // Null for company-wide settings
    public string? NotificationType { get; set; } // Null for all types
    public string Channel { get; set; } = string.Empty; // IN_APP, EMAIL, BOTH, NONE
    public bool Enabled { get; set; } = true;
    public string? MinimumPriority { get; set; }
    public bool SoundEnabled { get; set; } = true;
    public bool DesktopNotificationsEnabled { get; set; } = true;
    public string? Notes { get; set; }
}

