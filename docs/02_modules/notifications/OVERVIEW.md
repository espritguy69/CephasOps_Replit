# Notification Module

CephasOps Notification System – Full Specification

---

## 1. Purpose

The Notification Module provides a centralized system for generating, storing, and delivering user notifications across the CephasOps platform. Notifications alert users about important events such as VIP emails, task assignments, order status changes, and system alerts.

---

## 2. Overview

The notification system supports:
- **Multiple notification types**: VIP emails, task assignments, task completions, order events, system alerts
- **Multiple delivery channels**: In-app, email, SMS (future), push notifications (future)
- **Priority-based filtering**: Low, Normal, High, Critical
- **Multi-level settings**: Global defaults, company overrides, user preferences
- **Notification management**: Read/unread tracking, archiving, retention policies

---

## 3. Data Model

### 3.1 Notification Entity

Represents a single notification sent to a user.

**Key Fields:**
- `Id` (Guid)
- `CompanyId` (Guid?, nullable for global notifications)
- `UserId` (Guid, recipient)
- `Type` (NotificationType enum)
- `Priority` (NotificationPriority enum)
- `Status` (NotificationStatus enum: Unread, Read, Archived)
- `Title` (string)
- `Message` (string)
- `ActionUrl` (string?, optional navigation URL)
- `ActionText` (string?, optional button text)
- `RelatedEntityId` (Guid?, optional reference to related entity)
- `RelatedEntityType` (string?, e.g., "EmailMessage", "TaskItem", "Order")
- `MetadataJson` (string?, JSON metadata)
- `ReadAt` (DateTime?, when notification was read)
- `ArchivedAt` (DateTime?, when notification was archived)
- `DeliveryChannels` (string?, JSON array of channels attempted)

**See:** `docs/05_data_model/entities/notification_entities.md`

### 3.2 NotificationSetting Entity

Represents user or company-level notification preferences.

**Key Fields:**
- `Id` (Guid)
- `CompanyId` (Guid?, nullable for user-only settings)
- `UserId` (Guid?, nullable for company-wide settings)
- `NotificationType` (NotificationType?, nullable for default preferences)
- `Channel` (string: "in-app", "email", "both", "none")
- `Enabled` (bool)
- `MinimumPriority` (NotificationPriority?, filter by priority)
- `SoundEnabled` (bool)
- `DesktopNotificationsEnabled` (bool)

---

## 4. Notification Types

### 4.1 Available Types

| Type | Enum Value | Description | Default Priority |
|------|------------|-------------|------------------|
| VipEmailReceived | 1 | VIP email received | High |
| OrderAssigned | 2 | Order assigned to user | Normal |
| OrderRescheduled | 3 | Order rescheduled | Normal |
| KpiThresholdBreached | 4 | KPI threshold breached | High |
| MaterialLowStock | 5 | Material low stock alert | Normal |
| InvoiceGenerated | 6 | Invoice generated | Normal |
| SystemAlert | 7 | System alert | High |
| TaskAssigned | 8 | Task assigned to user | Normal |
| TaskCompleted | 9 | Task completed | Normal |
| Other | 99 | Generic notification | Normal |

### 4.2 Notification Priority

- **Low**: Informational, non-urgent
- **Normal**: Standard notifications
- **High**: Important, requires attention (e.g., VIP emails)
- **Critical**: Urgent, immediate action required

---

## 5. Settings & Configuration

### 5.1 Global Settings

Global settings provide system-wide defaults for notifications.

**Keys:**
- `NotificationDefaultChannel` (String): Default channel for all notifications (default: "in-app")
- `NotificationVipEmailChannel` (String): Channel for VIP emails (default: "in-app")
- `NotificationTaskAssignmentChannel` (String): Channel for task assignments (default: "in-app")
- `NotificationTaskStatusChannel` (String): Channel for task status updates (default: "in-app")
- `EmailVipStrictMode` (Bool): Require notifications for VIP emails (default: false)
- `NotificationRetentionDays` (Int): Days to retain notifications before auto-archive (default: 90)

**See:** `docs/02_modules/GLOBAL_SETTINGS_MODULE.md`

### 5.2 Company Settings

Company settings allow companies to override global defaults.

**Keys (same as Global Settings):**
- `NotificationDefaultChannel`
- `NotificationVipEmailChannel`
- `NotificationTaskAssignmentChannel`
- `NotificationTaskStatusChannel`

**Resolution Order:**
1. Company setting (if exists)
2. Global setting (if exists)
3. Hard-coded default

### 5.3 User Settings

User settings (via `NotificationSetting` entity) allow individual users to:
- Override company/global channel preferences
- Disable specific notification types
- Set minimum priority filters
- Configure sound/desktop notification preferences

**Resolution Order:**
1. User setting (if exists)
2. Company setting (if exists)
3. Global setting (if exists)
4. Hard-coded default

---

## 6. Notification Flow

### 6.1 Creating Notifications

```csharp
await _notificationService.CreateAsync(
    userId: userId,
    type: NotificationType.VipEmailReceived,
    title: "VIP Email Received",
    message: "Email from ceo@company.com",
    companyId: companyId,
    priority: NotificationPriority.High,
    actionUrl: "/email-messages/123",
    actionText: "View Email",
    relatedEntityId: emailId,
    relatedEntityType: "EmailMessage",
    metadata: { ... }
);
```

### 6.2 Channel Resolution

1. Check user `NotificationSetting` for this type
2. Check company `NotificationSetting` for this type
3. Check global setting (`NotificationVipEmailChannel`, etc.)
4. Fall back to `NotificationDefaultChannel`
5. Final fallback: "in-app"

### 6.3 Delivery

Currently, all notifications are delivered in-app only. Future channels:
- **Email**: Send email notifications via SMTP
- **SMS**: Send SMS via external service
- **Push**: Browser/mobile push notifications

---

## 7. Integration Points

### 7.1 Email Parser (VIP Emails)

**File:** `backend/src/CephasOps.Application/Parser/Services/EmailIngestionService.cs`

**Trigger:** When a VIP email is ingested

**Flow:**
1. `EmailIngestionService` detects VIP email via rule evaluation
2. Calls `VipEmailNotificationService.NotifyVipEmailReceivedAsync`
3. `VipEmailNotificationService` determines target users
4. Creates notifications for target users

**Target User Resolution:**
1. `EmailRule.TargetUserId` (if rule specifies user)
2. `VipEmail.NotifyUserId` (if VIP entry specifies user)
3. `VipEmail.NotifyRole` (find users with role) - ✅ **Implemented**
4. `EmailRule.TargetDepartmentId` (find department members) - ✅ **Implemented**
5. Default VIP recipients (configured in settings) - ✅ **Implemented**

**See:** `docs/01_system/EMAIL_PARSER.md`

### 7.2 Task Module

**File:** `backend/src/CephasOps.Application/Tasks/TaskService.cs`

**Triggers:**
1. **Task Assigned**: When `CreateTaskAsync` creates a task with `AssignedToUserId`
   - Notification type: `TaskAssigned`
   - Recipient: `AssignedToUserId`
   - Channel: `NotificationTaskAssignmentChannel` setting

2. **Task Completed**: When `UpdateTaskStatusAsync` changes status to `Completed`
   - Notification type: `TaskCompleted`
   - Recipient: `RequestedByUserId` (task requester)
   - Channel: `NotificationTaskStatusChannel` setting
   - **Note**: Only sent if `RequestedByUserId != updatedByUserId` (to avoid self-notification)

**See:** `docs/02_modules/tasks_module.md`

### 7.3 Future Integrations

- **Order Module**: Order assigned, rescheduled, blocked
- **Inventory Module**: Low stock alerts
- **Billing Module**: Invoice generated
- **KPI Module**: SLA threshold breached

---

## 8. API Endpoints

### 8.1 Get User Notifications

```
GET /api/notifications/my
Query Parameters:
  - companyId (Guid?, optional)
  - status (NotificationStatus?, optional: Unread, Read, Archived)
  - limit (int?, optional: limit results)

Response: List<NotificationDto>
```

### 8.2 Get Unread Count

```
GET /api/notifications/my/unread-count
Query Parameters:
  - companyId (Guid?, optional)

Response: { count: int }
```

### 8.3 Mark as Read

```
PUT /api/notifications/{id}/read

Response: 204 No Content
```

### 8.4 Mark All as Read

```
PUT /api/notifications/my/read-all
Query Parameters:
  - companyId (Guid?, optional)

Response: 204 No Content
```

### 8.5 Archive Notification

```
PUT /api/notifications/{id}/archive

Response: 204 No Content
```

**See:** `backend/src/CephasOps.Api/Controllers/NotificationsController.cs`

---

## 9. Frontend UI

### 9.1 Notification Bell

**Location:** Top navigation bar

**Features:**
- Badge count showing unread notifications
- Red badge if VIP notifications present
- Click to open dropdown panel

**See:** `docs/07_frontend/ui/vip_email_notifications.md`

### 9.2 Notification Dropdown

**Features:**
- VIP email alerts (pinned at top)
- Recent notifications (last 10)
- "View All" link
- Mark as read on click

### 9.3 Notifications Page

**Route:** `/notifications`

**Features:**
- Full list of notifications
- Tabs: All | Unread | VIP
- Sort options (newest first, priority)
- Bulk actions (mark all as read, archive all read)
- Filter by type, date range

### 9.4 Notification Settings Page

**Route:** `/settings/notifications`

**Features:**
- Toggle notification types on/off
- Set channel preferences per type
- Configure sound/desktop notifications
- Set minimum priority filters

**Status:** ⏳ **Pending Implementation**

---

## 10. Retention & Cleanup

### 10.1 Auto-Archive

Notifications older than `NotificationRetentionDays` (default: 90) are automatically archived.

**Status:** ⏳ **Pending Implementation** (requires background job)

### 10.2 Hard Delete

Archived notifications older than 1 year can be hard-deleted.

**Status:** ⏳ **Future Enhancement**

---

## 11. Future Enhancements

1. **Email Channel**: SMTP integration for email notifications
2. **SMS Channel**: SMS gateway integration
3. **Push Notifications**: Browser/mobile push via service worker
4. **Real-time Updates**: WebSocket/SignalR for live notifications
5. **Notification Templates**: Customizable notification templates
6. **Notification Groups**: Group related notifications (e.g., multiple VIP emails from same sender)
7. **Smart Notifications**: Machine learning to prioritize notifications based on user behavior
8. **Notification Preferences UI**: Full settings page for user preferences

---

## 12. Related Documentation

- **Data Model**: `docs/05_data_model/entities/notification_entities.md`
- **Relationships**: `docs/05_data_model/relationships/notification_relationships.md`
- **Global Settings**: `docs/02_modules/GLOBAL_SETTINGS_MODULE.md`
- **Email Parser**: `docs/01_system/EMAIL_PARSER.md`
- **Tasks Module**: `docs/02_modules/tasks_module.md`
- **Frontend UI**: `docs/07_frontend/ui/vip_email_notifications.md`

---

## 13. Implementation Status

| Component | Status | Completeness |
|-----------|--------|--------------|
| Notification Entity | ✅ Complete | 100% |
| Notification Service | ✅ Complete | 100% |
| Notification Repository | ✅ Complete | 100% |
| API Controller | ✅ Complete | 100% |
| Email Parser Integration | ✅ Complete | 100% |
| Task Module Integration | ✅ Complete | 100% |
| Global Settings | ✅ Complete | 100% |
| Company Settings | ✅ Complete | 100% |
| NotificationSetting Entity | ✅ Complete | 100% |
| Role-based Lookup | ⏳ Pending | 0% |
| Department Lookup | ⏳ Pending | 0% |
| Frontend UI | ⏳ Pending | 0% |
| Email/SMS Channels | ⏳ Future | 0% |
| Real-time Updates | ⏳ Future | 0% |

**Overall Backend Completeness:** ~85%

---

**End of Notification Module Specification**

