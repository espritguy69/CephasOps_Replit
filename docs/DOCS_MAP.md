# Docs Map – Required Documentation Set

**Source of truth:**  
- docs/_source/Codebase_Summary_SourceOfTruth.md  
- docs/_source/Business_Processes_SourceOfTruth.md

**Hard rules:** No invented business modules or flows; code vs docs conflicts → _discrepancies.md; single-company + department-scoped RBAC; consistent naming and cross-links.

---

## Required doc set (minimum A–P)

| ID | Required doc | Purpose | Existing doc that satisfies | Missing / to create | Status |
|----|----------------|---------|-----------------------------|----------------------|--------|
| **A** | Product overview | Business type (fibre/GPON contractor); core processes; tech stack summary | overview/product_overview.md | Single consolidated view | **DONE** |
| **B** | Business process flows | End-to-end main flow + side paths (email→order→schedule→field→docket→invoice→payment) | business/process_flows.md | One place for main + side paths | **DONE** |
| **C** | Department responsibilities & RBAC | Who does what; department-scoped access; roles | business/department_rbac.md | Consolidated | **DONE** |
| **D** | Order lifecycle & status/workflow | Status definitions; transitions; KPI responsibility; checklist gating; blocker/reschedule; docket/invoice loops; overrides | **business/order_lifecycle_and_statuses.md** | **Authoritative – GPON lifecycle and statuses** | **DONE** |
| **D-summary** | Order lifecycle summary | Short pointer to canonical lifecycle | **business/order_lifecycle_summary.md** | **Overview / pointer** | **DONE** |
| **E** | SI app journey + data captured | Login→schedule→on the way→met customer→complete/block; GPS, photos, splitter, ONU, signature | business/si_app_journey.md | SI journey and fieldwork data | **DONE** |
| **F** | Docket process | Receive→verify→reject→upload to partner | business/docket_process.md | Docket process in one place | **DONE** |
| **G** | Billing + MyInvois | Invoice creation; PDF; submission; rejection/reinvoice; who can act; payment matching; audit; out-of-scope | **modules/billing_and_invoicing.md** | **Authoritative (Billing)** | **DONE** |
| **G-summary** | Billing & MyInvois flow | Process overview; pointer to canonical billing | **business/billing_myinvois_flow.md** | **Overview / pointer** | **DONE** |
| **H** | Inventory/ledger model | Ledger as source of truth; serialised lifecycle; receive/allocate/issue/return; RMA; snapshots; who can act; audit; out-of-scope | **modules/inventory_ledger_and_serials.md** | **Authoritative (Inventory)** | **DONE** |
| **H-summary** | Inventory & ledger summary | Short overview; pointer to canonical inventory | **business/inventory_ledger_summary.md** | **Overview / pointer** | **DONE** |
| **I** | Payroll & rate engine | SI rate plans; job type/level/KPI; payroll periods; export | 02_modules/payroll/OVERVIEW.md; 02_modules/rate_engine/RATE_ENGINE.md | **business/payroll_rate_overview.md** | **DONE** |
| **J** | P&L analytics boundaries | Revenue (invoices); direct costs (materials, SI pay); overheads; not GL | 02_modules/pnl/OVERVIEW.md | **business/pnl_boundaries.md** | **DONE** |
| **K** | Integrations | Email POP3/IMAP; WhatsApp/SMS providers; OneDrive; MyInvois | Scattered in modules | **integrations/overview.md** | **DONE** |
| **L** | Background jobs / schedulers | Email ingest; stock snapshots; MyInvois poll; ledger reconcile; etc. | 02_modules/background_jobs/OVERVIEW.md; 08_infrastructure/background_jobs_infrastructure.md | **operations/background_jobs.md** | **DONE** |
| **M** | “Not handled yet” scope | Leads, statutory payroll, offline SI, partner API, multi-company, payment gateway | None | **operations/scope_not_handled.md** | **DONE** |
| **N** | Developer onboarding | Local setup; env vars; run scripts; architecture map | 06_ai/DEVELOPER_GUIDE.md; 06_ai/QUICK_START.md; README | **dev/onboarding.md** | **DONE** |
| **O** | API surface summary | Controllers grouped by module | 04_api/API_OVERVIEW.md; API_CONTRACTS_SUMMARY | **architecture/api_surface_summary.md** | **DONE** |
| **P** | Data model overview | Key entities and relationships | 05_data_model/DATA_MODEL_INDEX.md; REFERENCE_TYPES_AND_RELATIONSHIPS.md | Keep; add **architecture/data_model_overview.md** – short map | **DONE** |
| — | Implementation truth inventory | Single snapshot of schema, backend, frontend from code/EF; used to align docs | — | **docs/DOCS_IMPLEMENTATION_TRUTH_INVENTORY.md** | **DONE** (updated on Global Truth Guardian Sync runs) |
| — | Feature completion matrix | Backend/frontend/DB status per module; completion %; top blockers | — | **docs/COMPLETION_STATUS_REPORT.md** | **DONE** |
| — | Go-live readiness checklist | GPON production readiness (security, workflow, billing, SI, etc.) | — | **docs/GO_LIVE_READINESS_CHECKLIST_GPON.md** | **DONE** |
| — | Roadmap to completion | Phased plan (Phase 0–5) to production-ready | — | **docs/ROADMAP_TO_COMPLETION.md** | **DONE** |

---

## Authoritative vs reference-only (lifecycle)

- **business/order_lifecycle_and_statuses.md** – **Authoritative.** Single canonical GPON lifecycle and status spec.
- **business/order_lifecycle_summary.md** – **Overview / pointer** to the canonical doc.
- **01_system/ORDER_LIFECYCLE.md** – **Reference only.** Legacy; content migrated into business/order_lifecycle_and_statuses.md.
- **05_data_model/WORKFLOW_STATUS_REFERENCE.md** – **Reference only.** Legacy workflow status reference.

## Authoritative vs reference-only (billing)

- **modules/billing_and_invoicing.md** – **Authoritative.** Single canonical billing and invoicing spec (partner billing, MyInvois, payment matching, audit, out-of-scope).
- **business/billing_myinvois_flow.md** – **Overview / pointer** to the canonical billing doc.
- **02_modules/billing/OVERVIEW.md**, **02_modules/billing/WORKFLOW.md**, **02_modules/billing/MYINVOIS_AUTOMATIC_SUBMISSION.md** – **Reference only.** Legacy billing docs.

## Authoritative vs reference-only (inventory)

- **modules/inventory_ledger_and_serials.md** – **Authoritative.** Single canonical inventory, ledger and serialised-equipment spec (ledger as source of truth, stock lifecycle, serials, RMA, snapshots, who can act, audit, out-of-scope).
- **business/inventory_ledger_summary.md** – **Overview / pointer** to the canonical inventory doc.
- **02_modules/inventory/OVERVIEW.md**, **02_modules/inventory/WORKFLOW.md**, **02_modules/inventory/MATERIAL_POPULATION_RULES.md** – **Reference only.** Legacy inventory docs.

## Existing docs that satisfy (no rewrite)

- **01_system/ORDER_LIFECYCLE.md** – Reference only (see above). Canonical lifecycle is business/order_lifecycle_and_statuses.md.
- **02_modules/department/OVERVIEW.md** – Department module and responsibilities.
- **02_modules/inventory/OVERVIEW.md** – Reference only (see above). Canonical inventory is modules/inventory_ledger_and_serials.md.
- **02_modules/billing/OVERVIEW.md** (and billing spec) – Reference only (see above). Canonical billing is modules/billing_and_invoicing.md.
- **02_modules/payroll/OVERVIEW.md** – Payroll and SI earnings.
- **02_modules/pnl/OVERVIEW.md** – P&L model.
- **02_modules/rbac/OVERVIEW.md** – RBAC.
- **05_data_model/REFERENCE_TYPES_AND_RELATIONSHIPS.md** – Reference types.
- **RBAC_MATRIX_REPORT.md** – Department-scoped endpoints and roles.
- **04_api/** – API contracts and overview.
- **backend/docs/operations/PLATFORM_SAFETY_HARDENING_INDEX.md** – Index of all runtime safety guards protecting tenant isolation, financial correctness, workflow integrity, and event consistency. Links to detailed reports; includes safety layers diagram and developer PR checklist.
- **backend/docs/architecture/SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md** – Security and tenant-safety architecture diagram and narrative (layered view: inputs → resolution → TenantScopeExecutor → application guards → persistence → observability). For technical design reviews, compliance, and onboarding; includes intentional exceptions (DatabaseSeeder, ApplicationDbContextFactory).
- **docs/SAAS_PLATFORM_PROGRESS.md** – Master index of SaaS phases (readiness → production readiness → scaling → hardening → platform operations) with doc links and deliverables. Single place to see “are we documenting all our progress.”
- **docs/saas_readiness/**, **docs/saas_scaling/**, **docs/saas_operations/** – SaaS readiness testing, scaling architecture/operations/hardening, and platform operations (onboarding, billing, support, runbooks).

---

## Docs needing rewrite or update

- **README.md (root)** – Add single-company note; fix/remove broken refs (⭐_READ_THIS_FIRST, etc.) or note “see docs/”.
- **03_business/MULTI_COMPANY_STORYBOOK.md** – Done (Mar 2026): Status and Current app note added; links to overview and department_rbac. “Outdated: app is single-company”- **architecture/00_company-systems-overview.md** – Align to single-company or move to appendix.

---

## New folder structure (created when adding docs)

```
docs/
├── overview/          # A – Product overview
├── business/          # B–J – Process flows, department, order, SI, docket, billing, inventory summary, payroll, P&L
├── architecture/      # O, P – API surface, data model overview (existing architecture/ kept)
├── modules/           # Optional; 02_modules remains canonical for module specs
├── integrations/      # K – Integrations overview
├── operations/        # L, M – Background jobs, scope not handled
├── dev/               # N – Developer onboarding
├── _templates/        # Template for new docs
├── DOCS_MAP.md        # This file
├── DOCS_INVENTORY.md  # STEP 1 inventory
├── CHANGELOG_DOCS.md  # STEP 4
└── _discrepancies.md  # STEP 4
```

---

## Cross-linking convention

- Each new doc starts with **Related:** links to 2–5 related docs.
- Each new doc has **Source of truth:** “Codebase Summary (Senior Architect Review); Business Processes (Business Systems Analyst Report).”
- **Assumptions** section only if needed; list explicitly.
