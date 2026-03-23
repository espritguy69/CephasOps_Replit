# CephasOps SI App Data Model Mapping
Version: 1.0  
Status: Design Reference  
Audience: Backend, Frontend, Product, Cursor, Documentation  

---

# 1. Purpose

This document maps the **CephasOps SI App** between:

- Screens
- User actions
- API responsibilities
- Backend entities
- Workflow status transitions
- Validation rules

It is intended to serve as the **bridge specification** between:

- `frontend-si`
- backend API
- business process documentation
- future SI App enhancements

This document should be read together with:

- `docs/07_frontend/si_app_architecture.md`
- `docs/business/si_app_journey.md`
- `docs/business/process_flows.md`
- `docs/architecture/data_model_overview.md`

---

# 2. Core Mapping Principle

Each SI App screen must clearly answer:

1. What business action is happening?
2. What backend entity changes?
3. What workflow state changes?
4. What validations must pass?
5. What audit records are created?

---

# 3. High-Level Domain Areas

The SI App mainly interacts with these domain areas:

| Domain Area | Purpose |
|---|---|
| Orders | Job assignment, progress, completion |
| Workflow | Status transitions and exception flow |
| Service Installers | Installer identity, earnings, skills |
| Inventory | Materials, serial tracking, stock ownership |
| Proof / Files | Photos, documents, evidence |
| Assurance | Removed/replacement device tracking |
| Extra Work | Customer-paid add-on services |
| Payroll / Earnings | Estimated and approved installer income |

---

# 4. Core Workflow Status Codes

Recommended internal status codes for SI execution:

```text
ASSIGNED
ON_THE_WAY
MET_CUSTOMER
START_WORK
COMPLETE
PROBLEM
RESCHEDULE
```

Recommended UI labels:

| Internal Code | UI Label |
|---|---|
| ASSIGNED | Assigned |
| ON_THE_WAY | On the Way |
| MET_CUSTOMER | Met Customer |
| START_WORK | Start Work |
| COMPLETE | Complete |
| PROBLEM | Report Problem |
| RESCHEDULE | Request Reschedule |

---

# 5. Screen-to-Domain Mapping

## 5.1 Login Screen

### User Purpose
Installer authenticates into SI App.

### Main Backend Concerns
- Authentication
- User profile resolution
- Installer record lookup
- Department / partner scope

### Likely API Responsibilities
- login
- refresh token
- fetch current installer profile

### Main Entities
- User
- RefreshToken
- ServiceInstaller
- DepartmentMembership

### Validations
- valid credentials
- active account
- installer-linked user exists

---

## 5.2 Home Screen

### User Purpose
See today's summary and current job.

### Main Backend Concerns
- today's assigned jobs
- active job
- today earnings summary
- unresolved problem count

### Likely API Responsibilities
- fetch installer dashboard summary
- fetch active/current job card
- fetch earnings snapshot

### Main Entities
- Order
- ScheduledSlot
- ServiceInstaller
- JobEarningRecord
- Notification

### Derived Data
- jobs today
- completed count
- problem count
- estimated earnings

---

## 5.3 Jobs Screen

### User Purpose
View assigned jobs by Today / Upcoming / History.

### Main Backend Concerns
- assigned jobs list
- filtering by date and status
- quick job metadata

### Likely API Responsibilities
- get jobs for installer
- filter by date range / status
- fetch basic payout preview

### Main Entities
- Order
- ScheduledSlot
- OrderStatusLog
- ServiceInstaller

### Validations
- installer can only see own jobs unless supervisor mode exists

---

## 5.4 Job Detail Screen

### User Purpose
View full information and perform job actions.

### Main Backend Concerns
- customer details
- order metadata
- job type
- current status
- existing installed device info
- required materials
- completion requirements

### Main Entities
- Order
- OrderStatusLog
- OrderMaterialUsage
- OrderMaterialReplacement
- File / ProofAttachment equivalent
- ScheduledSlot

### Derived Sections
- customer info
- admin notes
- expected materials
- assurance context
- action buttons

---

## 5.5 Status Action Flow

### User Purpose
Move through guided job states.

### Main Backend Concerns
- transition validation
- audit history
- timestamp capture
- optional GPS capture

### Suggested API Responsibility
- submit workflow transition

### Main Entities
- Order
- OrderStatusLog
- WorkflowDefinition
- WorkflowTransition
- OrderStatusChecklistItem
- OrderStatusChecklistAnswer

### Transition Rules

| From | To | Notes |
|---|---|---|
| ASSIGNED | ON_THE_WAY | installer starts travel |
| ON_THE_WAY | MET_CUSTOMER | reached customer / site |
| MET_CUSTOMER | START_WORK | installer begins service |
| START_WORK | COMPLETE | all required conditions passed |
| any active state | PROBLEM | exception path |
| any active state | RESCHEDULE | reschedule path |

### Required Audit Fields
- changed by
- changed at
- GPS if enabled
- note if required

---

## 5.6 Materials Section

### User Purpose
View required items and record what was used.

### Main Backend Concerns
- required material display
- installer inventory check
- serial validation
- quantity capture
- stock audit trail

### Main Entities
- Material
- SerialisedItem
- StockLedgerEntry
- StockMovement
- StockAllocation
- OrderMaterialUsage
- MaterialAllocation

### Split by Category

#### Serialized Items
Examples:
- ONT
- Router
- Mesh

#### Non-Serialized Items
Examples:
- LAN cable
- casing
- clips

### Validations
- serialized item exists
- serialized item belongs to installer / assigned stock / reserved order
- serialized item not already installed elsewhere
- quantity entry must be numeric and non-negative

---

## 5.7 Scan Screen

### User Purpose
Fast material / serial actions.

### Supported Scan Modes
- Install
- Remove
- Return
- Faulty
- Lookup

### Main Backend Concerns
- serial lookup
- material movement creation
- order linkage
- ownership validation

### Main Entities
- SerialisedItem
- StockMovement
- StockLedgerEntry
- OrderMaterialUsage
- OrderMaterialReplacement

### Typical Results
- valid for current job
- not in your inventory
- already installed
- marked faulty
- unknown serial

---

## 5.8 Assurance Replacement Screen

### User Purpose
Remove faulty / replaced device and install new one.

### Business Action
Device swap during assurance job.

### Main Backend Concerns
- removed device capture
- removed device condition
- replacement device scan
- inventory handoff
- order linkage
- fault trail

### Main Entities
- Order
- OrderMaterialReplacement
- SerialisedItem
- StockMovement
- StockLedgerEntry
- RmaRequest / RmaRequestItem (later-stage warehouse flow)

### Recommended New / Extended Records
- RemovedDeviceRecord
- DeviceReplacementRecord
- FaultyReturnRecord

### Validation Rules
- removed serial must match site device if known
- replacement serial must belong to installer or reserved order
- replacement serial must be usable
- condition is required

### Output
- old device linked as removed from site
- new device linked as installed to order

---

## 5.9 Extra Work Screen

### User Purpose
Record customer-paid add-on work safely.

### Business Action
Installer performs additional work outside standard partner scope.

### Examples
- repull cable
- casing installation
- router relocation
- additional LAN point

### Main Backend Concerns
- service catalog pricing
- quantity capture
- customer approval
- proof collection
- payment recording
- receipt generation

### Recommended Main Entities
- ExtraWorkCatalog
- OrderExtraWork
- ExtraWorkApproval
- ExtraWorkPayment
- ReceiptRecord

### Suggested Existing Cross-Link Entities
- Order
- File / ProofAttachment equivalent
- JobEarningRecord

### Validation Rules
- selected extra work must exist in catalog
- quantity required
- unit price derived from catalog, not manually typed
- customer approval required before charge finalization
- payment method required if collected

---

## 5.10 Proof Capture Screen

### User Purpose
Upload photos and evidence required for completion.

### Main Backend Concerns
- required proof checklist by job type
- photo upload
- metadata capture
- proof completeness validation

### Main Entities
- File
- OrderDocket
- OrderStatusChecklistAnswer
- OrderMaterialUsage
- OrderMaterialReplacement

### Recommended Proof Types
- ONT serial photo
- router installed photo
- before photo
- after photo
- extra work route photo
- removed device serial photo
- replacement device serial photo
- customer confirmation proof

### Validation Rules
- proof type must match requirement profile
- mandatory proof must exist before completion

---

## 5.11 Problem / Reschedule Screen

### User Purpose
Record exceptions and request next action.

### Main Backend Concerns
- issue categorization
- evidence capture
- reschedule handling
- workflow transition logging

### Main Entities
- Order
- OrderBlocker
- OrderReschedule
- OrderStatusLog
- File

### Standard Problem Categories
- Building Issue
- Customer Issue
- Technical Issue
- Reschedule

### Validation Rules
- category required
- reason required
- minimum evidence photo required for certain categories
- same-day reschedule may require additional proof rules

---

## 5.12 Completion Review Screen

### User Purpose
Review all required actions before final submission.

### Main Backend Concerns
- completion gate validation
- missing-item summary
- transition readiness

### Main Entities
- Order
- OrderStatusChecklistItem
- OrderStatusChecklistAnswer
- OrderMaterialUsage
- OrderMaterialReplacement
- File

### Validation Categories
- statuses completed
- mandatory scans complete
- mandatory proof complete
- customer approval complete where relevant
- extra work payment state recorded where relevant

---

## 5.13 Job Submitted Screen

### User Purpose
See success confirmation and earnings preview.

### Main Backend Concerns
- order submission acknowledgment
- estimated earnings preview
- next job suggestion

### Main Entities
- Order
- JobEarningRecord
- OrderExtraWork
- ReceiptRecord

---

## 5.14 Earnings Screen

### User Purpose
Track daily, weekly, monthly income.

### Main Backend Concerns
- earnings aggregation
- base vs bonus vs extra work split
- pending approval amounts

### Main Entities
- JobEarningRecord
- PayrollRun
- PayrollPeriod
- GponSiJobRate
- GponSiCustomRate
- ServiceInstaller

### Suggested Breakdown Areas
- base job earnings
- assurance earnings
- extra work earnings
- bonuses
- penalties
- pending approval

---

## 5.15 Inventory Screen

### User Purpose
View installer-held stock and take stock actions.

### Main Backend Concerns
- current serialized stock
- current non-serialized stock
- return flow
- faulty flow
- stock request flow

### Main Entities
- Material
- SerialisedItem
- StockBalance
- StockLedgerEntry
- StockMovement
- StockLocation
- Warehouse
- Bin

### Suggested Actions
- scan item
- return item
- mark faulty
- request stock
- transfer stock

---

## 5.16 Profile Screen

### User Purpose
View account and support info.

### Main Backend Concerns
- profile lookup
- role / team / partner display
- support contact display

### Main Entities
- User
- ServiceInstaller
- Partner
- DepartmentMembership

---

# 6. Recommended New / Extended Entity Set

Some concepts may already partially exist in CephasOps. The following list defines the recommended logical model for the SI App.

## Already-aligned or likely existing
- Order
- OrderStatusLog
- OrderReschedule
- OrderBlocker
- OrderMaterialUsage
- OrderMaterialReplacement
- ServiceInstaller
- Material
- SerialisedItem
- StockMovement
- StockLedgerEntry
- File
- JobEarningRecord

## Recommended additions or explicit documentation concepts
- RemovedDeviceRecord
- DeviceReplacementRecord
- FaultyReturnRecord
- ExtraWorkCatalog
- OrderExtraWork
- ExtraWorkApproval
- ExtraWorkPayment
- ReceiptRecord
- InstallerInventoryView or InstallerStockSummaryView

---

# 7. Material Movement Mapping

## 7.1 Standard Installation Use

```text
Warehouse / Installer Stock
→ Scan for Order
→ OrderMaterialUsage
→ Installed at Customer Site
```

### Entity Impact
- SerialisedItem current holder updated
- StockMovement created
- StockLedgerEntry created if needed
- OrderMaterialUsage created

---

## 7.2 Assurance Replacement Use

```text
Customer Site Device
→ Removed from Site
→ Installer holds faulty device
→ Returned to Warehouse / RMA

Installer Stock Replacement Device
→ Scanned to Order
→ Installed at Customer Site
```

### Entity Impact
- RemovedDeviceRecord created
- DeviceReplacementRecord created
- replacement usage recorded
- faulty return movement recorded

---

## 7.3 Non-Serialized Consumption

```text
Installer Cable Stock
→ Quantity Entered
→ OrderMaterialUsage
→ Reduced stock summary
```

### Entity Impact
- OrderMaterialUsage created
- stock ledger / allocation adjusted

---

# 8. Extra Work Mapping

## Business Rule
Extra work must never be free-typed as an arbitrary price by the installer.

## Mapping

```text
ExtraWorkCatalog
→ selected on job
→ quantity entered
→ OrderExtraWork created
→ customer approval captured
→ proof uploaded
→ payment recorded
→ receipt generated
```

### Entity Impact
- OrderExtraWork
- ExtraWorkApproval
- ExtraWorkPayment
- ReceiptRecord
- optional JobEarningRecord link for installer share

---

# 9. Recommended API Capability Map

This is a logical capability map, not a final endpoint contract.

| Capability | Purpose |
|---|---|
| Auth / Login | authenticate installer |
| Current Installer Profile | fetch SI user context |
| Installer Dashboard Summary | home screen summary |
| Installer Jobs List | today/upcoming/history |
| Installer Job Detail | full job info |
| Submit Status Transition | workflow progression |
| Get Required Materials | job material requirements |
| Scan / Validate Serial | serial lookup and ownership validation |
| Record Material Usage | serialized and non-serialized usage |
| Record Replacement | removed + replacement device flow |
| Upload Proof | image / evidence upload |
| Submit Problem | blocker / issue submission |
| Submit Reschedule | reschedule request |
| Record Extra Work | controlled customer-paid work |
| Capture Extra Work Approval | OTP/signature approval |
| Record Extra Work Payment | payment method and amount |
| Generate / Fetch Receipt | receipt confirmation |
| Get Inventory Summary | installer stock view |
| Return / Faulty Stock | reverse stock action |
| Get Earnings Summary | income dashboard |

---

# 10. Validation Matrix

## 10.1 Completion Validation

| Requirement | Installation | Assurance | Extra Work |
|---|---|---|---|
| Required status steps complete | Yes | Yes | Yes |
| Required serialized scans complete | Yes | If replacement | If materialized |
| Non-serialized quantities recorded | If used | If used | If used |
| Mandatory proof uploaded | Yes | Yes | Yes |
| Customer confirmation captured | Optional / rule-based | Optional / rule-based | Required |
| Payment status recorded | No | No | Required if charged |

---

## 10.2 Problem Validation

| Field | Required |
|---|---|
| category | Yes |
| reason | Yes |
| note | Rule-based |
| evidence photo | Usually Yes |

---

## 10.3 Serial Validation

| Check | Required |
|---|---|
| serial exists | Yes |
| serial status usable | Yes |
| serial owned by installer / order | Yes |
| serial not already installed elsewhere | Yes |
| serial type matches expected material | Yes |

---

# 11. Documentation Sync Checklist

When this mapping is adopted, the following docs should be updated together:

- `docs/07_frontend/si_app_architecture.md`
- `docs/business/si_app_journey.md`
- `docs/business/process_flows.md`
- `docs/business/inventory_ledger_summary.md`
- `docs/business/payroll_rate_overview.md`
- `docs/architecture/data_model_overview.md`
- `docs/CHANGELOG_DOCS.md`
- `docs/_discrepancies.md`

---

# 12. Cursor Usage Guidance

This file should be used by Cursor when performing any of these tasks:

- SI App frontend implementation
- backend API alignment for SI App
- documentation updates for SI App
- material tracking workflow design
- assurance replacement design
- extra work / receipt feature design

Cursor should treat this document as the **reference mapping layer** between UX and backend behavior.

---

# 13. Summary

This mapping ensures that CephasOps SI App development stays aligned across:

- User experience
- Business process
- Workflow engine
- Inventory control
- Assurance replacement handling
- Extra work charging controls
- Earnings and payroll linkage

The frontend should remain simple for installers, while the backend preserves strong auditability and operational control.
