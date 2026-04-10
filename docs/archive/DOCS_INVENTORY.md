# Documentation Inventory (STEP 1)

**Source of truth:** Codebase Summary (Senior Architect Review) + Business Processes (Business Systems Analyst Report).  
**Purpose:** List existing docs with purpose and status relative to source of truth.  
**Last run:** March 2026 (living-docs governance pass).  
**Status values:** ACTIVE | DUPLICATE | LEGACY | ARCHIVE_CANDIDATE | MISPLACED | UNKNOWN  
**Actions:** KEEP | MOVE | MERGE | ARCHIVE | REVIEW | UPDATE

---

## 1. Root and index

| Doc path | Purpose | Last updated guess | Status |
|----------|---------|--------------------|--------|
| docs/README.md | Master index; where to start; folder overview | 2026 | OK |
| docs/_INDEX.md | Complete index with quick nav and folder structure | Feb 2026 | OK |
| docs/00_QUICK_NAVIGATION.md | Quick links by topic (modules, data model, frontend) | Jan 2026 | OK |
| docs/DOCS_STATUS.md | Doc vs app reality; link fixes; completed vs doc; upkeep | Feb 2026 | OK |
| docs/DOCS_IMPLEMENTATION_TRUTH_INVENTORY.md | Implementation truth snapshot (schema, backend, frontend from code/EF; Live DB optional) | Feb 2026 | OK |
| README.md (repo root) | Product splash; quick start; key features; references docs/ | 2026 | OK (links fixed per DOCS_STATUS; single-company noted) |

---

## 2. System (01_system)

| Doc path | Purpose | Last updated guess | Status |
|----------|---------|--------------------|--------|
| 01_system/SYSTEM_OVERVIEW.md | End-to-end journey; order lifecycle; high-level capabilities | Dec 2025 | OK |
| 01_system/ORDER_LIFECYCLE.md | GPON order status flow; definitions; KPI matrix; docket/invoice rules | 2025 | OK (authoritative for lifecycle) |
| 01_system/ARCHITECTURE_BOOK.md | System design; modules; data flows; integrations | — | OK |
| 01_system/WORKFLOW_ENGINE.md | Workflow definitions; transitions; guards | — | OK |
| 01_system/EMAIL_PIPELINE.md | Email ingestion and parsing flow | — | OK |
| 01_system/MULTI_COMPANY_ARCHITECTURE.md | Multi-tenant architecture (reference; app is single-company) | — | OK (has single-company note) |
| 01_system/TECHNICAL_ARCHITECTURE.md | Technical stack and structure | — | OK |
| 01_system/TECH_STACK.md | Tech stack summary | — | OK |
| 01_system/WORKFLOW_ENGINE_FLOW.md | Workflow flow detail | — | OK |
| 01_system/SERVER_STATUS.md | Server status | — | OK |

---

## 3. Modules (02_modules)

| Doc path | Purpose | Last updated guess | Status |
|----------|---------|--------------------|--------|
| 02_modules/orders/OVERVIEW.md | Orders module specification | — | OK |
| 02_modules/orders/ORDER_CREATION_LOGIC.md | Order creation logic | — | OK |
| 02_modules/orders/WORKFLOW.md | Order workflow | — | OK |
| 02_modules/department/OVERVIEW.md | Department module; responsibilities; scope | — | OK |
| 02_modules/department/FILTERING.md | Department filtering logic | — | OK |
| 02_modules/email_parser/OVERVIEW.md | Email parser introduction | — | OK |
| 02_modules/email_parser/SETUP.md | Email account configuration | — | OK |
| 02_modules/scheduler/OVERVIEW.md | Scheduler / job scheduling | — | OK |
| 02_modules/inventory/OVERVIEW.md | Inventory and ledger; department RBAC | — | OK |
| 02_modules/billing/OVERVIEW.md | Billing and tax (full spec in BILLING_*; may be long) | — | OK |
| 02_modules/payroll/OVERVIEW.md | Payroll and SI earnings | — | OK |
| 02_modules/pnl/OVERVIEW.md | P&L analytics | — | OK |
| 02_modules/rbac/OVERVIEW.md | RBAC; department scope | — | OK |
| 02_modules/reports_hub/OVERVIEW.md | Reports hub; run and export | — | OK |
| 02_modules/background_jobs/OVERVIEW.md | Background jobs | — | OK |
| 02_modules/buildings/WORKFLOW.md | Buildings workflow | — | OK |
| 02_modules/notifications/OVERVIEW.md | Notifications | — | OK |
| 02_modules/global_settings/* | Settings; companies; logging; SLA | — | OK |
| 02_modules/MODULE_INVENTORY.md | Module inventory list | — | OK |

---

## 4. Business (03_business)

| Doc path | Purpose | Last updated guess | Status |
|----------|---------|--------------------|--------|
| 03_business/STORYBOOK.md | GPON business scenarios | — | OK |
| 03_business/MASTER_PRD.md | Master PRD | — | OK |
| 03_business/BUSINESS_POLICIES.md | Business policies | — | OK |
| 03_business/USE_CASES.md | Use cases | — | OK |
| 03_business/USER_FLOWS.md | User flows | — | OK |
| 03_business/MULTI_COMPANY_STORYBOOK.md | Multi-company storybook | — | OUTDATED (app single-company) |
| 03_business/PAGES.md | Pages inventory | — | OK |

---

## 5. API (04_api)

| Doc path | Purpose | Last updated guess | Status |
|----------|---------|--------------------|--------|
| 04_api/API_OVERVIEW.md | API overview | — | OK |
| 04_api/API_CONTRACTS_SUMMARY.md | Endpoint contracts | — | OK |
| 04_api/PHASE2_SETTINGS_API.md | Phase 2 settings API | — | OK |
| 04_api/SWAGGER_SETUP.md | Swagger setup | — | OK |

---

## 6. Data model (05_data_model)

| Doc path | Purpose | Last updated guess | Status |
|----------|---------|--------------------|--------|
| 05_data_model/DATA_MODEL_INDEX.md | Schema overview | — | OK |
| 05_data_model/DATA_MODEL_SUMMARY.md | Data model summary | — | OK |
| 05_data_model/REFERENCE_TYPES_AND_RELATIONSHIPS.md | Reference types; departments; building/order/installation types | Feb 2026 | OK |
| 05_data_model/WORKFLOW_STATUS_REFERENCE.md | Workflow status reference | — | OK |
| 05_data_model/entities/*.md | Per-entity definitions | — | OK |
| 05_data_model/relationships/*.md | Relationship definitions | — | OK |

---

## 7. Architecture (architecture/)

| Doc path | Purpose | Last updated guess | Status |
|----------|---------|--------------------|--------|
| architecture/README.md | Architecture index | — | OK |
| architecture/20_workflow_email_to_order.md | Email to order workflow | — | OK |
| architecture/21_workflow_order_lifecycle.md | Order lifecycle flow | — | OK |
| architecture/00_company-systems-overview.md | Company systems overview | — | NEEDS-MOVE (align to single-company) |

---

## 8. Status and UI reports (root docs)

| Doc path | Purpose | Last updated guess | Status |
|----------|---------|--------------------|--------|
| COMPLETION_STATUS_REPORT.md | Feature completion matrix; backend/frontend/DB status per module; top blockers; reality vs docs | Feb 2026 | OK |
| GO_LIVE_READINESS_CHECKLIST_GPON.md | GPON production readiness checklist (security, data integrity, workflow, billing, SI app, etc.) | Feb 2026 | OK |
| ROADMAP_TO_COMPLETION.md | Phased roadmap (Phase 0–5) to production-ready GPON | Feb 2026 | OK |
| RBAC_MATRIX_REPORT.md | RBAC matrix; department-scoped endpoints | — | OK |
| UI_CONSISTENCY_STATUS.md | UI consistency status | Feb 2026 | OK |
| REPORTS_HUB_IMPLEMENTATION_COVERAGE.md | Reports hub coverage | — | OK |
| GLOBAL_STYLES_ALIGNMENT_SUMMARY.md | Global styles | — | OK |
| P0_UI_CONSISTENCY_PATCH_SUMMARY.md | P0 UI patch summary | — | OK |
| THEME_ALIGNMENT_SUMMARY.md | Theme alignment | — | OK |

---

## 9. 06_ai, 07_frontend, 08_infrastructure

| Doc path | Purpose | Last updated guess | Status |
|----------|---------|--------------------|--------|
| 06_ai/DEVELOPER_GUIDE.md | Developer onboarding | — | OK |
| 06_ai/QUICK_START.md | Quick start | — | OK |
| 06_ai/DEV_HANDBOOK.md | Dev handbook | — | OK |
| 06_ai/BACKEND_IMPLEMENTATION_STATUS.md | Backend implementation status | — | OK |
| 06_ai/* (many implementation notes) | Parser, SI, rate engine, seed fixes, etc. | — | OK / NEEDS-MOVE (historical; could live in appendix or archive) |
| 07_frontend/FRONTEND_STRATEGY.md | Frontend strategy | — | OK |
| 07_frontend/si_app/SI_APP_STRATEGY.md | SI app strategy | — | OK |
| 08_infrastructure/TESTING_SETUP.md | Testing setup | — | OK |
| 08_infrastructure/background_jobs_infrastructure.md | Background jobs infra | — | OK |

---

## 10. Archive (docs/archive/)

| Path | Purpose | Status | Action |
|------|---------|--------|--------|
| archive/README.md | Explains archive; pointers to current docs | ACTIVE | KEEP |
| archive/event_bus/ | Legacy event bus phase docs (Phase 1–7, validation, rollout) | LEGACY | KEEP (archived) |
| archive/deliverables/ | One-time deliverable/audit docs | LEGACY | KEEP (archived) |
| archive/evidence/ | Dated parser/scheduler/ingestion evidence | LEGACY | KEEP (archived) |

**Canonical replacements:** Event bus → PHASE_8_PLATFORM_EVENT_BUS.md, EVENT_BUS_OPERATIONS_RUNBOOK.md.

---

## 11. Duplicates and moves

- **DUPLICATE:** 07_frontend/storybook/* duplicates or mirrors 03_business content (STORYBOOK, PAGES, USER_FLOWS, etc.); keep one canonical set.
- **NEEDS-MOVE:** New consolidated docs (from DOCS_MAP) go under docs/overview, docs/business, docs/operations, docs/integrations, docs/dev; existing 01_system, 02_modules, etc. stay; cross-links added.
- **MISSING:** Single consolidated “Product overview” (business type + core processes in one place); “Not handled yet” scope doc; “Developer onboarding” one-pager with env and run scripts; “API surface summary” (controllers by module).

---

**Summary**

- **OK:** 01_system, 02_modules, 03_business, 04_api, 05_data_model, overview, business, operations, dev, architecture, integrations, modules, DOCS_MAP (A–P DONE), DOCS_STATUS, _INDEX, 00_QUICK_NAVIGATION, archive.
- **UPDATED (Mar 2026):** MULTI_COMPANY_STORYBOOK single-company note; DOCS_MAP required set DONE; inventory and alignment checklist refreshed.
- **DEFERRED:** Storybook consolidation; 06_ai notes to appendix (see _discrepancies).
