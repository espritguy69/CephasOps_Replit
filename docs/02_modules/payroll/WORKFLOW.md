# Payroll – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Payroll module, covering period creation, payroll run calculation, rate resolution, KPI adjustments, and payment processing

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         PAYROLL MODULE SYSTEM                            │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   PAYROLL PERIODS      │      │   PAYROLL RUNS         │
        │  (Time Periods)        │      │  (Calculation Runs)     │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Period (YYYY-MM)     │      │ • Period Start/End    │
        │ • Period Start Date    │      │ • Status (Draft/Final/Paid)│
        │ • Period End Date      │      │ • Total Amount        │
        │ • Status               │      │ • Payroll Lines       │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   JOB EARNING RECORDS  │      │   SI RATE PLANS         │
        │  (Per-Order Earnings)  │      │  (Bonus/Penalty Rates) │
        └───────────────────────┘      └───────────────────────┘
```

---

## Complete Workflow: Payroll Calculation

```
[STEP 1: CREATE PAYROLL PERIOD]
         |
         v
┌────────────────────────────────────────┐
│ CREATE PAYROLL PERIOD                    │
│ POST /api/payroll/periods                │
└────────────────────────────────────────┘
         |
         v
CreatePayrollPeriodDto {
  Period: "2025-12"
  PeriodStart: 2025-12-01
  PeriodEnd: 2025-12-31
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE PERIOD                          │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |         |
   |         v
   |    [Reject Creation]
   |    [Error: Period already exists]
   |
   v
Checks:
  ✓ Period format: YYYY-MM
  ✓ PeriodStart < PeriodEnd
  ✓ Period not already exists
         |
         v
┌────────────────────────────────────────┐
│ CREATE PAYROLL PERIOD                    │
└────────────────────────────────────────┘
         |
         v
PayrollPeriod {
  Id: "period-123"
  CompanyId: Cephas
  Period: "2025-12"
  PeriodStart: 2025-12-01
  PeriodEnd: 2025-12-31
  Status: "Draft"
  IsLocked: false
  CreatedByUserId: "admin-123"
}
         |
         v
[STEP 2: CREATE PAYROLL RUN]
         |
         v
┌────────────────────────────────────────┐
│ CREATE PAYROLL RUN                       │
│ POST /api/payroll/runs                   │
└────────────────────────────────────────┘
         |
         v
CreatePayrollRunDto {
  PayrollPeriodId: "period-123"
  PeriodStart: 2025-12-01
  PeriodEnd: 2025-12-31
  Notes: "December 2025 payroll"
}
         |
         v
┌────────────────────────────────────────┐
│ CREATE PAYROLL RUN RECORD                 │
└────────────────────────────────────────┘
         |
         v
PayrollRun {
  Id: "run-456"
  CompanyId: Cephas
  PayrollPeriodId: "period-123"
  PeriodStart: 2025-12-01
  PeriodEnd: 2025-12-31
  Status: "Draft"
  TotalAmount: 0
  Notes: "December 2025 payroll"
}
         |
         v
[STEP 3: FIND COMPLETED ORDERS]
         |
         v
┌────────────────────────────────────────┐
│ QUERY COMPLETED ORDERS                   │
└────────────────────────────────────────┘
         |
         v
Order.find(
  CompanyId = Cephas
  Status IN ["Completed", "DocketsReceived", "DocketsUploaded"]
  AppointmentDate BETWEEN PeriodStart AND PeriodEnd
  AssignedSiId IS NOT NULL
  PayrollPeriodId IS NULL (not yet processed)
)
         |
         v
[Orders Found: 150 orders]
         |
         v
[STEP 4: GROUP ORDERS BY SI]
         |
         v
[Group Orders by AssignedSiId]
  SI-123: [order-1, order-2, order-3, ...] (25 orders)
  SI-456: [order-4, order-5, ...] (30 orders)
  SI-789: [order-6, ...] (20 orders)
  ...
         |
         v
[STEP 5: CALCULATE EARNINGS FOR EACH ORDER]
         |
         v
[For each Order]
  Order {
    Id: "order-1"
    PartnerId: TIME
    OrderTypeId: ACTIVATION
    AssignedSiId: "SI-123"
    AppointmentDate: 2025-12-15
  }
         |
         v
┌────────────────────────────────────────┐
│ RESOLVE PARTNER GROUP                    │
└────────────────────────────────────────┘
         |
         v
Partner.find(Id = TIME)
  Partner {
    GroupId: "partner-group-1"
  }
         |
         v
┌────────────────────────────────────────┐
│ RESOLVE RATE USING RATE ENGINE            │
│ RateEngineService.ResolveGponRates()      │
└────────────────────────────────────────┘
         |
         v
GponRateResolutionRequest {
  OrderTypeId: ACTIVATION
  InstallationTypeId: PRELAID
  InstallationMethodId: null
  PartnerGroupId: "partner-group-1"
  PartnerId: TIME
  ServiceInstallerId: "SI-123"
  SiLevel: "Senior"
  ReferenceDate: 2025-12-15
}
         |
         v
[Rate Resolution Priority]
  1. Custom Rate (SI-specific override)
  2. Payout Rate (SI level-based)
  3. Revenue Rate (fallback)
         |
         v
RateResult {
  PayoutAmount: RM 100.00
  PayoutSource: "SiRatePlan"
  PayoutRateId: "rate-123"
}
         |
         v
[Base Payout Amount: RM 100.00]
         |
         v
┌────────────────────────────────────────┐
│ EVALUATE KPI FOR ORDER                    │
│ KpiProfileService.EvaluateOrder()         │
└────────────────────────────────────────┘
         |
         v
KpiEvaluation {
  KpiResult: "OnTime" | "Late" | "ExceededSla"
  KpiScore: 95
  EvaluationDate: 2025-12-15
}
         |
         v
[KPI Result: "OnTime"]
         |
         v
┌────────────────────────────────────────┐
│ GET SI RATE PLAN                          │
└────────────────────────────────────────┘
         |
         v
SiRatePlan.find(
  ServiceInstallerId = "SI-123"
  IsActive = true
  EffectiveFrom <= PeriodEnd
  EffectiveTo >= PeriodStart
)
         |
         v
SiRatePlan {
  OnTimeBonus: RM 10.00
  LatePenalty: RM 5.00
}
         |
         v
┌────────────────────────────────────────┐
│ CALCULATE KPI ADJUSTMENT                  │
└────────────────────────────────────────┘
         |
         v
[Based on KPI Result]
         |
    ┌────┴────┐
    |         |
    v         v
[ONTIME] [LATE/EXCEEDED]
   |            |
   |            v
   |       KpiAdjustment = -LatePenalty
   |       = -RM 5.00
   |
   v
KpiAdjustment = OnTimeBonus
= RM 10.00
         |
         v
┌────────────────────────────────────────┐
│ CALCULATE FINAL PAY                       │
└────────────────────────────────────────┘
         |
         v
FinalPay = BasePayoutAmount + KpiAdjustment
         = RM 100.00 + RM 10.00
         = RM 110.00
         |
         v
┌────────────────────────────────────────┐
│ CREATE JOB EARNING RECORD                 │
└────────────────────────────────────────┘
         |
         v
JobEarningRecord {
  Id: "earning-789"
  CompanyId: Cephas
  OrderId: "order-1"
  ServiceInstallerId: "SI-123"
  PayrollRunId: "run-456"
  OrderTypeId: ACTIVATION
  OrderTypeCode: "ACTIVATION"
  OrderTypeName: "Activation"
  KpiResult: "OnTime"
  BaseRate: RM 100.00
  KpiAdjustment: RM 10.00
  FinalPay: RM 110.00
  Period: "2025-12"
  Status: "Pending"
  RateSource: "SiRatePlan"
  RateId: "rate-123"
}
         |
         v
[Mark Order as Processed]
  Order {
    PayrollPeriodId: "period-123"
  }
         |
         v
[Repeat for all Orders]
         |
         v
[STEP 6: CREATE PAYROLL LINES]
         |
         v
[For each SI Group]
         |
         v
[Calculate SI Totals]
  SI-123:
    TotalJobs: 25
    TotalPay: RM 2,500.00 (base, without adjustments)
    Adjustments: RM 250.00 (KPI bonuses - penalties)
    NetPay: RM 2,750.00 (final including adjustments)
         |
         v
┌────────────────────────────────────────┐
│ CREATE PAYROLL LINE                       │
└────────────────────────────────────────┘
         |
         v
PayrollLine {
  Id: "line-123"
  PayrollRunId: "run-456"
  ServiceInstallerId: "SI-123"
  TotalJobs: 25
  TotalPay: RM 2,500.00
  Adjustments: RM 250.00
  NetPay: RM 2,750.00
}
         |
         v
[Repeat for all SIs]
         |
         v
[STEP 7: CALCULATE TOTAL RUN AMOUNT]
         |
         v
TotalRunAmount = Sum(All PayrollLines.NetPay)
         = RM 15,000.00
         |
         v
[Update Payroll Run]
  PayrollRun {
    TotalAmount: RM 15,000.00
  }
         |
         v
[Save All Records]
         |
         v
[STEP 8: FINALIZE PAYROLL RUN]
         |
         v
┌────────────────────────────────────────┐
│ FINALIZE PAYROLL RUN                     │
│ POST /api/payroll/runs/{id}/finalize     │
└────────────────────────────────────────┘
         |
         v
[Validate Status]
  ✓ Status must be "Draft"
         |
         v
[Update Status]
  PayrollRun {
    Status: "Final"
    FinalizedAt: 2025-12-31
  }
         |
         v
[STEP 9: MARK AS PAID]
         |
         v
┌────────────────────────────────────────┐
│ MARK PAYROLL RUN AS PAID                  │
│ POST /api/payroll/runs/{id}/mark-paid     │
└────────────────────────────────────────┘
         |
         v
[Update Status]
  PayrollRun {
    Status: "Paid"
    PaidAt: 2026-01-05
  }
         |
         v
[Payroll Run Complete]
```

---

## Rate Resolution Flow

```
[For Each Order in Payroll Run]
         |
         v
┌────────────────────────────────────────┐
│ RESOLVE RATE USING RATE ENGINE            │
│ RateEngineService.ResolveGponRates()      │
└────────────────────────────────────────┘
         |
         v
[Priority 1: Custom Rate]
  SiRatePlan.find(
    ServiceInstallerId = "SI-123"
    OrderTypeId = ACTIVATION
    IsActive = true
    EffectiveFrom <= ReferenceDate
    EffectiveTo >= ReferenceDate
  )
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Priority 2: Payout Rate]
   |           PayoutRate.find(
   |             PartnerGroupId = "partner-group-1"
   |             OrderTypeId = ACTIVATION
   |             SiLevel = "Senior"
   |             IsActive = true
   |           )
   |            |
   |       ┌────┴────┐
   |       |         |
   |       v         v
   |   [FOUND] [NOT FOUND]
   |       |            |
   |       |            v
   |       |       [Priority 3: Revenue Rate]
   |       |           RevenueRate.find(
   |       |             PartnerGroupId = "partner-group-1"
   |       |             OrderTypeId = ACTIVATION
   |       |             IsActive = true
   |       |           )
   |       |            |
   |       |       ┌────┴────┐
   |       |       |         |
   |       |       v         v
   |       |   [FOUND] [NO RATE]
   |       |       |            |
   |       |       |            v
   |       |       |       [Error: No rate found]
   |       |       |       [Use default or skip order]
   |       |       |
   |       └───────┴────────────┘
   |               |
   └───────────────┘
         |
         v
[Rate Resolved]
  RateResult {
    PayoutAmount: RM 100.00
    PayoutSource: "SiRatePlan" | "PayoutRate" | "RevenueRate"
    PayoutRateId: "rate-123"
  }
```

---

## KPI Adjustment Flow

```
[For Each Order]
         |
         v
┌────────────────────────────────────────┐
│ EVALUATE KPI FOR ORDER                    │
│ KpiProfileService.EvaluateOrder()         │
└────────────────────────────────────────┘
         |
         v
[KPI Evaluation Logic]
  - Check job completion time vs SLA
  - Check quality metrics
  - Check customer satisfaction
  - Calculate KPI score
         |
         v
KpiEvaluation {
  KpiResult: "OnTime" | "Late" | "ExceededSla"
  KpiScore: 95
  EvaluationDate: 2025-12-15
}
         |
         v
┌────────────────────────────────────────┐
│ GET SI RATE PLAN                          │
└────────────────────────────────────────┘
         |
         v
SiRatePlan {
  OnTimeBonus: RM 10.00
  LatePenalty: RM 5.00
  ExceededSlaPenalty: RM 10.00
}
         |
         v
[Calculate Adjustment Based on KPI Result]
         |
    ┌────┴────┐
    |         |
    v         v
[ONTIME] [LATE]
   |         |
   |         v
   |    KpiAdjustment = -LatePenalty
   |    = -RM 5.00
   |
   v
KpiAdjustment = OnTimeBonus
= RM 10.00
         |
         v
[If ExceededSla]
         |
         v
KpiAdjustment = -ExceededSlaPenalty
= -RM 10.00
         |
         v
[Final Pay Calculation]
  FinalPay = BaseRate + KpiAdjustment
         |
         v
[Example]
  BaseRate: RM 100.00
  KpiResult: "OnTime"
  KpiAdjustment: RM 10.00
  FinalPay: RM 110.00
```

---

## Entities Involved

### PayrollPeriod Entity
```
PayrollPeriod
├── Id (Guid)
├── CompanyId (Guid)
├── Period (string: YYYY-MM)
├── PeriodStart (DateTime)
├── PeriodEnd (DateTime)
├── Status (string: Draft, Final, Locked)
├── IsLocked (bool)
└── CreatedAt, UpdatedAt
```

### PayrollRun Entity
```
PayrollRun
├── Id (Guid)
├── CompanyId (Guid)
├── PayrollPeriodId (Guid)
├── PeriodStart (DateTime)
├── PeriodEnd (DateTime)
├── Status (string: Draft, Final, Paid)
├── TotalAmount (decimal)
├── Notes (string?)
├── FinalizedAt (DateTime?)
├── PaidAt (DateTime?)
└── CreatedAt, UpdatedAt
```

### PayrollLine Entity
```
PayrollLine
├── Id (Guid)
├── PayrollRunId (Guid)
├── ServiceInstallerId (Guid)
├── TotalJobs (int)
├── TotalPay (decimal)
├── Adjustments (decimal)
├── NetPay (decimal)
└── CreatedAt
```

### JobEarningRecord Entity
```
JobEarningRecord
├── Id (Guid)
├── CompanyId (Guid)
├── OrderId (Guid)
├── ServiceInstallerId (Guid)
├── PayrollRunId (Guid)
├── OrderTypeId (Guid)
├── OrderTypeCode (string)
├── OrderTypeName (string)
├── KpiResult (string: OnTime, Late, ExceededSla)
├── BaseRate (decimal)
├── KpiAdjustment (decimal)
├── FinalPay (decimal)
├── Period (string: YYYY-MM)
├── Status (string: Pending, Confirmed, Paid)
├── RateSource (string)
├── RateId (Guid?)
├── ConfirmedAt (DateTime?)
└── PaidAt (DateTime?)
```

### SiRatePlan Entity
```
SiRatePlan
├── Id (Guid)
├── CompanyId (Guid)
├── DepartmentId (Guid?)
├── ServiceInstallerId (Guid)
├── InstallationMethodId (Guid?)
├── RateType (string)
├── Level (string)
├── PrelaidRate (decimal?)
├── NonPrelaidRate (decimal?)
├── SduRate (decimal?)
├── RdfPoleRate (decimal?)
├── ActivationRate (decimal?)
├── ModificationRate (decimal?)
├── AssuranceRate (decimal?)
├── AssuranceRepullRate (decimal?)
├── FttrRate (decimal?)
├── FttcRate (decimal?)
├── OnTimeBonus (decimal?)
├── LatePenalty (decimal?)
├── ReworkRate (decimal?)
├── IsActive (bool)
├── EffectiveFrom (DateTime?)
└── EffectiveTo (DateTime?)
```

---

## API Endpoints Involved

### Payroll Periods
- `GET /api/payroll/periods` - List payroll periods
- `GET /api/payroll/periods/{id}` - Get period details
- `POST /api/payroll/periods` - Create payroll period
- `PUT /api/payroll/periods/{id}` - Update payroll period

### Payroll Runs
- `GET /api/payroll/runs` - List payroll runs
- `GET /api/payroll/runs/{id}` - Get run details
- `POST /api/payroll/runs` - Create payroll run (calculates earnings)
- `POST /api/payroll/runs/{id}/finalize` - Finalize payroll run
- `POST /api/payroll/runs/{id}/mark-paid` - Mark run as paid

### Job Earning Records
- `GET /api/payroll/earnings` - Get job earning records
- `GET /api/payroll/earnings/{id}` - Get earning record details

### SI Rate Plans
- `GET /api/payroll/si-rate-plans` - List SI rate plans
- `GET /api/payroll/si-rate-plans/{id}` - Get rate plan details
- `POST /api/payroll/si-rate-plans` - Create SI rate plan
- `PUT /api/payroll/si-rate-plans/{id}` - Update SI rate plan
- `DELETE /api/payroll/si-rate-plans/{id}` - Delete SI rate plan

---

## Module Rules & Validations

### Payroll Period Rules
- Period format must be YYYY-MM
- PeriodStart must be < PeriodEnd
- Period must be unique per company
- Cannot create overlapping periods

### Payroll Run Rules
- Only orders in Completed/DocketsReceived/DocketsUploaded status are processed
- Orders must have AssignedSiId
- Orders must not have PayrollPeriodId (not yet processed)
- AppointmentDate must be within PeriodStart and PeriodEnd
- Run status: Draft → Final → Paid

### Rate Resolution Rules
- Priority: Custom Rate → Payout Rate → Revenue Rate
- Rate must be active and effective on reference date
- SI level considered for payout rate resolution
- Partner group considered for rate lookup

### KPI Adjustment Rules
- KPI evaluation performed per order
- KPI result: OnTime, Late, or ExceededSla
- OnTime: Apply OnTimeBonus (positive adjustment)
- Late: Apply LatePenalty (negative adjustment)
- ExceededSla: Apply ExceededSlaPenalty (negative adjustment)
- Adjustments from SiRatePlan

### Job Earning Record Rules
- One record per order per payroll run
- BaseRate from rate resolution
- KpiAdjustment from KPI evaluation
- FinalPay = BaseRate + KpiAdjustment
- Status: Pending → Confirmed → Paid

### Payroll Line Rules
- One line per SI per payroll run
- TotalJobs = count of orders for SI
- TotalPay = sum of base rates (without adjustments)
- Adjustments = sum of KPI adjustments
- NetPay = TotalPay + Adjustments

### Finalization Rules
- Only Draft runs can be finalized
- Finalized runs cannot be modified
- Finalization sets Status to "Final"

### Payment Rules
- Only Final runs can be marked as paid
- Payment sets Status to "Paid"
- PaidAt timestamp recorded

---

## Integration Points

### Orders Module
- Completed orders queried for payroll calculation
- Orders marked with PayrollPeriodId after processing
- Order details (PartnerId, OrderTypeId, AssignedSiId) used for rate resolution

### Rate Engine Module
- RateEngineService.ResolveGponRates() used for rate resolution
- Supports Custom Rate, Payout Rate, and Revenue Rate
- Rate source and ID tracked in JobEarningRecord

### KPI Module
- KpiProfileService.EvaluateOrder() used for KPI evaluation
- KPI result determines adjustment (bonus or penalty)
- KPI score and evaluation date tracked

### Service Installers Module
- SI details (SiLevel) used for rate resolution
- SI rate plans provide bonus/penalty amounts
- SI capacity and availability not directly used in payroll

### PNL Module
- Payroll costs feed into PNL calculations
- JobEarningRecord data used for labor cost allocation
- Cost center allocation from department

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/payroll/OVERVIEW.md` - Payroll module overview
- `docs/02_modules/rate_engine/RATE_ENGINE.md` - Rate engine specification
- `docs/02_modules/kpi/KPI_SYSTEM_FLOW.md` - KPI system flow

