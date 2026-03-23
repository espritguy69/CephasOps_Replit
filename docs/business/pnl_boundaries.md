# P&L Analytics Boundaries

**Related:** [Product overview](../overview/product_overview.md) | [Billing & MyInvois](billing_myinvois_flow.md) | [Payroll & rate overview](payroll_rate_overview.md) | [02_modules/pnl/OVERVIEW](../02_modules/pnl/OVERVIEW.md)

**Source of truth:** Codebase Summary (Senior Architect Review); Business Processes (Business Systems Analyst Report).

---

## 1. What P&L is

- **Analytics/reporting** to answer: “Are we making or losing money?” and “Which work is profitable?”
- **Not** the official accounting system. Full **GL (general ledger)** and statutory reporting remain in **external accounting software**.
- P&L **stitches together:** revenue from invoices; direct costs (materials, SI job payments); indirect costs (overheads); and optional KPIs (duration, reschedules, rework).

---

## 2. Dimensions

- By **company** (single-company in current deployment; multi-company in spec is reference/future).
- By **partner** (e.g. TIME, Celcom, Digi, U Mobile).
- By **order type**, **cost centre**, **time period** (month, quarter, year).
- **Per-order profitability:** revenue for that job − materials cost − SI labour cost.
- **Drill-down:** e.g. partner profitability, building/site (optional).

---

## 3. Data sources

- **Revenue** – From **invoices** (Billing).
- **Direct costs** – **Materials** used on jobs (Inventory/ledger); **SI payments** per job (Payroll/rates).
- **Indirect costs** – **Overheads** (fixed/variable) entered or imported; no full GL in CephasOps.
- **Reconciliation** – Compare CephasOps revenue with external accounting exports where needed.
