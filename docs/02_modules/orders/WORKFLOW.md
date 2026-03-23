# Orders – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Orders module, covering order lifecycle from creation to completion  

**Status authority:** Order statuses and transitions are defined in [WORKFLOW_STATUS_REFERENCE.md](../../05_data_model/WORKFLOW_STATUS_REFERENCE.md) and seeded in [07_gpon_order_workflow.sql](../../backend/scripts/postgresql-seeds/07_gpon_order_workflow.sql). **InProgress** is not an order status; field flow is Assigned → OnTheWay → MetCustomer → OrderCompleted.

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         ORDERS MODULE SYSTEM                              │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   ORDER CREATION       │      │   ORDER LIFECYCLE     │
        │  (Parser/Manual/API)   │      │  (Status Transitions) │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Email Parser         │      │ • Pending             │
        │ • Manual Entry         │      │ • Assigned            │
        │ • API Import           │      │ • OnTheWay            │
        │ • Material Templates    │      │ • MetCustomer         │
        │ • Building Defaults     │      │ • OrderCompleted      │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   WORKFLOW ENGINE      │      │   BILLING INTEGRATION  │
        │  (Transitions/Guards)  │      │  (Invoice Generation)  │
        └───────────────────────┘      └───────────────────────┘
```

---

## Complete Workflow: Order Lifecycle

```
[STEP 1: ORDER CREATION]
         |
         v
┌────────────────────────────────────────┐
│ ORDER CREATION SOURCES                   │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[EMAIL PARSER] [MANUAL/API]
   |              |
   |              v
   |         ┌────────────────────────────────────────┐
   |         │ MANUAL ORDER CREATION                    │
   |         │ POST /api/orders                        │
   |         └────────────────────────────────────────┘
   |              |
   |              v
   |         CreateOrderDto {
   |           ServiceId: "TBBN1234567"
   |           PartnerId: TIME
   |           OrderTypeId: ACTIVATION
   |           CustomerName: "John Doe"
   |           CustomerPhone: "0123456789"
   |           AddressLine1: "123 Main St"
   |           BuildingId: "building-123"
   |           AppointmentDate: 2025-12-15
   |         }
   |
   v
┌────────────────────────────────────────┐
│ EMAIL PARSER → ORDER CREATION            │
│ (See Email Parser Workflow)              │
└────────────────────────────────────────┘
         |
         v
[Order Created]
  Order {
    Id: "order-456"
    Status: "Pending"
    CompanyId: Cephas
    DepartmentId: GPON
    ServiceId: "TBBN1234567"
    PartnerId: TIME
    OrderTypeId: ACTIVATION
    BuildingId: "building-123"
  }
         |
         v
┌────────────────────────────────────────┐
│ APPLY MATERIAL TEMPLATES                 │
│ MaterialTemplateService.GetEffectiveTemplate()│
└────────────────────────────────────────┘
         |
         v
[Materials Auto-Populated]
  OrderMaterial {
    MaterialId: "ONU-HG8240H"
    PlannedQuantity: 1
    Source: "MaterialTemplate"
  }
         |
         v
┌────────────────────────────────────────┐
│ APPLY BUILDING DEFAULT MATERIALS         │
│ BuildingDefaultMaterialService.GetMaterialsForOrder()│
└────────────────────────────────────────┘
         |
         v
[Building Defaults Applied]
         |
         v
[STEP 2: ORDER ASSIGNMENT]
         |
         v
┌────────────────────────────────────────┐
│ ASSIGN SERVICE INSTALLER                  │
│ PUT /api/orders/{id}/assign              │
└────────────────────────────────────────┘
         |
         v
Assignment Request {
  AssignedSiId: "SI-123"
  AppointmentDate: 2025-12-15
  AppointmentWindowFrom: "09:00"
  AppointmentWindowTo: "12:00"
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE ASSIGNMENT                      │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |         |
   |         v
   |    [Reject Assignment]
   |    [Show Errors]
   |
   v
Checks:
  ✓ Service ID present
  ✓ Customer details complete
  ✓ Address valid
  ✓ Building selected
  ✓ SI available
  ✓ Appointment date/time valid
         |
         v
┌────────────────────────────────────────┐
│ CREATE SCHEDULED SLOT                    │
│ SchedulerService.CreateSlot()            │
└────────────────────────────────────────┘
         |
         v
ScheduledSlot {
  OrderId: "order-456"
  ServiceInstallerId: "SI-123"
  Date: 2025-12-15
  WindowFrom: "09:00"
  WindowTo: "12:00"
}
         |
         v
┌────────────────────────────────────────┐
│ TRANSITION TO ASSIGNED                    │
│ WorkflowEngineService.ExecuteTransition()│
└────────────────────────────────────────┘
         |
         v
[Status: Pending → Assigned]
         |
         v
[Order Status Log Created]
  OrderStatusLog {
    OrderId: "order-456"
    FromStatus: "Pending"
    ToStatus: "Assigned"
    ChangedByUserId: "admin-123"
    ChangedAt: 2025-12-12
  }
         |
         v
[Notification Sent to SI]
         |
         v
[STEP 3: FIELD OPERATIONS]
         |
         v
[SI App: On The Way]
         |
         v
┌────────────────────────────────────────┐
│ TRANSITION TO ONTHEWAY                    │
│ PUT /api/orders/{id}/status              │
└────────────────────────────────────────┘
         |
         v
Status Update {
  TargetStatus: "OnTheWay"
  GPS: { lat: 3.1390, lng: 101.6869 }
  Photo: [file]
  Timestamp: 2025-12-15 08:30
}
         |
         v
[Workflow Engine Validates]
  ✓ Current status is "Assigned"
  ✓ GPS captured
  ✓ Photo uploaded
  ✓ Timestamp valid
         |
         v
[Status: Assigned → OnTheWay]
         |
         v
[SI App: Met Customer]
         |
         v
┌────────────────────────────────────────┐
│ TRANSITION TO METCUSTOMER                 │
└────────────────────────────────────────┘
         |
         v
Status Update {
  TargetStatus: "MetCustomer"
  GPS: { lat: 3.1390, lng: 101.6869 }
  Photo: [file]
  Timestamp: 2025-12-15 09:15
}
         |
         v
[Status: OnTheWay → MetCustomer]
         |
         v
[For Assurance Orders: RMA Fields Unlocked]
  OrderMaterialReplacement {
    OldSerialNumber: "SN001"
    NewSerialNumber: "SN002"
    MaterialId: "ONU-HG8240H"
  }
         |
         v
[SI App: Order Completed]
         |
         v
┌────────────────────────────────────────┐
│ TRANSITION TO ORDERCOMPLETED              │
└────────────────────────────────────────┘
         |
         v
Completion Data {
  SplitterId: "SPL-001"
  PortNumber: 8
  OnuSerialNumber: "SN002"
  Photos: [completion_photo_1, completion_photo_2]
  Signature: [signature_file]
  MaterialsUsed: [
    { MaterialId: "ONU-HG8240H", SerialNumber: "SN002", Quantity: 1 },
    { MaterialId: "PATCHCORD-6M", Quantity: 2 }
  ]
}
         |
         v
[Workflow Engine Validates]
  ✓ Splitter ID present
  ✓ Port number valid
  ✓ ONU serial scanned
  ✓ Completion photos uploaded
  ✓ Signature captured
  ✓ Materials used recorded
         |
         v
[Status: MetCustomer → OrderCompleted]
         |
         v
┌────────────────────────────────────────┐
│ UPDATE INVENTORY                          │
│ InventoryService.RecordInstallation()    │
└────────────────────────────────────────┘
         |
         v
[For Serialized Items]
  SerializedItem {
    SerialNumber: "SN002"
    Status: "InstalledAtCustomer"
    LastOrderId: "order-456"
  }
         |
         v
[For Non-Serialized Items]
  StockBalance {
    MaterialId: "PATCHCORD-6M"
    StockLocationId: SI_InHand
    Quantity: 18 → 16 (decreased)
  }
         |
         v
[STEP 4: DOCKET PROCESSING]
         |
         v
[Admin: Docket Received]
         |
         v
┌────────────────────────────────────────┐
│ TRANSITION TO DOCKETSRECEIVED             │
│ PUT /api/orders/{id}/status              │
└────────────────────────────────────────┘
         |
         v
Docket Data {
  DocketNumber: "DOC-2025-00123"
  ReceivedDate: 2025-12-16
  ReceivedBy: "admin-123"
}
         |
         v
[Status: OrderCompleted → DocketsReceived]
         |
         v
[Admin: Verify Docket]
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |         |
   |         v
   |    ┌────────────────────────────────────────┐
   |    │ TRANSITION TO DOCKETSREJECTED             │
   |    └────────────────────────────────────────┘
   |         |
   |         v
   |    [Status: DocketsReceived → DocketsRejected]
   |         |
   |         v
   |    [SI Must Correct]
   |         |
   |         v
   |    [Can Re-enter: DocketsRejected → DocketsReceived]
   |
   v
[Admin: Docket Verified]
         |
         v
┌────────────────────────────────────────┐
│ TRANSITION TO DOCKETSVERIFIED            │
└────────────────────────────────────────┘
         |
         v
[Status: DocketsReceived → DocketsVerified]
         |
         v
[Admin: Upload to TIME Portal]
         |
         v
┌────────────────────────────────────────┐
│ TRANSITION TO DOCKETSUPLOADED             │
└────────────────────────────────────────┘
         |
         v
Docket Upload {
  PortalSubmissionId: "PORTAL-12345"
  UploadedAt: 2025-12-17
  UploadedBy: "admin-123"
}
         |
         v
[Status: DocketsVerified → DocketsUploaded]
         |
         v
[STEP 5: INVOICING]
         |
         v
[Admin: Prepare Invoice]
         |
         v
┌────────────────────────────────────────┐
│ TRANSITION TO READYFORINVOICE             │
└────────────────────────────────────────┘
         |
         v
[Workflow Engine Validates]
  ✓ Docket uploaded
  ✓ Splitter details complete
  ✓ ONU serial recorded
  ✓ Photos uploaded
  ✓ For Assurance: RMA approvals complete (if applicable)
         |
         v
[Status: DocketsUploaded → ReadyForInvoice]
         |
         v
┌────────────────────────────────────────┐
│ GENERATE INVOICE                          │
│ BillingService.CreateInvoice()           │
└────────────────────────────────────────┘
         |
         v
Invoice {
  OrderId: "order-456"
  InvoiceNumber: "INV-2025-00123"
  PartnerId: TIME
  Amount: RM 150.00
  LineItems: [
    { Description: "Activation", Amount: RM 100.00 },
    { Description: "ONU Device", Amount: RM 50.00 }
  ]
}
         |
         v
┌────────────────────────────────────────┐
│ TRANSITION TO INVOICED                    │
└────────────────────────────────────────┘
         |
         v
Invoice Submission {
  SubmissionId: "SUBMIT-12345"
  SubmittedAt: 2025-12-18
  SubmittedBy: "admin-123"
}
         |
         v
[Status: ReadyForInvoice → Invoiced]
         |
         v
[Due Date Set: 45 days from submission]
         |
         v
[Admin: Submit to TIME Portal]
         |
         v
┌────────────────────────────────────────┐
│ TRANSITION TO SUBMITTEDTOPORTAL           │
└────────────────────────────────────────┘
         |
         v
[Status: Invoiced → SubmittedToPortal]
         |
         v
[STEP 6: PAYMENT & COMPLETION]
         |
         v
[Finance: Payment Received]
         |
         v
┌────────────────────────────────────────┐
│ RECORD PAYMENT                            │
│ PaymentService.CreatePayment()           │
└────────────────────────────────────────┘
         |
         v
Payment {
  InvoiceId: "invoice-123"
  Amount: RM 150.00
  PaymentDate: 2025-12-20
  PaymentMethod: "Bank Transfer"
  Reference: "TXN-12345"
}
         |
         v
┌────────────────────────────────────────┐
│ TRANSITION TO COMPLETED                   │
└────────────────────────────────────────┘
         |
         v
[Status: SubmittedToPortal → Completed]
         |
         v
[Order Lifecycle Complete]
```

---

## Blocker Workflow

```
[Order Status: Assigned or OnTheWay or MetCustomer]
         |
         v
┌────────────────────────────────────────┐
│ SET BLOCKER                               │
│ PUT /api/orders/{id}/blocker             │
└────────────────────────────────────────┘
         |
         v
Blocker Data {
  Category: "PreCustomer" | "PostCustomer"
  Reason: "Building denies access"
  Remark: "Security guard refused entry"
  Evidence: [photo_1, photo_2]
  GPS: { lat: 3.1390, lng: 101.6869 }
  ReportedBy: "SI-123"
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE BLOCKER                          │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |         |
   |         v
   |    [Reject Blocker]
   |    [Show Errors]
   |
   v
Checks:
  ✓ Category matches current status
  ✓ Reason from allowed list
  ✓ Remark provided
  ✓ Evidence uploaded (SI requires ≥ 1 photo)
  ✓ GPS captured (if SI)
         |
         v
[Status: Current → Blocker]
         |
         v
OrderBlocker {
  OrderId: "order-456"
  Category: "PreCustomer"
  Reason: "Building denies access"
  Remark: "Security guard refused entry"
  EvidenceFiles: [file_id_1, file_id_2]
  ReportedBy: "SI-123"
  ReportedAt: 2025-12-15 10:00
}
         |
         v
[Blocker Resolution Options]
         |
    ┌────┴────┐
    |         |
    v         v
[RESCHEDULE] [CANCEL]
   |            |
   |            v
   |       [Status: Blocker → Cancelled]
   |
   v
[Status: Blocker → ReschedulePendingApproval]
         |
         v
[Wait for TIME Approval Email]
         |
         v
[Email Parser Detects Approval]
         |
         v
[Status: ReschedulePendingApproval → Assigned]
```

---

## Reschedule Workflow

```
[Order Status: Assigned or Blocker]
         |
         v
┌────────────────────────────────────────┐
│ REQUEST RESCHEDULE                        │
│ PUT /api/orders/{id}/reschedule          │
└────────────────────────────────────────┘
         |
         v
Reschedule Request {
  ProposedDate: 2025-12-20
  ProposedTime: "14:00-17:00"
  Reason: "Customer requested"
  Remark: "Customer unavailable"
  CustomerEvidence: [whatsapp_screenshot]
}
         |
         v
[Check Reschedule Type]
         |
    ┌────┴────┐
    |         |
    v         v
[SAME-DAY] [NORMAL]
   |            |
   |            v
   |       [Status: Current → ReschedulePendingApproval]
   |            |
   |            v
   |       [Lock Order]
   |            |
   |            v
   |       [Generate TIME Approval Email]
   |            |
   |            v
   |       [Wait for TIME Email Approval]
   |            |
   |            v
   |       [Email Parser Detects Approval]
   |            |
   |            v
   |       [Status: ReschedulePendingApproval → Assigned]
   |
   v
[Status: Assigned → Assigned]
  (Same-day reschedule, no approval needed)
         |
         v
[Update Appointment]
  Order {
    AppointmentDate: 2025-12-15 (same day)
    AppointmentWindowFrom: "14:00"
    AppointmentWindowTo: "17:00"
    RescheduleCount: 1
  }
         |
         v
[Update Scheduled Slot]
  ScheduledSlot {
    Date: 2025-12-15
    WindowFrom: "14:00"
    WindowTo: "17:00"
  }
```

---

## Entities Involved

### Order Entity
```
Order
├── Id (Guid)
├── CompanyId (Guid)
├── DepartmentId (Guid?)
├── Status (see WORKFLOW_STATUS_REFERENCE.md: Pending, Assigned, OnTheWay, MetCustomer, OrderCompleted, DocketsReceived, …, Rejected, Reinvoice, Cancelled; no InProgress)
├── ServiceId (string: TBBN or Partner Service ID)
├── TicketId (string?)
├── PartnerId (Guid)
├── OrderTypeId (Guid)
├── CustomerName (string)
├── CustomerPhone (string)
├── CustomerEmail (string?)
├── AddressLine1 (string)
├── BuildingId (Guid?)
├── AssignedSiId (Guid?)
├── AppointmentDate (DateTime?)
├── AppointmentWindowFrom (TimeSpan?)
├── AppointmentWindowTo (TimeSpan?)
├── SplitterId (Guid?)
├── PortNumber (int?)
├── OnuSerialNumber (string?)
├── OnuPasswordEncrypted (string?)
├── DocketNumber (string?)
├── InvoiceId (Guid?)
├── PayrollPeriodId (Guid?)
└── CreatedAt, UpdatedAt
```

### OrderStatusLog Entity
```
OrderStatusLog
├── Id (Guid)
├── OrderId (Guid)
├── FromStatus (string)
├── ToStatus (string)
├── ChangedByUserId (Guid)
├── ChangedAt (DateTime)
├── Remarks (string?)
└── MetadataJson (string?)
```

### OrderBlocker Entity
```
OrderBlocker
├── Id (Guid)
├── OrderId (Guid)
├── Category (enum: PreCustomer, PostCustomer)
├── Reason (string)
├── Remark (string)
├── EvidenceFiles (List<Guid>)
├── ReportedBy (Guid?)
├── ReportedAt (DateTime)
└── ResolvedAt (DateTime?)
```

### OrderReschedule Entity
```
OrderReschedule
├── Id (Guid)
├── OrderId (Guid)
├── OriginalDate (DateTime)
├── ProposedDate (DateTime)
├── Reason (string)
├── Remark (string)
├── CustomerEvidence (List<Guid>)
├── TimeApprovalEmailId (Guid?)
├── RequestedByUserId (Guid)
├── RequestedAt (DateTime)
└── ApprovedAt (DateTime?)
```

### OrderMaterialUsage Entity
```
OrderMaterialUsage
├── Id (Guid)
├── OrderId (Guid)
├── MaterialId (Guid)
├── PlannedQuantity (decimal)
├── ActualQuantity (decimal?)
├── SerialNumber (string?)
├── IsPlanned (bool)
├── IsUsed (bool)
└── UsedAt (DateTime?)
```

---

## API Endpoints Involved

### Order Management
- `GET /api/orders` - List orders with filters
- `GET /api/orders/{id}` - Get order details
- `POST /api/orders` - Create new order
- `PUT /api/orders/{id}` - Update order
- `DELETE /api/orders/{id}` - Delete order (soft delete)

### Order Status Transitions
- `PUT /api/orders/{id}/status` - Transition order status
- `GET /api/orders/{id}/status-history` - Get status change history
- `GET /api/orders/{id}/allowed-transitions` - Get valid next statuses

### Order Assignment
- `PUT /api/orders/{id}/assign` - Assign SI and set appointment
- `PUT /api/orders/{id}/reassign` - Reassign to different SI

### Blocker Management
- `PUT /api/orders/{id}/blocker` - Set blocker
- `DELETE /api/orders/{id}/blocker` - Resolve blocker

### Reschedule Management
- `PUT /api/orders/{id}/reschedule` - Request reschedule
- `GET /api/orders/{id}/reschedules` - Get reschedule history

### Material Management
- `GET /api/orders/{id}/materials` - Get order materials
- `POST /api/orders/{id}/materials` - Add material to order
- `PUT /api/orders/{id}/materials/{materialId}` - Update material usage
- `DELETE /api/orders/{id}/materials/{materialId}` - Remove material

### Docket Management
- `PUT /api/orders/{id}/docket` - Upload/receive docket
- `GET /api/orders/{id}/docket` - Get docket details

### ONU Password
- `GET /api/orders/{id}/onu-password` - Get decrypted ONU password (authorized only)

---

## Module Rules & Validations

### Order Creation Rules
- Service ID must be unique per partner (or TBBN format)
- Customer phone must be valid format
- Address must include at least AddressLine1
- Building must exist and be active
- Order type must be valid for company/department

### Assignment Rules
- SI must be active and available
- Appointment date cannot be in the past
- Appointment window must be within SI's working hours
- SI must have capacity (max jobs per day)
- Materials must be available in warehouse (optional check)

### Status Transition Rules
- Transitions must follow workflow definition
- Guard conditions must pass
- Required fields must be present for each status
- Role-based permissions enforced
- No skipping statuses (except HOD/SuperAdmin override)

### Blocker Rules
- Pre-Customer blockers only from Assigned or OnTheWay
- Post-Customer blockers only from MetCustomer
- SI must provide evidence (≥ 1 photo)
- GPS must be captured (if SI)
- Reason must be from allowed list

### Reschedule Rules
- Same-day reschedule requires customer evidence
- Normal reschedule requires TIME email approval
- Reschedule count tracked (may have limits)
- Cannot reschedule to past date

### Material Rules
- Serialized materials must have serial numbers
- Actual quantity cannot exceed planned quantity (without override)
- Materials must be marked as used when installed
- RMA replacements require TIME approval (for Assurance orders)

### Docket Rules
- Docket can only be uploaded after OrderCompleted
- Splitter and port must be recorded
- ONU serial must match scanned serial
- Completion photos required

### Invoice Rules
- Invoice can only be generated after DocketsUploaded
- All RMA approvals must be complete (for Assurance orders)
- Rate card must be resolved
- Invoice submission ID required for Invoiced status

---

## Integration Points

### Workflow Engine
- All status transitions go through WorkflowEngineService
- Guard conditions validated before transition
- Side effects executed after transition
- Workflow definitions configurable per department

### Scheduler Module
- Order assignment creates ScheduledSlot
- Appointment changes update ScheduledSlot
- SI availability checked before assignment
- Calendar integration for scheduling

### Inventory Module
- Material templates applied on order creation
- Building default materials applied
- Material usage recorded on completion
- Stock movements created for installations

### Billing Module
- Invoice generated from ReadyForInvoice status
- Invoice linked to order
- Payment recorded against invoice
- Invoice submission tracked

### PNL Module
- Order completion triggers PNL calculation
- Material costs allocated
- Revenue recorded
- Cost center allocation applied

### Payroll Module
- Order completion creates JobEarningRecord
- SI earnings calculated based on order
- KPI adjustments applied
- Payroll period tracking

### Email Parser Module
- Orders created from parsed emails
- Reschedule approvals detected from emails
- MRA emails trigger RMA workflows

### Notifications Module
- Status changes trigger notifications
- SI notified on assignment
- Admin notified on blockers
- Customer notifications (optional)

### KPI Module
- Status transitions trigger KPI evaluation
- SI KPI vs Admin KPI tracked
- KPI profiles determine job duration
- Performance metrics calculated

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/01_system/ORDER_LIFECYCLE.md` - Complete order lifecycle specification
- `docs/01_system/WORKFLOW_ENGINE_FLOW.md` - Workflow engine details
- `docs/02_modules/orders/OVERVIEW.md` - Orders module overview

