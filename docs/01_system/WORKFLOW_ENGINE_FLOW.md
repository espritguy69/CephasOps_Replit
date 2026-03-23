# Workflow Engine Flow Diagram

**Date:** December 12, 2025  
**Purpose:** Visual representation of workflow engine architecture, status transitions, validation flow, and side effects

---

## Workflow Engine Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      WORKFLOW ENGINE ARCHITECTURE                       │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │  WORKFLOW DEFINITIONS  │      │  STATUS TRANSITIONS   │
        │  (Configuration)       │      │  (Runtime)            │
        ├───────────────────────┤      ├───────────────────────┤
        │ • EntityType           │      │ • From Status         │
        │ • PartnerId (opt)      │      │ • To Status           │
        │ • DepartmentId (opt)   │      │ • Guard Conditions    │
        │ • IsActive             │      │ • Side Effects        │
        │ • Transitions[]        │      │ • Validation Rules    │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │    TRANSITION EXECUTION        │
                    │  (Guard → Validate → Execute) │
                    └───────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   AUDIT TRAIL          │      │   DOMAIN EVENTS       │
        │  (Status History)      │      │  (Side Effects)       │
        └───────────────────────┘      └───────────────────────┘
```

---

## Status Transition Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    MASTER STATUS TRANSITION DIAGRAM                      │
└─────────────────────────────────────────────────────────────────────────┘

                    [Pending]
                         |
                         | Admin assigns SI
                         v
                    [Assigned]
                         |
        ┌────────────────┴────────────────┐
        |                                  |
        | SI starts journey                | Admin reschedules
        v                                  v
   [OnTheWay]                    [ReschedulePendingApproval]
        |                                  |
        | SI arrives                       | TIME approval received
        v                                  v
   [MetCustomer]                      [Assigned] (new appointment)
        |
        |
        ├──────────────────────────────────┐
        |                                  |
        | SI completes                     | Blocker occurs
        v                                  v
   [OrderCompleted]                  [Blocker]
        |                                  |
        |                                  ├──> [ReschedulePendingApproval]
        |                                  |
        |                                  └──> [Assigned] (retry)
        |
        | Admin receives docket
        v
   [DocketsReceived]
        |
        ├──────────────────────────────────┐
        |                                  |
        | Valid docket                     | Invalid docket
        v                                  v
   [DocketsUploaded]                 [DocketsRejected]
        |                                  |
        |                                  └──> [DocketsReceived] (resubmit)
        |
        | System validates
        v
   [ReadyForInvoice]
        |
        | Admin uploads invoice
        v
   [Invoiced]
        |
        ├──────────────────────────────────┐
        |                                  |
        | Payment received                 | Invoice rejected
        v                                  v
   [Completed]                      [InvoiceRejected]
                                        |
                                        ├──> [ReadyForInvoice] (regenerate)
                                        |
                                        └──> [Reinvoice] ──> [Invoiced]

TERMINAL STATES:
   [Cancelled] - Can occur from any pre-invoice status
   [Completed] - Final state after payment
```

---

## Workflow Activation Resolution

**Runtime behaviour:** Orders do **not** store a workflow definition id; workflow is resolved at **each transition**. For Order entities, the engine uses the same resolution context (PartnerId, DepartmentId, OrderTypeCode) for execution and for allowed-transitions. Context is from the request DTO or resolved from the order (and OrderType: parent code when subtype). Resolution order: (1) Partner-specific, (2) Department-specific, (3) OrderType-specific, (4) General. See `docs/WORKFLOW_RESOLUTION_RULES.md`.

```
[Order/Entity Created]
         |
         v
[System needs Workflow Definition]
         |
         v
┌────────────────────────────────────────┐
│ WORKFLOW RESOLUTION (Priority)          │
└────────────────────────────────────────┘
         |
         v
[Step 1: Partner-Specific Workflow]
    EntityType = "Order"
    PartnerId = TIME
    IsActive = true
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND]  [NOT FOUND]
   |         |
   |         v
   |    [Step 2: General Workflow]
   |        EntityType = "Order"
   |        PartnerId = null
   |        IsActive = true
   |         |
   |    ┌────┴────┐
   |    |         |
   |    v         v
   | [FOUND]  [NOT FOUND]
   |    |         |
   |    |         v
   |    |    [Step 3: Department-Specific]
   |    |        EntityType = "Order"
   |    |        DepartmentId = GPON
   |    |        IsActive = true
   |    |         |
   |    |    ┌────┴────┐
   |    |    |         |
   |    |    v         v
   |    | [FOUND]  [NOT FOUND]
   |    |    |         |
   |    |    |         v
   |    |    |    [Step 4: Company-Wide]
   |    |    |        EntityType = "Order"
   |    |    |        DepartmentId = null
   |    |    |        PartnerId = null
   |    |    |        IsActive = true
   |    |    |         |
   |    |    |    ┌────┴────┐
   |    |    |    |         |
   |    |    |    v         v
   |    |    | [FOUND]  [NO WORKFLOW]
   |    |    |    |         |
   |    |    |    |         v
   |    |    |    |    [TRANSITION BLOCKED]
   |    |    |    |         |
   |    └────┴────┴─────────┘
   |         |
   └─────────┘
         |
         v
[Workflow Definition Selected]
         |
         v
[Load Transitions for this workflow]
         |
         v
[Ready for Status Change]
```

---

## Status Transition Execution Flow

```
[User/System requests status change]
    Example: Order status: Assigned → OnTheWay
         |
         v
┌────────────────────────────────────────┐
│ STEP 1: WORKFLOW RESOLUTION            │
│ GetEffectiveWorkflowDefinitionAsync()  │
└────────────────────────────────────────┘
         |
         v
[Workflow Definition Found]
         |
         v
┌────────────────────────────────────────┐
│ STEP 2: TRANSITION VALIDATION          │
│ Check if transition is allowed         │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[ALLOWED] [NOT ALLOWED]
   |         |
   |         v
   |    [BLOCK TRANSITION]
   |    [Return Error]
   |
   v
┌────────────────────────────────────────┐
│ STEP 3: GUARD CONDITIONS               │
│ Evaluate all guard conditions          │
│ for this transition                    │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[ALL PASS] [ANY FAIL]
   |         |
   |         v
   |    [BLOCK TRANSITION]
   |    [Return Guard Failure]
   |
   v
┌────────────────────────────────────────┐
│ STEP 4: VALIDATION RULES               │
│ Run validation rules for target status │
│ (see Validation Engine below)           │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[VALID]  [INVALID]
   |         |
   |         v
   |    [BLOCK TRANSITION]
   |    [Return Validation Error]
   |
   v
┌────────────────────────────────────────┐
│ STEP 5: PERMISSION CHECK                │
│ Verify user role can perform transition │
│ - SI: OnTheWay, MetCustomer, Completed  │
│ - Admin: All transitions                │
│ - HOD/SuperAdmin: Override capability   │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[ALLOWED] [DENIED]
   |         |
   |         v
   |    [BLOCK TRANSITION]
   |    [Return Permission Error]
   |
   v
┌────────────────────────────────────────┐
│ STEP 6: EXECUTE TRANSITION             │
│ Update entity status                    │
│ Update timestamps                       │
│ Update department ownership             │
└────────────────────────────────────────┘
         |
         v
┌────────────────────────────────────────┐
│ STEP 7: SIDE EFFECTS                   │
│ Execute all side effects for transition│
│ - Send notifications                   │
│ - Update materials                     │
│ - Update splitter usage                │
│ - Trigger KPI evaluation               │
│ - Generate domain events               │
└────────────────────────────────────────┘
         |
         v
┌────────────────────────────────────────┐
│ STEP 8: AUDIT TRAIL                    │
│ Log status change in StatusHistory     │
│ - performedBy                          │
│ - timestamp                            │
│ - fromStatus                           │
│ - toStatus                             │
│ - remark                               │
│ - evidence[]                           │
└────────────────────────────────────────┘
         |
         v
[TRANSITION COMPLETE]
[Return Success]
```

---

## Validation Engine Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    VALIDATION RULES BY STATUS                            │
└─────────────────────────────────────────────────────────────────────────┘

BEFORE "Assigned"
─────────────────
Required Checks:
  ✓ serviceId or partnerOrderId present
  ✓ appointment.date and appointment.time set
  ✓ At least one SI assigned
  ✓ Building selected
  ✓ Materials list generated (or confirmed empty)
         |
    ┌────┴────┐
    |         |
    v         v
[ALL PASS] [ANY FAIL]
   |         |
   |         v
   |    [BLOCK TRANSITION]
   |
   v
[ALLOW TRANSITION]

BEFORE "OnTheWay"
─────────────────
Required Checks:
  ✓ Current status = Assigned
  ✓ Triggered by SI App OR Admin (TIME X Portal mirror)
  ✓ If SI → GPS & timestamp recorded
  ✓ If Admin → remark required
         |
    ┌────┴────┐
    |         |
    v         v
[ALL PASS] [ANY FAIL]
   |         |
   |         v
   |    [BLOCK TRANSITION]
   |
   v
[ALLOW TRANSITION]

BEFORE "MetCustomer"
────────────────────
Required Checks:
  ✓ Current status = OnTheWay OR Assigned (TIME X Portal)
  ✓ If SI → GPS & timestamp recorded
  ✓ If Admin → remark required
         |
    ┌────┴────┐
    |         |
    v         v
[ALL PASS] [ANY FAIL]
   |         |
   |         v
   |    [BLOCK TRANSITION]
   |
   v
[ALLOW TRANSITION]

BEFORE "OrderCompleted"
───────────────────────
Required Checks:
  ✓ Current status = MetCustomer
  ✓ Completion details valid
  ✓ Photos uploaded (if required)
  ✓ Splitter usage valid (or HOD override)
         |
    ┌────┴────┐
    |         |
    v         v
[ALL PASS] [ANY FAIL]
   |         |
   |         v
   |    [BLOCK TRANSITION]
   |    [HOD can override with evidence]
   |
   v
[ALLOW TRANSITION]
[Trigger: KPI Evaluation (Job Duration)]

BEFORE "DocketsReceived"
─────────────────────────
Required Checks:
  ✓ Current status = OrderCompleted
  ✓ Docket number present
  ✓ Docket source recorded (physical/WhatsApp/email/SI app)
         |
    ┌────┴────┐
    |         |
    v         v
[ALL PASS] [ANY FAIL]
   |         |
   |         v
   |    [BLOCK TRANSITION]
   |
   v
[ALLOW TRANSITION]
[Trigger: KPI Evaluation (Docket KPI)]

BEFORE "ReadyForInvoice"
─────────────────────────
Required Checks:
  ✓ Current status = DocketsUploaded
  ✓ Docket uploaded to TIME portal
  ✓ Docket number present
  ✓ Splitter usage valid (or HOD override)
  ✓ Completion details valid
  ✓ Photos complete
  ✓ Billing scenario known
  ✓ RMA approval (if Assurance with serialised replacement)
         |
    ┌────┴────┐
    |         |
    v         v
[ALL PASS] [ANY FAIL]
   |         |
   |         v
   |    [BLOCK TRANSITION]
   |
   v
[ALLOW TRANSITION]

BEFORE "Invoiced"
─────────────────
Required Checks:
  ✓ Current status = ReadyForInvoice
  ✓ Invoice uploaded to TIME portal
  ✓ invoice.submissionId present
  ✓ invoice.portalUploadDate recorded
         |
    ┌────┴────┐
    |         |
    v         v
[ALL PASS] [ANY FAIL]
   |         |
   |         v
   |    [BLOCK TRANSITION]
   |
   v
[ALLOW TRANSITION]
[Set invoice.dueDate = portalUploadDate + 45 days]
```

---

## Guard Conditions Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    GUARD CONDITIONS EVALUATION                          │
└─────────────────────────────────────────────────────────────────────────┘

[Transition Requested]
         |
         v
[Load Guard Conditions for Transition]
    From: Assigned
    To: OnTheWay
         |
         v
┌────────────────────────────────────────┐
│ GUARD CONDITION 1: TimeWindowCheck      │
│ Rule: OnTheWay only allowed within      │
│       ±2 hours of appointment time     │
│                                         │
│ Evaluation:                             │
│   currentTime = Now()                   │
│   appointmentTime = Order.Appointment   │
│   timeDiff = |currentTime - appointmentTime|│
│                                         │
│   if timeDiff <= 2 hours:               │
│     → PASS                              │
│   else:                                 │
│     → FAIL                              │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[PASS]    [FAIL]
   |         |
   |         v
   |    [BLOCK TRANSITION]
   |    [Error: "OnTheWay only allowed within 2 hours of appointment"]
   |
   v
┌────────────────────────────────────────┐
│ GUARD CONDITION 2: SIAssignedCheck      │
│ Rule: SI must be assigned               │
│                                         │
│ Evaluation:                             │
│   if Order.ServiceInstallerId != null:  │
│     → PASS                              │
│   else:                                 │
│     → FAIL                              │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[PASS]    [FAIL]
   |         |
   |         v
   |    [BLOCK TRANSITION]
   |    [Error: "Service Installer must be assigned"]
   |
   v
┌────────────────────────────────────────┐
│ GUARD CONDITION 3: MaterialsReadyCheck  │
│ Rule: Materials must be ready           │
│                                         │
│ Evaluation:                             │
│   if Order.Materials.Count > 0 OR       │
│      Order.OrderType == "Assurance":    │
│     → PASS                              │
│   else:                                 │
│     → FAIL                              │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[PASS]    [FAIL]
   |         |
   |         v
   |    [BLOCK TRANSITION]
   |
   v
[ALL GUARD CONDITIONS PASSED]
         |
         v
[PROCEED TO VALIDATION RULES]
```

---

## Side Effects Execution

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    SIDE EFFECTS BY TRANSITION                            │
└─────────────────────────────────────────────────────────────────────────┘

TRANSITION: Assigned → OnTheWay
────────────────────────────────
Side Effects:
  1. Update Order.Status = "OnTheWay"
  2. Update Order.StatusOnTheWayAt = Now()
  3. Update Order.CurrentDepartmentId (via DepartmentWorkflowRule)
  4. Send notification to Admin: "SI {name} is on the way"
  5. Update SI App: Job status changed
  6. Log StatusHistory entry
         |
         v
[All side effects executed]

TRANSITION: MetCustomer → OrderCompleted
─────────────────────────────────────────
Side Effects:
  1. Update Order.Status = "OrderCompleted"
  2. Update Order.StatusOrderCompletedAt = Now()
  3. Update Order.CurrentDepartmentId (via DepartmentWorkflowRule)
  4. Update Materials: Mark materials as "Used"
  5. Update Splitter: Mark port as "In Use"
  6. Trigger KPI Evaluation (Job Duration)
  7. Send notification to Admin: "Order {id} completed"
  8. Generate Domain Event: OrderCompletedEvent
  9. Log StatusHistory entry
         |
         v
[All side effects executed]

TRANSITION: DocketsUploaded → ReadyForInvoice
──────────────────────────────────────────────
Side Effects:
  1. Update Order.Status = "ReadyForInvoice"
  2. Update Order.StatusReadyForInvoiceAt = Now()
  3. Update Order.CurrentDepartmentId = FINANCE
  4. Validate RMA approval (if Assurance with serialised replacement)
  5. Generate invoice data (if auto-generate enabled)
  6. Send notification to Finance: "Order {id} ready for invoice"
  7. Log StatusHistory entry
         |
         v
[All side effects executed]

TRANSITION: ReadyForInvoice → Invoiced
───────────────────────────────────────
Side Effects:
  1. Update Order.Status = "Invoiced"
  2. Update Order.StatusInvoicedAt = Now()
  3. Update Invoice.DueDate = portalUploadDate + 45 days
  4. Update Ageing Report
  5. Update Revenue Data
  6. Send notification to Finance: "Invoice {id} submitted"
  7. Generate Domain Event: InvoiceSubmittedEvent
  8. Log StatusHistory entry
         |
         v
[All side effects executed]

TRANSITION: Invoiced → Completed
─────────────────────────────────
Side Effects:
  1. Update Order.Status = "Completed"
  2. Update Order.StatusCompletedAt = Now()
  3. Update Invoice.IsPaid = true
  4. Update Payment record
  5. Update Ageing Report
  6. Update Revenue Data
  7. Update Partner Payout Dashboard
  8. Trigger Payroll Calculation (if applicable)
  9. Generate Domain Event: OrderCompletedEvent (final)
  10. Log StatusHistory entry
         |
         v
[All side effects executed]
```

---

## Override Flow (HOD/SuperAdmin)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    HOD/SUPERADMIN OVERRIDE FLOW                          │
└─────────────────────────────────────────────────────────────────────────┘

[Transition Blocked by Validation/Guard]
         |
         v
[User Role Check]
         |
    ┌────┴────┐
    |         |
    v         v
[HOD/      [SI/Admin]
SuperAdmin]   |
   |         |
   |         v
   |    [BLOCK TRANSITION]
   |    [Return Error]
   |
   v
[Override Requested]
         |
         v
┌────────────────────────────────────────┐
│ OVERRIDE VALIDATION                     │
│ Required Fields:                        │
│ - override.enabled = true               │
│ - override.role (HOD/SuperAdmin/Director)│
│ - override.reason (required)            │
│ - override.remark (required)            │
│ - override.evidence[] (at least 1 file) │
│ - override.timestamp                    │
│ - override.performedBy                 │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[VALID]  [INVALID]
   |         |
   |         v
   |    [BLOCK OVERRIDE]
   |    [Return Error]
   |
   v
[Override Approved]
         |
         v
┌────────────────────────────────────────┐
│ EXECUTE TRANSITION WITH OVERRIDE        │
│ - Bypass validation rules               │
│ - Force status change                   │
│ - Mark port as "Used (HOD Override)"    │
│ - Add to Critical Attention Queue        │
│ - Write dedicated audit entry            │
└────────────────────────────────────────┘
         |
         v
[TRANSITION COMPLETE WITH OVERRIDE FLAG]
         |
         v
[Override appears in:]
  - StatusHistory[]
  - AuditTrail[]
  - SplitterAudit
  - Critical Attention Queue
```

---

## Department Ownership Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    DEPARTMENT OWNERSHIP TRANSITION                      │
└─────────────────────────────────────────────────────────────────────────┘

[Status Change Requested]
         |
         v
[Load DepartmentWorkflowRule]
    Match: CompanyId + OrderType + Status
         |
    ┌────┴────┐
    |         |
    v         v
[RULE      [NO RULE]
FOUND]        |
   |         v
   |    [Use Default Department]
   |    (from GlobalSettings)
   |
   v
[DepartmentWorkflowRule.DepartmentId]
         |
         v
┌────────────────────────────────────────┐
│ UPDATE DEPARTMENT OWNERSHIP             │
│ - Order.PreviousDepartmentId =          │
│   Order.CurrentDepartmentId             │
│ - Order.CurrentDepartmentId =           │
│   DepartmentWorkflowRule.DepartmentId    │
└────────────────────────────────────────┘
         |
         v
[Department Ownership Updated]
         |
         v
[KPIs/Queues Updated]
  - "My Department's Orders" view
  - Department-level SLA tracking
  - Department-level load tracking
```

---

## Key Takeaways

1. **Workflow Resolution**: Priority-based system (Partner > General > Department > Company-Wide)
2. **Transition Execution**: 8-step process (Resolution → Validation → Guards → Rules → Permission → Execute → Side Effects → Audit)
3. **Guard Conditions**: Custom rules that must pass before transition (e.g., time windows, prerequisites)
4. **Validation Rules**: Status-specific requirements (e.g., SI assigned, materials ready)
5. **Side Effects**: Automatic actions on status change (notifications, material updates, KPI triggers)
6. **Override Capability**: HOD/SuperAdmin can bypass validations with evidence
7. **Department Ownership**: Automatic department assignment based on workflow rules
8. **Audit Trail**: Every transition logged with full context

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/01_system/WORKFLOW_ENGINE.md` - Full Workflow Engine Specification
- `docs/01_system/ORDER_LIFECYCLE.md` - Order Lifecycle and Status Definitions
- `docs/02_modules/workflow/WORKFLOW_ACTIVATION_RULES.md` - Workflow Activation Details
- `docs/02_modules/workflow/CHECKLIST_GUARD_CONDITION.md` - Guard Conditions Guide

