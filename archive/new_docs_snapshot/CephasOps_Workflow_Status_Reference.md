# CephasOps – Workflow Status Reference

**Version:** 1.0  
**Date:** December 2025  
**Status:** Single Source of Truth  
**Purpose:** Consolidated reference for all workflow statuses

---

## ⚠️ IMPORTANT: Status Naming Convention

**ALL status values MUST use PascalCase and match exactly (case-sensitive):**
- ✅ Correct: `OnTheWay`, `MetCustomer`, `OrderCompleted`, `DocketsVerified`, `SubmittedToPortal`
- ❌ Wrong: `on_the_way`, `met_customer`, `order_completed`, `docket_verified`, `submitted_to_portal`

**Backend Source of Truth:** `backend/src/CephasOps.Domain/Orders/Enums/OrderStatus.cs`

---

## 1. ORDER WORKFLOW STATUSES (17 Total)

### 1.1 Main Flow Statuses (12 Statuses)

| Order | Code | Display Name | Phase | Triggered By | Description |
|-------|------|--------------|-------|--------------|-------------|
| 1 | `Pending` | Pending | Creation | System | Order created via parser/manual/API. Not yet assigned. |
| 2 | `Assigned` | Assigned | FieldWork | Admin | SI assigned and appointment confirmed. |
| 3 | `OnTheWay` | On The Way | FieldWork | SI | SI is travelling to the site. |
| 4 | `MetCustomer` | Met Customer | FieldWork | SI | SI has met the customer on-site. |
| 5 | `OrderCompleted` | Order Completed | FieldWork | SI | Physical job done. Splitter + materials recorded. |
| 6 | `DocketsReceived` | Dockets Received | Documentation | Admin | Admin received docket from SI. |
| 7 | `DocketsVerified` | Dockets Verified | Documentation | Admin | Docket validated and QA passed. |
| 8 | `DocketsUploaded` | Dockets Uploaded | Documentation | Admin | Docket uploaded to partner portal. |
| 9 | `ReadyForInvoice` | Ready For Invoice | Billing | System | All validations passed. Ready for billing. |
| 10 | `Invoiced` | Invoiced | Billing | Admin | Invoice generated. Due date = upload + 45 days. |
| 11 | `SubmittedToPortal` | Submitted To Portal | Billing | Admin | Invoice uploaded to TIME portal (e-Invoice/MyInvois). |
| 12 | `Completed` | Completed | Closure | System | Payment received. Order fully closed. |

### 1.2 Side States (5 Statuses)

| Code | Display Name | Phase | Triggered By | Description |
|------|--------------|-------|--------------|-------------|
| `Blocker` | Blocker | FieldWork | SI/Admin | Job cannot proceed (Customer/Building/Network issue). |
| `ReschedulePendingApproval` | Reschedule Pending Approval | FieldWork | Admin | Waiting for TIME approval for reschedule. |
| `Rejected` | Rejected | Various | Admin/System | Order rejected. |
| `Cancelled` | Cancelled | Closure | Admin/System | Order cancelled. Cannot be reactivated. |
| `Reinvoice` | Reinvoice | Billing | System/Agent | Payment rejected. Invoice needs to be resubmitted with new SubmissionId. |

### 1.3 Standard Order Flow

```
Pending
  ↓
Assigned
  ↓
OnTheWay
  ↓
MetCustomer
  ↓
OrderCompleted
  ↓
DocketsReceived
  ↓
DocketsVerified
  ↓
DocketsUploaded
  ↓
ReadyForInvoice
  ↓
Invoiced
  ↓
SubmittedToPortal
  ↓
Completed
```

### 1.4 Side Paths

**Blocker Path:**
- Can occur from: `Assigned`, `OnTheWay`, `MetCustomer`
- Can transition to: `MetCustomer`, `ReschedulePendingApproval`, `Cancelled`

**Reschedule Path:**
- Can occur from: `Assigned`, `Blocker`
- Can transition to: `Assigned`, `Cancelled`

**Cancellation Path:**
- Can occur from: `Pending`, `Assigned`, `Blocker`, `ReschedulePendingApproval`
- Terminal state (no further transitions)

**Reinvoice Path:**
- Can occur from: `Invoiced`, `SubmittedToPortal`
- Can transition back to: `Invoiced` (with new submission ID)

---

## 2. RMA WORKFLOW STATUSES (11 Total)

Return Material Authorization workflow for faulty device returns.

| Order | Code | Display Name | Phase | Triggered By | Description |
|-------|------|--------------|-------|--------------|-------------|
| 1 | `RMARequested` | RMA Requested | Initiation | Admin/SI | Faulty device identified. RMA request created. |
| 2 | `RMAPendingReview` | Pending Review | Review | Admin | Admin reviewing RMA request and device serials. |
| 3 | `RMAMraReceived` | MRA Document Received | Review | System/Admin | Partner MRA email/PDF received and attached. |
| 4 | `RMAApproved` | RMA Approved | Processing | Admin | RMA approved. Ready for shipment to partner. |
| 5 | `RMAInTransit` | In Transit to Partner | Processing | Warehouse | Faulty devices shipped to partner for repair/replacement. |
| 6 | `RMAAtPartner` | At Partner | Processing | Partner | Devices received by partner. Awaiting resolution. |
| 7 | `RMARepaired` | Repaired | Resolution | Partner | Device repaired and returned to warehouse. |
| 8 | `RMAReplaced` | Replaced | Resolution | Partner | Device replaced with new unit. |
| 9 | `RMACredited` | Credited | Resolution | Partner | Credit note issued by partner. |
| 10 | `RMAScrapped` | Scrapped | Resolution | Admin/Partner | Device scrapped. Warranty void or beyond repair. |
| 11 | `RMAClosed` | RMA Closed | Closure | Admin | RMA process completed. All records updated. |

---

## 3. KPI WORKFLOW STATUSES (14 Total)

Performance tracking statuses for SI, Admin, and Employer KPIs.

### 3.1 SI Performance (5 Statuses)

| Order | Code | Display Name | Phase | Triggered By | Description |
|-------|------|--------------|-------|--------------|-------------|
| 1 | `KpiPending` | KPI Pending | SI Performance | System | Job in progress. KPI not yet calculated. |
| 2 | `KpiOnTime` | On Time | SI Performance | System | SI completed job within SLA window. |
| 3 | `KpiLate` | Late | SI Performance | System | SI completed job but exceeded SLA. Minor breach. |
| 4 | `KpiExceededSla` | Exceeded SLA | SI Performance | System | SI significantly exceeded SLA. Major breach. |
| 5 | `KpiExcused` | Excused | SI Performance | Admin | KPI breach excused due to valid reason (blocker/reschedule). |

### 3.2 Admin Performance (6 Statuses)

| Order | Code | Display Name | Phase | Triggered By | Description |
|-------|------|--------------|-------|--------------|-------------|
| 6 | `KpiDocketPending` | Docket KPI Pending | Admin Performance | System | Docket received. Processing time KPI started. |
| 7 | `KpiDocketOnTime` | Docket On Time | Admin Performance | System | Admin processed docket within SLA. |
| 8 | `KpiDocketLate` | Docket Late | Admin Performance | System | Admin processed docket but exceeded SLA. |
| 9 | `KpiInvoicePending` | Invoice KPI Pending | Admin Performance | System | Ready for invoice. Billing KPI started. |
| 10 | `KpiInvoiceOnTime` | Invoice On Time | Admin Performance | System | Invoice generated within SLA window. |
| 11 | `KpiInvoiceLate` | Invoice Late | Admin Performance | System | Invoice generated but exceeded SLA. |

### 3.3 Employer Review (3 Statuses)

| Order | Code | Display Name | Phase | Triggered By | Description |
|-------|------|--------------|-------|--------------|-------------|
| 12 | `KpiEmployerPending` | Employer Review Pending | Employer Review | System | SI performance pending employer review. |
| 13 | `KpiEmployerApproved` | Employer Approved | Employer Review | Employer | SI performance approved by employer. |
| 14 | `KpiEmployerFlagged` | Employer Flagged | Employer Review | Employer | SI performance flagged for review/action. |

---

## 4. Status Validation Rules

### 4.1 Blocker Rules

**Pre-Customer Blockers** (allowed from):
- `Assigned`
- `OnTheWay`

**Post-Customer Blockers** (allowed from):
- `MetCustomer`

### 4.2 Status Validation

All status values must:
1. Use PascalCase (e.g., `OnTheWay`, not `on_the_way`)
2. Match exactly (case-sensitive) with `OrderStatus` enum constants
3. Be validated using `OrderStatus.IsValid(status)`

---

## 5. Implementation Guidelines

### 5.1 Backend

**Source of Truth:** `backend/src/CephasOps.Domain/Orders/Enums/OrderStatus.cs`

```csharp
// ✅ Correct usage
if (order.Status == OrderStatus.OnTheWay) { ... }
if (OrderStatus.IsValid(status)) { ... }

// ❌ Wrong usage
if (order.Status == "on_the_way") { ... }
if (order.Status == "On The Way") { ... }
```

### 5.2 Frontend

**Status Constants:** Use PascalCase matching backend exactly.

```typescript
// ✅ Correct
export const ORDER_STATUS = {
  Pending: 'Pending',
  Assigned: 'Assigned',
  OnTheWay: 'OnTheWay',
  MetCustomer: 'MetCustomer',
  OrderCompleted: 'OrderCompleted',
  DocketsVerified: 'DocketsVerified',
  SubmittedToPortal: 'SubmittedToPortal',
  // ... etc
} as const;

// ❌ Wrong
export const ORDER_STATUS = {
  on_the_way: 'on_the_way',  // snake_case
  met_customer: 'met_customer',  // snake_case
  'On The Way': 'On The Way',  // spaces
} as const;
```

### 5.3 API Responses

Status values in API responses must use PascalCase:
```json
{
  "status": "OnTheWay",  // ✅ Correct
  "status": "on_the_way"  // ❌ Wrong
}
```

---

## 6. Quick Reference

### Order Workflow (Main Flow)
```
Pending → Assigned → OnTheWay → MetCustomer → OrderCompleted 
  → DocketsReceived → DocketsVerified → DocketsUploaded 
  → ReadyForInvoice → Invoiced → SubmittedToPortal → Completed
```

### All Order Statuses (17)
1. `Pending`
2. `Assigned`
3. `OnTheWay`
4. `MetCustomer`
5. `OrderCompleted`
6. `DocketsReceived`
7. `DocketsVerified`
8. `DocketsUploaded`
9. `ReadyForInvoice`
10. `Invoiced`
11. `SubmittedToPortal`
12. `Completed`
13. `Blocker`
14. `ReschedulePendingApproval`
15. `Rejected`
16. `Cancelled`
17. `Reinvoice`

---

**Last Updated:** December 2025  
**Maintained By:** Development Team  
**Review Frequency:** Quarterly or when workflow changes

