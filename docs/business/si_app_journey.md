# SI App Journey & Data Captured

**Related:** [Process flows](process_flows.md) | [Order lifecycle summary](order_lifecycle_summary.md) | [07_frontend/si_app/SI_APP_STRATEGY](../07_frontend/si_app/SI_APP_STRATEGY.md)

**Source of truth:** Codebase Summary (Senior Architect Review); Business Processes (Business Systems Analyst Report).

---

## 1. Installer (SI) journey

1. **Login** – SI logs into the SI app (mobile-first); sees jobs for their assignments.
2. **View schedule** – List of jobs and calendar; can view earnings.
3. **Start job** – Marks “On the way”; system can capture GPS (punctuality KPI).
4. **On site** – Marks “Met customer.” For **assurance** jobs, can record material replacements (old/new serials or non-serialised) from this point.
5. **Complete or block**  
   - **Complete:** Enters splitter ID, port, ONU serial; uploads photos; optional signature; submits.  
   - **Block:** Selects category and reason (e.g. building access denied, wrong address, customer postpones); adds remark and ≥1 photo; submits.  
   - **Reschedule:** Requests reschedule; admin handles TIME approval (except same-day; same-day needs customer evidence).
6. **Materials** – Can view/scan materials allocated to the job; can record returns or faulty items.
7. **If docket rejected** – Admin rejects docket (wrong splitter/ONU, missing photos, etc.); SI corrects or resends; status returns to Dockets received when admin accepts.
8. **Earnings** – Can view own earnings (driven by job type, level, KPI and payroll runs).

---

## 2. Data captured (fieldwork)

| Data | When / where |
|------|----------------------|
| GPS | On the way / on site (optional; for punctuality) |
| Splitter ID | Order completed |
| Port | Order completed |
| ONU serial | Order completed |
| Photos | Completion; blocker/reschedule evidence |
| Signature | Completion (optional) |
| Blocker reason + remark + evidence (≥1 photo) | When job is blocked |
| Material replacements (assurance) | From Met customer; old/new serials or non-serialised type and quantity |
| Material returns / faulty | Materials flow in SI app |

---

## 3. SI app scope

- **Online only** in current implementation; offline job list and sync when back online are **not** implemented (see [Scope not handled](../operations/scope_not_handled.md)).
