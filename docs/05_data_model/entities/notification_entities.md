# Notification Entities

## Notification

Represents a user notification (in-app, email, SMS, etc.)

### Properties

| Property | Type | Required | Max Length | Description |
|----------|------|----------|------------|-------------|
| `Id` | Guid | Yes | - | Primary key |
| `CompanyId` | Guid? | No | - | Company scope (nullable for global notifications) |
| `UserId` | Guid | Yes | - | User who should receive this notification |
| `Type` | NotificationType | Yes | - | Notification type (VIP_EMAIL, TASK_ASSIGNED, etc.) |
| `Priority` | NotificationPriority | Yes | - | Priority level (Low, Normal, High, Critical) |
| `Status` | NotificationStatus | Yes | - | Current status (Unread, Read, Archived) |
| `Title` | string | Yes | - | Notification title/heading |
| `Message` | string | Yes | - | Notification message/body |
| `ActionUrl` | string? | No | - | Optional URL to navigate to when clicked |
| `ActionText` | string? | No | - | Optional action button text |
| `RelatedEntityId` | Guid? | No | - | Reference to related entity (e.g. EmailMessage ID, TaskItem ID) |
| `RelatedEntityType` | string? | No | - | Type of related entity (e.g. "EmailMessage", "TaskItem") |
| `MetadataJson` | string? | No | - | Additional metadata as JSON |
| `ReadAt` | DateTime? | No | - | When notification was read (null if unread) |
| `ArchivedAt` | DateTime? | No | - | When notification was archived (null if not archived) |
| `ReadByUserId` | Guid? | No | - | User who read/archived the notification |
| `ExpiresAt` | DateTime? | No | - | Expiration date (notifications older than this may be auto-archived) |
| `DeliveryChannels` | string? | No | - | Delivery channels attempted (JSON array: ["in-app", "email", "sms"]) |
| `CreatedAt` | DateTime | Yes | - | Creation timestamp |
| `UpdatedAt` | DateTime | Yes | - | Last update timestamp |

### Enums

#### NotificationType

- `VipEmailReceived = 1`
- `OrderAssigned = 2`
- `OrderRescheduled = 3`
- `KpiThresholdBreached = 4`
- `MaterialLowStock = 5`
- `InvoiceGenerated = 6`
- `SystemAlert = 7`
- `TaskAssigned = 8`
- `TaskCompleted = 9`
- `Other = 99`

#### NotificationPriority

- `Low = 1`
- `Normal = 2`
- `High = 3`
- `Critical = 4`

#### NotificationStatus

- `Unread = 1`
- `Read = 2`
- `Archived = 3`

### Indexes

- `IX_Notifications_UserId_CompanyId_Status`: For efficient querying of user notifications by status
- `IX_Notifications_CompanyId_Type_CreatedAt`: For efficient querying by type and date
- `IX_Notifications_RelatedEntityId_RelatedEntityType`: For efficient lookup by related entity

## NotificationSetting

Represents user or company-level notification preferences. Allows users/companies to override global notification settings.

### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Id` | Guid | Yes | Primary key |
| `CompanyId` | Guid? | No | Company scope (nullable for user-only settings) |
| `UserId` | Guid? | No | User scope (nullable for company-wide settings) |
| `NotificationType` | NotificationType? | No | Type this setting applies to (null = all types) |
| `Channel` | string | Yes | "IN_APP", "EMAIL", "BOTH", or "NONE" |
| `Enabled` | bool | Yes | Whether notifications of this type are enabled |
| `MinimumPriority` | NotificationPriority? | No | Minimum priority level for notifications |
| `SoundEnabled` | bool | Yes | Whether to send sound alerts |
| `DesktopNotificationsEnabled` | bool | Yes | Whether to send desktop/browser push notifications |
| `Notes` | string? | No | Optional notes/description |
| `CreatedAt` | DateTime | Yes | Creation timestamp |
| `UpdatedAt` | DateTime | Yes | Last update timestamp |

### Indexes

- `IX_NotificationSettings_UserId_CompanyId_NotificationType`: For efficient lookup of user/company settings by type
- `IX_NotificationSettings_CompanyId_NotificationType`: For efficient lookup of company-wide settings

### Business Rules

1. If both `CompanyId` and `UserId` are set, `UserId` takes precedence (user-specific override)
2. If only `CompanyId` is set, applies to all users in the company for this notification type
3. If only `UserId` is set, applies only to this specific user
4. If `NotificationType` is null, applies to all notification types (default preference)
