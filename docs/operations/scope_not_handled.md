# Scope Not Handled (Yet)

**Related:** [Product overview](../overview/product_overview.md) | [Process flows](../business/process_flows.md) | [Department & RBAC](../business/department_rbac.md) | [GO-Live Readiness Checklist](../GO_LIVE_READINESS_CHECKLIST_GPON.md)

**Source of truth:** Codebase Summary (Senior Architect Review); Business Processes (Business Systems Analyst Report).

---

**GPON Go-Live:** CephasOps is **GPON-ready** for single-company deployment. The items below are **explicitly out of scope** for current go-live. GPON remains the single focus; future scope is documented here to avoid confusion.

---

## 1. What the system does **not** yet handle

- **Leads and quotations** – A **Quotation** entity exists and is referenced (e.g. by PurchaseOrder), but there is **no full lead-to-quote-to-order** process; no dedicated lead handling or quote workflow in the main flow. **Future only.**
- **Statutory payroll** – **EPF, SOCSO, PCB** (and similar) are **not** calculated; payroll exports support external calculation and bank upload.
- **Leave and claims** – **Leave** for SIs and **expense/claims** (mileage, toll, etc.) are **out of scope** or future.
- **CWO/NWO departments** – **CWO** and **NWO** are **future departments**; no active workflows. Department routes may exist but are not populated. **To be defined when activated.** GPON is the only active lifecycle.
- **Partner API integration** – **No API** with TIME (or others) for order status or docket submission; **email + manual portal** only. See [partner_portal_manual_process.md](partner_portal_manual_process.md).
- **Offline SI app** – SI app is **online only**; **offline** job list and **sync when back online** are not implemented. **Future only.**
- **Payment gateway** – **No Stripe/PayPal** (or similar); **payment** = internal tracking and matching to invoice/order, not card/online payment.
- **Full accounting/GL** – **No double-entry GL**; P&L is analytics; **accounting remains in external system**; optional reconciliation with exported data.
- **Multi-company** – App is **single-company**; multi-company and multi-entity billing/payroll/P&L are documented for future or legacy; current operations are single-company.
- **Barbershop / travel (Kingsman, Menorah)** – **Out of scope for GPON go-live.** Kingsman (barbershop) and Menorah (travel) commission logic appear in the payroll spec; they are **not implemented** for GPON. GPON/SI is the sole focus.

---

## 2. Assumptions

- **GPON go-live (Phases 0–5):** All critical GPON flows are operational. Future scope is documented above to prevent confusion.
- This list is derived from code and docs; product may extend scope later. When in doubt, treat as “not in scope” unless the code clearly implements it.
