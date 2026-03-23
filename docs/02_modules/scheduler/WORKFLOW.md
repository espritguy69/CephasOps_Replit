# Scheduler – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Scheduler module, covering slot creation, SI availability, assignment, confirmation, posting, and reschedule workflows

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         SCHEDULER MODULE SYSTEM                          │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   SCHEDULE SLOTS        │      │   SI AVAILABILITY      │
        │  (Order Assignments)    │      │  (Working Days/Hours)   │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Draft                 │      │ • Working Days         │
        │ • Confirmed             │      │ • Working Hours        │
        │ • Posted                │      │ • Max Jobs per Day     │
        │ • RescheduleRequested   │      │ • Current Jobs Count   │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   CONFLICT DETECTION    │      │   KPI INTEGRATION      │
        │  (Overlapping Slots)   │      │  (Job Duration)        │
        └───────────────────────┘      └───────────────────────┘
```

---

## Complete Workflow: Schedule Slot Lifecycle

```
[STEP 1: CREATE SCHEDULE SLOT]
         |
         v
┌────────────────────────────────────────┐
│ CREATE SCHEDULE SLOT                     │
│ POST /api/scheduler/slots                │
└────────────────────────────────────────┘
         |
         v
CreateScheduleSlotDto {
  OrderId: "order-456"
  ServiceInstallerId: "SI-123"
  Date: 2025-12-15
  WindowFrom: "09:00"
  WindowTo: "12:00"
  PlannedTravelMin: 30
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE SLOT CREATION                   │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |         |
   |         v
   |    [Reject Creation]
   |    [Show Errors]
   |
   v
Checks:
  ✓ Order exists and is in Pending/Assigned status
  ✓ SI exists and is active
  ✓ Date is not in the past
  ✓ Window times are valid
  ✓ SI has availability (optional check)
         |
         v
[Calculate Sequence Index]
  Get existing slots for SI on this date
  MaxSequenceIndex = Max(existing slots)
  SequenceIndex = MaxSequenceIndex + 1
         |
         v
┌────────────────────────────────────────┐
│ CREATE SCHEDULED SLOT                    │
└────────────────────────────────────────┘
         |
         v
ScheduledSlot {
  Id: "slot-789"
  CompanyId: Cephas
  OrderId: "order-456"
  ServiceInstallerId: "SI-123"
  Date: 2025-12-15
  WindowFrom: "09:00"
  WindowTo: "12:00"
  PlannedTravelMin: 30
  SequenceIndex: 1
  Status: "Draft"
  CreatedByUserId: "admin-123"
}
         |
         v
[STEP 2: CONFIRM SCHEDULE]
         |
         v
┌────────────────────────────────────────┐
│ CONFIRM SCHEDULE SLOT                    │
│ POST /api/scheduler/slots/{id}/confirm   │
└────────────────────────────────────────┘
         |
         v
[Validate Status]
  ✓ Status must be "Draft"
         |
         v
[Update Slot]
  ScheduledSlot {
    Status: "Confirmed"
    ConfirmedByUserId: "admin-123"
    ConfirmedAt: 2025-12-12
  }
         |
         v
[STEP 3: POST TO SI]
         |
         v
┌────────────────────────────────────────┐
│ POST SCHEDULE TO SI                      │
│ POST /api/scheduler/slots/{id}/post      │
└────────────────────────────────────────┘
         |
         v
[Validate Status]
  ✓ Status must be "Confirmed"
         |
         v
┌────────────────────────────────────────┐
│ DETECT SCHEDULING CONFLICTS              │
│ SchedulerService.DetectSchedulingConflicts()│
└────────────────────────────────────────┘
         |
         v
[Check for Conflicts]
  - Overlapping time windows
  - Same SI, same date
  - Travel time conflicts
  - Max jobs per day exceeded
         |
    ┌────┴────┐
    |         |
    v         v
[NO CONFLICTS] [CONFLICTS DETECTED]
   |              |
   |              v
   |         [Log Conflicts]
   |         [Continue with Warning]
   |         [Metadata includes conflict info]
   |
   v
[If Order Status is Pending]
         |
         v
┌────────────────────────────────────────┐
│ TRANSITION ORDER TO ASSIGNED              │
│ WorkflowEngineService.ExecuteTransition()│
└────────────────────────────────────────┘
         |
         v
[Status: Pending → Assigned]
         |
         v
[Update Slot Status]
  ScheduledSlot {
    Status: "Posted"
    PostedByUserId: "admin-123"
    PostedAt: 2025-12-12
  }
         |
         v
[Notification Sent to SI]
         |
         v
[STEP 4: SI RESCHEDULE REQUEST]
         |
         v
[SI App: Request Reschedule]
         |
         v
┌────────────────────────────────────────┐
│ REQUEST RESCHEDULE                        │
│ POST /api/scheduler/slots/{id}/reschedule│
└────────────────────────────────────────┘
         |
         v
Reschedule Request {
  NewDate: 2025-12-20
  NewWindowFrom: "14:00"
  NewWindowTo: "17:00"
  Reason: "Customer unavailable"
  Notes: "Customer requested later time"
  SiId: "SI-123"
}
         |
         v
[Check Reschedule Type]
         |
    ┌────┴────┐
    |         |
    v         v
[SAME-DAY] [DIFFERENT-DAY]
   |              |
   |              v
   |         [Different Day Reschedule]
   |              |
   |              v
   |         [Status: Assigned → ReschedulePendingApproval]
   |              |
   |              v
   |         [Lock Order]
   |              |
   |              v
   |         [Send Notifications to Admin/Manager]
   |
   v
[Update Slot]
  ScheduledSlot {
    Status: "RescheduleRequested"
    RescheduleRequestedDate: 2025-12-20
    RescheduleRequestedTime: "14:00"
    RescheduleReason: "Customer unavailable"
    RescheduleNotes: "Customer requested later time"
    RescheduleRequestedBySiId: "SI-123"
    RescheduleRequestedAt: 2025-12-15
  }
         |
         v
[STEP 5: ADMIN APPROVE/RESCHEDULE]
         |
         v
┌────────────────────────────────────────┐
│ APPROVE RESCHEDULE                        │
│ POST /api/scheduler/slots/{id}/approve-reschedule│
└────────────────────────────────────────┘
         |
         v
[Validate Status]
  ✓ Status must be "RescheduleRequested"
         |
         v
[Update Order Appointment]
  Order {
    AppointmentDate: 2025-12-20 (from RescheduleRequestedDate)
    AppointmentWindowFrom: "14:00" (from RescheduleRequestedTime)
    AppointmentWindowTo: "17:00" (calculated from duration)
  }
         |
         v
[If Order Status is ReschedulePendingApproval]
         |
         v
┌────────────────────────────────────────┐
│ TRANSITION ORDER TO ASSIGNED              │
│ WorkflowEngineService.ExecuteTransition()│
└────────────────────────────────────────┘
         |
         v
[Status: ReschedulePendingApproval → Assigned]
         |
         v
[Update Slot]
  ScheduledSlot {
    Date: 2025-12-20
    WindowFrom: "14:00"
    WindowTo: "17:00"
    Status: "RescheduleApproved"
    RescheduleRequestedDate: null (cleared)
    RescheduleRequestedTime: null (cleared)
    RescheduleReason: null (cleared)
  }
         |
         v
[Notification Sent to SI]
```

---

## SI Availability Management Flow

```
[STEP 1: CREATE SI AVAILABILITY]
         |
         v
┌────────────────────────────────────────┐
│ CREATE SI AVAILABILITY                   │
│ POST /api/scheduler/availability          │
└────────────────────────────────────────┘
         |
         v
CreateSiAvailabilityDto {
  ServiceInstallerId: "SI-123"
  Date: 2025-12-15
  IsWorkingDay: true
  WorkingFrom: "08:00"
  WorkingTo: "18:00"
  MaxJobs: 5
  Notes: "Normal working day"
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE AVAILABILITY                    │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |         |
   |         v
   |    [Reject Creation]
   |
   v
Checks:
  ✓ SI exists and is active
  ✓ Date is valid
  ✓ Working hours are valid
  ✓ MaxJobs > 0
         |
         v
┌────────────────────────────────────────┐
│ CREATE SI AVAILABILITY                   │
└────────────────────────────────────────┘
         |
         v
SiAvailability {
  Id: "avail-456"
  CompanyId: Cephas
  ServiceInstallerId: "SI-123"
  Date: 2025-12-15
  IsWorkingDay: true
  WorkingFrom: "08:00"
  WorkingTo: "18:00"
  MaxJobs: 5
  CurrentJobsCount: 0
  Notes: "Normal working day"
}
         |
         v
[STEP 2: UPDATE AVAILABILITY]
         |
         v
[When Slot is Posted]
         |
         v
[Increment CurrentJobsCount]
  SiAvailability {
    CurrentJobsCount: 0 → 1
  }
         |
         v
[Check Capacity]
  If CurrentJobsCount >= MaxJobs:
    → SI at capacity
    → Cannot assign more jobs
         |
         v
[When Slot is Cancelled]
         |
         v
[Decrement CurrentJobsCount]
  SiAvailability {
    CurrentJobsCount: 1 → 0
  }
```

---

## Conflict Detection Flow

```
[Before Posting Schedule]
         |
         v
┌────────────────────────────────────────┐
│ DETECT SCHEDULING CONFLICTS              │
│ SchedulerService.DetectSchedulingConflicts()│
└────────────────────────────────────────┘
         |
         v
[Query Existing Slots]
  ScheduledSlot.find(
    ServiceInstallerId = "SI-123"
    Date = 2025-12-15
    Status IN ["Draft", "Confirmed", "Posted"]
    SlotId != currentSlotId
  )
         |
         v
[For each Existing Slot]
         |
         v
[Check Time Overlap]
  New Slot: 09:00 - 12:00
  Existing Slot: 10:00 - 13:00
         |
    ┌────┴────┐
    |         |
    v         v
[OVERLAPS] [NO OVERLAP]
   |            |
   |            v
   |       [No Conflict]
   |
   v
[Check Travel Time]
  PlannedTravelMin: 30 minutes
  Gap between slots: 15 minutes
         |
    ┌────┴────┐
    |         |
    v         v
[INSUFFICIENT] [SUFFICIENT]
   |              |
   |              v
   |         [No Conflict]
   |
   v
[Create Conflict Record]
  Conflict {
    SlotId: "slot-789"
    OrderId: "order-456"
    ConflictType: "TimeOverlap"
    ConflictDescription: "Overlaps with existing slot"
    WindowFrom: "10:00"
    WindowTo: "13:00"
  }
         |
         v
[Check Max Jobs]
  SiAvailability {
    MaxJobs: 5
    CurrentJobsCount: 5
  }
         |
    ┌────┴────┐
    |         |
    v         v
[AT CAPACITY] [HAS CAPACITY]
   |              |
   |              v
   |         [No Conflict]
   |
   v
[Create Conflict Record]
  Conflict {
    ConflictType: "MaxJobsExceeded"
    ConflictDescription: "SI has reached max jobs for the day"
  }
         |
         v
[Return Conflicts List]
         |
         v
[If Conflicts Exist]
  → Log warnings
  → Include in metadata
  → Allow posting with warning (or block based on settings)
```

---

## Calendar Integration Flow

```
[STEP 1: GET CALENDAR VIEW]
         |
         v
┌────────────────────────────────────────┐
│ GET CALENDAR                            │
│ GET /api/scheduler/calendar              │
└────────────────────────────────────────┘
         |
         v
Query Parameters {
  CompanyId: Cephas
  FromDate: 2025-12-01
  ToDate: 2025-12-31
}
         |
         v
[For each Date in Range]
         |
         v
[Get Schedule Slots]
  ScheduledSlot.find(
    CompanyId = Cephas
    Date = currentDate
  )
         |
         v
[Get SI Availabilities]
  SiAvailability.find(
    CompanyId = Cephas
    Date = currentDate
  )
         |
         v
[Resolve KPI Profile for Each Slot]
         |
         v
[For each Slot]
  KpiProfileService.GetEffectiveProfile(
    CompanyId = Cephas
    PartnerId = order.PartnerId
    OrderType = orderType.Name
    BuildingTypeId = building.BuildingTypeId
    ReferenceDate = slot.Date
  )
         |
         v
[Calculate Expected Duration]
  ExpectedDurationMinutes = KpiProfile.MaxJobDurationMinutes
         |
         v
[Build Calendar DTO]
  CalendarDto {
    Date: 2025-12-15
    Slots: [
      {
        OrderId: "order-456"
        ServiceInstallerId: "SI-123"
        WindowFrom: "09:00"
        WindowTo: "12:00"
        Status: "Posted"
        ExpectedDurationMinutes: 180
        KpiProfileName: "Standard Activation Profile"
      }
    ]
    Availabilities: [
      {
        ServiceInstallerId: "SI-123"
        IsWorkingDay: true
        WorkingFrom: "08:00"
        WorkingTo: "18:00"
        MaxJobs: 5
        CurrentJobsCount: 1
      }
    ]
  }
         |
         v
[Return Calendar Data]
```

---

## Unassigned Orders Flow

```
[STEP 1: GET UNASSIGNED ORDERS]
         |
         v
┌────────────────────────────────────────┐
│ GET UNASSIGNED ORDERS                    │
│ GET /api/scheduler/unassigned-orders     │
└────────────────────────────────────────┘
         |
         v
[Query Orders]
  Order.find(
    Status IN ["Pending", "Assigned"]
    NOT IN (SELECT OrderId FROM ScheduledSlots)
  )
         |
         v
[For each Unassigned Order]
         |
         v
[Resolve KPI Profile]
  KpiProfileService.GetEffectiveProfile(
    CompanyId = Cephas
    PartnerId = order.PartnerId
    OrderType = orderType.Name
    BuildingTypeId = building.BuildingTypeId
    ReferenceDate = order.AppointmentDate
  )
         |
         v
[Calculate Expected Duration]
  ExpectedDurationMinutes = KpiProfile.MaxJobDurationMinutes
         |
         v
[Build Unassigned Order DTO]
  UnassignedOrderDto {
    Id: "order-456"
    ServiceId: "TBBN1234567"
    CustomerName: "John Doe"
    BuildingName: "Tower A"
    PartnerName: "TIME"
    AppointmentDate: 2025-12-15
    ExpectedDurationMinutes: 180
    KpiProfileName: "Standard Activation Profile"
  }
         |
         v
[Return Unassigned Orders List]
```

---

## Entities Involved

### ScheduledSlot Entity
```
ScheduledSlot
├── Id (Guid)
├── CompanyId (Guid)
├── OrderId (Guid)
├── ServiceInstallerId (Guid)
├── Date (DateTime)
├── WindowFrom (TimeSpan)
├── WindowTo (TimeSpan)
├── PlannedTravelMin (int?)
├── SequenceIndex (int)
├── Status (string: Draft, Confirmed, Posted, RescheduleRequested, RescheduleApproved, Cancelled)
├── ConfirmedByUserId (Guid?)
├── ConfirmedAt (DateTime?)
├── PostedByUserId (Guid?)
├── PostedAt (DateTime?)
├── RescheduleRequestedDate (DateTime?)
├── RescheduleRequestedTime (TimeSpan?)
├── RescheduleReason (string?)
├── RescheduleNotes (string?)
├── RescheduleRequestedBySiId (Guid?)
├── RescheduleRequestedAt (DateTime?)
└── CreatedAt, UpdatedAt
```

### SiAvailability Entity
```
SiAvailability
├── Id (Guid)
├── CompanyId (Guid)
├── ServiceInstallerId (Guid)
├── Date (DateTime)
├── IsWorkingDay (bool)
├── WorkingFrom (TimeSpan?)
├── WorkingTo (TimeSpan?)
├── MaxJobs (int)
├── CurrentJobsCount (int)
├── Notes (string?)
└── CreatedAt, UpdatedAt
```

---

## API Endpoints Involved

### Schedule Slots
- `GET /api/scheduler/slots` - Get schedule slots with filters
- `GET /api/scheduler/slots/{id}` - Get slot details
- `POST /api/scheduler/slots` - Create schedule slot
- `PUT /api/scheduler/slots/{id}` - Update schedule slot
- `POST /api/scheduler/slots/{id}/confirm` - Confirm schedule slot
- `POST /api/scheduler/slots/{id}/post` - Post schedule to SI
- `POST /api/scheduler/slots/{id}/reschedule` - Request reschedule (SI)
- `POST /api/scheduler/slots/{id}/approve-reschedule` - Approve reschedule (Admin)
- `POST /api/scheduler/slots/{id}/reject-reschedule` - Reject reschedule (Admin)
- `POST /api/scheduler/slots/{id}/return-to-draft` - Return to draft

### Calendar
- `GET /api/scheduler/calendar` - Get calendar view (slots + availability)
- `GET /api/scheduler/unassigned-orders` - Get unassigned orders

### SI Availability
- `GET /api/scheduler/availability` - Get SI availability
- `POST /api/scheduler/availability` - Create SI availability
- `PUT /api/scheduler/availability/{id}` - Update SI availability

### Conflict Detection
- `GET /api/scheduler/conflicts` - Detect scheduling conflicts

---

## Module Rules & Validations

### Slot Creation Rules
- Order must exist and be in Pending or Assigned status
- SI must be active
- Date cannot be in the past
- Window times must be valid (WindowFrom < WindowTo)
- Sequence index auto-calculated (increments for same SI/date)

### Confirmation Rules
- Only Draft slots can be confirmed
- Confirmed slots can be returned to Draft
- Confirmation requires user ID

### Posting Rules
- Only Confirmed slots can be posted
- Posting triggers order status transition (Pending → Assigned)
- Conflicts are detected but may not block posting (configurable)
- SI availability is checked (MaxJobs)

### Reschedule Rules
- Only Posted slots can be rescheduled
- Same-day reschedule: No approval needed
- Different-day reschedule: Requires approval, transitions order to ReschedulePendingApproval
- Reschedule request must include reason

### Availability Rules
- MaxJobs must be > 0
- Working hours must be valid (WorkingFrom < WorkingTo)
- CurrentJobsCount auto-increments when slots are posted
- CurrentJobsCount auto-decrements when slots are cancelled

### Conflict Detection Rules
- Time overlap: Windows overlap on same date for same SI
- Travel time: Insufficient gap between consecutive slots
- Max jobs: CurrentJobsCount >= MaxJobs
- Conflicts are logged but may not block (configurable)

---

## Integration Points

### Orders Module
- Order assignment creates ScheduledSlot
- Order status transitions triggered by slot posting
- Order appointment dates updated on reschedule approval
- Order status changed to ReschedulePendingApproval on different-day reschedule

### Workflow Engine
- Slot posting triggers order status transition (Pending → Assigned)
- Reschedule approval triggers order status transition (ReschedulePendingApproval → Assigned)
- All transitions go through WorkflowEngineService

### KPI Module
- KPI profiles resolved for each slot to determine expected duration
- Expected duration displayed in calendar and slot views
- KPI profile name included in slot DTOs

### Notifications Module
- SI notified when schedule is posted
- Admin/Manager notified on reschedule requests
- SI notified on reschedule approval/rejection

### Service Installers Module
- SI availability checked before assignment
- SI capacity (MaxJobs) enforced
- SI working hours validated

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/scheduler/OVERVIEW.md` - Scheduler module overview
- `docs/02_modules/orders/WORKFLOW.md` - Orders workflow
- `docs/02_modules/kpi/KPI_SYSTEM_FLOW.md` - KPI system flow

