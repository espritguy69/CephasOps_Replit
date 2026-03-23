
# PNL_MODULE.md
CephasOps Profit & Loss (P&L) & Operational Performance Analytics – Full Architecture

---

## 1. Purpose

The P&L Module gives you **one place** to see:

- Are we making or losing money?
- By **company** (Cephas, Cephas Trading, Kingsman, Menorah).
- By **partner** (TIME, Celcom, Digi, U-Mobile, etc.).
- By **vertical** (ISP, Barbershop, Travel).
- By **cost centre** (e.g. ISP Ops, Warehouse, Admin, Marketing).
- By **order type** (Activation, Assurance, Relocation, FTTR/FTTC/SDU/RDF POLE).

It stitches together:

- **Revenue** from invoices (Billing & Finance).
- **Direct costs**:
  - Materials (from Inventory & RMA).
  - SI job payments (from Payroll & SI rates).
- **Indirect costs**:
  - Fixed overheads (e.g. rentals, salaries).
  - Variable overheads (e.g. mileage, toll, marketing).
- **KPIs** based on operational performance (duration vs KPI, reschedules, rework).

> This is an **analytics/reporting module**, not an accounting system.  
> It helps you decide: *Which work is truly profitable?*  
> Full GL/accounting still lives in your external accounting software.

---

## 2. Scope

### 2.1 In-Scope

- Aggregated P&L by:
  - Company
  - Partner
  - Vertical
  - Cost centre
  - Time period (month, quarter, year)
- Per-order profitability:
  - Revenue for that job
  - Materials cost
  - SI labour cost
- Drill-down views:
  - Partner profitability (e.g. TIME vs Celcom jobs).
  - Building or site profitability (optional).
- Reconciliation support:
  - Compare CephasOps revenue with external accounting exports.

### 2.2 Out-of-Scope (v1)

- Double-entry bookkeeping.
- Official statutory financial statements (SST returns, audited reports).
- Multi-currency revaluation or complex financial instruments.

---

## 3. P&L Model – High-Level

Conceptually, P&L is:

> **Profit = Revenue − DirectCosts − IndirectCosts**

Where:

- **Revenue**:
  - From **Invoices** (Billing module).
- **DirectCosts**:
  - Materials used in jobs (Inventory).
  - SI payments per job (Payroll).
- **IndirectCosts**:
  - Fixed & variable overhead allocations imported or configured.

We treat the P&L module as a **data warehouse** style aggregator that reads from:

- Billing & Finance
- Inventory & RMA
- Payroll
- Settings (Cost Centres, Companies, Verticals)

---

## 4. Core Concepts & Dimensions

### 4.1 Dimensions

Key “dimensions” for slicing P&L:

1. **Company**
   - Cephas Sdn. Bhd (ISP)
   - Cephas Trading & Services (ISP)
   - Kingsman Classic Services (Barbershop/Spa)
   - Menorah Travel & Tours (Travel)

2. **Partner / Customer Group**
   - TIME
   - Celcom
   - Digi
   - U-Mobile
   - Walk-in customers (Kingsman)
   - Tour agencies / corporate clients (Menorah)

3. **Vertical**
   - ISP
   - Barbershop
   - Travel

4. **Cost Centre**
   - ISP Operations
   - Warehouse & Logistics
   - Admin & HR
   - Sales & Marketing
   - Barbershop Branches
   - Travel Operations

5. **Period**
   - Month (e.g. 2025-01)
   - Quarter (e.g. 2025-Q1)
   - Year (e.g. FY2025)

6. **Order / Job Type (for ISP)**
   - Activation
   - Modification / Relocation
   - Assurance
   - FTTR / FTTC / SDU / RDF POLE

7. **Company Branch / Location**
   - PJ Office
   - Warehouse
   - Kingsman branch (e.g. SS15, Bahau, etc.)
   - Travel office

Not all dimensions are mandatory initially, but the model is future-proof to support them.

---

## 5. Data Sources

### 5.1 Revenue Source – Billing Module

From **Invoice** and **InvoiceLine**:

- `InvoiceTotal = TotalAmount`
- Mapping to:
  - CompanyId
  - PartnerId
  - CostCentreId (if set per line)
  - Period (IssueDate or DueDate; configurable)

Revenue per invoice line is aggregated by:

- Company
- Partner
- Cost centre
- Period
- OrderType (via linked Order)

### 5.2 Direct Costs – Inventory

From **Inventory & RMA**:

- Each order has material usage:
  - Sum of `DefaultCost` × quantity for non-serial items used.
  - For serialised items:
    - Use `DefaultCost` or assigned cost when received (GRN).
- Cost is tracked at:
  - `OrderId`
  - `CompanyId`
  - `PartnerId`
- These feed **CostOfGoodsSold (COGS)** for ISP operations.

### 5.3 Direct Costs – Payroll (SI)

From Payroll & SI rates module:

- Each job (order) has:
  - SI(s) assigned.
  - KPI performance (on-time, late, rework).
  - SI rate plan:
    - Different rates per order type (FTTH, Assurance, FTTR, etc.).
    - Seniority level (e.g. junior, senior subcon).
- For each order:
  - LabourCost = Sum of SI payments attributed to that job.

This becomes part of DirectCosts.

### 5.4 Indirect Costs

Indirect costs can be loaded as:

- **Manual entries** per period, per cost centre:
  - Rent, utilities, salaries, etc.
- Or **imports** from external accounting system (CSV/Excel).

We can store them as **OverheadEntry** records:

- `CompanyId`
- `CostCentreId`
- `Period`
- `Amount`
- `Description`
- Optional:
  - `AllocationBasis` (e.g. percentage to ISP vs Barbershop).

---

## 6. Data Model (Conceptual)

### 6.1 PnlFact

A fact table that stores aggregated values per “slice” (e.g., per month, company, partner, cost centre).

- `Id`
- `CompanyId`
- `PartnerId` (nullable)
- `VerticalId` (nullable)
- `CostCentreId` (nullable)
- `Period` (e.g. `2025-01`)
- `OrderType` (nullable – Activation, Assurance, etc.)
- `RevenueAmount`
- `DirectMaterialCost`
- `DirectLabourCost`
- `IndirectCost`
- `GrossProfit` (Revenue − DirectMaterialCost − DirectLabourCost)
- `NetProfit` (GrossProfit − IndirectCost)
- `JobsCount`
- `OrdersCompletedCount`
- `ReschedulesCount`
- `AssuranceJobsCount` (for ISP)
- `CreatedAt`
- `LastRecalculatedAt`

### 6.2 PnlDetailPerOrder (Optional Detailed View)

For more granular analysis, we keep details by order:

- `Id`
- `OrderId`
- `CompanyId`
- `PartnerId`
- `Period`
- `OrderType`
- `RevenueAmount`
- `MaterialCost`
- `LabourCost`
- `OverheadAllocated`
- `ProfitForOrder`
- `KpiResult` (OnTime / Late / Exceeded / Rework)
- `RescheduledCount`

This feeds drill-down UI for detailed forensics.

### 6.3 OverheadEntry

- `Id`
- `CompanyId`
- `CostCentreId`
- `VerticalId` (optional)
- `Period`
- `Amount`
- `Description`
- `Source` (Manual / Imported / System)
- `CreatedAt`
- `CreatedByUserId`

---

## 7. KPIs & Dashboard Metrics

The P&L module feeds and consumes KPIs.

### 7.1 Financial KPIs

- Revenue by:
  - Company
  - Partner
  - Vertical
  - Order type
- Gross margin:
  - `GrossMargin% = GrossProfit / Revenue * 100`
- Net margin:
  - `NetMargin% = NetProfit / Revenue * 100`
- Cost breakdown:
  - Material vs labour vs overhead
- Revenue per SI
- Profit per SI (based on aggregated labour vs revenue of jobs they handled)

### 7.2 Operational KPIs (from Scheduler & Orders)

- Average job duration per order type.
- On-time vs late jobs (vs KPI thresholds).
- Jobs with multiple reschedules.
- Rate of Assurance jobs vs Activation jobs.

These can be visualised alongside financial results to show:

- Where operational issues are causing financial pain.

---

## 8. Calculation Flow

### 8.1 Periodic Rebuild

At end of each day or on demand:

1. Pull all invoices for the period.
2. Pull materials cost per order.
3. Pull labour cost per order (Payroll).
4. Pull overhead entries per period.
5. Aggregate into:
   - PnlDetailPerOrder
   - PnlFact (monthly/periodic summaries).

### 8.2 Allocation of Overheads

Two main strategies (configurable per company):

1. **Simple per-company**:
   - All overhead is allocated to the company as a whole.
   - This is easiest but does not split by partner or order type.

2. **By basis** (future enhancement):
   - Allocate overhead to:
     - Partners
     - Verticals
     - Cost centres
   - Based on:
     - Revenue proportion
     - Number of jobs
     - Other configured weights.

---

## 9. API Contracts (Docs Only)

**Base path:** `/api/pnl`

### 9.1 Get Summary P&L

- `GET /api/pnl/summary`
- Query:
  - `companyId`
  - `fromPeriod`
  - `toPeriod`
  - Optional filters: `partnerId`, `verticalId`, `costCentreId`
- Response:
  - List of `PnlFactDto` with aggregated numbers.

### 9.2 Get P&L Per Order

- `GET /api/pnl/orders`
- Query:
  - `companyId`
  - `period`
  - Optional: `partnerId`, `orderType`, `serviceInstallerId`
- Response:
  - List of `PnlOrderDetailDto` with:
    - Revenue
    - Material cost
    - Labour cost
    - OverheadAllocated
    - ProfitForOrder.

### 9.3 Trigger Recalculation

- `POST /api/pnl/recalculate`
- Body:
  - `companyId`
  - `fromPeriod`
  - `toPeriod`
- Effect:
  - Rebuild the PnlFact & PnlDetailPerOrder for the specified range.

### 9.4 Manage Overhead Entries

- `GET /api/pnl/overheads`
  - Query: `companyId`, `period`, `costCentreId`
- `POST /api/pnl/overheads`
  - Create overhead entry.
- `PUT /api/pnl/overheads/{id}`
  - Update overhead entry.
- `DELETE /api/pnl/overheads/{id}`
  - Remove entry (if not locked).

---

## 10. UI / Dashboard Ideas

The P&L app section could provide:

1. **Company Overview Page**
   - Cards with:
     - Total Revenue
     - Gross Profit
     - Net Profit
     - Number of completed jobs
   - Trend chart (Revenue & Profit over months).

2. **Partner Profitability Page**
   - Table of partners:
     - Revenue
     - Direct cost
     - Gross margin %
     - Net margin %
   - Drilldown into per-partner orders.

3. **Service Installer Performance (linked to SI Dashboard)**
   - Revenue vs labour cost per SI.
   - Job count, reschedules, assurance vs activation mix.
   - Profit contribution per SI (for in-house and subcon).

4. **Vertical Comparison**
   - ISP vs Barbershop vs Travel:
     - Revenue
     - Profit
     - Margin.

5. **Cost Centre View**
   - Overhead by cost centre.
   - Allocation impact on net profit.

---

## 11. Multi-Company Handling

- P&L always filtered by `companyId` by default.
- A “Global Director” role can consolidate across:

  - Cephas Sdn. Bhd
  - Cephas Trading & Services
  - Kingsman Classic Services
  - Menorah Travel

Using a:

- `GroupPnlSummary` view for cross-company analysis.

---

## 12. Error Handling & Validation

Examples:

- Missing cost data:
  - If an order has revenue but no material cost or labour cost,
    - Show as incomplete in a “Data Quality” section.
- Negative gross profit:
  - Highlight in UI so you can identify loss-making jobs or partners.
- Overhead not matching external accounting:
  - Provide notes for partial reconciliation and manual adjustments.

---

## 13. Notes for Cursor / Dev Implementation

- P&L computations may be heavy:
  - Consider background jobs (e.g. Hangfire, worker service) for recalculation.
- Ensure idempotent recalculation:
  - Rebuilding for a period replaces older values consistently.
- Do not use P&L tables as a core source of truth for live operations:
  - They are **derived data** from Billing, Inventory, Payroll, and Settings.
- Strong audit log for:
  - Overhead changes
  - Manual adjustments
  - Recalculation operations.

The design ensures you can eventually see **everything in one place** instead of jumping between Excel files, bank statements, and portals.
