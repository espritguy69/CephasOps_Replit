# VIP Email Notifications UI

This document describes the UI components and screens for displaying VIP email alerts clearly in the CephasOps admin interface.

## Overview

VIP email notifications alert users (especially CEOs, directors, and business contacts) when important emails are received. The UI should make these alerts highly visible and easy to act upon.

---

## 1. Notification Bell/Badge (Top Navigation)

### Location
- Top right of navigation bar
- Next to user profile menu

### Design
```
┌─────────────────────────────┐
│ [Logo]  CephasOps  [🔔 5]  │
└─────────────────────────────┘
```

### Behavior
- **Badge count**: Shows unread notification count (including VIP emails)
- **Badge color**: 
  - Normal: Gray/blue
  - Has VIP emails: **Red** with pulsing animation
  - Empty: Hidden
- **Click action**: Opens notification dropdown panel

### Implementation
```typescript
interface NotificationBellProps {
  unreadCount: number;
  hasVipNotifications: boolean;
  onClick: () => void;
}
```

---

## 2. Notification Dropdown Panel

### Appearance
- Slides down from notification bell
- Width: 400px
- Max height: 600px
- Scrollable if many notifications

### Sections
1. **VIP Email Alerts** (pinned at top)
2. **Recent Notifications**
3. **View All** link at bottom

### VIP Email Alert Card Design

```
┌─────────────────────────────────────┐
│ 🔴 VIP Email Received              │
│ From: ceo@company.com              │
│ Subject: Urgent: Project Update    │
│ [View Email]         2 minutes ago │
└─────────────────────────────────────┘
```

### Visual Indicators
- **Red border** or background highlight for VIP emails
- **VIP badge/icon** (crown icon 🔴 or "VIP" label)
- **Priority indicator**: High/Critical
- **Unread indicator**: Blue dot or bold text

### Card Components
- **Header**: "VIP Email Received" (red/bold)
- **From Address**: Sender email (bold)
- **Subject**: Email subject (truncated if long)
- **Action Button**: "View Email" (primary button)
- **Timestamp**: Relative time (e.g., "2 minutes ago")
- **Status**: Unread/Read indicator

---

## 3. Notification List Page

### Route
`/notifications` or `/notifications/my`

### Layout
```
┌─────────────────────────────────────────────┐
│ Notifications                     [Filter]  │
├─────────────────────────────────────────────┤
│ [Tabs: All | Unread | VIP]                 │
├─────────────────────────────────────────────┤
│ 🔴 VIP Email Received        [High]  ⭐    │
│ From: ceo@company.com                      │
│ Subject: Urgent: Project Update            │
│ [View Email]  Mark Read  Archive          │
│ 2 minutes ago                              │
├─────────────────────────────────────────────┤
│ Order Assigned                 [Normal]     │
│ Order #12345 has been assigned to you      │
│ [View Order]  Mark Read  Archive          │
│ 1 hour ago                                 │
└─────────────────────────────────────────────┘
```

### Features
1. **Tab Filtering**
   - All notifications
   - Unread only
   - VIP emails only (highlighted)

2. **Sort Options**
   - Newest first (default)
   - Oldest first
   - Priority (High → Low)

3. **Bulk Actions**
   - "Mark all as read"
   - "Archive all read"

4. **Search/Filter**
   - Search by sender email
   - Filter by notification type
   - Filter by date range

---

## 4. VIP Email Alert Card (Detailed)

### Design
```
┌─────────────────────────────────────────────┐
│ 🔴 VIP EMAIL                                │
│ ──────────────────────────────────────────  │
│ From: ceo@company.com                       │
│ Subject: Urgent: Project Update             │
│ Received: 2025-01-20 14:30                  │
│                                             │
│ Priority: High                              │
│ Status: Unread                              │
│                                             │
│ [View Email Details]  [Mark as Read]       │
└─────────────────────────────────────────────┘
```

### Components
- **VIP Badge**: Prominent red indicator
- **Sender Info**: Email address and display name
- **Email Details**: Subject, received time
- **Metadata**: Priority, status, attachments indicator
- **Actions**: View, Mark Read, Archive

---

## 5. Email Messages List - VIP Indicator

### Location
`/companies/{companyId}/email-messages`

### Design
```
┌─────────────────────────────────────────────┐
│ Email Messages                              │
├─────────────────────────────────────────────┤
│ 🔴 VIP | From              Subject    Status│
│ ───────┼────────────────────────────────────│
│    ⭐  | ceo@company.com  Urgent...  ✅    │
│        | noreply@time...  FTTH Order  ✅    │
│    ⭐  | director@comp...  Review...  ⏳    │
└─────────────────────────────────────────────┘
```

### Features
- **VIP Column**: Star icon (⭐) or VIP badge for VIP emails
- **Highlighted Row**: Light red/pink background for VIP emails
- **Sortable**: Can sort by VIP status
- **Filterable**: Filter to show only VIP emails

---

## 6. Dashboard Widget - VIP Email Alerts

### Location
Main dashboard (`/dashboard`)

### Widget Design
```
┌─────────────────────────────────┐
│ VIP Email Alerts         [🔴 3] │
├─────────────────────────────────┤
│ 🔴 CEO Email                     │
│ From: ceo@company.com            │
│ 5 minutes ago                    │
│ [View]                           │
├─────────────────────────────────┤
│ 🔴 Director Email                │
│ From: director@company.com       │
│ 1 hour ago                       │
│ [View]                           │
└─────────────────────────────────┘
```

### Behavior
- Shows **last 5 VIP emails** (unread first)
- **Auto-refresh** every 30 seconds
- Click to navigate to email details
- Badge shows unread VIP count

---

## 7. Real-time Updates

### WebSocket/Polling
- Real-time updates when new VIP emails arrive
- Toast notification: "New VIP email from {sender}"
- Notification bell badge updates automatically
- Sound alert (optional, configurable)

### Toast Notification Design
```
┌────────────────────────────────┐
│ 🔴 VIP Email Received          │
│ From: ceo@company.com          │
│ Subject: Urgent...             │
│ [View]  [Dismiss]              │
└────────────────────────────────┘
```

---

## 8. Email Detail View - VIP Badge

### Location
`/companies/{companyId}/email-messages/{id}`

### Design
```
┌─────────────────────────────────────────────┐
│ Email Details                               │
├─────────────────────────────────────────────┤
│ 🔴 VIP EMAIL                                │
│                                             │
│ From: ceo@company.com                       │
│ To: orders@cephas.com                       │
│ Subject: Urgent: Project Update             │
│ Received: 2025-01-20 14:30                  │
│ Status: Parsed                              │
│                                             │
│ [View Original] [Mark as Read] [Archive]   │
└─────────────────────────────────────────────┘
```

### Features
- Large **VIP badge** at top
- **Highlighted section** (red border or background)
- **Metadata display**: Matched rule, VIP entry info
- **Quick actions**: Mark as read, archive

---

## 9. Notification Settings

### Location
`/settings/notifications` or user profile settings

### Options
- **VIP Email Notifications**
  - Enable/disable VIP email alerts
  - Notification channels: In-app, Email, SMS
  - Sound alerts: On/Off
  - Desktop notifications: On/Off

- **Notification Preferences**
  - Default priority threshold
  - Auto-mark as read after X days
  - Auto-archive after X days

---

## 10. Color Scheme & Visual Guidelines

### VIP Email Colors
- **Primary**: Red (#DC2626 or #EF4444)
- **Background**: Light red/pink (#FEE2E2 or #FEF2F2)
- **Border**: Red (#DC2626)
- **Badge**: Red with white text

### Priority Colors
- **Critical**: Dark red (#991B1B)
- **High**: Red (#DC2626)
- **Normal**: Blue/Gray (#3B82F6)
- **Low**: Gray (#6B7280)

### Icons
- **VIP Badge**: Crown icon (👑) or star (⭐)
- **Notification Bell**: 🔔
- **Alert**: 🔴 (red circle)
- **Email**: ✉️ or 📧

---

## 11. Accessibility

### Requirements
- **Keyboard navigation**: Tab through notifications
- **Screen reader**: Announce "VIP email received" with sender
- **ARIA labels**: Proper labels for all interactive elements
- **Color contrast**: Meet WCAG AA standards
- **Focus indicators**: Clear focus states

---

## 12. Mobile/Responsive Design

### Mobile Layout
- **Notification bell**: Same position
- **Dropdown**: Full screen overlay on mobile
- **Cards**: Stack vertically
- **Actions**: Touch-friendly button sizes

### Tablet Layout
- **Dropdown**: Wider panel (600px)
- **Cards**: 2-column grid (if space allows)

---

## 13. Component Structure

### React/Component Example Structure
```
<NotificationBell>
  <NotificationBadge count={5} hasVip={true} />
  <NotificationDropdown>
    <VipEmailSection>
      <VipEmailCard key={id} email={email} />
    </VipEmailSection>
    <RecentNotifications>
      <NotificationCard key={id} notification={notification} />
    </RecentNotifications>
  </NotificationDropdown>
</NotificationBell>

<NotificationListPage>
  <NotificationTabs />
  <NotificationList>
    <VipEmailCard priority="high" />
    <NotificationCard />
  </NotificationList>
  <BulkActions />
</NotificationListPage>
```

---

## 14. API Integration

### Endpoints Used
- `GET /api/notifications/my` - Get user notifications
- `GET /api/notifications/my/unread-count` - Get unread count
- `PUT /api/notifications/{id}/read` - Mark as read
- `PUT /api/notifications/my/read-all` - Mark all as read
- `PUT /api/notifications/{id}/archive` - Archive notification

### Notification DTO Structure
```typescript
interface NotificationDto {
  id: string;
  type: 'VipEmailReceived' | 'OrderAssigned' | ...;
  priority: 'Low' | 'Normal' | 'High' | 'Critical';
  status: 'Unread' | 'Read' | 'Archived';
  title: string;
  message: string;
  actionUrl?: string;
  actionText?: string;
  relatedEntityId?: string;
  relatedEntityType?: string;
  metadataJson?: string; // Contains email details
  createdAt: string;
  readAt?: string;
}
```

---

## 15. Testing Checklist

- [ ] VIP email creates notification
- [ ] Notification bell shows correct badge count
- [ ] VIP notifications appear at top of list
- [ ] Clicking notification navigates to email
- [ ] Mark as read works correctly
- [ ] Archive works correctly
- [ ] Real-time updates work
- [ ] Mobile responsive design
- [ ] Accessibility (keyboard navigation, screen reader)
- [ ] Notification settings work

---

## 16. Future Enhancements

1. **Notification Groups**: Group multiple VIP emails from same sender
2. **Smart Notifications**: Learn user preferences, prioritize accordingly
3. **Email Preview**: Show email body preview in notification
4. **Bulk Actions**: Select multiple notifications for bulk operations
5. **Notification History**: View archived notifications
6. **Email Templates**: Customizable notification email templates
7. **Push Notifications**: Browser push notifications for VIP emails

---

## Related Documentation

- [EMAIL_PARSER.md](../01_system/EMAIL_PARSER.md) - Parser specification
- [EMAIL_PARSER_SETUP.md](../02_modules/EMAIL_PARSER_SETUP.md) - Email setup
- [VIP Email Implementation Summary](../../EMAIL_PARSER_VIP_IMPLEMENTATION_SUMMARY.md) - Backend implementation

