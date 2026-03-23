# Recommended Execution Order — SaaS Readiness Tests

**Date:** 2026-03-13

Clear order for running tests from foundational validation through final readiness signoff. Early failures should invalidate or de-prioritise later steps to avoid noise.

---

## 1. Principles

- **Foundation first:** Auth, tenant resolution, and list/detail isolation must pass before trusting reports, exports, or workflows.
- **Automated before manual where possible:** Run tenant-safety and integration tests first; use manual UAT for flows that are not yet automated.
- **Blocking gates:** Failure in a gate may stop or conditionally skip dependent stages; document and track.

---

## 2. Execution phases

### Phase 1 — Tenant safety and auth (foundation)

**Goal:** Confirm that tenant context is set and enforced; no API runs without a valid tenant (except allowlisted paths).

| Step | What to run | Pass criterion | If fail |
|------|-------------|----------------|--------|
| 1.1 | Backend tenant-safety regression suite (see TENANT_SAFETY_DEVELOPER_GUIDE.md): TenantScopeExecutorTests, NotificationRetentionServiceTests, EventReplayServiceTenantScopeTests, InboundWebhookRuntimeTenantScopeTests, OrderAssignedOperationsHandlerTests (null company), NotificationServiceTests (company null/set), NotificationDispatchRequestServiceTests | All tests pass. | **Block:** Fix tenant scope/executor or null-company behaviour before proceeding. |
| 1.2 | Integration: TenantGuardMiddleware blocks request when company unresolved (e.g. GET /api/orders with JWT without company and no department fallback) | 403; no business logic executed. | **Block:** Fix TenantGuardMiddleware or TenantProvider. |
| 1.3 | Integration: Login returns JWT with companyId for user with company; detail-by-ID with other tenant’s order ID returns 404 | JWT claim present; 404 for cross-tenant ID. | **Block:** Fix auth/tenant resolution or controller/service filter. |

**Exit:** Phase 1 complete. Proceed to Phase 2.

---

### Phase 2 — List and detail isolation

**Goal:** No list or detail endpoint returns another tenant’s data.

| Step | What to run | Pass criterion | If fail |
|------|-------------|----------------|--------|
| 2.1 | Integration: Two-tenant list isolation (orders, buildings, service-installers, invoices, materials) | Each list returns only the requesting tenant’s IDs. | **Block:** Fix global query filter or service-level company filter. |
| 2.2 | Integration: Detail-by-ID for orders, invoices, buildings, SIs with other-tenant ID | 404 (or 403) for every probe. | **Block:** Ensure detail endpoints use tenant-scoped lookup. |
| 2.3 | Optional E2E: Login tenant A, open orders list; assert only A’s data on first page | UI shows only tenant A orders. | **High:** Fix frontend/API contract or scope. |

**Exit:** Phase 2 complete. Proceed to Phase 3.

---

### Phase 3 — Reports and exports

**Goal:** All report runs and exports are tenant- and department-scoped; no companyId override via query.

| Step | What to run | Pass criterion | If fail |
|------|-------------|----------------|--------|
| 3.1 | Integration: Run orders-list report as tenant A and B; assert rows belong to correct tenant | No cross-tenant rows. | **Block:** Fix report data path (companyId from _tenantProvider, department resolve). |
| 3.2 | Integration: Export orders-list (csv) as A; parse and assert all rows tenant A | File contains only A. | **Block:** Same as 3.1. |
| 3.3 | Integration: Same for stock-summary, ledger, materials-list, scheduler-utilization if automated | All scoped. | **High:** Fix per-report. |
| 3.4 | Manual UAT: Stage 3 of [02_manual_uat_plan.md](02_manual_uat_plan.md) (reports and exports) | All steps pass. | **High:** Document and fix. |

**Exit:** Phase 3 complete. Proceed to Phase 4.

---

### Phase 4 — Single-tenant regression (full flow)

**Goal:** One tenant can complete the full order lifecycle and related flows without regression.

| Step | What to run | Pass criterion | If fail |
|------|-------------|----------------|--------|
| 4.1 | Manual UAT: Stage 4 of [02_manual_uat_plan.md](02_manual_uat_plan.md) — full order lifecycle (draft → assign → SI complete → docket → invoice → payment → completed) | All steps pass for one tenant. | **High:** Fix workflow, billing, or SI app. |
| 4.2 | Manual UAT: Stage 5 — SI app tenant scope (job list, transition, cross-tenant job ID) | SI sees only own assigned jobs; cross-tenant ID 404. | **High:** Fix SI app or SiAppController. |
| 4.3 | Optional: E2E order lifecycle if automated | Order reaches Completed; invoice and P&L reflect tenant. | **Medium:** Add or fix E2E. |

**Exit:** Phase 4 complete. Proceed to Phase 5.

---

### Phase 5 — Background jobs and notifications

**Goal:** Jobs and notifications run in correct tenant scope; no cross-tenant side effects.

| Step | What to run | Pass criterion | If fail |
|------|-------------|----------------|--------|
| 5.1 | Integration: Job execution under job.CompanyId (and null → bypass where designed) | Scope set per job; no wrong-tenant write. | **High:** Fix BackgroundJobProcessorService or enqueue paths. |
| 5.2 | Integration: Scheduler-enqueued job has CompanyId (e.g. P&L rebuild, email ingest) | Payload or job row has CompanyId. | **High:** Fix scheduler. |
| 5.3 | Manual or integration: Create notification; verify recipient and data tenant-scoped | Notification and dispatch correct tenant. | **High:** Fix NotificationService/Dispatch. |
| 5.4 | Manual: Trigger P&L rebuild for tenant A; verify P&L data only for A | No B data in P&L. | **High:** Fix PnlRebuildService or job scope. |

**Exit:** Phase 5 complete. Proceed to Phase 6.

---

### Phase 6 — Permissions, files, and remaining surfaces

**Goal:** Department scope, file access, search, and admin boundaries correct.

| Step | What to run | Pass criterion | If fail |
|------|-------------|----------------|--------|
| 6.1 | Integration: Department scope — report run with other tenant’s departmentId returns 403 | 403. | **High:** Fix ResolveDepartmentScopeAsync / DepartmentAccessService. |
| 6.2 | Integration: File download with other-tenant file ID returns 404 | 404. | **High:** Fix FilesController/DocumentsController. |
| 6.3 | Integration: Search/autocomplete (orders, materials, buildings) tenant-scoped | No B results when as A. | **Medium:** Fix search filters. |
| 6.4 | Manual UAT: Stage 7 (permissions), Stage 8 (files/documents) | Pass. | **Medium:** Document and fix. |

**Exit:** Phase 6 complete. Proceed to Phase 7.

---

### Phase 7 — Multi-tenant stability and signoff

**Goal:** Two tenants active; no cross-tenant leakage; performance and concurrency acceptable.

| Step | What to run | Pass criterion | If fail |
|------|-------------|----------------|--------|
| 7.1 | Manual UAT: Stage 6 (background jobs) and parallel execution (Stage 4 for A and B in parallel) | Both tenants complete flows; no mixed data. | **High:** Fix job or event scope. |
| 7.2 | Concurrency smoke: Parallel API calls for tenant A and B (list, report); no cross-tenant data, no 500 | Pass. | **Medium:** Investigate caching or concurrency. |
| 7.3 | Master checklist [01_master_checklist.md](01_master_checklist.md): All sections signed off | All items passed or accepted with waiver. | **Block:** Resolve remaining items. |
| 7.4 | Go/no-go per [10_go_no_go_criteria.md](10_go_no_go_criteria.md) | Go or Conditional Go. | **Block for Go:** Address criteria. |

**Exit:** SaaS readiness signoff or conditional go with documented exceptions.

---

## 3. Summary flow

```
Phase 1 (Tenant safety & auth) → Phase 2 (List/detail isolation) → Phase 3 (Reports/exports)
    → Phase 4 (Single-tenant regression) → Phase 5 (Background jobs & notifications)
    → Phase 6 (Permissions, files, search) → Phase 7 (Multi-tenant stability & signoff)
```

- **Block** in Phase 1 or 2: Do not run Phase 3–7 for signoff until fixed.
- **Block** in Phase 3: Do not run Phase 4–7 for signoff until reports/exports fixed.
- **High** in Phase 4–6: Can proceed to next phase in parallel with fix; signoff requires fixes or documented waivers.
- **Phase 7** is the final gate: master checklist and go/no-go criteria must be satisfied for “SaaS-ready” declaration.

---

## 4. Traceability

- Phase 1 ↔ [09_background_job_tenant_safety.md](09_background_job_tenant_safety.md), TENANT_SAFETY_DEVELOPER_GUIDE.md.
- Phase 2–3 ↔ [07_tenant_isolation_attack_surface.md](07_tenant_isolation_attack_surface.md), [06_module_test_matrix.md](06_module_test_matrix.md).
- Phase 4 ↔ [08_single_tenant_regression_plan.md](08_single_tenant_regression_plan.md).
- Phase 5 ↔ [09_background_job_tenant_safety.md](09_background_job_tenant_safety.md).
- Phase 7 ↔ [01_master_checklist.md](01_master_checklist.md), [10_go_no_go_criteria.md](10_go_no_go_criteria.md).
