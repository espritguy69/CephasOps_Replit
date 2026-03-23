# Payroll & Rate Engine Overview

**Related:** [Product overview](../overview/product_overview.md) | [02_modules/payroll/OVERVIEW](../02_modules/payroll/OVERVIEW.md) | [02_modules/rate_engine/RATE_ENGINE](../02_modules/rate_engine/RATE_ENGINE.md) | [02_modules/gpon/GPON_RATECARDS](../02_modules/gpon/GPON_RATECARDS.md)

**Source of truth:** Codebase Summary (Senior Architect Review); Business Processes (Business Systems Analyst Report).

---

## 1. SI rate plans

- **Rate plans** define pay per **job type** (Activation, Modification Indoor/Outdoor, Assurance, Value Added Service), **order category** (FTTH, FTTO, FTTR, FTTC), **installation method**, and **SI level** (e.g. Junior, Senior, Lead, Subcon).
- **KPI** can adjust pay (e.g. on-time bonus, late/reschedule penalty).
- Configured in **Settings → SI rate plans** (and partner rate cards in Settings).

---

## 2. Payroll calculation

- **Payroll runs** by **period** (e.g. monthly).
- System **calculates earnings** per SI from completed jobs and applicable rates.
- **Exports** (e.g. CSV/Excel) support external accounting or bank (e.g. GIRO).
- **Statutory deductions** (EPF, SOCSO, PCB) are **out of scope**—computed or applied externally. See [Scope not handled](../operations/scope_not_handled.md).

---

## 3. Partner billing rates

- **Partner rate cards** (e.g. TIME) define what the company charges the partner per job type, order category, installation method; used for **invoicing** and **P&L revenue**.
- Rate engine resolves both **partner billing** and **SI payout** rates for a given order context.
