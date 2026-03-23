# CephasOps Discrepancies – Audit Register

**Purpose:** Audit-safe register of code vs docs mismatches. Source of truth: actual code behavior.

**Last validated:** March 2026 (architecture audit pass). Architecture vs code alignment: api_surface_summary and operations/background_jobs updated for eventing/operational controllers and hosted services; see [ARCHITECTURE_AUDIT_REPORT.md](ARCHITECTURE_AUDIT_REPORT.md). **Level 15 watchdog (March 2026):** Drift scan run; no new drift; watch docs created (service_sprawl_watch, controller_sprawl_watch, dependency_leak_watch, worker_coupling_watch, module_boundary_regression); see [ARCHITECTURE_WATCHDOG_REPORT.md](ARCHITECTURE_WATCHDOG_REPORT.md).

---

## 1️⃣ Closed Discrepancies

| # | Description | Where fixed | Confirmation |
|---|-------------|-------------|--------------|
| C1 | **Workflow status alignment** – DocketsVerified, SubmittedToPortal, DocketsRejected, Rejected (display: Invoice Rejected) | `OrderStatus.cs`, `OrderStatusesController.cs`, `order_lifecycle_and_statuses.md` | Code and docs aligned. 18 statuses in both. |
| C2 | **Workflow transitions** – Assigned→Blocker, Blocker→Assigned, Blocker→MetCustomer, ReschedulePendingApproval→Assigned/Cancelled | `07_gpon_order_workflow.sql`, `OrderStatusesController.cs` fallback | All present in DB seed and fallback. |
| C3 | **Docket rejection flow** – DocketsRejected status, DocketsReceived↔DocketsRejected transitions | `OrderStatus.cs`, `07_gpon_order_workflow.sql`, `DocketsPage.tsx` | Implemented: Reject modal, Accept corrected, Rejected filter. |
| C4 | **Invoice rejection loop** – Rejected↔ReadyForInvoice, Rejected→Reinvoice, Reinvoice→Invoiced | `07_gpon_order_workflow.sql`, `OrderStatusesController.cs` | Transitions in DB and fallback. |
| C5 | **EmailIngestionService workflow bypass** – Status changes bypassed workflow | `EmailIngestionService.cs` | Cancelled and Blocker now route through `IWorkflowEngineService.ExecuteTransitionAsync`. |
| C6 | **Multi-company vs single-company** – Docs implied multi-company; app is single-company | `product_overview.md`, `DOCS_MAP.md`, `scope_not_handled.md` | Clarified: current deployment is single-company. |
| C7 | **Kingsman / Menorah** – Payroll spec mentioned barbershop/travel; scope unclear | `scope_not_handled.md` | Documented as not in scope; no implementation. |
| C8 | **Quotation / lead-to-order** – Entities exist; no full process | `scope_not_handled.md` | Documented as not handled; no API/UI flow exposed. |
| C9 | **Root README broken links** (was O1) – References to non-existent files at repo root | `README.md` | All links replaced with valid `docs/` paths: 00_QUICK_NAVIGATION.md, dev/onboarding.md, COMPLETION_STATUS_REPORT.md. Added single-company note. |
| C10 | **Debug logging to hardcoded path** (was O2) – Middleware/controller wrote to `c:\Projects\CephasOps\.cursor\debug.log` | `Program.cs`, `OrdersController.cs` | Removed all agent-log middleware blocks and controller debug writes. CORS-fix middleware retained (no file I/O). Production-safe; no hardcoded paths. |

---

## 2️⃣ Open – Must Fix (Blocking)

*None. All blocking items resolved.*

---

## 3️⃣ Accepted Gaps (Non-Blocking)

| # | Description | Why acceptable | Assumptions |
|---|-------------|----------------|-------------|
| A1 | **EmailCleanupService dual registration** – Registered as both `Scoped` and `HostedService` | Intentional: Scoped allows `TriggerCleanup` API; HostedService runs scheduled cleanup. Both resolve to same implementation. | HostedService runs on schedule; controller TriggerCleanup is manual. Same implementation class; observed behavior: scheduled and on-demand operate independently. |
| A2 | **Syncfusion fallback license** – Fallback key in code when env var not set | Documented in `dev/onboarding`; production must set `SYNCFUSION_LICENSE_KEY`. Fallback enables local dev without env. | Production deployment config includes env var. |
| A3 | **DocketsPage "SI will be notified"** – Toast says "SI will be notified to correct and resubmit"; no automated notification sent | Ops notifies SI manually (WhatsApp, email, or in-person). Rejection reason stored in OrderStatusLog for audit. | Manual notification is operational norm. |
| A4 | **data_model_overview.md** – Lists key entities only; full table list in `docs/DOCS_IMPLEMENTATION_TRUTH_INVENTORY.md` | Doc is intentionally high-level. Inventory is reference for full schema. | Add note: "For full table list see docs/DOCS_IMPLEMENTATION_TRUTH_INVENTORY.md." |
| A5 | **CurrencyExchangeService** – TODO for external exchange rate API; uses manual/static rates | GPON ops do not require real-time FX; manual rate updates suffice for v1. | See Deferred D1. |

---

## 4️⃣ Deferred / Roadmap

| # | Description | Reason for deferral | Suggested phase |
|---|-------------|---------------------|-----------------|
| D1 | **CurrencyExchangeService external API** – TODO: integrate Bank Negara Malaysia API | Manual rate entry sufficient for v1 | Future: Billing hardening |
| D2 | **MaterialsDisplay faulty status indicator** – TODO: add when backend supports | Backend does not yet expose faulty status for materials | Future: Inventory enhancements |
| D3 | **SI app profile navigation** – TODO: navigate to profile page | Profile page not implemented | Future: SI app UX |
| D4 | **Storybook consolidation** – Duplicate content in `07_frontend/storybook/` and `03_business/` | Low impact; consolidation is doc hygiene | Future: Doc cleanup |
| D5 | **06_ai historical notes** – Move implementation notes to `99_appendix` or archive | No functional impact | Future: Doc reorg |
| D6 | **DOCS_INVENTORY / DOCS_MAP / _INDEX** – Add row for `docs/DOCS_IMPLEMENTATION_TRUTH_INVENTORY.md`; cross-link | Minor doc hygiene | Future: Doc maintenance |

---

## Validation Summary

| Category | Count |
|----------|-------|
| Closed | 10 |
| Open – Must Fix | 0 |
| Accepted Gaps | 5 |
| Deferred | 6 |

**Code TODOs audited:** CurrencyExchangeService (D1), MaterialsDisplay (D2), MainLayout profile (D3). None block production.

**UI audit:** DocketsPage reject toast (A3) is the only UI text implying behavior not implemented; documented as accepted with manual-notification assumption.
