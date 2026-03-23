
# PAYROLL_MODULE.md
Full Architecture Documentation — CephasOps Payroll & SI Earnings Module

---

## 1. Purpose

The Payroll Module in CephasOps is responsible for:

- Calculating **earnings for Service Installers (SI)** (in-house + subcon) based on:
  - Job type (FTTH, Modification, Assurance, FTTR, FTTC, SDU, RDF POLE)
  - Company (Cephas ISP, Cephas Trading, Kingsman, Menorah)
  - KPI performance (on-time, late, rescheduled, rework)
  - Seniority / level
- Handling **barbershop staff (Kingsman)** payroll:
  - Base salary
  - Commission per service / product
- Supporting **travel staff (Menorah)**:
  - Commissions per package / booking
- Providing **exportable payroll summaries** for external accounting / bank upload.

This module does **not** replace a full HRMS but acts as an **operations-linked earnings engine**.

---

## 2. Scope

### 2.1 In-Scope

- Define SI rates and rules per company.
- Track job completion for payroll purposes.
- Compute pay per SI, per period (e.g. monthly).
- Support fixed pay + variable pay combinations.
- Generate exports:
  - CSV / Excel for bank GIRO
  - Reports for accounting
- Support Kingsman + Menorah commission logic.

### 2.2 Out-of-Scope (v1)

- Statutory deductions (EPF, SOCSO, PCB, etc.) — can be summarized but computed externally.
- Leave management.
- Claims/reimbursements (mileage, toll, etc.) — could be added later.

---

## 3. Core Concepts

### 3.1 SI Rate Profile

Each SI has a **Rate Profile** per company:

- `ServiceInstallerId`
- `CompanyId`
- `Level` (Junior, Senior, Lead, Subcon)
- `BaseRatePerJobType`:
  - Activation FTTH
  - Modification
  - Assurance
  - FTTR / FTTC / SDU / RDF POLE
- `KpiAdjustments`:
  - Bonus for on-time or within SLA
  - Penalty for late or repeated reschedule
- `PaymentModel`:
  - Per job
  - Per day (future)
  - Hybrid

These are configured in **Settings → SI Rates & Payroll**.

### 3.2 Barbershop Staff Rate Profile (Kingsman)

- Base salary
- Commission per:
  - Service type (haircut, beard, coloring)
  - Product sold
- Team-based bonuses (future)

### 3.3 Travel Staff Rate Profile (Menorah)

- Commission per package sold
- Tiered commission by total sales

---

## 4. Data Inputs to Payroll

### 4.1 From Orders & Scheduler

For ISP:

- Completed orders (status = Order Completed, Dockets Uploaded, Ready for Invoice, etc.).
- Duration metrics:
  - **On The Way** timestamp
  - **Met Customer** timestamp
  - **Order Completed** timestamp
- KPI classification:
  - `OnTime`, `Late`, `Exceeded` (based on KPI durations you defined)
  - For example:
    - Prelaid – 1 hr
    - Non-prelaid – 2 hrs
    - FTTR / FTTC / SDU / RDF POLE – 3 hrs

### 4.2 From Docket & QA

- Verification that job was done correctly.
- Rejected jobs might:
  - Not be paid
  - Be paid partially
  - Trigger rework with adjusted rate

### 4.3 From Inventory & RMA

- Material misuse or loss may (optionally) reduce payout based on company policy (future rule).

---

## 5. Core Calculation Logic (Conceptual)

For each **completed job** within a payroll period:

1. Identify:
   - CompanyId
   - ServiceInstallerId(s)
   - Job type (Activation, Assurance, etc.)
   - KPI result (OnTime / Late / Rework)
   - Partner (TIME, Celcom, etc.)

2. Look up SI Rate Profile for:
   - That company
   - That SI
   - That job type

3. Calculate **BasePay**:
   - `BasePay = RatePerJobType`

4. Apply KPI adjustments:

   - If OnTime:
     - `Pay = BasePay + KpiBonus` (if configured)
   - If Late:
     - `Pay = BasePay - LatePenalty` (if configured)
   - If Rework:
     - `Pay = ReworkRate` or `0` depending on rules.

5. Aggregate per SI per period:

   - `TotalPay = Sum(Pay for all jobs in period)`

6. Produce **Payroll Summary**.

---

## 6. Data Model (Conceptual)

### 6.1 SiRateProfile

- `Id`
- `ServiceInstallerId`
- `CompanyId`
- `Level` (Junior / Senior / Subcon)
- `ActivationRate`
- `ModificationRate`
- `AssuranceRate`
- `FttrRate`
- `FttcRate`
- `SduRate`
- `RdfPoleRate`
- `OnTimeBonus`
- `LatePenalty`
- `ReworkRate`
- `IsActive`

### 6.2 JobEarningRecord

- `Id`
- `OrderId`
- `CompanyId`
- `ServiceInstallerId`
- `JobType`
- `KpiResult`
- `BaseRate`
- `KpiAdjustment`
- `FinalPay`
- `Period` (e.g. `2025-11`)
- `Status` (Draft / Confirmed / Paid)
- `CreatedAt`
- `ConfirmedAt`
- `PaidAt`

### 6.3 PayrollRun

Represents a batch run:

- `Id`
- `CompanyId`
- `PeriodStart`
- `PeriodEnd`
- `CreatedBy`
- `CreatedAt`
- `Status` (Draft / Final / Exported / Paid)
- `TotalAmount`
- `Notes`

### 6.4 PayrollLine

Per SI:

- `Id`
- `PayrollRunId`
- `ServiceInstallerId`
- `TotalJobs`
- `TotalPay`
- `Adjustments`
- `NetPay`
- `ExportReference` (e.g. GIRO batch ID)

---

## 7. Payroll Process Flow

### 7.1 For Cephas ISP / Cephas Trading

1. At month-end, Admin selects:
   - Company: Cephas ISP
   - Period: 1–31 Nov 2025

2. System fetches:
   - All completed orders in that period.
   - Related SI assignments.
   - KPI results per job.

3. System generates **JobEarningRecords** per SI.

4. Admin reviews:
   - Can exclude problematic jobs.
   - Can adjust pay for specific jobs (with reason).

5. System creates a **PayrollRun**:
   - One row per SI in `PayrollLine`.

6. Admin exports:
   - CSV for bank upload
   - PDF summary for management

7. When payment completed:
   - Admin marks PayrollRun as Paid.

### 7.2 For Kingsman (Barbershop)

- Use:
  - POS records (services + products)
  - BarberStaffProfile (base + commission).
- Generate:
  - Commission per barber.
  - Summary per period.

---

## 8. Settings → SI Rates & Payroll

You requested this under Master Settings; this module links to that area.

In Settings:

- Define SI rate profiles
- Define KPI rules (already in Workflow & KPIs)
- Define per-company payroll rules:
  - Round to nearest RM
  - Minimum pay thresholds
  - Whether late jobs get paid partially or fully

---

## 9. Integration With Other Modules

### 9.1 Orders & Scheduler

- Provides job completion data + timestamps
- Provides SI assignments

### 9.2 Inventory

- Can optionally influence deductions for lost/damaged material (future behaviour).

### 9.3 Billing & Finance

- Linking earnings vs revenue for SI productivity analysis.
- P&L will compare:
  - Revenue per SI vs labour cost per SI.

### 9.4 P&L Module

- Labour cost per SI and per job flows into:
  - `DirectLabourCost`
  - P&L, profitability per partner, etc.

---

## 10. Exports & Reporting

### 10.1 Payroll Export (Bank / Accounting)

- CSV fields:
  - Name
  - Bank account
  - Amount
  - Reference
  - Period

### 10.2 SI Statement

Per SI:

- Jobs done
- KPI compliance
- Pay breakdown per job type
- Total net pay

### 10.3 Management Report

- Labour cost per company
- Labour cost per partner (TIME vs others)
- Average earning per job type
- Outlier SIs (very high/low productivity)

---

## 11. API Endpoints (Documentation Only)

Base path: `/api/payroll`

- `POST /api/payroll/run`
  - Create payroll run for given company & period.

- `GET /api/payroll/run/{id}`
  - View payroll run summary.

- `GET /api/payroll/run/{id}/lines`
  - See per-SI lines.

- `POST /api/payroll/run/{id}/export`
  - Generate export file.

- `POST /api/payroll/run/{id}/mark-paid`
  - Mark as paid.

- `GET /api/payroll/si/{siId}/earnings?period=YYYY-MM`
  - SI can view their earnings.

---

## 12. Security & RBAC

- Payroll data is highly sensitive.
- Only roles with:
  - `payroll.view`, `payroll.compute`, `payroll.export`
  - Should access this module.
- SIs can only access **their own** earnings via SI App.

---

## 13. Summary

The Payroll Module ties operations to payments:

- SIs (in-house + subcon) are paid based on:
  - Completed jobs
  - KPI performance
  - Job type & company-specific rates.

- Barbers (Kingsman) and travel staff (Menorah) are paid based on:
  - Commissions + base salaries.

- P&L uses the payroll output for labour cost.

This provides a **single operational source of truth** for who earned what, for which jobs, in which company, and in which period.
