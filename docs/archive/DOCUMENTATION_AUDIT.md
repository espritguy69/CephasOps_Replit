# Documentation Audit

**Purpose:** Full discovery of documentation across the repository; status and recommended actions.  
**Last run:** March 2026.  
**Used for:** Reorganization, consolidation, and living-docs alignment.

---

## 1. Scope

- **In scope:** All `*.md`, `*.mdx`, `*.txt`, `*.rst`, `*.adoc` under the repo (excluding `node_modules`, `.git`, `obj`, `bin`).
- **Status values:** ACTIVE | DUPLICATE | LEGACY | ARCHIVE_CANDIDATE | MISPLACED | UNKNOWN
- **Recommended actions:** KEEP | MOVE | MERGE | ARCHIVE | REVIEW

---

## 2. Existing /docs Topology (Preserved)

| Layer | Path | Role |
|-------|------|------|
| Portal / index | README.md, 00_QUICK_NAVIGATION.md, _INDEX.md, DOCS_MAP.md, DOCS_INVENTORY.md, DOCS_STATUS.md | Entry points and doc governance |
| Numbered sections | 01_system, 02_modules, 03_business, 04_api, 05_data_model, 06_ai, 07_frontend, 08_infrastructure | Canonical structure by domain/layer |
| Source-of-truth aligned | overview/, business/, architecture/, integrations/, operations/, dev/, modules/ | Consolidated views (DOCS_MAP A–P) |
| Appendix | 99_appendix/ | Reference / legacy material |
| Archive | docs/archive/ (created) | Superseded, obsolete, or duplicate docs |

**Conventions preserved:** OVERVIEW.md per module; WORKFLOW.md where applicable; small focused files; _index.md in numbered sections; no replacement with a generic template.

---

## 3. Audit Table

### 3.1 Docs root – indexes and governance

| File | Current Path | Likely Topic | Status | Recommended Action |
|------|--------------|--------------|--------|---------------------|
| README.md | docs/README.md | Master index | ACTIVE | KEEP |
| 00_QUICK_NAVIGATION.md | docs/00_QUICK_NAVIGATION.md | Quick links | ACTIVE | KEEP |
| _INDEX.md | docs/_INDEX.md | Complete index | ACTIVE | KEEP |
| DOCS_MAP.md | docs/DOCS_MAP.md | Required doc set A–P | ACTIVE | KEEP |
| DOCS_INVENTORY.md | docs/DOCS_INVENTORY.md | Inventory & status | ACTIVE | KEEP |
| DOCS_STATUS.md | docs/DOCS_STATUS.md | Link fixes, gaps | ACTIVE | KEEP |
| DOCS_IMPLEMENTATION_TRUTH_INVENTORY.md | docs/ | Implementation snapshot | ACTIVE | KEEP |
| CHANGELOG_DOCS.md | docs/ | Doc changes | ACTIVE | KEEP |
| _discrepancies.md | docs/ | Audit register | ACTIVE | KEEP |
| DOCUMENTATION_ALIGNMENT_CHECKLIST.md | docs/ | Alignment checklist | ACTIVE | KEEP |
| _templates/doc_template.md | docs/_templates/ | Template | ACTIVE | KEEP |

### 3.2 Docs root – status and go-live

| File | Current Path | Likely Topic | Status | Recommended Action |
|------|--------------|--------------|--------|---------------------|
| COMPLETION_STATUS_REPORT.md | docs/ | Feature completion matrix | ACTIVE | KEEP |
| GO_LIVE_READINESS_CHECKLIST_GPON.md | docs/ | GPON readiness | ACTIVE | KEEP |
| ROADMAP_TO_COMPLETION.md | docs/ | Phased roadmap | ACTIVE | KEEP |
| go-live-smoke-test.md | docs/ | Smoke test | ACTIVE | KEEP |
| RBAC_MATRIX_REPORT.md | docs/ | RBAC matrix | ACTIVE | KEEP |

### 3.3 Docs root – event bus / platform (consolidation candidates)

| File | Current Path | Likely Topic | Status | Recommended Action |
|------|--------------|--------------|--------|---------------------|
| PHASE_8_PLATFORM_EVENT_BUS.md | docs/ | Platform event bus (current) | ACTIVE | KEEP (canonical) |
| EVENT_BUS_OPERATIONS_RUNBOOK.md | docs/ | Event bus ops | ACTIVE | KEEP |
| EVENT_METADATA_CONTRACT.md | docs/ | Metadata contract | ACTIVE | KEEP |
| EVENT_BUS_IMPLEMENTATION_PHASE1.md | docs/ | Phase 1 history | LEGACY | ARCHIVE |
| EVENT_BUS_PHASE2_EVENT_STORE.md | docs/ | Phase 2 | LEGACY | ARCHIVE |
| EVENT_BUS_PHASE3_CORRELATION.md | docs/ | Phase 3 | LEGACY | ARCHIVE |
| EVENT_BUS_PHASE4_*.md (all) | docs/ | Phase 4 variants | LEGACY | ARCHIVE |
| EVENT_BUS_PHASE5_7_SUMMARY.md | docs/ | Phase 5–7 summary | LEGACY | ARCHIVE |
| EVENT_BUS_PHASE8_9_SUMMARY.md | docs/ | Phase 8–9 summary | ACTIVE | KEEP |
| EVENT_BUS_IDEMPOTENCY_GUARD.md | docs/ | Idempotency | ACTIVE | KEEP |
| EVENT_BUS_OBSERVABILITY.md | docs/ | Observability | ACTIVE | KEEP |
| EVENT_BUS_PRODUCTION_VALIDATION_REPORT.md | docs/ | Validation | ARCHIVE_CANDIDATE | ARCHIVE |
| EVENT_BUS_ROLLOUT_READINESS_SUMMARY.md | docs/ | Rollout | ARCHIVE_CANDIDATE | ARCHIVE |
| DISTRIBUTED_*.md (all) | docs/ | Distributed platform | ACTIVE/LEGACY | KEEP key; ARCHIVE audits/closure |
| DOMAIN_EVENT_ARCHITECTURE.md | docs/ | Domain events | ACTIVE | KEEP |
| EVENT_LEDGER_FOUNDATION.md | docs/ | Ledger | ACTIVE | KEEP |
| EVENT_REPLAY_POLICY.md | docs/ | Replay policy | ACTIVE | KEEP |
| MIGRATION_NOTES_PHASE_8.md | docs/ | Phase 8 migration | ACTIVE | KEEP |

### 3.4 Docs root – workflow engine

| File | Current Path | Likely Topic | Status | Recommended Action |
|------|--------------|--------------|--------|---------------------|
| WORKFLOW_ENGINE_AUDIT.md | docs/ | Audit | ACTIVE | KEEP |
| WORKFLOW_ENGINE_EVOLUTION_SUMMARY.md | docs/ | Evolution | ACTIVE | KEEP |
| WORKFLOW_ENGINE_EVOLUTION_STRATEGY.md | docs/ | Strategy | ACTIVE | KEEP |
| WORKFLOW_ENGINE_EXECUTION_MODEL.md | docs/ | Execution model | ACTIVE | KEEP |
| WORKFLOW_ENGINE_GAP_ANALYSIS.md | docs/ | Gap analysis | ACTIVE | KEEP |
| WORKFLOW_AUDIT_SUMMARY.md | docs/ | Summary | ACTIVE | KEEP |
| WORKFLOW_EVENT_INTEGRATION.md | docs/ | Event integration | ACTIVE | KEEP |
| WORKFLOW_EVOLUTION_UI_AND_TESTING.md | docs/ | UI/testing | ACTIVE | KEEP |
| WORKFLOW_RESOLUTION_*.md | docs/ | Resolution | ACTIVE | KEEP |
| workflow-seeding-runbook.md | docs/ | Seeding runbook | ACTIVE | KEEP |

### 3.5 Docs root – operational / replay / EF

| File | Current Path | Likely Topic | Status | Recommended Action |
|------|--------------|--------------|--------|---------------------|
| OPERATIONAL_REPLAY_ENGINE_*.md | docs/ | Replay engine | ACTIVE | KEEP |
| OPERATIONAL_STATE_REBUILDER.md | docs/ | State rebuilder | ACTIVE | KEEP |
| OPERATIONAL_TIMELINE_*.md | docs/ | Timeline | ACTIVE | KEEP |
| REPLAY_*.md | docs/ | Replay design/ops | ACTIVE | KEEP |
| recover-stuck-background-job.md | docs/ | Recovery | ACTIVE | KEEP |
| EF_MIGRATION_STABILIZATION.md | docs/ | EF stabilization | ACTIVE | KEEP |
| MIGRATION_HYGIENE.md | docs/ | Migration hygiene | ACTIVE | KEEP |

### 3.6 Docs root – deliverables and audits (archive candidates)

| File | Current Path | Likely Topic | Status | Recommended Action |
|------|--------------|--------------|--------|---------------------|
| ADMIN_USER_MANAGEMENT_*.md | docs/ | Admin UM deliverables | ARCHIVE_CANDIDATE | ARCHIVE (keep one audit if needed) |
| GPON_*.md (deliverables/audits) | docs/ | GPON deliverables | ARCHIVE_CANDIDATE | ARCHIVE (keep architecture/audit) |
| ORDER_*.md (deliverables) | docs/ | Order deliverables | ARCHIVE_CANDIDATE | ARCHIVE |
| PAYOUT_*_DELIVERABLE.md | docs/ | Payout deliverables | ARCHIVE_CANDIDATE | ARCHIVE |
| RATE_*_DELIVERABLE.md | docs/ | Rate deliverables | ARCHIVE_CANDIDATE | ARCHIVE |
| RBAC_V2_*_DELIVERABLE.md | docs/ | RBAC deliverables | ARCHIVE_CANDIDATE | ARCHIVE |
| SERVICE_PROFILE_*_DELIVERABLE.md | docs/ | Service profile | ARCHIVE_CANDIDATE | ARCHIVE |
| P0_UI_*, P2_*_DELIVERABLES.md | docs/ | UI deliverables | ARCHIVE_CANDIDATE | ARCHIVE |
| STABILIZATION_PASS_*.md | docs/ | Stabilization | ARCHIVE_CANDIDATE | ARCHIVE |
| INSTALLER_PAYOUT_*.md | docs/ | Installer payout | ARCHIVE_CANDIDATE | ARCHIVE |
| TRACE_EXPLORER_*.md | docs/ | Trace explorer | ACTIVE | KEEP (runbook) |
| JOB_OBSERVABILITY_*.md | docs/ | Job observability | ACTIVE | KEEP |
| SLA_*.md | docs/ | SLA | ACTIVE | KEEP |
| FINANCIAL_LEDGER_READINESS_AUDIT.md | docs/ | Ledger audit | ACTIVE | KEEP |
| SYSTEM_HARDENING_AUDIT.md | docs/ | Hardening | ACTIVE | KEEP |
| REPORTS_HUB_*.md | docs/ | Reports hub | ACTIVE | KEEP |
| SYNCFUSION_*.md, THEME_*, GLOBAL_STYLES_*.md | docs/ | UI/style | ACTIVE | KEEP |
| UI_CONSISTENCY_*.md | docs/ | UI consistency | ACTIVE | KEEP |
| PROJECT_TASKS.md | docs/ | Project tasks | UNKNOWN | REVIEW |
| RELEASE_NOTES_v1.0.md | docs/ | Release notes | ACTIVE | KEEP |
| ADMIN_API_SAFETY.md | docs/ | Admin API safety | ACTIVE | KEEP |
| CLEANUP_COMPLETE_SUMMARY.md | docs/ | Cleanup (if at root) | ARCHIVE_CANDIDATE | ARCHIVE |

### 3.7 Docs root – parser / backend evidence (archive candidates)

| File | Current Path | Likely Topic | Status | Recommended Action |
|------|--------------|--------------|--------|---------------------|
| backend-ingestion-chain-audit-20260303.md | docs/ | Ingestion audit | ARCHIVE_CANDIDATE | ARCHIVE |
| parser-*.md (evidence/plan) | docs/ | Parser evidence | ARCHIVE_CANDIDATE | ARCHIVE |
| scheduler-processor-evidence-20260303.md | docs/ | Scheduler evidence | ARCHIVE_CANDIDATE | ARCHIVE |
| architecture-and-production-hardening-plan.md | docs/ | Hardening plan | ACTIVE | KEEP |

### 3.8 Docs root – pricing / order / ledger (reference)

| File | Current Path | Likely Topic | Status | Recommended Action |
|------|--------------|--------------|--------|---------------------|
| VERSIONED_PRICING_ARCHITECTURE.md | docs/ | Pricing architecture | ACTIVE | KEEP |
| ORDER_SCOPE_RESOLVER_ARCHITECTURE.md | docs/ | Scope resolver | ACTIVE | KEEP |
| ORDER_PROFITABILITY.md, ORDER_FINANCIAL_ALERTS.md | docs/ | Order finance | ACTIVE | KEEP |
| LEDGER_APPEND_CONFLICT_HANDLING.md | docs/ | Ledger conflict | ACTIVE | KEEP |
| PARSER_MATERIAL_*.md | docs/ | Parser material | ACTIVE | KEEP |
| SINGLE_EVENT_RETRY_SUPPRESS_ASYNC.md | docs/ | Retry/suppress | ACTIVE | KEEP |
| REPAIR_DUPLICATE_ASSURANCE_PARENT.md | docs/ | Repair | ACTIVE | KEEP |
| REGRESSION_SWEEP_*.md | docs/ | Regression | ARCHIVE_CANDIDATE | ARCHIVE |
| PAYOUT_ANOMALY_*.md (non-deliverable) | docs/ | Payout anomaly | ACTIVE | KEEP |

### 3.9 01_system

| File | Current Path | Likely Topic | Status | Recommended Action |
|------|--------------|--------------|--------|---------------------|
| _index.md, README.md | docs/01_system/ | Index | ACTIVE | KEEP |
| SYSTEM_OVERVIEW.md | docs/01_system/ | System overview | ACTIVE | KEEP |
| ARCHITECTURE_BOOK.md | docs/01_system/ | Architecture | ACTIVE | KEEP |
| ORDER_LIFECYCLE.md | docs/01_system/ | Order lifecycle (reference) | ACTIVE | KEEP |
| WORKFLOW_ENGINE.md, WORKFLOW_ENGINE_FLOW.md | docs/01_system/ | Workflow engine | ACTIVE | KEEP |
| EMAIL_PIPELINE.md | docs/01_system/ | Email pipeline | ACTIVE | KEEP |
| MULTI_COMPANY_ARCHITECTURE.md | docs/01_system/ | Multi-tenant (ref) | ACTIVE | KEEP |
| TECHNICAL_ARCHITECTURE.md, TECH_STACK.md | docs/01_system/ | Tech stack | ACTIVE | KEEP |
| SERVER_STATUS.md | docs/01_system/ | Server status | ACTIVE | KEEP (or MOVE to 08_infrastructure) |

### 3.10 02_modules

All module OVERVIEW, WORKFLOW, and topic-specific files under 02_modules/ are ACTIVE / KEEP. Duplicate storybook content vs 03_business is noted in DOCS_INVENTORY; 07_frontend/storybook kept as frontend-specific, 03_business as business.

### 3.11 03_business, 04_api, 05_data_model

| Area | Status | Recommended Action |
|------|--------|---------------------|
| 03_business/* | ACTIVE | KEEP; MULTI_COMPANY_STORYBOOK add "outdated – single-company" note |
| 04_api/* | ACTIVE | KEEP |
| 05_data_model/* (entities, relationships, indexes) | ACTIVE | KEEP |

### 3.12 06_ai

| File pattern | Status | Recommended Action |
|--------------|--------|---------------------|
| QUICK_START.md, DEVELOPER_GUIDE.md, DEV_HANDBOOK.md, CURSOR_ONBOARDING.md | ACTIVE | KEEP |
| Implementation notes, fix reports, SERVICE_INSTALLER_*, BUILDING_TYPE_*, etc. | LEGACY / ARCHIVE_CANDIDATE | KEEP in 06_ai or move to 99_appendix/implementation_notes; do not delete |

### 3.13 07_frontend, 08_infrastructure

| Area | Status | Recommended Action |
|------|--------|---------------------|
| 07_frontend/* | ACTIVE | KEEP; storybook mirrors 03_business – keep both, cross-link |
| 08_infrastructure/* | ACTIVE | KEEP |

### 3.14 architecture/, business/, dev/, integrations/, modules/, operations/, overview/

| Path | Status | Recommended Action |
|------|--------|---------------------|
| overview/product_overview.md | ACTIVE | KEEP |
| business/* | ACTIVE | KEEP |
| architecture/* | ACTIVE | KEEP |
| dev/onboarding.md | ACTIVE | KEEP |
| integrations/overview.md | ACTIVE | KEEP |
| modules/billing_and_invoicing.md, inventory_ledger_and_serials.md | ACTIVE | KEEP (canonical) |
| operations/* | ACTIVE | KEEP |

### 3.15 operations/ (EF migration and event ops)

| File | Current Path | Likely Topic | Status | Recommended Action |
|------|--------------|--------------|--------|---------------------|
| EF_*.md (migrations, recovery, rebaseline) | docs/operations/ | EF operations | ACTIVE | KEEP |
| EVENT_BUS_OPERATIONS.md, EVENT_DRIVEN_*.md, EVENT_STORE_*.md | docs/operations/ | Event ops | ACTIVE | KEEP |

### 3.16 Outside /docs

| File | Current Path | Likely Topic | Status | Recommended Action |
|------|--------------|--------------|--------|---------------------|
| README.md | repo root | Product splash | ACTIVE | KEEP (fix links per DOCS_STATUS) |
| AGENTS.md | repo root | Agent instructions | ACTIVE | KEEP |
| archive/new_docs_snapshot/* | archive/ | Old snapshot | ARCHIVE | KEEP in place (already archived) |
| backend/docs/*, backend/scripts/*.md | backend/ | Backend/scripts docs | ACTIVE | KEEP; consider pointer from docs/08_infrastructure or docs/operations |
| cursor/*.md | cursor/ | Cursor prompts | ACTIVE | KEEP (tooling, not product docs) |
| frontend/*.md, frontend/src/**/*.md | frontend/ | Frontend docs | ACTIVE | KEEP |
| tests/*.md | tests/ | Test scenarios | ACTIVE | KEEP |
| infra/README.md, scripts/README.md | repo root | Infra/scripts | ACTIVE | KEEP |
| CLEANUP_COMPLETE_SUMMARY.md | repo root (if present) | Cleanup | ARCHIVE_CANDIDATE | MOVE to docs/archive or remove if trivial |

---

## 4. Duplicates and Overlaps (Summary)

- **Event bus:** Multiple phase docs (Phase 1–7); PHASE_8_PLATFORM_EVENT_BUS.md + EVENT_BUS_PHASE8_9_SUMMARY.md as current; older phase docs → archive.
- **Storybook / business:** 03_business (STORYBOOK, PAGES, USER_FLOWS) and 07_frontend/storybook overlap; keep both, canonical business in 03_business.
- **Order lifecycle:** 01_system/ORDER_LIFECYCLE.md (reference) vs business/order_lifecycle_and_statuses.md (canonical); already documented in DOCS_MAP.
- **Billing / inventory:** modules/* canonical; 02_modules/billing, inventory as reference; DOCS_MAP already defines.

---

## 5. Inconsistencies to Repair

- **06_ai/CURSOR_ONBOARDING.md:** Fixed: replaced broken paths (EXEC_SUMMARY, architecture/ARCHITECTURE_OVERVIEW, storybook/, spec/api, spec/database, spec/frontend) with correct docs (01_system/SYSTEM_OVERVIEW, ARCHITECTURE_BOOK, 03_business/STORYBOOK, 04_api/, 05_data_model/, 07_frontend/).
- **Root README.md:** Ensure all links point to existing docs (00_QUICK_NAVIGATION, dev/onboarding, COMPLETION_STATUS_REPORT, etc.).
- **99_appendix:** Ensure README states "reference/legacy"; no orphan links from main indexes to deleted files.

---

## 6. Archive Location

- **Created:** `docs/archive/` for superseded/obsolete docs.
- **Archive note:** Each moved file gets a short note: why archived, what supersedes it, historical value.
- **Existing:** Repo root `archive/new_docs_snapshot/` remains unchanged (pre-existing snapshot).
