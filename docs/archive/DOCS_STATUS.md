# Documentation Status & Audit

**Last audit:** March 2026 (living-docs governance pass)  
**Purpose:** Track docs vs app reality, broken links, and what’s current.

---

## 0. Overall app status (PRD & Cursor rules)

**App status:** Production-ready for single-company GPON operations; Phase 2 enhancements in progress.

### PRD alignment (Master PRD: `docs/03_business/MASTER_PRD.md`)

| PRD element | Status | Notes |
|-------------|--------|-------|
| **Vision** | On track | Single-company, multi-department; workflow-driven operations, email-to-invoice automation, audit trails. |
| **Phase 1 (Core)** | Complete | Order lifecycle, workflow engine, email parser, basic SI app, inventory, billing/invoicing. |
| **Phase 2 (Enhancements)** | In progress | Syncfusion UI, scheduler improvements, KPI/document generation, inventory ledger & reporting (see §4). |
| **Phase 3 (Future)** | Planned | CWO/NWO workflows, advanced analytics, customer portal, native mobile, accounting integration. |
| **Pillars** | | |
| Order Management | Done | Lifecycle, status flow, scheduling, materials, dockets. |
| Workflow Engine | Done | Definitions, guards, side effects, overrides. |
| Rate Engine | Done | SI rate cards, partner billing, payroll. |
| Materials & Inventory | Done + Phase 2 | Ledger as source of truth; reports, export, RBAC. |
| KPI Engine | Done | Profiles, on-time, docket quality, department KPIs. |
| Finance Engine | Done | Invoicing, P&L, payment tracking. |
| SI App (frontend-si) | Done (basic) | Job list, status transitions, photos; mobile-first. |
| RMA Module | In place | Request tracking, MRA linkage. |
| Email Parser | Done | Ingestion, templates, drafts, duplicate detection, health in `/api/admin/health`. |
| Scheduler | Done | Calendar, SI availability, leave, unassigned orders. |

### Cursor / workspace rules alignment (`.cursorrules`, `.cursor/rules/`)

| Rule area | Status | Notes |
|-----------|--------|-------|
| Single-company, multi-department | Applied | One company context; department scoping and RBAC enforced. |
| Documentation-first | Applied | Docs under `/docs`; canonical module files (OVERVIEW, SETUP, SPECIFICATION, etc.); changes tied to docs. |
| Stack | Applied | Backend: .NET 10, EF Core, PostgreSQL. Frontend: React, Vite, Tailwind, shadcn, TanStack Query. frontend-si: same stack, mobile-first. |
| API & frontend | Applied | Response envelope, API client with department context, hooks + TanStack Query; cursor-guides patterns. |
| Syncfusion | Applied | License from env (no hardcoding). |
| TypeScript | Applied | New frontend files are .ts/.tsx. |
| Docs steward | Applied | Deduplication, canonical names, links and _INDEX maintained. |
| PostgreSQL (rules) | Reference | Connection details in `.cursor/rules/postgress.mdc` (local dev). |
| Ask mode (rules) | Reference | `.cursor/rules/ask.mdc` — no code, layman terms when using Ask. |

### Limitations (per PRD §6)

- **Department scope:** GPON fully operational; CWO/NWO infrastructure-ready, workflows to be defined.
- **Partners:** TIME and related (Digi, Celcom, U Mobile) supported; new partners via settings.
- **SI app:** Basic feature set; offline/advanced scanning/sync are future.
- **Notifications:** In-app and email; SMS/WhatsApp infrastructure ready for future.

---

## 1. Current state

- **App model:** Single-company, multi-department (see `03_business/STORYBOOK.md`, `01_system/TECHNICAL_ARCHITECTURE.md`).
- **Key entry points:** `README.md`, `00_QUICK_NAVIGATION.md`, `_INDEX.md` — all updated Jan 2026; links point to current paths under `02_modules/<module>/`.
- **Backend status:** `06_ai/BACKEND_IMPLEMENTATION_STATUS.md` — paths and Email Parser/ingestion status updated; single-company note added.
- **Email parser:** Full ingestion (POP3/IMAP, scheduler, jobs) implemented. Parser health in `GET /api/admin/health` → `emailParser`. See `02_modules/email_parser/SETUP.md` § Monitoring.

---

## 2. Link and path fixes (Jan 2026; Mar 2026 living-docs)

- **00_QUICK_NAVIGATION.md:** All “Core Modules” links updated from legacy top-level names (e.g. `ORDERS_MODULE.md`) to current paths (`orders/OVERVIEW.md`, `email_parser/OVERVIEW.md`, `scheduler/OVERVIEW.md`, `background_jobs/OVERVIEW.md`, and other modules).
- **_INDEX.md:** “Last Updated” set to January 2026; added pointer to this doc.
- **docs/README.md:** “Where to Start” now references `01_system/SYSTEM_OVERVIEW.md` (no longer missing `EXEC_SUMMARY.md`). Note on `99_appendix/` clarified (legacy/deprecated, not removed).
- **06_ai/BACKEND_IMPLEMENTATION_STATUS.md:** All documentation paths aligned to `02_modules/<module>/...`; Email Ingestion marked complete; parser health and single-company mode noted.
- **Mar 2026 living-docs pass:** DOCS_MAP A–P marked DONE; 03_business/MULTI_COMPANY_STORYBOOK given single-company disclaimer; DOCS_INVENTORY and DOCUMENTATION_ALIGNMENT_CHECKLIST updated; DOCS_STATUS and CHANGELOG_DOCS refreshed.
- **Mar 2026 codebase intelligence (Level 13):** architecture/CODEBASE_INTELLIGENCE_MAP.md and five relationship maps (controller_service, module_dependency, background_worker, integration, entity_domain) created; CODEBASE_INTELLIGENCE_REPORT.md added; README, 00_QUICK_NAVIGATION, _INDEX, architecture/README, DOCUMENTATION_ALIGNMENT_CHECKLIST, and CHANGELOG_DOCS updated with intelligence layer links.
- **Mar 2026 refactor safety (Level 14):** REFACTOR_SAFETY_REPORT.md and architecture refactor-safety docs (high_coupling_modules, hidden_dependencies, module_fragility_map, safe_refactor_zones, refactor_danger_zones, refactor_sequence_plan, worker_dependency_risks) created; portal and CODEBASE_INTELLIGENCE_MAP updated with links.
- **Mar 2026 architecture watchdog (Level 15):** ARCHITECTURE_WATCHDOG_REPORT.md and watch docs (service_sprawl_watch, controller_sprawl_watch, dependency_leak_watch, worker_coupling_watch, module_boundary_regression) created; drift scan found no new drift; CODEBASE_INTELLIGENCE_MAP, _discrepancies, DOCUMENTATION_ALIGNMENT_CHECKLIST, portal, CHANGELOG_DOCS updated.
- **Mar 2026 governance systems index:** architecture/ARCHITECTURE_GOVERNANCE_SYSTEMS.md created (Change Impact Predictor, Architecture Policy Engine, Auto Documentation Sync, Architecture Risk Dashboard, Self-Maintaining Architecture, Portal, Governance logs); portal navigation and governance logs updated; CHANGELOG_DOCS and DOCS_STATUS updated.

---

## 3. Known gaps and stale areas

| Area | Issue | Action |
|------|--------|--------|
| **01_system/MULTI_COMPANY_ARCHITECTURE.md** | Describes multi-tenant model; app is single-company | Keep as reference; intro note that current deployment is single-company |
| **01_system/README.md** | Says “all data scoped by companyId” | OK for single-company (one company); optional tweak to “single-company” |
| **infra/README.md** (repo root) | References `infra/docker/` and `infra/ci-cd/` | Folders missing; README updated to describe only existing folders (k8s, terraform, monitoring) |

---

## 4. Completed vs doc

- **Email parser:** Implemented (ingestion, templates, drafts, orders). Docs: `02_modules/email_parser/OVERVIEW.md`, `SETUP.md`, `SPECIFICATION.md`, `WORKFLOW.md`; SETUP includes Monitoring (parser health).
- **System health:** `GET /api/admin/health` returns database + `emailParser`. Documented in email_parser SETUP § Monitoring and BACKEND_IMPLEMENTATION_STATUS.
- **Single-company:** Reflected in STORYBOOK, TECHNICAL_ARCHITECTURE, companies WORKFLOW, DEPARTMENT FILTERING, and BACKEND_IMPLEMENTATION_STATUS.
- **Document templates UI:** Receipt-management-style list (card/table view, type chips, duplicate from list). Documented in `02_modules/document_generation/DOCUMENT_TEMPLATES_MODULE.md`, `02_modules/document_generation/OVERVIEW.md`, `07_frontend/ui/RATES_AND_TEMPLATES.md`.
- **Invoice Document Templates integration:** Invoice PDF download, print, and preview use the Document Templates system (Handlebars) instead of hardcoded layouts. Default invoice template seeded on startup; optional `?templateId=` on PDF/preview endpoints. Frontend: print preview and print fetch HTML from `GET /api/billing/invoices/{id}/preview-html`. See document_generation module docs.
- **Building merge tool:** Admin tool (list similar buildings, preview merge, merge and soft-delete). API: GET merge-candidates, GET merge-preview, POST merge. UI: Building Merge page at `/settings/buildings-merge`. Documented in `02_modules/buildings/WORKFLOW.md`.
- **Inventory (ledger):** Ledger as single source of truth; no direct `StockBalance.Quantity` writes. GET `/api/inventory/stock` and report APIs (`reports/usage-summary`, `reports/serial-lifecycle`, `reports/stock-by-location-history`) derive from ledger; LedgerBalanceCache; department RBAC. See `02_modules/inventory/OVERVIEW.md`.
- **Reports Hub:** Single page at `/reports` with search by name/tags, category filter, and report runner at `/reports/:reportKey`. Backend: `GET api/reports/definitions`, `POST api/reports/{reportKey}/run`; in-memory report registry; department-scoped run (403 when not allowed). Starter reports: orders-list (keyword + pagination), materials-list, stock-summary, ledger, scheduler-utilization. Export (CSV, XLSX, PDF) for orders-list, materials-list, stock-summary, ledger, scheduler-utilization via `GET api/reports/{report}/export?format=csv|xlsx|pdf`. Dedicated `GET api/scheduler/utilization` for flattened slots by date range. See `02_modules/reports_hub/OVERVIEW.md` and `docs/REPORTS_HUB_IMPLEMENTATION_COVERAGE.md`.
- **RBAC (department scope):** Department scope enforced on all department-scoped endpoints (Orders, Inventory, Scheduler, Departments, Skills, Payroll, BillingRatecard, BusinessHours, ServiceInstallers, OrderTypes, BuildingTypes, SplitterTypes, ApprovalWorkflows, SlaProfiles, AutomationRules, AgentMode, Users, EscalationRules, Tasks, Pnl). Requesting another department returns 403. See `docs/RBAC_MATRIX_REPORT.md` and `02_modules/rbac/OVERVIEW.md`.
- **Reference types (planning & record):** Single doc lists all default types (departments, building types, order types, order categories, installation methods, splitter types) and how they relate. Source of truth for seeded lists is `DatabaseSeeder.cs`; doc is the planning/record view. See `05_data_model/REFERENCE_TYPES_AND_RELATIONSHIPS.md`.
- **Source-of-truth doc alignment (Feb 2026):** Documentation aligned to two reference documents (Codebase Summary – Senior Architect Review; Business Processes – Business Systems Analyst Report). New folders: `overview/`, `business/`, `integrations/`, `operations/`, `dev/`; new docs for product overview, process flows, department/RBAC, order lifecycle summary, SI journey, docket, billing/MyInvois, inventory/ledger summary, payroll/rate, P&L boundaries, integrations, background jobs, scope not handled, developer onboarding, API surface summary, data model overview. See `DOCS_MAP.md`, `DOCS_INVENTORY.md`, `CHANGELOG_DOCS.md`, `_discrepancies.md`.
- **Audit logging:** AuditLog entity, GET `/api/logs/audit`, order status changes → audit. See `02_modules/global_settings/LOGGING_AND_AUDIT_MODULE.md`.

---

## 5. Recommended upkeep

1. After major features: update `BACKEND_IMPLEMENTATION_STATUS.md` and any module spec under `02_modules/`.
2. When adding or changing seeded reference types (building types, order types, departments, etc.): update `DatabaseSeeder.cs` and `05_data_model/REFERENCE_TYPES_AND_RELATIONSHIPS.md`.
3. When adding or renaming modules: update `00_QUICK_NAVIGATION.md` and `_INDEX.md`.
4. When changing deployment/infra: update `infra/README.md` and `08_infrastructure/` so they match the repo (e.g. no references to missing `docker/` or `ci-cd/`).
5. Run a link check (e.g. markdown link checker) periodically; fix broken links in entry points first. **Feb 2026:** _INDEX.md broken links fixed (Rate Engine → RATE_ENGINE.md, GPON → GPON_RATECARDS.md, Document Templates → DOCUMENT_TEMPLATES_MODULE.md).
6. When changing module or doc paths: run a link check on BACKEND_IMPLEMENTATION_STATUS.md and entry points (README.md, 00_QUICK_NAVIGATION.md, _INDEX.md).

---

## 6. Improvement & evolution

- **Done:** Workflow enforcement, parser coverage (Digi/Celcom), building matching (fuzzy), audit logging (AuditLog + GET /api/logs/audit), parser UX (bulk approve, filters, inline edit, duplicate warning), address normalization, parser analytics dashboard, building merge tool.
- **Optional:** Dev docker-compose for local API + DB + workers.

---

## 7. UI consistency status

- **Summary:** P0 quick wins, theme alignment, global styles, Syncfusion styling (Admin), and the UI consistency gate are complete. **P1 PageShell audit complete (Feb 2026):** all Admin content pages use PageShell. P2 (SI primitives, Toast, DataTable, SI Card title/subtitle/footer) and P3-6 (Syncfusion vs DataTable rule) done. Remaining work is apply-when-touching (P3 standards) and optional Skeleton on more SI pages.
- **Details:** See [UI_CONSISTENCY_STATUS.md](UI_CONSISTENCY_STATUS.md) for completed work and [UI_CONSISTENCY_BACKLOG.md](UI_CONSISTENCY_BACKLOG.md) for backlog items.

---

**Next review:** After next major release or architecture change.
