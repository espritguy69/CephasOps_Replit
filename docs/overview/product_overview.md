# Product Overview

**Related:** [Process flows](../business/process_flows.md) | [Department & RBAC](../business/department_rbac.md) | [Order lifecycle](../business/order_lifecycle_summary.md) | [01_system/SYSTEM_OVERVIEW](../01_system/SYSTEM_OVERVIEW.md)

**Source of truth:** Codebase Summary (Senior Architect Review); Business Processes (Business Systems Analyst Report).

---

## 1. Main business type

**Fibre/GPON (Fibre-to-the-Home) operations contractor.**

CephasOps is built for a company that:

- Performs fibre installations and activations for telco partners (e.g. TIME, CelcomDigi, U Mobile).
- Manages **new installations (activations)**, **modifications (indoor/outdoor)**, **assurance (fault repair)**, and **value-added services**.
- Employs **service installers (SIs)**—in-house and subcontractors—who are scheduled, complete jobs on site, and submit dockets.
- Invoices partners (e.g. TIME) and pays SIs based on job type and performance.
- Tracks materials, buildings, splitters, and ONU/equipment by job.

---

## 2. Core business processes (top 10)

1. **Email work order ingestion** – Partner emails parsed into order drafts.
2. **Order creation and approval** – From parser drafts or manual entry; admin enriches (Service ID, customer, address, building, appointment).
3. **Order assignment and scheduling** – Admin assigns SI and appointment; jobs appear in SI app.
4. **Field execution and status updates** – SI moves job through statuses (On the way → Met customer → Completed or Blocker/Reschedule); GPS, photos, splitter/port/ONU, signature.
5. **Blocker and reschedule handling** – Blockers (access denied, wrong address, etc.); reschedules (TIME approval by email except same-day; same-day needs customer evidence).
6. **Docket receipt and upload** – Admin receives docket (paper/WhatsApp/email); can reject; when correct, uploads to partner portal.
7. **Invoice generation and submission** – Admin creates invoice (BOQ, rate card, materials); submission to MyInvois (e-invoice); status polling.
8. **Material and inventory tracking** – Ledger as source of truth; receive, allocate, issue, return; serialised equipment by job.
9. **SI payroll / earnings** – Earnings by job type, level, KPI; payroll periods and runs; export for accounting/bank.
10. **P&L and operational reporting** – Revenue (invoices), direct costs (materials, SI pay), overheads; by partner, order type, period.

---

## 3. Tech stack (summary)

| Layer | Technology |
|-------|------------|
| Backend | .NET 10, ASP.NET Core Web API, EF Core 10 |
| Database | PostgreSQL |
| Admin frontend | React 18, Vite, TypeScript, Tailwind, Syncfusion EJ2, TanStack Query |
| SI app | React, Vite, TypeScript, Tailwind (mobile-first; no Syncfusion) |
| Auth | JWT (Bearer); department-scoped RBAC |

---

## 4. Operating model

- **Single company,** **multi-department** (e.g. GPON active; CWO/NWO future).
- **Department-scoped access:** Users see and act only on data for their department(s); 403 otherwise.
- **Partner portal (e.g. TIME)** is reference only—no API; admin mirrors statuses with evidence.
- **SI app** is source of truth for fieldwork (GPS, ONU scan, port, photos, signature).

---

## 5. Assumptions

- Current deployment is single-company; multi-company is documented as reference/future only.
- GPON department is the primary lifecycle; CWO/NWO workflows to be defined when activated.
