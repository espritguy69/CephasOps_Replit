# Notification Relationships

## Overview

The Notification module integrates with several other modules to provide comprehensive notification capabilities across the system.

## Entity Relationships

### Notification Relationships

#### Company (Many-to-One, Optional)
- **Relationship**: `Notification` → `Company`
- **Foreign Key**: `Notification.CompanyId` → `Company.Id`
- **Cardinality**: Many notifications belong to one company (optional for global notifications)
- **Behavior**: Notifications are scoped by company for multi-tenant isolation
- **Cascade**: Restrict (cannot delete company with notifications)

#### User (Many-to-One)
- **Relationship**: `Notification` → `User` (Recipient)
- **Foreign Key**: `Notification.UserId` → `User.Id`
- **Cardinality**: Many notifications can be sent to one user
- **Behavior**: Tracks who should receive the notification
- **Cascade**: Restrict (cannot delete user with notifications)

#### ReadByUser (Many-to-One, Optional)
- **Relationship**: `Notification` → `User` (Reader)
- **Foreign Key**: `Notification.ReadByUserId` → `User.Id`
- **Cardinality**: Many notifications can be read by one user
- **Behavior**: Tracks who read/archived the notification (may differ from recipient)
- **Cascade**: Restrict

### NotificationSetting Relationships

#### Company (Many-to-One, Optional)
- **Relationship**: `NotificationSetting` → `Company`
- **Foreign Key**: `NotificationSetting.CompanyId` → `Company.Id`
- **Cardinality**: Many settings belong to one company (optional for user-only settings)
- **Behavior**: Company-wide notification preferences
- **Cascade**: Restrict

#### User (Many-to-One, Optional)
- **Relationship**: `NotificationSetting` → `User`
- **Foreign Key**: `NotificationSetting.UserId` → `User.Id`
- **Cardinality**: Many settings belong to one user (optional for company-wide settings)
- **Behavior**: User-specific notification preferences
- **Cascade**: Restrict

## Integration Points

### Email Parser Module
- VIP email detection triggers notifications
- `EmailMessage` entities are linked via `RelatedEntityId` and `RelatedEntityType = "EmailMessage"`
- `EmailRule` and `VipEmail` determine notification targets

### Task Module
- Task assignment triggers notifications
- Task completion triggers notifications
- `TaskItem` entities are linked via `RelatedEntityId` and `RelatedEntityType = "TaskItem"`

### Orders Module
- Order assignment triggers notifications
- Order rescheduling triggers notifications
- `Order` entities are linked via `RelatedEntityId` and `RelatedEntityType = "Order"`

### Settings Module
- `GlobalSetting` and `CompanySetting` control notification channels
- Settings resolution: Global → Company → User
- Keys: `NotificationDefaultChannel`, `NotificationVipEmailDefaultChannel`, `NotificationTaskDefaultChannel`

### Department Module
- Department members can receive notifications when emails are routed to their department
- `Department` entities are referenced via `EmailRule.TargetDepartmentId`

### RBAC Module
- Role-based notification targeting (e.g., notify all users with "CEO" role)
- `Role` and `UserRole` entities are used to find target users

## Data Flow

1. **VIP Email Notification Flow**:
   - Email received → `EmailIngestionService`
   - Rules evaluated → `EmailRuleEvaluationService`
   - VIP detected → `VipEmailNotificationService.NotifyVipEmailReceivedAsync`
   - Target users resolved (from rule, VIP email, department, or defaults)
   - Notifications created via `NotificationService.CreateForMultipleUsersAsync`
   - Notifications stored in database

2. **Task Notification Flow**:
   - Task created → `TaskService.CreateTaskAsync`
   - Notification created for `AssignedToUserId` via `NotificationService.CreateAsync`
   - Task completed → `TaskService.UpdateTaskStatusAsync`
   - Notification created for `RequestedByUserId` (if different from completer)

3. **Notification Retrieval Flow**:
   - User requests notifications → `NotificationService.GetUserNotificationsAsync`
   - Filtered by company, status, and limit
   - Returned as DTOs to frontend

4. **Notification Mark as Read Flow**:
   - User clicks notification → `NotificationService.MarkAsReadAsync`
   - `Status` updated to `Read`
   - `ReadAt` and `ReadByUserId` set
   - Notification updated in database

## Access Control

- **User Level**: Users can only see their own notifications (`UserId = currentUserId`)
- **Company Level**: All operations are scoped by `CompanyId` for multi-tenant isolation
- **Admin Level**: Admins may have access to view all notifications in their company (future enhancement)

## Notification Target Resolution (VIP Emails)

Priority order for determining who receives VIP email notifications:

1. **EmailRule.TargetUserId** or **VipEmail.NotifyUserId** (specific user)
2. **VipEmail.NotifyRole** (all users with that role in the company)
3. **EmailRule.TargetDepartmentId** (all active members of that department)
4. **Default recipients** from `VipEmailDefaultRecipients` setting (user IDs or role names)

If no targets are found and `EmailVipStrictMode = true`, notification is still created but logged as warning.
