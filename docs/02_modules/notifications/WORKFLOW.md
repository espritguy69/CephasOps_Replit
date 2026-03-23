# Notifications – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Notifications module, covering notification creation, delivery channels, read/unread tracking, and archival

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      NOTIFICATIONS MODULE SYSTEM                         │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   NOTIFICATIONS        │      │   DELIVERY CHANNELS    │
        │  (In-App Messages)    │      │  (Email/SMS/WhatsApp) │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Type                 │      │ • Email               │
        │ • Priority             │      │ • SMS                 │
        │ • Status (Unread/Read) │      │ • WhatsApp            │
        │ • Title/Message        │      │ • In-App              │
        │ • Action URL           │      └───────────────────────┘
        └───────────────────────┘
                    │
                    ▼
        ┌───────────────────────┐
        │   NOTIFICATION SETTINGS│
        │  (User Preferences)   │
        └───────────────────────┘
```

---

## Complete Workflow: Notification Lifecycle

```
[STEP 1: NOTIFICATION TRIGGER]
         |
         v
[System Event Occurs]
  Examples:
    - Order status changed
    - New order assigned
    - Blocker raised
    - Reschedule requested
    - Invoice created
    - Payment received
         |
         v
┌────────────────────────────────────────┐
│ CREATE NOTIFICATION                      │
│ NotificationService.CreateNotification()│
└────────────────────────────────────────┘
         |
         v
[Resolve Recipients]
         |
         v
┌────────────────────────────────────────┐
│ RESOLVE USERS BY ROLE                     │
│ NotificationService.ResolveUsersByRole()  │
└────────────────────────────────────────┘
         |
         v
[For Order Status Change]
  ResolveUsersByRole("Manager")
  → Returns: [user-1, user-2, user-3]
         |
         v
[For SI Assignment]
  ResolveUsersByRole("ServiceInstaller")
  → Returns: [si-user-123]
         |
         v
[For each Recipient]
         |
         v
┌────────────────────────────────────────┐
│ CREATE NOTIFICATION RECORD                 │
└────────────────────────────────────────┘
         |
         v
Notification {
  Id: "notif-789"
  CompanyId: Cephas
  UserId: "user-123"
  Type: "OrderStatusChanged"
  Priority: "Normal" | "High" | "Urgent"
  Status: "Unread"
  Title: "Order TBBN1234567 status changed to Assigned"
  Message: "Order TBBN1234567 has been assigned to SI Ahmad"
  ActionUrl: "/orders/order-456"
  ActionText: "View Order"
  RelatedEntityId: "order-456"
  RelatedEntityType: "Order"
  DeliveryChannels: ["InApp", "Email"]
  ExpiresAt: null
  CreatedAt: 2025-12-12
}
         |
         v
[STEP 2: DELIVERY TO CHANNELS]
         |
         v
[For each Delivery Channel]
         |
    ┌────┴────┐
    |         |
    v         v
[IN-APP] [EMAIL]
   |         |
   |         v
   |    ┌────────────────────────────────────────┐
   |    │ SEND EMAIL                                │
   |    │ EmailService.SendNotification()            │
   |    └────────────────────────────────────────┘
   |         |
   |         v
   |    [Email Sent]
   |
   v
[In-App Notification Available]
  - Stored in database
  - Visible in notification bell
  - Count updated
         |
         v
[If SMS Channel]
         |
         v
┌────────────────────────────────────────┐
│ SEND SMS                                  │
│ SmsMessagingService.SendNotification()    │
└────────────────────────────────────────┘
         |
         v
[SMS Sent]
         |
         v
[If WhatsApp Channel]
         |
         v
┌────────────────────────────────────────┐
│ SEND WHATSAPP                            │
│ WhatsAppMessagingService.SendNotification()│
└────────────────────────────────────────┘
         |
         v
[WhatsApp Sent]
         |
         v
[STEP 3: USER VIEWS NOTIFICATION]
         |
         v
[User Opens Notification Center]
         |
         v
┌────────────────────────────────────────┐
│ GET USER NOTIFICATIONS                   │
│ GET /api/notifications/my                │
└────────────────────────────────────────┘
         |
         v
[Query Notifications]
  Notification.find(
    UserId = "user-123"
    Status IN ["Unread", "Read"]
    ExpiresAt IS NULL OR ExpiresAt > NOW()
  )
         |
         v
[Return Notifications List]
  [
    {
      Id: "notif-789"
      Type: "OrderStatusChanged"
      Priority: "Normal"
      Status: "Unread"
      Title: "Order TBBN1234567 status changed"
      ActionUrl: "/orders/order-456"
      CreatedAt: 2025-12-12
    },
    ...
  ]
         |
         v
[STEP 4: MARK AS READ]
         |
         v
[User Clicks Notification]
         |
         v
┌────────────────────────────────────────┐
│ MARK NOTIFICATION AS READ                 │
│ PUT /api/notifications/{id}/status       │
└────────────────────────────────────────┘
         |
         v
MarkNotificationStatusDto {
  IsRead: true
  IsArchived: false
}
         |
         v
[Update Notification]
  Notification {
    Status: "Read"
    ReadAt: 2025-12-12 10:30
    ReadByUserId: "user-123"
  }
         |
         v
[STEP 5: ARCHIVE NOTIFICATION]
         |
         v
[User Archives Notification]
         |
         v
┌────────────────────────────────────────┐
│ ARCHIVE NOTIFICATION                      │
│ PUT /api/notifications/{id}/status       │
└────────────────────────────────────────┘
         |
         v
MarkNotificationStatusDto {
  IsRead: true
  IsArchived: true
}
         |
         v
[Update Notification]
  Notification {
    Status: "Archived"
    ArchivedAt: 2025-12-12 11:00
  }
         |
         v
[Notification Archived]
```

---

## Notification Creation Flow

```
[System Event Triggers Notification]
         |
         v
[Example: Order Status Changed]
  Order {
    Id: "order-456"
    Status: "Assigned"
    AssignedSiId: "SI-123"
  }
         |
         v
┌────────────────────────────────────────┐
│ ORDER STATUS CHANGED NOTIFICATION HANDLER │
│ OrderStatusChangedNotificationHandler.HandleAsync()│
└────────────────────────────────────────┘
         |
         v
[Determine Notification Type]
  NotificationType: "OrderStatusChanged"
         |
         v
[Determine Recipients]
         |
    ┌────┴────┐
    |         |
    v         v
[SI ASSIGNED] [ADMIN/MANAGER]
   |              |
   |              v
   |         [Resolve by Role]
   |             - Managers
   |             - Admins
   |             - HOD
   |
   v
[Resolve SI User]
  ServiceInstaller {
    UserId: "si-user-123"
  }
         |
         v
[For each Recipient]
         |
         v
┌────────────────────────────────────────┐
│ CREATE NOTIFICATION                      │
└────────────────────────────────────────┘
         |
         v
Notification {
  UserId: "si-user-123"
  Type: "OrderAssigned"
  Priority: "Normal"
  Title: "New Order Assigned: TBBN1234567"
  Message: "You have been assigned order TBBN1234567 for customer John Doe"
  ActionUrl: "/si-app/orders/order-456"
  RelatedEntityId: "order-456"
  RelatedEntityType: "Order"
  DeliveryChannels: ["InApp", "SMS"]
}
         |
         v
[Send via Delivery Channels]
         |
         v
[In-App: Stored in database]
[Email: Sent via EmailService]
[SMS: Sent via SmsMessagingService]
[WhatsApp: Sent via WhatsAppMessagingService]
```

---

## Delivery Channel Flow

```
[Notification Created]
         |
         v
[For each Delivery Channel in DeliveryChannels]
         |
    ┌────┴────┐
    |         |
    v         v
[IN-APP] [EMAIL]
   |         |
   |         v
   |    ┌────────────────────────────────────────┐
   |    │ SEND EMAIL                                │
   |    │ UnifiedMessagingService.SendEmail()        │
   |    └────────────────────────────────────────┘
   |         |
   |         v
   |    [Email Template Resolved]
   |         |
   |         v
   |    [Email Sent]
   |         |
   |         v
   |    [Delivery Status Tracked]
   |
   v
[In-App Notification]
  - Stored in Notifications table
  - Visible in notification bell
  - Unread count incremented
         |
         v
[If SMS in DeliveryChannels]
         |
         v
┌────────────────────────────────────────┐
│ SEND SMS                                  │
│ UnifiedMessagingService.SendSms()        │
└────────────────────────────────────────┘
         |
         v
[SMS Template Resolved]
         |
         v
[SMS Sent via Gateway]
         |
         v
[Delivery Status Tracked]
         |
         v
[If WhatsApp in DeliveryChannels]
         |
         v
┌────────────────────────────────────────┐
│ SEND WHATSAPP                            │
│ UnifiedMessagingService.SendWhatsApp()  │
└────────────────────────────────────────┘
         |
         v
[WhatsApp Template Resolved]
         |
         v
[WhatsApp Sent via Provider]
         |
         v
[Delivery Status Tracked]
```

---

## Notification Status Flow

```
[Notification Created]
         |
         v
Notification {
  Status: "Unread"
}
         |
         v
[User Views Notification]
         |
         v
┌────────────────────────────────────────┐
│ MARK AS READ                             │
│ PUT /api/notifications/{id}/status       │
└────────────────────────────────────────┘
         |
         v
[Update Status]
  Notification {
    Status: "Read"
    ReadAt: 2025-12-12 10:30
    ReadByUserId: "user-123"
  }
         |
         v
[User Archives Notification]
         |
         v
┌────────────────────────────────────────┐
│ ARCHIVE NOTIFICATION                      │
│ PUT /api/notifications/{id}/status       │
└────────────────────────────────────────┘
         |
         v
[Update Status]
  Notification {
    Status: "Archived"
    ArchivedAt: 2025-12-12 11:00
  }
         |
         v
[Notification Expires (if ExpiresAt set)]
         |
         v
[Notification No Longer Visible]
```

---

## Entities Involved

### Notification Entity
```
Notification
├── Id (Guid)
├── CompanyId (Guid?)
├── UserId (Guid)
├── Type (string: OrderStatusChanged, OrderAssigned, BlockerRaised, etc.)
├── Priority (string: Normal, High, Urgent)
├── Status (string: Unread, Read, Archived)
├── Title (string)
├── Message (string)
├── ActionUrl (string?)
├── ActionText (string?)
├── RelatedEntityId (Guid?)
├── RelatedEntityType (string?)
├── MetadataJson (string?)
├── ReadAt (DateTime?)
├── ReadByUserId (Guid?)
├── ArchivedAt (DateTime?)
├── ExpiresAt (DateTime?)
├── DeliveryChannels (string[], JSON)
└── CreatedAt, UpdatedAt
```

### NotificationSetting Entity
```
NotificationSetting
├── Id (Guid)
├── CompanyId (Guid)
├── UserId (Guid)
├── NotificationType (string)
├── EmailEnabled (bool)
├── SmsEnabled (bool)
├── WhatsAppEnabled (bool)
├── InAppEnabled (bool)
└── CreatedAt, UpdatedAt
```

---

## API Endpoints Involved

### Notifications
- `GET /api/notifications/my` - Get current user's notifications
- `GET /api/notifications/my/unread-count` - Get unread count
- `GET /api/notifications/{id}` - Get notification details
- `PUT /api/notifications/{id}/status` - Mark as read/archived
- `DELETE /api/notifications/{id}` - Delete notification

### Notification Settings
- `GET /api/notifications/settings` - Get user notification settings
- `PUT /api/notifications/settings` - Update notification settings

---

## Module Rules & Validations

### Notification Creation Rules
- UserId is required
- Type must be valid
- Title and Message required
- Priority defaults to "Normal" if not specified
- Status defaults to "Unread"
- DeliveryChannels must be valid (InApp, Email, SMS, WhatsApp)

### Delivery Channel Rules
- In-App: Always available, stored in database
- Email: Requires email address for user
- SMS: Requires phone number for user
- WhatsApp: Requires phone number for user
- Delivery failures logged but don't block notification creation

### Status Transition Rules
- Unread → Read: User views notification
- Read → Archived: User archives notification
- Archived notifications not shown in default list
- Expired notifications (ExpiresAt < Now) not shown

### User Resolution Rules
- Resolve by Role: Returns all users with specified role
- Resolve by User ID: Single user notification
- Resolve by Department: All users in department
- Company context applied (if multi-company)

---

## Integration Points

### Orders Module
- Order status changes trigger notifications
- Order assignment notifies SI
- Blocker raised notifies admin/manager
- Reschedule requests notify admin

### Workflow Engine
- Status transitions trigger notifications
- Side effects can create notifications
- Notification handlers registered

### Email Module
- Email delivery via EmailService
- Email templates used for formatting
- Delivery status tracked

### SMS Module
- SMS delivery via SmsMessagingService
- SMS templates used
- Gateway integration

### WhatsApp Module
- WhatsApp delivery via WhatsAppMessagingService
- WhatsApp templates used
- Provider integration

### Service Installers Module
- SI assignment notifications
- SI job completion notifications
- SI availability notifications

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/notifications/OVERVIEW.md` - Notifications overview
- `docs/02_modules/orders/WORKFLOW.md` - Orders workflow

