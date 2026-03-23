
# SCHEDULER_MODULE.md  
CephasOps Scheduling Architecture – Full Version (with Backend Contracts)

---

## 1. Purpose

The Scheduler Module coordinates **when** and **by whom** each order is executed.

It provides:

- A **calendar view** of all jobs (orders) and Service Installers (SIs).
- **Drag-and-drop scheduling** from unassigned orders into SI slots.
- **Reschedule logic** with blocker reasons (Customer / Building / Network / SI / Weather / Other).
- Enforcement of **KPI durations**:
  - Prelaid: 1 hour
  - Non-Prelaid: 2 hours
  - FTTR/FTTC/SDU/RDF/Assurance: 3 hours
  - Docket submission: 30 minutes after completion
- Integration with:
  - Orders status workflow
  - SI App (mobile PWA)
  - Inventory (to ensure materials are ready)
  - Settings & KPIs (for future-proof configuration)

> This file is **documentation-only**. It defines behaviour, data models, and API contracts, but **no C# implementation**.

---

## 2. Responsibilities

### 2.1 What Scheduler DOES

- Shows all orders that require SI assignment or rescheduling.
- Lets Admin/Scheduler:
  - Assign a job to an SI and time slot.
  - Change the time or SI (reschedule).
  - Mark job as blocked (Customer / Building / Network).
- Calculates **expected job duration** based on:
  - Order type
  - Building type
  - KPI rules from Settings → Workflow & KPIs.
- Feeds **KPI calculations**:
  - Actual vs expected duration
  - On-time vs delayed
  - Reschedule frequency per order / SI
- Integrates with SI App for live updates:
  - Assigned
  - OnTheWay
  - MetCustomer
  - OrderCompleted

### 2.2 What Scheduler DOES NOT do (in v1)

- It does **not** create invoices.
- It does **not** change financial fields.
- It does **not** manage materials directly (only checks availability).
- It does **not** override business rules in Settings (it reads from there).

---

## 3. Scheduler & Order Lifecycle

### 3.1 Relevant Statuses

From the full order status list:

1. `Pending`  
2. `Assigned`  
3. `OnTheWay`  
4. `MetCustomer`  
5. `OrderCompleted`  
6. `DocketsReceived`  
7. `DocketsUploaded`  
8. `ReadyForInvoice`  
9. `Invoiced`  
10. `Completed`  

The Scheduler module primarily manages transitions:

- `Pending` → `Assigned`
- `Assigned` → `OnTheWay` (via SI App)
- `OnTheWay` → `MetCustomer` (via SI App)
- `MetCustomer` → `OrderCompleted` (via SI App)
- Any of the above → `Rescheduled` (with reason)
- Any of the above → `Blocked` (with reason)

### 3.2 Blockers

Blocker categories:

1. Customer
2. Building
3. Network
4. SI (e.g. sick, vehicle breakdown)
5. Weather
6. Other (free text)

When a blocker is applied:

- Order status becomes `Blocked`.
- Scheduler must create a **SchedulerLog** entry with:
  - Blocker type
  - Reason text
  - Optional images (via SI App)
  - Optional contact attempts (customer/building manager)
- Admin can later decide to:
  - Reschedule
  - Cancel order (if allowed by partner rules)

---

## 4. UI/UX – Admin Scheduler Calendar

### 4.1 Views

- **Weekly view** (default)
- Daily view
- Monthly overview (for density / load)

### 4.2 Columns & Rows

- **Rows**: Time slots (e.g. 09:00, 10:00, …)
- **Columns**: SIs (in-house + subcon), filtered by:
  - Company
  - Region / branch
  - Skillset (e.g. FTTR-capable SIs)

### 4.3 Order Card Layout

Each scheduled item is rendered with:

- `ServiceId / WO / TicketId`  
- Customer name  
- Time (start–end)
- Building short name
- Partner (TIME, Celcom, etc.)
- Status (color-coded)
- SI name

Color-coded by status:

- Grey: Pending (unassigned, in backlog panel)
- Blue: Assigned
- Yellow: OnTheWay / MetCustomer
- Green: OrderCompleted (waiting for docket)
- Red: Blocked / Overdue

### 4.4 Interactions

- Drag from “Unassigned orders” list → drop onto SI & timeslot → `Assigned`.
- Drag an existing block to a different time/SI → `Rescheduled`.
- Right-click / context menu on block:
  - Mark as Blocked (with reason).
  - View order details.
  - Open SI history (KPI, previous jobs).

---

## 5. SI Availability & Working Patterns

### 5.1 Availability Data

For each SI:

- Default working hours per day (e.g. 09:00–18:00).
- Working days (Mon–Sun flags).
- Public holidays / company holidays (from Settings).
- Leaves:
  - Annual leave
  - Sick leave
  - Off days
- Temporary adjustments (e.g. half-day available).

### 5.2 Business Rules

1. Scheduler cannot assign a job **outside** SI working hours (warning or block).
2. Scheduler is warned when:
   - Overlapping jobs conflict.
   - Expected duration exceeds working hours.
3. SI type:
   - In-house: may have stricter scheduling rules.
   - Subcon: may have looser rules but strict KPI-based pay.

---

## 6. KPI Integration

Scheduler is the **source of truth** for timing data:

- Status change timestamps:
  - AssignedAt
  - OnTheWayAt
  - MetCustomerAt
  - OrderCompletedAt

From these we derive:

- Travel time (AssignedAt → MetCustomerAt)
- Work time (MetCustomerAt → OrderCompletedAt)
- Total job duration
- On-time or delayed relative to KPI rules (Settings → Workflow & KPIs)

These values are later consumed by:

- Payroll Module (to adjust SI pay by KPI tier).
- PNL Module (time-to-complete efficiency).
- Partner KPI reporting (e.g., TIME’s SLA compliance).

---

## 7. Data Model (Documentation-Only)

> Note: this is a conceptual view for architects and Cursor.  
> Actual C# classes / EF models will be implemented later.

### 7.1 SchedulerEntry

Represents one scheduled job for an SI.

Key fields (simplified):

- `Id` – unique identifier
- `OrderId` – link to Order
- `ServiceId` – convenience copy of order’s Service ID (TBBNxxxx, CELCOMxxx, etc.)
- `CompanyId`
- `ServiceInstallerId`
- `StartDateTime`
- `EndDateTime`
- `PlannedDurationMinutes`
- `ActualDurationMinutes` (calculated once completed)
- `Status` (Pending, Assigned, OnTheWay, MetCustomer, OrderCompleted, Blocked, Rescheduled)
- `BlockerType` (if blocked)
- `BlockerReason`
- `CreatedByUserId`
- `UpdatedByUserId`
- `CreatedAt`
- `UpdatedAt`

### 7.2 SchedulerLog

Tracks every scheduler-related event.

- `Id`
- `SchedulerEntryId`
- `OrderId`
- `EventType` (Assigned, Rescheduled, Blocked, StatusChanged, KPIFlagged, etc.)
- `OldValue`
- `NewValue`
- `Notes`
- `PerformedByUserId` (or “system” for automated events)
- `Timestamp`

### 7.3 SIAvailability & SILeave

#### SIAvailability

- `Id`
- `ServiceInstallerId`
- `DayOfWeek`
- `StartTime`
- `EndTime`
- `IsActive`

#### SILeave

- `Id`
- `ServiceInstallerId`
- `StartDateTime`
- `EndDateTime`
- `LeaveType` (Annual, Sick, Emergency, Off)
- `Notes`

---

## 8. Backend API Contracts (Documentation Only)

> The following describes **intended API surface** for the Scheduler module.  
> Cursor/engineers will later generate controllers and DTOs consistent with these specs.  
> No implementation is provided here.

### 8.1 DTOs (Shapes)

#### 8.1.1 SchedulerEntryDto (read model)

Represents scheduler entry for UI consumption.

Fields:

- `id: string`
- `orderId: string`
- `serviceId: string`
- `companyId: string`
- `serviceInstallerId: string`
- `serviceInstallerName: string`
- `partnerName: string`
- `customerName: string`
- `buildingShortName: string`
- `status: string` (enum)
- `startDateTime: string` (ISO 8601)
- `endDateTime: string` (ISO 8601)
- `plannedDurationMinutes: number`
- `actualDurationMinutes?: number`
- `blockerType?: string`
- `blockerReason?: string`
- `kpiResult?: string` (OnTime / Late / Exceeded)
- `createdAt: string`
- `updatedAt: string`

#### 8.1.2 CreateSchedulerEntryRequest

Used when assigning an SI to an order for the first time.

Fields:

- `orderId: string`
- `serviceInstallerId: string`
- `startDateTime: string`
- `endDateTime?: string` (optional; backend can calculate using KPIs)
- `notes?: string`

#### 8.1.3 UpdateSchedulerEntryRequest

Used for rescheduling or changing SI.

Fields:

- `schedulerEntryId: string`
- `serviceInstallerId?: string`
- `startDateTime?: string`
- `endDateTime?: string`
- `rescheduleReason?: string`
- `rescheduleReasonCategory?: string` (Customer / Building / Network / SI / Weather / Other)

#### 8.1.4 BlockSchedulerEntryRequest

Fields:

- `schedulerEntryId: string`
- `blockerType: string` (Customer / Building / Network / SI / Weather / Other)
- `blockerReason: string`
- `attachments?: AttachmentRef[]` (from file storage module)

#### 8.1.5 GetCalendarRequest

Query parameters:

- `companyId: string`
- `from: string` (ISO date)
- `to: string` (ISO date)
- `serviceInstallerIds?: string[]`
- `statusFilters?: string[]` (Optional filter by statuses)

Response:

- List of `SchedulerEntryDto`  
- Optionally, aggregated metrics (total jobs per SI).

#### 8.1.6 SIAvailabilityDto

Fields:

- `serviceInstallerId: string`
- `weeklyPattern: WeeklyAvailabilitySlot[]`
- `leaves: SILeaveDto[]`

---

### 8.2 Proposed HTTP Endpoints

**Base path:** `/api/scheduler`

1. **Get Calendar**
   - `GET /api/scheduler/calendar`
   - Query: `companyId`, `from`, `to`, optional `serviceInstallerIds`, `statusFilters`
   - Response: array of `SchedulerEntryDto`.

2. **Create Scheduler Entry (Assign Order)**
   - `POST /api/scheduler/entries`
   - Body: `CreateSchedulerEntryRequest`
   - Response: `SchedulerEntryDto`

3. **Update Scheduler Entry (Reschedule / Change SI)**
   - `PUT /api/scheduler/entries/{id}`
   - Body: `UpdateSchedulerEntryRequest`
   - Response: `SchedulerEntryDto`

4. **Block Scheduler Entry**
   - `POST /api/scheduler/entries/{id}/block`
   - Body: `BlockSchedulerEntryRequest`
   - Response: `SchedulerEntryDto`

5. **Get SI Availability**
   - `GET /api/scheduler/availability`
   - Query: `companyId`, optional `serviceInstallerIds`
   - Response: array of `SIAvailabilityDto`

6. **Set SI Weekly Availability**
   - `PUT /api/scheduler/availability/{serviceInstallerId}`
   - Body: Weekly availability model (defined in SETTINGS or SI module docs).

7. **Add SI Leave**
   - `POST /api/scheduler/leave`
   - Body: `SILeaveDto`
   - Response: `SILeaveDto`

8. **Get Scheduler Logs for Order**
   - `GET /api/scheduler/orders/{orderId}/logs`
   - Response: list of `SchedulerLogDto`

---

## 9. Integration Points

### 9.1 With Orders Module

- Reads:
  - Order basic info (customer, building, partner, type).
  - KPIs / expected duration per order type + building type.
- Writes:
  - Updates Order status (Assigned, Rescheduled, Blocked).
  - Logs status change timestamps for KPI.

### 9.2 With Service Installer App

- Provides per-SI job list:
  - Today’s jobs
  - Upcoming jobs
- Receives:
  - Status updates:
    - OnTheWay
    - MetCustomer
    - OrderCompleted
  - Evidence:
    - GPS location
    - Time captured from device
    - Photos / notes

### 9.3 With Settings Module

- Reads:
  - KPI rules per order type and building type
  - Working hours defaults per SI role
  - Public holidays
- Provides:
  - Audit data for rule changes and their impact.

### 9.4 With Notifications & Alerts

- Triggers when:
  - New job assigned to SI
  - Job rescheduled
  - KPI threshold breached
- Notification channels (configurable):
  - Push
  - Email
  - WhatsApp / SMS (integrated via future notification gateway)

---

## 10. Multi-Company Behaviour

- Always filter data by `companyId`.
- Each company has its **own set** of:
  - SIs
  - Orders
  - Schedules
  - KPIs & rules (can differ between companies)
- Director-level roles may access aggregated KPI across multiple companies.

---

## 11. Error Handling & Validation Rules

Key validation examples:

1. Cannot assign:
   - Without `orderId`, `serviceInstallerId`, `startDateTime`.
2. Cannot assign:
   - If SI is on leave or outside availability window (hard block or warning as per Settings).
3. Cannot reschedule:
   - Without `rescheduleReason` when reason category is provided.
4. KPI-critical warnings:
   - If scheduled job start + expected duration crosses end-of-day or KPI limit, UI should warn.

---

## 12. Notes for Cursor / Future Dev

- Implementation should use:
  - Clean separation: Controller → Application Service → Domain.
  - One SchedulerService responsible for business rules.
- **Do not** hardcode KPI values in code:
  - Always read from Settings → Workflow & KPIs.
- **Do not** hardcode SI lists:
  - SIs are managed via ServiceInstaller module and can be in-house or subcon.
