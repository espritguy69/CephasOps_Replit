# Suggested Automated Test Scenarios — SaaS Readiness

**Date:** 2026-03-13

Practical automated test cases to add or strengthen, grouped by type. Align with existing tenant-safety regression suite (see `backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md`) and [06_module_test_matrix.md](06_module_test_matrix.md).

---

## 1. Integration tests (backend)

| Scenario | Description | Priority |
|----------|-------------|----------|
| **Two-tenant list isolation** | Create two companies (A, B); seed orders/buildings/SIs/users/warehouses for each. As user A, GET list endpoints (orders, buildings, service-installers, users, warehouses); assert only A’s IDs. As user B, same; assert only B’s IDs. | P0 |
| **Detail-by-ID cross-tenant returns 404** | As user A, GET /api/orders/{id} with order id belonging to B. Expect 404 (or 403). Repeat for invoices, buildings, SIs, **users**, **warehouses**. | P0 |
| **Report run scoped to tenant** | As user A, run orders-list report (API); assert all rows have CompanyId = A. As user B, same; assert CompanyId = B. | P0 |
| **Export contains only current tenant** | As user A, export orders-list (csv); parse and assert every row’s companyId (if present) or order IDs belong to A. | P0 |
| **Department scope rejects other tenant’s department** | User A has access to dept A1. Call report run with departmentId = B’s department. Expect 403. | P1 |
| **TenantGuard blocks when no company** | Request with valid JWT but no company claim and no department fallback to GET /api/orders. Expect 403. | P1 |
| **SuperAdmin X-Company-Id override** | As SuperAdmin, GET /api/orders with X-Company-Id: B. Expect 200 and only B’s orders. As non-SuperAdmin, same header; expect A’s orders (header ignored). | P1 |
| **Auth: JWT contains companyId** | Login as user with CompanyId; decode JWT; assert companyId claim equals user’s company. | P1 |

---

## 2. API tests (tenant-scoped controllers)

| Scenario | Description | Priority |
|----------|-------------|----------|
| **OrdersController list** | GET /api/orders with tenant A user; verify query or service receives companyId and returns only A. | P0 |
| **OrdersController get by id** | GET /api/orders/{id} with B’s order id as A user; 404. | P0 |
| **BillingController list invoices** | Same pattern for invoices list and get-by-id. | P0 |
| **InventoryController materials/list** | Materials list scoped to tenant. | P1 |
| **UsersController list and by-id** | GET /api/users and GET /api/users/{id} return only current tenant's users; other-tenant user id → 404. | P1 |
| **WarehousesController list, GetById, Update, Delete** | List and detail/mutations scoped to tenant; other-tenant warehouse id → 404. | P1 |
| **BuildingsController list** | Buildings list scoped to tenant. | P1 |
| **ServiceInstallersController list** | SI list scoped to tenant. | P1 |
| **ParserController drafts/sessions** | Parsed drafts and sessions scoped to tenant. | P1 |
| **FilesController download** | GET file by id; other-tenant file id returns 404. | P1 |
| **ReportsController run and export** | Run report and export; assert response/file scope. | P0 |

---

## 3. Workflow and event tests

| Scenario | Description | Priority |
|----------|-------------|----------|
| **Workflow transition sets event CompanyId** | Execute transition (e.g. Assigned → OnTheWay); load last event for order; assert entry.CompanyId equals order.CompanyId. | P1 |
| **OrderAssignedOperationsHandler enqueues job with CompanyId** | When order assigned, handler enqueues SLA job; assert job.CompanyId = order.CompanyId. | P1 |
| **OrderAssignedOperationsHandler when CompanyId null** | Order and event have no CompanyId; handler does not enqueue or skips with log; no job created with null company. | P1 (existing in tenant-safety suite) |
| **Event replay under entry.CompanyId** | Replay one event; assert handler runs with TenantScope = entry.CompanyId (e.g. no SaveChanges for other tenant). | P1 |

---

## 4. Background job tests

| Scenario | Description | Priority |
|----------|-------------|----------|
| **Job execution runs under job.CompanyId** | Enqueue job with CompanyId = A; in test, run processor; assert TenantScope.CurrentTenantId (or SaveChanges guard) was A during execution. | P1 |
| **Job with null CompanyId runs under bypass** | Enqueue job with CompanyId null (if allowed); execution uses RunWithTenantScopeOrBypassAsync(null); no tenant-scoped entity written without bypass. | P1 |
| **Reap uses platform bypass only for state update** | ReapStaleRunningJobsAsync marks jobs Failed; assert no tenant data mixed; only job state updated. | P2 |
| **Scheduler-enqueued job has CompanyId** | Email ingestion or P&L rebuild enqueues per-tenant job; assert payload or job row has CompanyId set. | P1 |

---

## 5. UI / E2E tests (Playwright or similar)

| Scenario | Description | Priority |
|----------|-------------|----------|
| **Login as tenant A; orders list shows only A** | E2E: login, navigate to orders list; assert table rows (or first page) contain only tenant A (e.g. by order id or company indicator if present). | P0 |
| **Cross-tenant order URL returns 404 or forbidden** | As tenant A user, navigate to /orders/{tenant-B-order-id}; expect 404 or error page, not B’s data. | P1 |
| **Report export downloads and contains only A** | Run report, export csv; in test parse file and assert all rows belong to current tenant. | P1 |
| **SI app: job list only assigned SI’s jobs** | Login SI app as SI of tenant A; assert job list matches assigned jobs for that SI in tenant A. | P1 |
| **Company switch (SuperAdmin)** | As SuperAdmin, switch company via UI (if supported); list and report reflect new company. | P2 |

---

## 6. Search / report / export tests

| Scenario | Description | Priority |
|----------|-------------|----------|
| **Order search keyword** | Search orders with keyword that exists only in tenant B; as tenant A user, expect no B results. | P1 |
| **Materials autocomplete** | Autocomplete materials; assert only current tenant’s materials. | P1 |
| **Ledger report** | Run ledger report; assert all ledger entries have correct CompanyId. | P1 |
| **Stock summary export** | Export stock-summary; assert locations/materials belong to tenant. | P1 |
| **Scheduler utilization report** | Run scheduler-utilization; assert slots/SIs are tenant-scoped. | P1 |

---

## 7. Cross-tenant security tests

| Scenario | Description | Priority |
|----------|-------------|----------|
| **Probe all list endpoints with two tenants** | For each major list endpoint (orders, buildings, SIs, invoices, materials, partners, departments), run as A and as B; assert no overlap in IDs. | P0 |
| **Probe all detail endpoints with other-tenant ID** | For each detail endpoint (order, invoice, building, SI, file), as A request B’s resource id; expect 404 or 403. | P0 |
| **Notification create requires company** | Call notification create without company context (or null); expect 400/403 or skip with log; no notification created for wrong tenant. | P1 |
| **Webhook with company context** | Send inbound webhook with CompanyId = B; process; assert only B’s data created/updated. | P1 |

---

## 8. Concurrency / performance smoke tests

| Scenario | Description | Priority |
|----------|-------------|----------|
| **Parallel requests two tenants** | Simulate concurrent requests: user A and user B each calling list/detail/report; assert no cross-tenant data and no 500. | P2 |
| **Background job A and B run in parallel** | Enqueue one job for A and one for B; run processor; assert each job’s side effects (e.g. P&L row, notification) only in correct tenant. | P2 |
| **Load: multiple tenants list orders** | Load test: N users from tenant A and M from tenant B repeatedly calling GET /api/orders; success rate and latency acceptable; no cross-tenant results. | P2 |

---

## 9. Existing tests to reuse or align

- **TenantScopeExecutorTests** — Already cover RunWithTenantScopeAsync, RunWithPlatformBypassAsync, RunWithTenantScopeOrBypassAsync, restore on exception, empty Guid. Keep and reference.
- **NotificationRetentionServiceTests, EventReplayServiceTenantScopeTests, InboundWebhookRuntimeTenantScopeTests** — Tenant scope and bypass behavior. Keep.
- **OrderAssignedOperationsHandlerTests** (null CompanyId, no duplicate task/SLA) — Keep; run in isolation if needed (TenantScopeTests collection).
- **NotificationServiceTests.CreateNotificationAsync_WhenCompanyId / WhenCompanyIdNull** — Keep.
- **BillingServiceFinancialIsolationTests, FinancialIsolationGuardTests** — Keep.
- **AdminApiSafetyTests** — Admin endpoints and tenant boundaries. Align with SaaS checklist.

---

## 10. Recommended implementation order

1. **P0 integration:** Two-tenant list isolation, detail-by-ID 404, report/export scope, OrdersController and ReportsController.
2. **P0 E2E:** Login and orders list tenant-scoped; optional report export.
3. **P1 API:** Remaining controllers (Billing, Inventory, Buildings, ServiceInstallers, Files, Parser); department scope reject; TenantGuard and SuperAdmin.
4. **P1 workflow/jobs:** Transition event CompanyId; job execution scope; scheduler enqueue CompanyId.
5. **P1 search/export:** Search, autocomplete, ledger, stock-summary, scheduler-utilization.
6. **P1 cross-tenant:** Probe all list/detail; notification create; webhook.
7. **P2:** Reap, E2E company switch, concurrency smoke, load.

This order supports [05_execution_order.md](05_execution_order.md) for test execution sequencing.
