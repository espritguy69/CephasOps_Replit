# Documentation Changelog (Source-of-Truth Alignment)

**Source of truth:**  
- docs/_source/Codebase_Summary_SourceOfTruth.md  
- docs/_source/Business_Processes_SourceOfTruth.md

This log lists docs created or updated to align with the above. No business modules or flows were invented beyond what those sources describe.

---

## Created (new)

| Doc | What changed | Why |
|-----|----------------|-----|
| **docs/DOCS_INVENTORY.md** | New inventory of existing docs with purpose and status (OK/OUTDATED/MISSING/DUPLICATE/NEEDS-MOVE). | STEP 1: Single place to see what exists and how it aligns to source of truth. |
| **docs/DOCS_MAP.md** | Required doc set (A–P); mapping to existing vs missing; new folder structure; cross-linking convention. | STEP 2: Define minimum doc set and where each is satisfied or must be created. |
| **docs/overview/product_overview.md** | Product overview: business type (fibre/GPON contractor), top 10 processes, tech stack, operating model. | Source of truth A: One consolidated product view. |
| **docs/business/process_flows.md** | End-to-end main flow + side paths (reschedule, blocker, docket reject, invoice reject, cancel). | Source of truth B: Single place for main and side flows. |
| **docs/business/department_rbac.md** | Department responsibilities (Operations, Field, Inventory, Finance, Settings); RBAC roles and department scoping; single-company. | Source of truth C: Department & RBAC in one place. |
| **docs/business/order_lifecycle_summary.md** | Short pointer to canonical lifecycle doc; status flow summary; key principles; legacy refs in subsection. | Source of truth D: Quick ref and link to canonical lifecycle. (Updated: now points to business/order_lifecycle_and_statuses.md; legacy links moved to “Legacy references”.) |
| **docs/business/si_app_journey.md** | SI journey (login → schedule → on the way → met customer → complete/block); data captured (GPS, photos, splitter, ONU, signature). | Source of truth E: SI journey and fieldwork data. |
| **docs/business/docket_process.md** | Docket receive → verify → reject → upload; rules from lifecycle. | Source of truth F: Docket process in one place. |
| **docs/business/billing_myinvois_flow.md** | Invoice creation; PDF; MyInvois submission and polling; rejection/reinvoice; payment; assurance/RMA. | Source of truth G: Billing and e-invoice flow. |
| **docs/business/inventory_ledger_summary.md** | Ledger as source of truth; serialised lifecycle; main operations (receive, allocate, issue, return). | Source of truth H: Inventory/ledger summary. |
| **docs/business/payroll_rate_overview.md** | SI rate plans; payroll calculation; partner billing rates; statutory out of scope. | Source of truth I: Payroll and rate engine overview. |
| **docs/business/pnl_boundaries.md** | P&L as analytics only; dimensions; data sources; not GL. | Source of truth J: P&L boundaries. |
| **docs/integrations/overview.md** | Email (POP3/IMAP), WhatsApp, SMS, MyInvois, OneDrive, partner portals (no API). | Source of truth K: Integrations in one place. |
| **docs/operations/background_jobs.md** | In-process jobs; hosted services; job types; UI. | Source of truth L: Background jobs and schedulers. |
| **docs/operations/scope_not_handled.md** | Leads, statutory payroll, leave/claims, CWO/NWO, partner API, offline SI, payment gateway, full GL, multi-company, barbershop/travel. | Source of truth M: “Not handled yet” scope. |
| **docs/dev/onboarding.md** | Local setup; env vars; run scripts; short architecture map; where to read next. | Source of truth N: Developer onboarding one-pager. |
| **docs/architecture/api_surface_summary.md** | Controllers grouped by module (core, organisation, settings, reports, other). | Source of truth O: API surface summary. |
| **docs/architecture/data_model_overview.md** | DB type; key entity groups; short relationships; ref to REFERENCE_TYPES. | Source of truth P: Data model overview. |
| **docs/_templates/doc_template.md** | Template with Related, Source of truth, sections, optional Assumptions. | Standard for new docs. |
| **docs/CHANGELOG_DOCS.md** | This file. | STEP 4: Track doc changes. |
| **docs/_discrepancies.md** | Code vs docs mismatches or unclear areas; resolution questions. | STEP 4: Record discrepancies. |

| **docs/business/order_lifecycle_and_statuses.md** | New **canonical** GPON order lifecycle and statuses: purpose/scope; full status list; allowed transitions and who triggers them; checklist gating; blocker/reschedule (TIME email, same-day evidence); docket rejection loop; invoice rejection/reinvoice loop; override rules; terminal states; KPI matrix; audit trail. Derived from legacy 01_system/ORDER_LIFECYCLE.md and 05_data_model/WORKFLOW_STATUS_REFERENCE.md. | Single source of truth for GPON lifecycle; authority moved from legacy docs into docs/business. |
| **docs/modules/billing_and_invoicing.md** | New **canonical** billing and invoicing spec: purpose and scope (partner billing only; not GL); billing prerequisites (docket uploaded, RMA approval); invoice lifecycle (ReadyForInvoice → Invoiced → InvoiceRejected/Reinvoice → Completed); MyInvois submission and background status polling; full regenerate vs simple correction; who can create/edit/submit/regenerate/resubmit; payment recording and matching; terminal billing states; audit and compliance; explicit out-of-scope (GL, payment gateway, partner APIs). Derived from business/billing_myinvois_flow.md and legacy 02_modules/billing. | Single source of truth for billing; authority under docs/modules. |
| **docs/modules/inventory_ledger_and_serials.md** | New **canonical** inventory, ledger and serialised-equipment spec: purpose and scope (materials + serials); ledger as sole source of truth (append-only); stock lifecycle (Receive → Allocate → Issue → Use → Return/Transfer); serialised equipment lifecycle (ONU, 2-in-1, etc.); order linkage rules; RMA and replacement handling; stock-by-location snapshots and reconciliation jobs; who can perform each action (Inventory vs Ops vs SI); audit rules and non-negotiable constraints; explicit out-of-scope (warehouse accounting, valuation, GL). Derived from business/inventory_ledger_summary.md and legacy 02_modules/inventory. | Single source of truth for inventory; authority under docs/modules. |
| **docs/operations/workflow_engine_validation_gpon.md** | New **workflow engine validation** doc: where GPON order transitions live (OrderStatus enum, WorkflowEngineService, OrderStatusesController fallback, InvoiceSubmissionService); actual transition table from code; status list code vs doc; Mermaid state diagram from code fallback; validation summary (status/transition counts). | Single place for extraction of implemented transition graph; used to validate canonical lifecycle doc. |

---

## Updated (this pass)

- **docs/business/order_lifecycle_summary.md** – “Authoritative document” now points to **docs/business/order_lifecycle_and_statuses.md** (canonical). Legacy links (01_system/ORDER_LIFECYCLE.md, 05_data_model/WORKFLOW_STATUS_REFERENCE.md) moved into a “Legacy references” subsection. Source-of-truth refs updated to docs/_source/*. Kept status flow and key principles unchanged for consistency.
- **docs/DOCS_MAP.md** – order_lifecycle_and_statuses.md marked as “Authoritative – GPON lifecycle and statuses”; order_lifecycle_summary.md marked as “Overview / pointer”; 01_system/ORDER_LIFECYCLE.md and 05_data_model/WORKFLOW_STATUS_REFERENCE.md labelled “Reference only”. Source-of-truth paths updated to docs/_source/*.
- **docs/CHANGELOG_DOCS.md** – Recorded creation of canonical lifecycle doc and authority correction. **Why:** Single source of truth alignment: authoritative docs live under docs/business; legacy 01_system/ORDER_LIFECYCLE.md is reference only. Recommended follow-ups: update root README (single-company note; fix broken refs); add “Outdated: single-company” to 03_business/MULTI_COMPANY_STORYBOOK.md; align architecture/00_company-systems-overview.md or move to appendix. See DOCS_MAP.md “Docs needing rewrite or update.”
- **docs/business/billing_myinvois_flow.md** – Converted to **process overview only**. Added “Authoritative document” section pointing to **docs/modules/billing_and_invoicing.md**. Moved legacy links (02_modules/billing/*) into “Legacy references (reference only)”. Flow content unchanged for consistency.
- **docs/business/inventory_ledger_summary.md** – Kept as short overview. Added “Authoritative document” section pointing to **docs/modules/inventory_ledger_and_serials.md**. Moved legacy links (02_modules/inventory/*, 05_data_model/REFERENCE_TYPES_AND_RELATIONSHIPS.md) into “Legacy references (reference only)”.
- **docs/DOCS_MAP.md** – Marked **modules/billing_and_invoicing.md** as Authoritative (Billing) and **modules/inventory_ledger_and_serials.md** as Authoritative (Inventory). Marked **business/billing_myinvois_flow.md** and **business/inventory_ledger_summary.md** as Overview/pointer. Added “Authoritative vs reference-only (billing)” and “Authoritative vs reference-only (inventory)” sections; labelled 02_modules/billing/* and 02_modules/inventory/* as Reference only.
- **docs/CHANGELOG_DOCS.md** – Recorded creation of both canonical billing and inventory docs and authority correction for business overview docs. **Rationale:** Single source of truth alignment: canonical module specs live under docs/modules; business docs are overview/pointer; legacy 02_modules billing and inventory docs are reference only.
- **docs/_discrepancies.md** – Added **section 10: Workflow engine vs canonical order lifecycle (GPON)**. Mismatches A–I: status naming/presence (DocketsVerified, SubmittedToPortal, DocketsRejected, InvoiceRejected vs Rejected); transition differences (Assigned → Blocker, Blocker → MetCustomer, docket/invoice rejection loops, billing path). Severity: BLOCKER (G, H), MAJOR (A–D, F, I), MINOR (E). Code locations and proposed resolutions (doc fix, code fix, or needs decision). Top 5 highest-risk listed.
- **docs/business/order_lifecycle_and_statuses.md** – **SubmittedToPortal** added as explicit step: in status list (item 11), Mermaid diagram (Invoiced → SubmittedToPortal → Completed / InvoiceRejected), text flow, transitions table, status definition, KPI matrix. New **section 14: Workflow engine validation (implementation)** with links to docs/operations/workflow_engine_validation_gpon.md and docs/_discrepancies.md (section 10). **Rationale:** Code is authoritative for SubmittedToPortal (InvoiceSubmissionService; OrderStatus enum); doc aligned to match; implementation differences (DocketsVerified, Rejected, Blocker → MetCustomer) left to discrepancies for decision.
- **docs/business/order_lifecycle_summary.md** – Short status flow updated to include SubmittedToPortal: Invoiced → SubmittedToPortal → (InvoiceRejected…)* → Completed.

---

## 2026-03: Architecture audit (Level 12)

**Rationale:** Verify architecture docs match code; detect drift; enforce source-of-truth; preserve /docs structure.

- **docs/ARCHITECTURE_AUDIT_REPORT.md** – **Created.** Executive summary; repository structure observed; verified architectural truths; drift findings (docs behind code); module boundary findings; documentation actions taken; remaining risks; recommended next docs.
- **docs/architecture/api_surface_summary.md** – **Updated.** New §6 Eventing, operational & observability (EventStoreController, EventLedgerController, EventsController, JobOrchestrationController, OperationalRebuild/Replay/Trace/TraceController, SystemWorkers/SystemScheduler, PayoutHealth, FinancialAlerts, NotificationsController, GPON rate controllers, AdminSecuritySessions). Notifications row in §5 clarified. Section 6 Convention renumbered to §7.
- **docs/operations/background_jobs.md** – **Updated.** Section 2 Hosted services: added NotificationDispatchWorkerHostedService, EventStoreDispatcherHostedService, EventBusMetricsCollectorHostedService, JobExecutionWorkerHostedService, WorkerHeartbeatHostedService, MissingPayoutSnapshotSchedulerService, JobPollingCoordinatorService.
- **docs/_discrepancies.md** – Last validated set to March 2026; architecture audit note and link to ARCHITECTURE_AUDIT_REPORT.
- **docs/DOCUMENTATION_ALIGNMENT_CHECKLIST.md** – Architecture audit (March 2026) pass and link to ARCHITECTURE_AUDIT_REPORT added.
- **docs/README.md** – Navigation line: added link to ARCHITECTURE_AUDIT_REPORT.
- **docs/00_QUICK_NAVIGATION.md** – Source-of-truth: added link to Architecture Audit Report.

---

## 2026-03: Living-docs governance pass (Level 11)

**Rationale:** Align documentation governance with code and business reality; keep DOCS_MAP, inventory, checklist, and indexes consistent; single-company clarity.

- **docs/DOCS_MAP.md** – Required doc set A–P marked **DONE** (all satisfied by overview/, business/, operations/, integrations/, dev/, architecture/, modules/). Existing-doc column updated to current paths.
- **docs/03_business/MULTI_COMPANY_STORYBOOK.md** – Added **Status** and **Current app** note at top: outdated for current deployment; single-company, multi-department; pointers to overview/product_overview.md and business/department_rbac.md.
- **docs/DOCS_INVENTORY.md** – Last run set to March 2026; status/action legend added; root README row set to OK; new §10 Archive (docs/archive/) with path, purpose, status, action, canonical replacements; §11 Duplicates and moves refreshed (DOCS_MAP A–P resolved); Summary updated.
- **docs/DOCUMENTATION_ALIGNMENT_CHECKLIST.md** – Living-docs pass note added: DOCS_MAP A–P DONE, MULTI_COMPANY_STORYBOOK note, inventory and governance refreshed.
- **docs/DOCS_STATUS.md** – Last audit set to March 2026; link-fixes section extended with Mar 2026 living-docs bullet (DOCS_MAP, MULTI_COMPANY_STORYBOOK, inventory, checklist, changelog).
- **docs/CHANGELOG_DOCS.md** – This entry. **Report:** docs/DOCUMENTATION_REORGANIZATION_REPORT.md updated with this pass.

---

## 2026-03: Codebase intelligence layer (Level 13)

**Rationale:** Build a permanent architecture intelligence layer: map modules, controllers, services, entities, workers, integrations; preserve /docs structure; docs-only.

- **docs/architecture/CODEBASE_INTELLIGENCE_MAP.md** – **Created.** Main intelligence hub: repository shape, runtime architecture, major domains/modules, core flows, architecture hotspots, governance cross-links.
- **docs/architecture/controller_service_map.md** – **Created.** Controller → main service(s), domain, dependencies, canonical docs.
- **docs/architecture/module_dependency_map.md** – **Created.** Upstream/downstream per module; ASCII dependency overview.
- **docs/architecture/background_worker_map.md** – **Created.** Hosted services and job types; purpose, trigger, dependencies.
- **docs/architecture/integration_map.md** – **Created.** External (email, WhatsApp, SMS, MyInvois, OneDrive, partner portals) and internal (event store, job orchestration, notification dispatch).
- **docs/architecture/entity_domain_map.md** – **Created.** Entities grouped by business area.
- **docs/CODEBASE_INTELLIGENCE_REPORT.md** – **Created.** Executive summary; architecture shape; artifacts; dependency findings; hotspots; coverage; governance updates; recommended next.
- **docs/README.md** – Navigation: added CODEBASE_INTELLIGENCE_MAP and CODEBASE_INTELLIGENCE_REPORT.
- **docs/00_QUICK_NAVIGATION.md** – New “Codebase intelligence” subsection with hub, report, and five maps.
- **docs/_INDEX.md** – Codebase Intelligence Map and Report plus “Codebase intelligence (relationship maps)” table.
- **docs/architecture/README.md** – New “Codebase intelligence” section with hub, five maps, report link.
- **docs/DOCUMENTATION_ALIGNMENT_CHECKLIST.md** – Codebase intelligence pass and link to report; checklist row for intelligence artifacts.
- **docs/DOCS_STATUS.md** – Mar 2026 codebase intelligence bullet in §2 Link and path fixes.

---

## 2026-03: Refactor safety audit (Level 14)

**Rationale:** Refactor safety auditor and dependency risk analysis; identify fragile modules, high coupling, hidden dependencies, safe/danger zones, and worker dependency risks; documentation only.

- **docs/REFACTOR_SAFETY_REPORT.md** – **Created.** Executive summary; overall refactor safety level; high-coupling modules; hidden dependencies; fragile modules; safe/danger zones; worker dependency risks; suggested refactor strategy; list of artifacts.
- **docs/architecture/high_coupling_modules.md** – **Created.** Modules ranked by coupling risk (Orders, Workflow, Billing, Scheduler, Inventory, etc.) with reasons.
- **docs/architecture/hidden_dependencies.md** – **Created.** Service→service (constructor and runtime GetRequiredService); cross-domain DbContext access (BuildingService, BillingService, SchedulerService→Orders/Invoices); workers affecting multiple modules.
- **docs/architecture/module_fragility_map.md** – **Created.** Per-module fragility (size, coupling, worker usage, operational criticality).
- **docs/architecture/safe_refactor_zones.md** – **Created.** Lower-risk areas: reports, settings CRUD, admin/auth, non-critical jobs, Tasks, Assets, RMA, Files, messaging templates, diagnostics, event bus metrics.
- **docs/architecture/refactor_danger_zones.md** – **Created.** High-risk areas: order lifecycle, billing/invoice/MyInvois, inventory ledger, event store/dispatcher, workflow guards/side effects, rates/payroll, Scheduler–Order–Workflow triangle, Parser→Order, notification dispatch; mitigation notes.
- **docs/architecture/refactor_sequence_plan.md** – **Created.** Suggested refactor order (1–20): Reports → Settings → … → Orders (status) → Event system.
- **docs/architecture/worker_dependency_risks.md** – **Created.** Worker service dependencies; hidden coupling (BackgroundJobProcessorService, EventStoreDispatcherHostedService, OrderStatusChangedNotificationHandler); workers affecting multiple modules.
- **docs/architecture/CODEBASE_INTELLIGENCE_MAP.md** – Governance cross-links: added REFACTOR_SAFETY_REPORT and refactor-safety docs.
- **docs/README.md** – Navigation: added REFACTOR_SAFETY_REPORT.
- **docs/00_QUICK_NAVIGATION.md** – Refactor Safety Report link; new “Refactor safety (Level 14)” subsection with all refactor-safety docs.
- **docs/_INDEX.md** – Refactor Safety Report in Source-of-Truth table; new “Refactor safety (architecture)” table with seven docs.
- **docs/DOCS_STATUS.md** – Mar 2026 refactor safety bullet in §2.

---

## 2026-03: Architecture watchdog (Level 15)

**Rationale:** Continuous architecture watchdog—detect drift, service/controller sprawl, dependency leaks, worker coupling growth, module boundary regression; refresh risk maps only if changed; documentation and governance only.

- **docs/ARCHITECTURE_WATCHDOG_REPORT.md** – **Created.** Executive summary; overall health (stable); no new drift; service sprawl findings (OrderService, WorkflowEngineService, BackgroundJobProcessorService, SchedulerService, etc.); controller sprawl; dependency leak findings; worker coupling; module boundary status; refactor risk (unchanged); documentation updates; remaining concerns; suggested next actions.
- **docs/architecture/service_sprawl_watch.md** – **Created.** Services with sprawl risk (constructor deps, many callers, runtime resolution); monitoring priority P1/P2.
- **docs/architecture/controller_sprawl_watch.md** – **Created.** Controller families and sprawl risk; 113 controller files; no high sprawl.
- **docs/architecture/dependency_leak_watch.md** – **Created.** Leak register (runtime resolution, cross-domain DbContext); suspected Scheduler–Workflow cycle.
- **docs/architecture/worker_coupling_watch.md** – **Created.** Worker coupling level and risk trend; 15 hosted services; stable.
- **docs/architecture/module_boundary_regression.md** – **Created.** Per-module boundary status (stable / drifting); Orders, Workflow, Scheduler, Billing, Buildings drifting; others stable.
- **docs/architecture/CODEBASE_INTELLIGENCE_MAP.md** – Governance cross-links: ARCHITECTURE_WATCHDOG_REPORT and five watch docs.
- **docs/_discrepancies.md** – Level 15 watchdog run note.
- **docs/DOCUMENTATION_ALIGNMENT_CHECKLIST.md** – Watchdog pass and link to ARCHITECTURE_WATCHDOG_REPORT.
- **docs/DOCS_STATUS.md** – Level 15 watchdog bullet in §2.
- **docs/README.md** – Navigation: added ARCHITECTURE_WATCHDOG_REPORT.
- **docs/00_QUICK_NAVIGATION.md** – Architecture Watchdog Report link; new “Architecture watchdog (Level 15)” subsection with report and five watch docs.
- **docs/_INDEX.md** – Architecture Watchdog Report in Source-of-Truth table; new “Architecture watchdog (Level 15)” table with five watch docs.
- **Refactor safety docs** (high_coupling_modules, hidden_dependencies, module_fragility_map, worker_dependency_risks, refactor_danger_zones, refactor_sequence_plan): Not updated; no material code change since Level 14.

---

## 2026-03: Architecture governance systems index

**Rationale:** Single reference for named architecture governance systems and confirmation that portal and governance logs are updated.

- **docs/architecture/ARCHITECTURE_GOVERNANCE_SYSTEMS.md** – **Created.** Index of: (1) Change Impact Predictor → refactor_sequence_plan, module_dependency_map, hidden_dependencies, dependency_leak_watch; (2) Architecture Policy Engine → high_coupling_modules, refactor_danger_zones, safe_refactor_zones, module_boundary_regression, service_sprawl_watch; (3) Auto Documentation Sync → CODEBASE_INTELLIGENCE_MAP refresh triggers, DOCUMENTATION_ALIGNMENT_CHECKLIST, DOCS_MAP, DOCS_STATUS, CHANGELOG_DOCS, _discrepancies; (4) Architecture Risk Dashboard → REFACTOR_SAFETY_REPORT, ARCHITECTURE_WATCHDOG_REPORT, module_fragility_map, worker_dependency_risks; (5) Self-Maintaining Architecture → refresh triggers and watchdog trigger rules; (6) Portal navigation → README, 00_QUICK_NAVIGATION, _INDEX, architecture/README; (7) Governance logs → CHANGELOG_DOCS, DOCS_STATUS, _discrepancies, DOCUMENTATION_ALIGNMENT_CHECKLIST.
- **docs/architecture/CODEBASE_INTELLIGENCE_MAP.md** – Governance cross-links: added ARCHITECTURE_GOVERNANCE_SYSTEMS.
- **docs/README.md, 00_QUICK_NAVIGATION.md, _INDEX.md, architecture/README.md** – Links to ARCHITECTURE_GOVERNANCE_SYSTEMS.md.

---

## 2026-02-09: Global Truth Guardian Sync (docs-only)

- **docs/DOCS_IMPLEMENTATION_TRUTH_INVENTORY.md** – **Created.** Single implementation truth snapshot: Live DB status (NO this run); tables from EF snapshot; reference data from DatabaseSeeder and seed SQL; backend entities, enums, controllers, hosted services, job types, integrations; frontend routes and major page fields/filters. No code or DB writes.
- **docs/_discrepancies.md** – **Appended:** section "Global Sync Run — Code + UI + Live PostgreSQL (2026-02-09)" with doc-vs-inventory classification (MATCH/STALE/MISSING), severity (BLOCKER/MAJOR/MINOR), required actions. Tally: BLOCKER 0, MAJOR 0, MINOR 5.
- **docs/DOCS_INVENTORY.md** – Added row for DOCS_IMPLEMENTATION_TRUTH_INVENTORY.md.
- **docs/DOCS_MAP.md** – Added Implementation Truth Inventory to required doc set (updated on sync runs).
- **docs/_INDEX.md** – Added Implementation Truth Inventory to Source-of-Truth Aligned table.
- **docs/00_QUICK_NAVIGATION.md** – Added link to Implementation Truth Inventory and Docs Inventory under Source-of-Truth.
- **docs/architecture/data_model_overview.md** – Added §5 Full table list with pointer to DOCS_IMPLEMENTATION_TRUTH_INVENTORY.md and ApplicationDbContextModelSnapshot.

---

## 2026-02-09: Completion Assessment Run (Feature Matrix, Go-Live, Roadmap)

- **docs/COMPLETION_STATUS_REPORT.md** – **Created.** Feature completion matrix: backend/frontend/DB status per module (Orders, Scheduler, SI app, Dockets, Billing, MyInvois, Inventory, Rates, Payroll, P&L, Workflow, etc.); completion % with evidence-based rationale; top 10 blockers by impact; known discrepancies link; reality vs docs notes.
- **docs/GO_LIVE_READINESS_CHECKLIST_GPON.md** – **Created.** GPON production readiness checklist: Security/RBAC; data integrity (ledger, serials); workflow correctness (DB-driven, seeded); billing/MyInvois; SI app usability; monitoring/logging; backup/restore; admin settings completeness.
- **docs/ROADMAP_TO_COMPLETION.md** – **Created.** Phased roadmap: Phase 0 (stabilize truth & governance); Phase 1 (core ops hardening); Phase 2 (billing hardening); Phase 3 (inventory/ledger); Phase 4 (reporting/P&L); Phase 5 (nice-to-haves, CWO/NWO future). Each phase: objective, scope, deliverables, acceptance criteria, dependencies, risks, definition of done.
- **docs/DOCS_INVENTORY.md** – Added rows for COMPLETION_STATUS_REPORT, GO_LIVE_READINESS_CHECKLIST_GPON, ROADMAP_TO_COMPLETION.
- **docs/DOCS_MAP.md** – Added required doc set entries for completion report, go-live checklist, roadmap.
- **docs/_INDEX.md** – Added links to Completion Status Report, Go-Live Readiness Checklist, Roadmap to Completion in Source-of-Truth Aligned table.
- **docs/00_QUICK_NAVIGATION.md** – Added quick links to Completion Status Report, Go-Live Readiness Checklist, Roadmap to Completion under Source-of-Truth.
- **Rationale:** Program Manager + Principal Architect + QA Lead assessment; fact-based evidence from code, UI routes, migrations; docs-only output; no code changes.

---

## 2026-02-09: Partner–Category derived label (display only)

- **docs/business/reference_data_taxonomy.md** – Added **Partner–Category labels (locked rule)** under Partners (Implemented): labels such as TIME-FTTH are derived from Partner.Code and OrderCategory.Code for display only and are not persisted; no composite partner rows. Link to taxonomy trace.
- **docs/architecture/taxonomy_trace_installation_partner.md** – Added **Locked rule** in the definitions section: Partner–Category labels are derived from Partner.Code and OrderCategory.Code for display only and are not persisted; computed in backend mapping/projection and exposed as `derivedPartnerCategoryLabel`.
- **Rationale:** Single source of truth for the taxonomy rule; aligns code (OrderDto, PnlDetailPerOrderDto, scheduler DTOs with derivedPartnerCategoryLabel) and UI (orders list/detail, P&L drilldown, filters by partnerId) with docs.

---

## 2026-02-03: Workflow engine validation run

- **docs/operations/workflow_engine_validation_gpon.md** – Validation run: added DB seed (create-order-workflow-if-missing.sql), EmailIngestionService bypass note, overrides/side-effects section, §6 canonical vs code comparison. Last validation date: 2026-02-03.
- **docs/_discrepancies.md** – Added §10.3 Workflow bypasses (J: EmailIngestionService direct status updates); renumbered §10.4 Summary; added validation reference.

---

## 2026-02-03: Option A – Doc alignment to engine truth

**Rationale:** Treat DocketsVerified and SubmittedToPortal as real OrderStatus values (not milestones). Docs updated to match workflow engine/code truth.

- **docs/business/order_lifecycle_and_statuses.md** – Canonical GPON status chain updated: DocketsVerified between DocketsReceived and DocketsUploaded; SubmittedToPortal between Invoiced and Completed. DocketsRejected removed (not in code). Blocker → MetCustomer added. Transition table, Mermaid diagram, status definitions (§5), docket verification (§8), KPI matrix updated. Short definitions for DocketsVerified and SubmittedToPortal (purpose, required evidence, who triggers).
- **docs/business/order_lifecycle_summary.md** – Status flow line updated: DocketsReceived → DocketsVerified → DocketsUploaded → ReadyForInvoice → Invoiced → SubmittedToPortal.
- **docs/operations/workflow_engine_validation_gpon.md** – §6 Comparison with canonical: alignment table updated; DocketsVerified and SubmittedToPortal marked aligned. §5 Validation summary and §2 Notes updated for Option A.
- **docs/_discrepancies.md** – A, B, C, G, I marked **Resolved (Option A)**. J (EmailIngestionService) escalated to **BLOCKER** until remediated. F (Blocker exits): MAJOR with recommended fix—allow both Blocker→Assigned and Blocker→MetCustomer via DB workflow roles.

---

## 2026-02-03: Status semantics and upload clarifications

- **docs/business/order_lifecycle_and_statuses.md** – SubmittedToPortal definition corrected: explicitly about **partner portal** (e.g. TIME portal) submission, not MyInvois itself. Clarified relationship: MyInvois submissionId is stored on Invoice (billing); SubmittedToPortal is order status indicating invoice submitted to partner. Added **§8.1 Upload semantics**: table clarifying DocketsUploaded (docket → partner portal) vs SubmittedToPortal (invoice → partner portal); MyInvois as separate billing/regulatory step. Updated text flow, transitions table, and checklist references accordingly.
- **docs/business/status_semantics_map.md** – New 1-page table: Status, Owner, Required fields, Meaning, Evidence for all 17 order statuses (Pending through Cancelled). Quick reference derived from order_lifecycle_and_statuses.md.

---

## 2026-02-03: EmailIngestionService – allowed direct status writes

**Decision:** EmailIngestionService may set **only `Pending`** directly. Rationale: Pending = order intake only; all subsequent transitions must go through WorkflowEngineService. Direct writes to Cancelled, Blocker, or any other status are disallowed. When setting Pending: create OrderStatusLog, source=EmailIngestionService, timestamp + parser/session reference.

- **docs/_discrepancies.md** – J: updated proposed resolution with allowed exception (Pending only) and audit requirement; Cancelled/Blocker direct writes remain BLOCKER violations.
- **docs/operations/workflow_engine_validation_gpon.md** – Bypasses row: clarified allowed (Pending only) vs violations (Cancelled, Blocker).

---

## 2026-02-09: Reference data taxonomy (settings)

- **docs/business/reference_data_taxonomy.md** – **New.** Reference data (settings taxonomy) in two clear parts: **Part A — Implemented Reference Data (Authoritative)** extracted from code and DB seeds (Order Types, Order Categories, Building Types, Departments, Installation Types, Installation Methods, Partners); **Part B — Suggested / Recommended Reference Data (Not Implemented)** with items labelled as suggestions only (e.g. Termination/Relocation/General order types, CWO/NWO departments, TIME/CelcomDigi/U Mobile as partners). Governance disclaimer at top: Part A = system truth; Part B = recommendations only. No invented implemented values; no merging of suggested into implemented.
- **docs/00_QUICK_NAVIGATION.md** – Added link to Reference Data Taxonomy under source-of-truth aligned.
- **docs/_INDEX.md** – Added Reference Data Taxonomy to source-of-truth table.
- **docs/CHANGELOG_DOCS.md** – This entry.

---

## 2026-02-09: Workflow validation → stable docs and baseline spec

**Goal:** Turn workflow validation results into stable, non-contradictory documentation and a configuration plan.

- **docs/business/order_lifecycle_and_statuses.md** – **Workflow authority:** Added explicit note that the DB workflow (WorkflowDefinition + WorkflowTransitions) is authoritative; the fallback controller graph (OrderStatusesController) is incomplete/minimal and must not be relied on. Linked to new [db_workflow_baseline_spec](operations/db_workflow_baseline_spec.md). **Docket “rejection”:** Clarified that it is documented as a **verification failure outcome**, not an OrderStatus; DocketsRejected does not exist in enum/DB; docket path remains DocketsReceived → DocketsVerified → DocketsUploaded. **Invoice rejection/reinvoice loop (§9):** Added note that the loop depends on DB workflow transitions; the fallback does not implement it; if only billing-level tracking is required, it can be tracked on the Billing entity without order status transitions.
- **docs/_discrepancies.md** – **Mismatch G (docket rejection loop):** Reclassified as **RESOLVED**; docs no longer claim DocketsRejected as a status; rejection is a verification failure outcome only. **Mismatch H (invoice rejection loop):** Clarified that whether the loop is tracked at OrderStatus level vs Billing entity level depends on DB workflow; keep as BLOCKER only if production requires it at order status level; otherwise track via Billing entity and reclassify as OPEN/MAJOR. Summary updated accordingly.
- **docs/operations/db_workflow_baseline_spec.md** – **New.** Minimum GPON transition set required in DB to support canonical flow: scheduling (Pending → Assigned → OnTheWay → MetCustomer), Blocker exits (Blocker → Assigned, Blocker → MetCustomer, plus ReschedulePendingApproval, Cancelled), docket path (DocketsReceived → DocketsVerified → DocketsUploaded), billing path (ReadyForInvoice → Invoiced → SubmittedToPortal → Completed). Optional: InvoiceRejected/Reinvoice loop (if product requires it). Includes AllowedRolesJson guidance per transition and override/optional transition notes.
- **docs/CHANGELOG_DOCS.md** – This entry.

---

## 2026-02-09: Phase 0 + Phase 1 — GPON Go-Live Readiness Execution

**Phase 0 — Stabilise Truth & Governance: COMPLETE**

- **0.1 Workflow engine seeding:** `07_gpon_order_workflow.sql` added (idempotent). All GPON order lifecycle transitions seeded: Pending→Assigned→OnTheWay→MetCustomer; Blocker exits (Assigned, MetCustomer, ReschedulePendingApproval, Cancelled); Docket path (DocketsReceived→DocketsVerified→DocketsUploaded); Billing path (ReadyForInvoice→Invoiced→SubmittedToPortal→Completed); Invoice rejection loop. Script registered in `run-all-seeds.ps1`.
- **0.2 Naming discrepancy:** Code/DB retain `Rejected`; display name "Invoice Rejected"; docs use conceptual InvoiceRejected. Documented in OrderStatus.cs, _discrepancies.md CLOSED.
- **0.3 Blocker→Assigned policy:** Added to DB workflow with AllowedRolesJson `["Ops","Admin","HOD"]`; fallback in OrderStatusesController. Rationale documented in order_lifecycle_and_statuses.md. Discrepancy F CLOSED.
- **0.4 Governance lock:** All Phase 0 items marked CLOSED in _discrepancies.md.

**Phase 1 — Core Ops Hardening: COMPLETE**

- **1.1 Docket Admin UI:** `/operations/dockets` page; filter by DocketsReceived/DocketsVerified; checklist (splitter, port, ONU, photos); Verify (→ DocketsVerified), Mark Uploaded (→ DocketsUploaded); docket file upload via Files API.
- **1.2 Orders UI visibility:** OrderCategory (Installation Type), InstallationMethod, derivedPartnerCategoryLabel shown in list; Technical Details section in OrderDetailPage.
- **1.3 MyInvois production runbook:** `docs/operations/myinvois_production_runbook.md` — credentials, env vars, submission flow, status polling, rejection handling, finance verification, known failure modes.

**Docs updated:** COMPLETION_STATUS_REPORT.md, GO_LIVE_READINESS_CHECKLIST_GPON.md, ROADMAP_TO_COMPLETION.md.

---

## 2026-02-09: Phase 2 — Billing Hardening (GPON)

**Phase 2 — Billing Hardening: COMPLETE**

- **Partner portal manual process:** `docs/operations/partner_portal_manual_process.md` — step-by-step docket upload and invoice submission to TIME/partner portal; no API integration.
- **MyInvois runbook:** IntegrationSettings UI path clarified (Settings → Integrations → MyInvois tab).
- **Invoice rejection loop:** Verified — WorkflowTransitionButton on OrderDetailPage shows Rejected→ReadyForInvoice, Rejected→Reinvoice, Reinvoice→Invoiced from fallback and 07_gpon_order_workflow.sql.
- **Integrations overview:** Linked to partner portal manual process doc.
- **ROADMAP:** Phase 2 marked complete.

---

## 2026-02-09: Phase 3 — Inventory + Ledger Hardening (GPON)

**Phase 3 — Inventory + Ledger Hardening: COMPLETE**

- **LedgerReconciliationSchedulerService:** New HostedService enqueues ReconcileLedgerBalanceCache job every 12h (no duplicate pending). Registered in Program.cs.
- **background_jobs.md:** Added LedgerReconciliationSchedulerService to schedulers table.
- **inventory_ledger_ops_runbook.md:** New ops runbook — ledger integrity, stock-by-location, serial lifecycle, RMA path, reports, movement validation.
- **GO_LIVE checklist §2:** All items (2.1–2.6) marked complete.
- **ROADMAP:** Phase 3 marked complete.

---

## 2026-02-09: Phase 4 — Reporting / P&L Operationalization (GPON)

**Phase 4 — Reporting / P&L Operationalization: COMPLETE**

- **PnlRebuildSchedulerService:** New HostedService enqueues PnlRebuild job daily (24h check; companyId + period from first active company). Registered in Program.cs.
- **background_jobs.md:** Added PnlRebuildSchedulerService to schedulers table.
- **reporting_pnl_ops_runbook.md:** New ops runbook — P&L rebuild, Reports Hub (export formats, department scope), KPI profiles, Dashboard.
- **ROADMAP:** Phase 4 marked complete.

---

## 2026-02-09: Phase 5 — Nice-to-Haves / Future Scope (GPON)

**Phase 5 — Future Scope Documentation: COMPLETE**

- **scope_not_handled.md:** GPON Go-Live note added; CWO/NWO explicitly "future; to be defined when activated"; Kingsman/Menorah "out of scope for GPON go-live"; link to partner_portal_manual_process; link to GO_LIVE_READINESS_CHECKLIST.
- **COMPLETION_STATUS_REPORT:** Overall GPON Go-Live Readiness marked Ready; Phase 5 complete; Docket line corrected (DocketsPage).
- **ROADMAP:** Phase 5 marked complete; immediate next step = go-live.

---

## Convention

- Each new doc includes **Related** and **Source of truth** at the top.  
- **Assumptions** only when needed; listed explicitly.  
- Docs kept short (aim 1–4 pages).  
- Single-company + department-scoped RBAC used consistently.
