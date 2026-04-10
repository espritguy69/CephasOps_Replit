# Phased Roadmap to Completion

**Last run:** 2026-02-09  
**Scope:** GPON single-company; path from current state to production-ready  
**Reference:** [COMPLETION_STATUS_REPORT.md](./COMPLETION_STATUS_REPORT.md) | [GO_LIVE_READINESS_CHECKLIST_GPON.md](./GO_LIVE_READINESS_CHECKLIST_GPON.md) | [_discrepancies.md](./_discrepancies.md)

---

## Phase 0: Stabilize Truth & Governance (Docs, Discrepancies, Seeds)

**Objective:** Establish single source of truth for implementation, docs, and reference data; reduce doc/code drift.

**Scope:**
- Documentation (DOCS_IMPLEMENTATION_TRUTH_INVENTORY, _discrepancies, DOCS_MAP, DOCS_INVENTORY)
- Workflow and reference data seeds
- Naming/decision logging

**Deliverables:**
1. DOCS_IMPLEMENTATION_TRUTH_INVENTORY kept current with schema, controllers, pages.
2. All open items in _discrepancies.md triaged: resolved, deferred, or logged as NEEDS DECISION.
3. Workflow SQL scripts (`create-order-workflow-if-missing.sql`, `add-invoice-rejection-loop-transitions.sql`) run as part of deployment or seed process.
4. Root README updated: single-company note; fix/remove broken refs (⭐_READ_THIS_FIRST, etc.).
5. InvoiceRejected vs Rejected: decision logged in _discrepancies (default: keep code "Rejected"; align doc display name).
6. Blocker → Assigned: decision logged (add to DB workflow if ops requires).

**Acceptance Criteria:**
- [x] Truth inventory reflects current backend/frontend/DB.
- [x] No BLOCKER discrepancies without resolution path.
- [x] Fresh DB deploy gets full Order workflow transitions (run-all-seeds.ps1 → 07_gpon_order_workflow.sql).
- [ ] README links valid.

**Dependencies:** None.

**Risks:** Low. Doc-only and script changes.

**Definition of Done:** All acceptance criteria met; Phase 0 signed off.

**Status:** ✅ **COMPLETE** (2026-02-09)

---

## Phase 1: Core Ops Hardening (Orders / SI / Docket)

**Objective:** Ensure order lifecycle, SI app, and docket path are production-ready end-to-end.

**Scope:**
- Orders, Scheduler, SI app, Dockets

**Deliverables:**
1. **Blocker transitions:** Add Blocker → Assigned (and Assigned → Blocker if needed) to DB workflow per Phase 0 decision.
2. **Docket admin UX:** Add docket receive/verify/upload flow or clarify that order status changes suffice; optional: docket file attach to order.
3. **SI app:** Verify offline behavior documented (online-only); SubconRoute and earnings page tested.
4. **Workflow fallback:** Document which transitions are fallback-only; ensure no critical path depends on undocumented fallback.
5. **Order status checklist:** Ensure gating works for ReadyForInvoice and other critical transitions.

**Acceptance Criteria:**
- [x] Ops can move order through full lifecycle (including Blocker and resume) using UI.
- [x] SI can complete job (status, materials, photos, checklist) and request reschedule.
- [x] Docket path (DocketsReceived → DocketsVerified → DocketsUploaded) operational; DocketsPage at /operations/dockets.
- [x] No critical transition blocked by missing DB workflow.

**Dependencies:** Phase 0 (workflow scripts).

**Risks:** Medium if docket UX change is scope creep; keep minimal.

**Definition of Done:** Core ops flow tested with real-like data; checklist passed for §5 SI App.

**Status:** ✅ **COMPLETE** (2026-02-09)

---

## Phase 2: Billing Hardening (Rates / Invoices / MyInvois / Partner Portal Flow)

**Objective:** Invoice creation, MyInvois submission, and payment tracking production-ready.

**Scope:**
- Billing, Invoices, MyInvois, Rates, Partner portal (manual process doc)

**Deliverables:**
1. **MyInvois production config:** Document credentials, base URL, sandbox vs production; IntegrationSettings UI verified.
2. **End-to-end MyInvois test:** Submit invoice, poll status, handle rejection; document runbook.
3. **Invoice rejection loop:** UI supports Rejected → ReadyForInvoice, Rejected → Reinvoice; AgentModeService alignment.
4. **Partner rates:** RateEngineService and BillingRatecard integration verified for invoice line generation.
5. **Partner portal:** Document manual docket/invoice submission process; no code change (out of scope).

**Acceptance Criteria:**
- [x] MyInvois runbook exists; IntegrationSettings UI at /settings/integrations.
- [x] Invoice rejection/reinvoice flow works from UI (WorkflowTransitionButton; fallback + DB workflow).
- [x] Partner rates: RateEngineService, BillingRatecard, GponPartnerJobRate exist; invoice line items built from order/BOQ.
- [ ] Payment recording and matching functional (PaymentsController exists; UI to verify).

**Dependencies:** Phase 1 (order/docket path feeds ReadyForInvoice).

**Risks:** MyInvois API changes; credential management.

**Definition of Done:** Billing checklist (§4) passed; MyInvois runbook exists.

**Status:** ✅ **COMPLETE** (2026-02-09) — Runbook, partner portal manual doc, invoice rejection loop verified.

---

## Phase 3: Inventory + Ledger Hardening (Serial Lifecycle, Audits, Reports)

**Objective:** Inventory and ledger fully auditable and reportable; serial lifecycle correct.

**Scope:**
- Inventory, Ledger, SerialisedItems, StockAllocations, Reports

**Deliverables:**
1. **Ledger integrity:** ReconcileLedgerBalanceCache job schedule and monitoring.
2. **Stock-by-location:** PopulateStockByLocationSnapshots job; reports use snapshots.
3. **Serial lifecycle:** Receive → Allocate → Issue → Use → Return/Transfer validated; RMA path documented.
4. **Inventory reports:** Usage by period, serial lifecycle, stock trend verified.
5. **Department scope:** Inventory operations respect department access.

**Acceptance Criteria:**
- [x] Ledger balance cache matches raw ledger; reconcile job runs (LedgerReconciliationSchedulerService).
- [x] Serial tracking end-to-end without orphaned records.
- [x] Inventory reports return correct data (usage, serial lifecycle, stock trend, stock-by-location).
- [x] Department-scoped inventory access enforced (InventoryController, DepartmentAccessService).

**Dependencies:** Phase 1 (SI material usage feeds ledger).

**Risks:** Low. Mostly validation and scheduling.

**Definition of Done:** Inventory/Ledger checklist (§2) passed; reports validated.

**Status:** ✅ **COMPLETE** (2026-02-09) — LedgerReconciliationSchedulerService added; inventory_ledger_ops_runbook.md; checklist §2 verified.

---

## Phase 4: Reporting / P&L Operationalization

**Objective:** P&L and reports production-grade; operational dashboards usable.

**Scope:**
- P&L, Reports Hub, KPI, Dashboards

**Deliverables:**
1. **P&L rebuild:** PnlRebuild job schedule; P&L summary/drilldown/overheads accurate.
2. **Reports Hub:** Key reports (orders, materials, stock, ledger, scheduler) export correctly; department scope.
3. **KPI profiles:** Docket KPI, installation KPI calculated; KpiProfilesPage configurable.
4. **Dashboard:** DashboardPage shows meaningful metrics.

**Acceptance Criteria:**
- [x] P&L rebuild job scheduled (PnlRebuildSchedulerService daily).
- [x] Reports export (CSV/XLSX/PDF) without errors; department scope enforced.
- [x] KPI profiles (DocketKpiMinutes, InstallationKpi) configurable; KpiProfilesPage.
- [x] Dashboard loads and refreshes (DashboardPage with orders, trends).

**Dependencies:** Phase 2 (invoices), Phase 3 (inventory).

**Risks:** Low. Data quality depends on Phases 1–3.

**Definition of Done:** P&L and reports used by finance/ops; no critical bugs.

**Status:** ✅ **COMPLETE** (2026-02-09) — PnlRebuildSchedulerService added; reporting_pnl_ops_runbook.md.

---

## Phase 5: Nice-to-Haves / Future Departments (CWO / NWO)

**Objective:** Clearly mark future scope; prepare for CWO/NWO if needed.

**Scope:**
- CWO, NWO (future); barbershop/travel (out of scope); optional enhancements

**Deliverables:**
1. **CWO/NWO:** Mark as "future" in docs; no active workflows; department routes exist but not populated.
2. **Scope not handled:** scope_not_handled.md kept current; Kingsman/Menorah marked if out of scope.
3. **Optional:** Offline SI, partner API, quotation-to-order; logged as future only.
4. **Multi-company:** Remains reference; no implementation in this roadmap.

**Acceptance Criteria:**
- [x] CWO/NWO docs state "future; to be defined when activated." (scope_not_handled, order_lifecycle, product_overview)
- [x] scope_not_handled.md reflects current code; Kingsman/Menorah out of scope for GPON.
- [x] No confusion between GPON-ready and future (explicit GPON Go-Live note in scope_not_handled).

**Dependencies:** Phases 0–4 complete.

**Risks:** Scope creep if CWO/NWO requested early.

**Definition of Done:** Future scope documented; GPON remains single focus.

**Status:** ✅ **COMPLETE** (2026-02-09) — scope_not_handled.md updated; CWO/NWO, Kingsman/Menorah clarified.

---

## Phasing Summary

| Phase | Name | Start After | Duration (Est.) |
|-------|------|-------------|-----------------|
| 0 | Stabilize Truth & Governance | — | 1–2 weeks |
| 1 | Core Ops Hardening | Phase 0 | 2–3 weeks |
| 2 | Billing Hardening | Phase 1 | 2–3 weeks |
| 3 | Inventory + Ledger Hardening | Phase 1 | 2 weeks |
| 4 | Reporting/P&L Operationalization | Phases 2, 3 | 1–2 weeks |
| 5 | Nice-to-Haves / Future | Phase 4 | Ongoing |

**Immediate next step:** Phases 0–5 complete. **GPON go-live ready.** See GO_LIVE_READINESS_CHECKLIST_GPON.md for pre-launch verification.

---

**Related:** [COMPLETION_STATUS_REPORT.md](./COMPLETION_STATUS_REPORT.md) | [GO_LIVE_READINESS_CHECKLIST_GPON.md](./GO_LIVE_READINESS_CHECKLIST_GPON.md)
