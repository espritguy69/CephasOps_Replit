# Notifications UI

## Overview

The Notifications UI provides a comprehensive in-app notification experience, allowing users to view, manage, and interact with notifications from various system events.

## Notification Bell / Dropdown

### Location
Top navigation bar (right side)

### Features

**Visual Indicator**:
- Bell icon with unread count badge
- Badge shows number of unread notifications
- Badge color changes based on priority (High/Critical = red)

**Dropdown Menu**:
- Opens on click
- Shows latest 10-15 notifications
- Grouped by:
  - Unread (top)
  - Read (bottom)
- Each notification shows:
  - Type icon (VIP email, Task, Order, etc.)
  - Title
  - Message preview (truncated)
  - Time ago (e.g., "2 minutes ago")
  - Priority indicator (if High/Critical)
- VIP email notifications have special badge/icon
- Clicking notification:
  - Marks as read (if unread)
  - Navigates to related entity (via `ActionUrl`)
- "View All" link to full notifications page

**Polling**:
- Polls backend every 30 seconds when dropdown is open
- Polls on window focus
- Uses `GET /api/companies/{companyId}/notifications/my/unread-count` for badge
- Uses `GET /api/companies/{companyId}/notifications/my?limit=15` for list

**Component**: `NotificationBell` or `NotificationDropdown`

## Notifications Page

### Route
`/notifications` or `/companies/{companyId}/notifications`

### Features

**Notification List**:
- Full list of all notifications for current user
- Sortable by:
  - Date (newest first, default)
  - Status (unread first)
  - Priority (high first)
  - Type
- Filterable by:
  - Status (All, Unread, Read, Archived)
  - Type (All, VIP Email, Task, Order, etc.)
  - Priority (All, High, Critical)
  - Date range

**Notification Card**:
- Type icon with color coding
- Title (bold)
- Message (full text)
- Related entity link (if `ActionUrl` present)
- Time ago + full timestamp on hover
- Priority badge (if High/Critical)
- VIP badge (if VIP email)
- Actions:
  - Mark as read (if unread)
  - Archive
  - View related entity (if `ActionUrl` present)

**Bulk Actions**:
- "Mark all as read" button
- "Archive all read" button (future)

**Empty States**:
- "No notifications" when list is empty
- "No unread notifications" when filtered to unread but none exist

**Pagination**:
- Load more / infinite scroll
- Default limit: 50 per page

**API Endpoints**:
- `GET /api/companies/{companyId}/notifications/my?status={status}&limit={limit}`
- `PUT /api/companies/{companyId}/notifications/{id}/read`
- `PUT /api/companies/{companyId}/notifications/my/read-all`
- `PUT /api/companies/{companyId}/notifications/{id}/archive`

**Component**: `NotificationsPage`

## Notification Settings UI (Optional)

### Route
`/settings/notifications` or `/companies/{companyId}/settings/notifications`

### Features

**Settings Form**:
- Per-notification-type preferences:
  - Channel: IN_APP, EMAIL, BOTH, NONE
  - Enabled: Yes/No
  - Minimum Priority: All, High, Critical
  - Sound alerts: Yes/No
  - Desktop notifications: Yes/No
- Notification types:
  - VIP Emails
  - Task Assignments
  - Task Completions
  - Order Assignments
  - Order Reschedules
  - KPI Threshold Breaches
  - Material Low Stock
  - Invoice Generated
  - System Alerts
- "Reset to Company Default" button per type
- "Reset All to Company Default" button

**Company Settings** (Admin only):
- Override global defaults for:
  - `NotificationDefaultChannel`
  - `NotificationVipEmailDefaultChannel`
  - `NotificationTaskDefaultChannel`
  - `EmailVipStrictMode`
  - `NotificationTaskStrictMode`

**Component**: `NotificationSettingsPage`

## UI Components

### NotificationCard
- Displays single notification
- Type icon
- Title and message
- Time ago
- Priority badge
- Action buttons
- Click to navigate

### NotificationBadge
- Unread count display
- Color-coded by priority
- Animated pulse for new notifications

### NotificationTypeIcon
- Icon per notification type:
  - VIP Email: Star/Envelope icon
  - Task: Checkbox/List icon
  - Order: Document icon
  - KPI: Chart icon
  - Material: Box icon
  - Invoice: Receipt icon
  - System: Alert icon

### PriorityBadge
- Low: Gray
- Normal: Blue
- High: Orange
- Critical: Red

### VIPBadge
- Special badge for VIP email notifications
- Gold/Star icon
- "VIP" text

## State Management

- Global notification state (context/store)
- Unread count state
- Notification list state
- Selected notification state
- Filter state
- Loading states
- Error states

## Real-Time Updates (Future)

- WebSocket/SignalR connection for real-time notifications
- Auto-update notification bell when new notification arrives
- Toast notification for new high-priority notifications
- Sound alert for critical notifications (if enabled)

## Responsive Design

- Mobile-friendly notification cards
- Swipe actions on mobile (mark as read, archive)
- Collapsible filters on mobile
- Touch-friendly action buttons
- Optimized for tablet and desktop

## Accessibility

- Keyboard navigation support
- Screen reader announcements for new notifications
- ARIA labels for notification actions
- Focus management when opening/closing dropdown

