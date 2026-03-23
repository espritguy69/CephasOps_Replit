# Codebase Intelligence Report

**Date:** March 2026  
**Pass:** Level 13 – Codebase intelligence layer  
**Scope:** Documentation-only; existing /docs structure preserved.

---

## 1. Executive summary

A **permanent architecture intelligence layer** was added to the CephasOps docs so that future architecture audits, documentation passes, onboarding, and refactors are faster and safer. All artifacts live under the existing docs hierarchy; no application code was changed.

**Created or updated:**

- **docs/architecture/CODEBASE_INTELLIGENCE_MAP.md** – Main intelligence hub: repository shape, runtime architecture, major domains/modules, core flows, architecture hotspots, governance cross-links.
- **docs/architecture/controller_service_map.md** – Controller → main service(s), domain, dependencies, canonical docs.
- **docs/architecture/module_dependency_map.md** – Per-module upstream/downstream, cross-cutting services, ASCII dependency overview.
- **docs/architecture/background_worker_map.md** – All hosted services and schedulers; purpose, trigger, dependencies, related controllers/docs.
- **docs/architecture/integration_map.md** – External (email, WhatsApp, SMS, MyInvois, OneDrive, partner portals) and internal (event store, job orchestration, notification dispatch); purpose, implementation, ownership, state.
- **docs/architecture/entity_domain_map.md** – Domain entities grouped by business area; cross-domain relationships for governance.
- **docs/CODEBASE_INTELLIGENCE_REPORT.md** – This report.

**Portal and governance:** README.md, 00_QUICK_NAVIGATION.md, _INDEX.md, architecture/README.md, DOCS_STATUS.md, DOCUMENTATION_ALIGNMENT_CHECKLIST.md, and CHANGELOG_DOCS.md were updated so the intelligence layer is discoverable from the docs portal.

---

## 2. Architecture shape discovered

- **Top-level:** backend (Api, Application, Domain, Infrastructure), frontend (admin), frontend-si (SI app), docs (01_system … 08_infrastructure, overview, business, operations, dev, architecture, integrations, modules, archive), infra, scripts, tests.
- **Runtime:** Single ASP.NET Core API; in-process hosted services (no Hangfire/Quartz); event store outbox + EventStoreDispatcherHostedService; department-scoped RBAC; single-company context.
- **Module families:** Orders, Parser, Scheduler, Inventory, Billing, Buildings, Workflow, Rates/Payroll, P&L, Reports, Notifications, Events, Operational replay/rebuild, Job orchestration, Settings/Reference data, Departments/RBAC, Admin/Auth.
- **Worker/integration surfaces:** 15+ hosted services (job processor, email ingestion scheduler, notification dispatch worker, event store dispatcher, job execution worker, heartbeat, etc.); integrations: Email (POP3/IMAP/SMTP), WhatsApp, SMS, MyInvois, OneDrive, partner portals (manual).

---

## 3. Intelligence artifacts created

| File | Purpose | Why it matters |
|------|---------|----------------|
| CODEBASE_INTELLIGENCE_MAP.md | Single hub for architecture intelligence | One entry point for modules, flows, hotspots, canonical docs, governance links. |
| controller_service_map.md | Controller → service mapping | Refactoring and ownership clarity; avoids “ghost” controllers. |
| module_dependency_map.md | Module dependencies | Safe change impact; onboarding; dependency direction. |
| background_worker_map.md | Hosted services and job types | Operational understanding; runbooks; observability. |
| integration_map.md | External and internal integrations | Integration ownership; current vs future state. |
| entity_domain_map.md | Entities by domain | Data model governance; domain boundaries. |

---

## 4. Important dependency findings

- **Parser** feeds **Orders** and **Buildings**; depends on **Background jobs** (EmailIngest).
- **Orders** drive **Scheduler**, **Inventory**, **Workflow**, **Billing**, **Notifications**; all department-scoped.
- **Workflow** is cross-cutting: order status transitions, guards, side effects (notifications, scheduler).
- **Event store** is cross-cutting: all domains can append; EventStoreDispatcherHostedService and replay/rebuild consume.
- **Settings/Reference data** is consumed by most modules (order types, building types, templates, global settings).
- **Rates/Payroll** and **P&L** are coupled (payroll runs, PnlRebuild job, payout anomaly alerts).

---

## 5. Architecture hotspots

| Hotspot | Why it matters | Current doc coverage | Drift risk |
|---------|----------------|----------------------|------------|
| Event store & dispatcher | Outbox; replay; many handlers | PHASE_8, EVENT_BUS_OPERATIONS_RUNBOOK | Medium |
| Notification dispatch | Event-driven outbound pipeline | operations/background_jobs, 02_modules/notifications | Low |
| Workflow engine | All order status transitions | WORKFLOW_ENGINE, order_lifecycle_and_statuses | Low |
| Inventory ledger | Single source of truth | modules/inventory_ledger_and_serials | Low |
| Settings / reference data | Many controllers and entities | REFERENCE_TYPES, 02_modules/global_settings | Medium |
| Rates / payroll / P&L | RateEngine, payroll, P&L rebuild, payout health | rate_engine, payroll OVERVIEW, pnl_boundaries | Medium |
| Background job processor | Single processor + many job types | operations/background_jobs, background_worker_map | Low |

---

## 6. Documentation coverage findings

- **Well-covered:** Order lifecycle, workflow engine, billing/MyInvois, inventory/ledger, SI journey, docket process, department RBAC, background jobs (after recent update), API surface (including eventing/operational §6), integrations overview.
- **Still shallow:** Some controller families only listed in api_surface_summary without deep narrative; frontend-to-backend coupling not mapped in one place; job types vs handlers could be expanded in a runbook.
- **Likely to drift:** Eventing and replay (new handlers, new job types); settings/reference (new small controllers); payout/financial alerts (new checks).

---

## 7. Governance updates made

- **docs/README.md** – Navigation line: added CODEBASE_INTELLIGENCE_MAP and CODEBASE_INTELLIGENCE_REPORT.
- **docs/00_QUICK_NAVIGATION.md** – New “Codebase intelligence” subsection with links to CODEBASE_INTELLIGENCE_MAP, CODEBASE_INTELLIGENCE_REPORT, and the five relationship maps.
- **docs/_INDEX.md** – New “Codebase intelligence” table with all seven artifacts.
- **docs/architecture/README.md** – New “Codebase intelligence” section in diagram index linking to CODEBASE_INTELLIGENCE_MAP and the five maps.
- **docs/DOCS_STATUS.md** – Note on codebase intelligence layer (March 2026).
- **docs/DOCUMENTATION_ALIGNMENT_CHECKLIST.md** – Level 13 intelligence pass and link to CODEBASE_INTELLIGENCE_REPORT.
- **docs/CHANGELOG_DOCS.md** – 2026-03 Codebase intelligence layer (Level 13) entry.

---

## 8. Recommended next intelligence docs

- **Frontend–backend coupling map** – Which admin/SI pages call which API controllers/modules (when useful for large features).
- **Job type → handler registry** – Explicit list of job type names and which service/handler executes them (for runbooks and onboarding).
- **Event handler index** – List of domain event types and handler responsibilities (when event surface grows).
- **Refresh triggers** – Document in CODEBASE_INTELLIGENCE_MAP (already started): when to refresh the intelligence maps (new controllers, workers, integrations, workflow/eventing changes).

---

**Definition of done (Level 13):** Codebase intelligence map exists and is useful; controller/service/module/entity/worker/integration relationships are documented at a governance level; portal and governance docs link to the new artifacts; architecture hotspots are identified; existing /docs structure preserved; this report summarizes findings and additions.
