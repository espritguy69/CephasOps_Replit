# Documentation Reorganization Report

**Date:** March 2026  
**Scope:** Documentation-only; no application code, database, or CI/CD logic changed.  
**Objective:** Turn `/docs` into a clean, living, maintainable documentation hub within the existing structure.

---

## 1. Summary

### What was wrong

- **Clutter:** Many one-time deliverable and audit docs (admin UM, GPON, payout, rate, RBAC, UI) sat in docs root alongside current reference docs.
- **Event bus sprawl:** Multiple phase docs (Phase 1–7, validation, rollout) mixed with current event bus docs (Phase 8, runbook, metadata contract).
- **Dated evidence:** Parser/scheduler/ingestion evidence and regression sweep docs lived in root with no clear “historical” boundary.
- **Broken references:** `06_ai/CURSOR_ONBOARDING.md` pointed to non-existent paths (EXEC_SUMMARY, architecture/ARCHITECTURE_OVERVIEW, storybook/, spec/api, spec/database, spec/frontend).
- **No single archive:** Superseded or obsolete docs had no dedicated place under `/docs` (repo root `archive/new_docs_snapshot` existed for an older snapshot only).

### What was improved

- **Archive under /docs:** Created `docs/archive/` with subfolders `event_bus/`, `deliverables/`, `evidence/`. Moved legacy event bus phase docs, one-time deliverables, and dated evidence/audit docs into archive.
- **Link fixes:** Updated `06_ai/CURSOR_ONBOARDING.md` to use existing paths (01_system, 03_business, 04_api, 05_data_model, 07_frontend).
- **Portal and navigation:** docs/README.md now includes navigation pointers, source-of-truth aligned folders, and archive; _INDEX.md and 00_QUICK_NAVIGATION.md updated with archive and folder structure.
- **Audit and governance:** Added DOCUMENTATION_AUDIT.md with full discovery, status, recommended actions, and topology; documented conventions and inconsistencies.

---

## 2. Existing /docs Structure Observed

- **Preserved:** Numbered sections 01_system through 08_infrastructure; 99_appendix; architecture/; source-of-truth aligned folders (overview/, business/, operations/, integrations/, dev/, modules/).
- **Conventions preserved:** OVERVIEW.md and WORKFLOW.md per module; _index.md in numbered sections; DOCS_MAP.md (A–P), DOCS_INVENTORY.md, DOCS_STATUS.md; small focused files; no replacement with a generic template.
- **New:** docs/archive/ (with README) for superseded and one-time docs only.

---

## 3. Files Moved

| Old path | New path | Reason |
|----------|----------|--------|
| docs/EVENT_BUS_IMPLEMENTATION_PHASE1.md | docs/archive/event_bus/ | Legacy phase; current is Phase 8 + runbook |
| docs/EVENT_BUS_PHASE2_EVENT_STORE.md | docs/archive/event_bus/ | Legacy phase |
| docs/EVENT_BUS_PHASE3_CORRELATION.md | docs/archive/event_bus/ | Legacy phase |
| docs/EVENT_BUS_PHASE4_FOLLOWUP_IMPLEMENTATION_PLAN.md | docs/archive/event_bus/ | Legacy phase |
| docs/EVENT_BUS_PHASE4_PRODUCTION.md | docs/archive/event_bus/ | Legacy phase |
| docs/EVENT_BUS_PHASE4_WORKFLOW_EVENTS.md | docs/archive/event_bus/ | Legacy phase |
| docs/EVENT_BUS_PHASE5_7_SUMMARY.md | docs/archive/event_bus/ | Legacy summary; Phase 8–9 summary kept in root |
| docs/EVENT_BUS_PRODUCTION_VALIDATION_REPORT.md | docs/archive/event_bus/ | One-time validation report |
| docs/EVENT_BUS_ROLLOUT_READINESS_SUMMARY.md | docs/archive/event_bus/ | One-time rollout summary |
| docs/backend-ingestion-chain-audit-20260303.md | docs/archive/evidence/ | Dated evidence |
| docs/parser-approve-building-dialog-audit.md | docs/archive/evidence/ | Dated evidence |
| docs/parser-list-not-updating-evidence.md | docs/archive/evidence/ | Dated evidence |
| docs/parser-not-updating-resolution-plan.md | docs/archive/evidence/ | Dated evidence |
| docs/parser-validation-status-evidence-20260303.md | docs/archive/evidence/ | Dated evidence |
| docs/scheduler-processor-evidence-20260303.md | docs/archive/evidence/ | Dated evidence |
| docs/REGRESSION_SWEEP_PAYOUT_SNAPSHOT.md | docs/archive/evidence/ | One-time regression sweep |
| docs/ADMIN_USER_MANAGEMENT_*.md (multiple) | docs/archive/deliverables/ | One-time deliverable/audit docs |
| docs/GPON_PRICING_DEBUG_FEATURE_DELIVERABLE.md | docs/archive/deliverables/ | One-time deliverable |
| docs/ORDER_PAYOUT_SNAPSHOT_DELIVERABLE.md, ORDER_TYPES_*.md | docs/archive/deliverables/ | One-time deliverable |
| docs/PAYOUT_*_DELIVERABLE.md (several) | docs/archive/deliverables/ | One-time deliverable |
| docs/RATE_*_DELIVERABLE.md (several) | docs/archive/deliverables/ | One-time deliverable |
| docs/RBAC_V2_PERMISSION_MATRIX_DELIVERABLE.md | docs/archive/deliverables/ | One-time deliverable |
| docs/P0_UI_*, P2_*_DELIVERABLES.md | docs/archive/deliverables/ | One-time deliverable |
| docs/STABILIZATION_PASS_*, INSTALLER_PAYOUT_*, SERVICE_PROFILE_*_DELIVERABLE.md | docs/archive/deliverables/ | One-time deliverable |

---

## 4. Files Merged

No files were merged in this pass. DOCS_MAP.md already defines canonical vs reference docs (e.g. business/order_lifecycle_and_statuses.md canonical; 01_system/ORDER_LIFECYCLE reference). Event bus consolidation was done by moving older phase docs to archive and keeping PHASE_8_PLATFORM_EVENT_BUS.md, EVENT_BUS_PHASE8_9_SUMMARY.md, and EVENT_BUS_OPERATIONS_RUNBOOK.md as the current set.

---

## 5. Files Archived

All moved files listed in §3 are considered archived. Archive location: `docs/archive/` with subfolders:

- **archive/event_bus/** – Phase 1–4 and Phase 5–7 summary, production validation, rollout readiness. Superseded by PHASE_8_PLATFORM_EVENT_BUS.md and EVENT_BUS_OPERATIONS_RUNBOOK.md.
- **archive/deliverables/** – Admin user management, GPON, order, payout, rate, RBAC, UI, stabilisation, installer payout, service profile deliverables. Historical record only.
- **archive/evidence/** – Dated parser/scheduler/ingestion audits and regression sweep. Historical evidence only.

See [archive/README.md](./archive/README.md) for summary and pointers to current docs.

---

## 6. New Docs Created

| File | Purpose |
|------|---------|
| docs/DOCUMENTATION_AUDIT.md | Full documentation discovery; status and recommended actions; topology; duplicates and inconsistencies. |
| docs/archive/README.md | Explains archive purpose, contents summary, and pointers to current docs. |
| docs/DOCUMENTATION_REORGANIZATION_REPORT.md | This report. |

No new feature or architecture docs were added; DOCS_MAP A–P were already satisfied (overview, business, operations, dev, architecture, modules).

---

## 7. Link Fixes

| Location | Change |
|----------|--------|
| docs/06_ai/CURSOR_ONBOARDING.md | Replaced broken paths (EXEC_SUMMARY, architecture/ARCHITECTURE_OVERVIEW, storybook/, spec/api, spec/database, spec/frontend) with correct links to 01_system/SYSTEM_OVERVIEW, ARCHITECTURE_BOOK, 03_business/STORYBOOK, 04_api/, 05_data_model/, 07_frontend/, 03_business/PAGES. |
| docs/README.md | Added navigation line (00_QUICK_NAVIGATION, _INDEX, DOCS_MAP, DOCS_STATUS). Added source-of-truth aligned and archive paragraphs. Fixed Operational Replay links to root-level phase docs. |
| docs/_INDEX.md | Archive section updated to point to archive/README.md and current event bus docs. |
| docs/00_QUICK_NAVIGATION.md | Added 99_appendix and archive to folder structure. |

No other references to the moved event-bus or evidence file names were found outside the audit table and the archived files themselves. Archived deliverables contain internal references to other archived files; those remain as historical context (same folder, so relative links still work where filenames are used).

---

## 8. Remaining Uncertainties

- **Root README.md:** Not fully audited for every link; DOCS_STATUS already notes fixing/verifying root README refs. Recommend a quick pass to ensure 00_QUICK_NAVIGATION, dev/onboarding, COMPLETION_STATUS_REPORT, go-live-smoke-test, etc. exist and are correct.
- **03_business/MULTI_COMPANY_STORYBOOK.md:** DOCS_INVENTORY marks as outdated (app single-company). A one-line “Outdated: app is single-company” note was not added in this pass; recommended as follow-up.
- **06_ai implementation notes:** Many implementation/fix notes in 06_ai could later be moved to 99_appendix or archive if they become purely historical; no change made in this pass to avoid scope creep.
- **Code/doc alignment:** DOCUMENTATION_AUDIT and DOCS_STATUS describe known gaps (e.g. single-company vs multi-company). Deeper drift was not verified against current code.

---

## 9. Recommended Next Steps

- **Maintain:** Keep DOCS_MAP, DOCS_INVENTORY, and DOCS_STATUS updated when adding or retiring docs. Use docs/archive/ for any future superseded or one-time docs.
- **On major code changes:** Update the corresponding doc under 02_modules, 05_data_model, or 04_api; update COMPLETION_STATUS_REPORT and DOCS_STATUS if needed.
- **Periodic:** Re-run a lightweight audit (e.g. link check, “required doc set” check from DOCS_MAP) and refresh DOCUMENTATION_AUDIT if the doc set grows or structure changes.
- **Optional:** Add a one-line “Last reviewed” or “Status: active” at the top of major docs only if the team adopts a light metadata style; not applied globally in this pass.

---

## 10. Living-docs governance pass (March 2026)

A follow-up **Level 11 living-docs** pass synchronized governance files and single-company clarity.

| What | Detail |
|------|--------|
| **DOCS_MAP** | Required doc set A–P marked **DONE**. |
| **Single-company** | 03_business/MULTI_COMPANY_STORYBOOK.md given Status and Current app note; pointers to overview and department_rbac. |
| **DOCS_INVENTORY** | Last run March 2026; §10 Archive; §11 Duplicates refreshed; Summary updated. |
| **DOCUMENTATION_ALIGNMENT_CHECKLIST** | Living-docs pass note added. |
| **DOCS_STATUS** | Last audit March 2026; Mar 2026 bullet. |
| **CHANGELOG_DOCS** | 2026-03 living-docs entry. |

Repo conventions preserved; documentation-only; no code changes.

---

**End of report.** Active documentation entry: [docs/README.md](./README.md) and [00_QUICK_NAVIGATION.md](./00_QUICK_NAVIGATION.md).
