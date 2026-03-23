# KPI – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the KPI module

---

## KPI System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         KPI SYSTEM ARCHITECTURE                         │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │    KPI PROFILES        │      │   KPI EVALUATION      │
        │  (Configuration)       │      │   (Runtime)           │
        ├───────────────────────┤      ├───────────────────────┤
        │ • CompanyId            │      │ • Order Timestamps    │
        │ • PartnerId (opt)      │      │ • KPI Profile Rules   │
        │ • OrderType            │      │ • Calculation Logic   │
        │ • BuildingTypeId (opt) │      │ • Result Generation   │
        │ • MaxJobDuration       │      │                       │
        │ • DocketKpiMinutes     │      │                       │
        │ • MaxReschedules       │      │                       │
        │ • IsDefault            │      │                       │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │      KPI RESULTS               │
                    │  (Stored in Order/Payroll)     │
                    ├───────────────────────────────┤
                    │ • OnTime / Late / ExceededSla  │
                    │ • ActualMinutes                │
                    │ • TargetMinutes                │
                    │ • Delta                        │
                    └───────────────────────────────┘
```

---

## KPI Profile Resolution Flow

```
[Order Created/Updated]
         |
         v
[System needs KPI Profile]
         |
         v
┌────────────────────────────────────────┐
│ KPI PROFILE RESOLUTION (Priority)      │
└────────────────────────────────────────┘
         |
         v
[Step 1: Most Specific Match]
    CompanyId + PartnerId + OrderType + BuildingTypeId
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND]  [NOT FOUND]
   |         |
   |         v
   |    [Step 2: Partner Match]
   |        CompanyId + PartnerId + OrderType
   |         |
   |    ┌────┴────┐
   |    |         |
   |    v         v
   | [FOUND]  [NOT FOUND]
   |    |         |
   |    |         v
   |    |    [Step 3: Default Profile]
   |    |        CompanyId + OrderType + IsDefault=true
   |    |         |
   |    |    ┌────┴────┐
   |    |    |         |
   |    |    v         v
   |    | [FOUND]  [NOT FOUND]
   |    |    |         |
   |    |    |         v
   |    |    |    [Step 4: Global Settings]
   |    |    |        GlobalSetting.KpiInstallationSlaHours
   |    |    |        GlobalSetting.KpiAssuranceSlaHours
   |    |    |        GlobalSetting.KpiPrelaidDefaultHours
   |    |    |         |
   |    |    |         v
   |    |    |    [FALLBACK VALUES]
   |    |    |         |
   |    └────┴─────────┘
   |         |
   └─────────┘
         |
         v
[KPI Profile Selected]
         |
         v
[Use for Evaluation]
```

---

## KPI Evaluation Flow

```
[Order Status Changes]
         |
         v
[Trigger: OrderCompleted / DocketsReceived]
         |
         v
┌────────────────────────────────────────┐
│ KPI EVALUATION SERVICE                 │
│ EvaluateOrderAsync(orderId)            │
└────────────────────────────────────────┘
         |
         v
[Load Order + Timestamps]
    - StatusAssignedAt
    - StatusOrderCompletedAt
    - StatusDocketsReceivedAt
         |
         v
[Resolve KPI Profile]
    (see Resolution Flow above)
         |
         v
[Calculate Metrics]
         |
    ┌────┴────┐
    |         |
    v         v
┌─────────────────┐    ┌─────────────────┐
│ JOB DURATION    │    │ DOCKET KPI      │
│ KPI             │    │ KPI             │
├─────────────────┤    ├─────────────────┤
│ ActualMinutes = │    │ ActualMinutes = │
│ OrderCompleted  │    │ DocketsReceived │
│ - Assigned      │    │ - OrderCompleted│
│                 │    │                 │
│ Compare vs:     │    │ Compare vs:      │
│ MaxJobDuration  │    │ DocketKpiMinutes │
│ Minutes         │    │                 │
│                 │    │                 │
│ Result:         │    │ Result:          │
│ - OnTime        │    │ - OnTime        │
│ - Late          │    │ - Late          │
│ - ExceededSla   │    │ - ExceededSla   │
└─────────────────┘    └─────────────────┘
         |                       |
         └───────────┬───────────┘
                     |
                     v
         ┌───────────────────────┐
         │   KPI RESULT OBJECT    │
         ├───────────────────────┤
         │ • JobKpiResult         │
         │ • DocketKpiResult       │
         │ • ActualJobMinutes      │
         │ • ActualDocketMinutes   │
         │ • TargetJobMinutes      │
         │ • TargetDocketMinutes   │
         │ • JobDelta              │
         │ • DocketDelta           │
         └───────────────────────┘
                     |
                     v
         [Store in Order.KpiResult]
                     |
                     v
         [Trigger KPI Events]
                     |
        ┌────────────┴────────────┐
        |                         |
        v                         v
┌──────────────────┐    ┌──────────────────┐
│ SI KPI Dashboard │    │ Admin KPI        │
│                  │    │ Dashboard        │
└──────────────────┘    └──────────────────┘
```

---

## KPI Integration Points

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    KPI SYSTEM INTEGRATION POINTS                         │
└─────────────────────────────────────────────────────────────────────────┘

1. SCHEDULER MODULE
   ┌─────────────────────────────────────┐
   │ When: Job Completed                 │
   │ Action: Evaluate KPI                 │
   │ Result: Display in scheduler view    │
   │ Impact: SI performance tracking      │
   └─────────────────────────────────────┘
                    │
                    v
   [Order.KpiResult updated]

2. SI APP / ORDERS MODULE
   ┌─────────────────────────────────────┐
   │ When: Order status changes           │
   │ Action: Calculate KPI metrics        │
   │ Result: Show KPI status in job card  │
   │ Impact: SI sees own performance      │
   └─────────────────────────────────────┘
                    │
                    v
   [KPI badge: ✓ OnTime / ⚠ Late / ✗ Exceeded]

3. PAYROLL MODULE
   ┌─────────────────────────────────────┐
   │ When: Payroll calculation            │
   │ Action: Read Order.KpiResult         │
   │ Logic:                                │
   │   - OnTime → Bonus applied           │
   │   - Late → Penalty or reduced rate   │
   │   - ExceededSla → Higher penalty     │
   │ Impact: SI earnings affected         │
   └─────────────────────────────────────┘
                    │
                    v
   [PayrollRecord includes KPI adjustments]

4. REPORTING / DASHBOARDS
   ┌─────────────────────────────────────┐
   │ When: Dashboard refresh               │
   │ Action: Aggregate KPI results         │
   │ Metrics:                              │
   │   - OnTime %                         │
   │   - Average job duration             │
   │   - SLA breach count                 │
   │ Impact: Management visibility         │
   └─────────────────────────────────────┘
                    │
                    v
   [KPI Dashboard Charts & Reports]
```

---

## KPI Responsibility Matrix

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    KPI RESPONSIBILITY BY STATUS                          │
└─────────────────────────────────────────────────────────────────────────┘

STATUS                  PRIMARY ACTOR    KPI IMPACT        EVALUATION TRIGGER
────────────────────────────────────────────────────────────────────────────
Pending                 Admin            Admin KPI         N/A (no KPI yet)
Assigned                Admin            Admin KPI         N/A (no KPI yet)
OnTheWay                SI               SI KPI            N/A (in progress)
MetCustomer             SI               SI KPI            N/A (in progress)
Blocker (Pre)           SI/Admin         Mixed              N/A (blocked)
Blocker (Post)          SI               SI KPI             N/A (blocked)
ReschedulePending       Admin            Admin KPI          N/A (pending)
OrderCompleted          SI               SI KPI             ✓ JOB DURATION KPI
DocketsReceived        Admin            Admin KPI          ✓ DOCKET KPI
DocketsRejected         SI               SI KPI             N/A (rejected)
DocketsUploaded         Admin            Admin KPI          N/A (uploaded)
ReadyForInvoice         Admin            Admin KPI          N/A (ready)
Invoiced                Admin            Admin KPI          N/A (invoiced)
InvoiceRejected         Admin/Clerk      Admin KPI          N/A (rejected)
Reinvoice               Admin            Admin KPI          N/A (reinvoice)
Completed               Finance          Finance KPI        N/A (completed)
Cancelled               Admin/SI         Depends on cause   N/A (cancelled)

KEY:
✓ = KPI evaluation triggered at this status
N/A = No KPI evaluation (status doesn't affect KPI metrics)
```

---

## KPI Profile Matching Examples

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    KPI PROFILE MATCHING EXAMPLES                         │
└─────────────────────────────────────────────────────────────────────────┘

EXAMPLE 1: TIME Activation, FTTH Building
──────────────────────────────────────────
Order:
  - CompanyId: Cephas
  - PartnerId: TIME
  - OrderType: Activation
  - BuildingTypeId: FTTH

Resolution:
  1. Try: CompanyId + PartnerId + OrderType + BuildingTypeId
     → Match: "TIME Activation FTTH KPI Profile"
  2. If not found → Try: CompanyId + PartnerId + OrderType
     → Match: "TIME Activation Default KPI Profile"
  3. If not found → Try: CompanyId + OrderType + IsDefault=true
     → Match: "Activation Default KPI Profile"
  4. If not found → Use GlobalSetting.KpiInstallationSlaHours

EXAMPLE 2: Celcom Assurance, No Building Type
──────────────────────────────────────────────
Order:
  - CompanyId: Cephas
  - PartnerId: Celcom
  - OrderType: Assurance
  - BuildingTypeId: null

Resolution:
  1. Try: CompanyId + PartnerId + OrderType + BuildingTypeId (null)
     → No match (BuildingTypeId required)
  2. Try: CompanyId + PartnerId + OrderType
     → Match: "Celcom Assurance KPI Profile"
  3. If not found → Try: CompanyId + OrderType + IsDefault=true
     → Match: "Assurance Default KPI Profile"
  4. If not found → Use GlobalSetting.KpiAssuranceSlaHours

EXAMPLE 3: Generic Activation (No Partner)
──────────────────────────────────────────
Order:
  - CompanyId: Cephas
  - PartnerId: null
  - OrderType: Activation
  - BuildingTypeId: FTTO

Resolution:
  1. Try: CompanyId + PartnerId (null) + OrderType + BuildingTypeId
     → No match (PartnerId required)
  2. Try: CompanyId + PartnerId (null) + OrderType
     → No match (PartnerId required)
  3. Try: CompanyId + OrderType + IsDefault=true
     → Match: "Activation Default KPI Profile"
  4. If not found → Use GlobalSetting.KpiInstallationSlaHours
```

---

## KPI Calculation Details

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    JOB DURATION KPI CALCULATION                          │
└─────────────────────────────────────────────────────────────────────────┘

INPUTS:
  - StatusAssignedAt: 2025-12-12 09:00:00
  - StatusOrderCompletedAt: 2025-12-12 11:30:00
  - KpiProfile.MaxJobDurationMinutes: 120 (2 hours)

CALCULATION:
  ActualMinutes = (StatusOrderCompletedAt - StatusAssignedAt).TotalMinutes
                = (11:30 - 09:00).TotalMinutes
                = 150 minutes

  TargetMinutes = KpiProfile.MaxJobDurationMinutes
                 = 120 minutes

  Delta = ActualMinutes - TargetMinutes
        = 150 - 120
        = +30 minutes (over target)

RESULT:
  if ActualMinutes <= TargetMinutes:
    Result = "OnTime"
  elif ActualMinutes <= (TargetMinutes * 1.2):  // 20% buffer
    Result = "Late"
  else:
    Result = "ExceededSla"

  In this case: 150 > 120 * 1.2 (144)
  → Result = "ExceededSla"

OUTPUT:
  {
    "jobKpiResult": "ExceededSla",
    "actualJobMinutes": 150,
    "targetJobMinutes": 120,
    "jobDelta": 30
  }

┌─────────────────────────────────────────────────────────────────────────┐
│                    DOCKET KPI CALCULATION                                │
└─────────────────────────────────────────────────────────────────────────┘

INPUTS:
  - StatusOrderCompletedAt: 2025-12-12 11:30:00
  - StatusDocketsReceivedAt: 2025-12-12 14:00:00
  - KpiProfile.DocketKpiMinutes: 60 (1 hour)

CALCULATION:
  ActualMinutes = (StatusDocketsReceivedAt - StatusOrderCompletedAt).TotalMinutes
                = (14:00 - 11:30).TotalMinutes
                = 150 minutes

  TargetMinutes = KpiProfile.DocketKpiMinutes
                 = 60 minutes

  Delta = ActualMinutes - TargetMinutes
        = 150 - 60
        = +90 minutes (over target)

RESULT:
  if ActualMinutes <= TargetMinutes:
    Result = "OnTime"
  elif ActualMinutes <= (TargetMinutes * 1.5):  // 50% buffer
    Result = "Late"
  else:
    Result = "ExceededSla"

  In this case: 150 > 60 * 1.5 (90)
  → Result = "ExceededSla"

OUTPUT:
  {
    "docketKpiResult": "ExceededSla",
    "actualDocketMinutes": 150,
    "targetDocketMinutes": 60,
    "docketDelta": 90
  }
```

---

## KPI Profile Configuration Structure

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    KPI PROFILE DATA MODEL                                │
└─────────────────────────────────────────────────────────────────────────┘

KpiProfile
├── Id (Guid)
├── CompanyId (Guid)
├── Name (string) - e.g., "TIME Prelaid KPI", "TIME SDU KPI"
├── PartnerId (Guid?) - nullable, for partner-specific profiles
├── OrderType (string) - Activation, Assurance, Modification, etc.
├── BuildingTypeId (Guid?) - nullable, for building-specific profiles
├── MaxJobDurationMinutes (int) - Target: Assigned → OrderCompleted
├── DocketKpiMinutes (int) - Target: OrderCompleted → DocketsReceived
├── MaxReschedulesAllowed (int?) - optional limit
├── IsDefault (bool) - default profile for OrderType
├── EffectiveFrom (DateTime?) - optional start date
├── EffectiveTo (DateTime?) - optional end date
├── CreatedAt (DateTime)
├── CreatedByUserId (Guid)
├── UpdatedAt (DateTime)
└── UpdatedByUserId (Guid)

INDEXES:
  - (CompanyId, PartnerId, OrderType, BuildingTypeId, EffectiveFrom)
  - (CompanyId, IsDefault, OrderType)
```

---

## KPI System API Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    KPI SYSTEM API ENDPOINTS                              │
└─────────────────────────────────────────────────────────────────────────┘

1. GET /api/kpi-profiles
   ┌─────────────────────────────────────┐
   │ Query Parameters:                    │
   │ - companyId                          │
   │ - partnerId (optional)               │
   │ - orderType (optional)               │
   │ - buildingTypeId (optional)          │
   │                                      │
   │ Returns: List of KpiProfile          │
   └─────────────────────────────────────┘

2. GET /api/kpi-profiles/{id}
   ┌─────────────────────────────────────┐
   │ Returns: Single KpiProfile          │
   └─────────────────────────────────────┘

3. POST /api/kpi-profiles
   ┌─────────────────────────────────────┐
   │ Body: KpiProfileCreateDto            │
   │ Returns: Created KpiProfile          │
   └─────────────────────────────────────┘

4. PUT /api/kpi-profiles/{id}
   ┌─────────────────────────────────────┐
   │ Body: KpiProfileUpdateDto            │
   │ Returns: Updated KpiProfile          │
   └─────────────────────────────────────┘

5. POST /api/kpi-profiles/{id}/set-default
   ┌─────────────────────────────────────┐
   │ Sets IsDefault = true                │
   │ Unsets other defaults for same       │
   │   (CompanyId, OrderType)             │
   │ Returns: Updated KpiProfile          │
   └─────────────────────────────────────┘

6. GET /api/kpi/evaluate-order/{orderId}
   ┌─────────────────────────────────────┐
   │ Evaluates KPI for specific order      │
   │ Returns: KpiEvaluationResult          │
   │   - jobKpiResult                      │
   │   - docketKpiResult                   │
   │   - actualJobMinutes                  │
   │   - actualDocketMinutes               │
   │   - targetJobMinutes                  │
   │   - targetDocketMinutes               │
   │   - jobDelta                          │
   │   - docketDelta                       │
   └─────────────────────────────────────┘
```

---

## Key Takeaways

1. **KPI Profiles are Configurable**: No hard-coded KPI values; all rules stored in database
2. **Priority-Based Resolution**: Most specific match wins (Partner + Building > Partner > Default > Global)
3. **Two Main KPIs**: Job Duration (Assigned → Completed) and Docket KPI (Completed → DocketsReceived)
4. **Integration Points**: Scheduler, SI App, Payroll, and Reporting all consume KPI results
5. **Responsibility Matrix**: Different statuses affect different KPI types (SI KPI vs Admin KPI)
6. **Evaluation Triggers**: KPI calculated when order reaches OrderCompleted or DocketsReceived
7. **Payroll Impact**: KPI results directly affect SI earnings (bonuses/penalties)

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/kpi/OVERVIEW.md` - KPI Profile Module Specification
- `docs/01_system/ORDER_LIFECYCLE.md` - Order Lifecycle and KPI Responsibility Matrix
- `docs/02_modules/payroll/OVERVIEW.md` - Payroll Integration

