# Tenant-Safe Database Query Guidelines

**Date:** 2026-03-12  
**Purpose:** Document the correct patterns for tenant-safe database queries in CephasOps so future developers understand when to rely on global query filters and when to use explicit company-scoped queries. This guideline reflects hardening work in WorkflowEngineService, OrderProfitabilityService, BillingService, and RateEngineService, and aims to prevent bugs caused by fragile reliance on AsyncLocal tenant context.

**See also:** [TENANT_SAFETY_DEVELOPER_GUIDE.md](TENANT_SAFETY_DEVELOPER_GUIDE.md) (scope and bypass), [EF_TENANT_SCOPE_SAFETY.md](EF_TENANT_SCOPE_SAFETY.md) (persistence layer), [operations/PLATFORM_SAFETY_HARDENING_INDEX.md](../operations/PLATFORM_SAFETY_HARDENING_INDEX.md) (index of safeguards).

---

## 1. How Tenant Isolation Works

### TenantScope.CurrentTenantId (AsyncLocal)

- **Type:** `TenantScope` exposes a static `AsyncLocal<Guid?>` holding the current tenant (company) id.
- **Role:** Drives EF global query filters and TenantSafetyGuard. When set, only data for that company is visible to unfiltered-style queries; when null, filters typically allow all rows (e.g. design-time, bypass).
- **Set by:** API middleware (from `ITenantProvider` after TenantGuardMiddleware) for normal HTTP requests; by job runners from `job.CompanyId` before executing a job; or explicitly in tests.

Normal API requests rely on middleware to set TenantScope before controller and application code run. Controllers and services do not set it for standard request handling.

### EF Global Query Filters

- **CompanyScopedEntity** and derived types (Order, Invoice, BillingRatecard, ServiceInstaller, etc.) have a global filter: `TenantScope.CurrentTenantId` is null **or** equals the entity’s `CompanyId`.
- When `CurrentTenantId` is set, only rows for that tenant are returned. When it is null, all rows pass the filter (for design-time and platform bypass).
- Other tenant-scoped types (User, BackgroundJob, JobExecution, OrderPayoutSnapshot, InboundWebhookReceipt) use the same pattern.

### TenantSafetyGuard

- Runs in **ApplicationDbContext.SaveChangesAsync** before calling base SaveChanges.
- If platform bypass is not active and `TenantScope.CurrentTenantId` is null or empty, any add/update/delete of a tenant-scoped entity causes an **InvalidOperationException**.
- Ensures tenant-scoped writes only happen when tenant context is set or a documented platform bypass is active.

### FinancialIsolationGuard

- Used in financial and billing paths (e.g. payout, rate resolution, invoice resolution).
- **RequireCompany(companyId, operationName)** throws if `companyId` is null or empty, enforcing that the caller has resolved company context before performing the operation.
- Complements TenantScope by requiring an explicit company id where business logic already knows it.

---

## 2. The AsyncLocal Limitation

`TenantScope.CurrentTenantId` is stored in **AsyncLocal**. It does not always propagate correctly in:

- **Background jobs** — Job runners must set scope from the job’s CompanyId before executing the delegate; continuations after await may run on a different context.
- **Workflow engines** — Transition and status logic may run after multiple awaits; ambient scope can be lost.
- **Async continuations** — After `await`, the continuation may run on a thread-pool thread where AsyncLocal was not flowed.
- **Test environments** — xUnit and other runners may not flow AsyncLocal across await boundaries; tests can see “not found” even when data exists.
- **Distributed execution paths** — Any path that crosses process or async boundaries may not have tenant set.

Relying **only** on AsyncLocal for critical entity lookups (e.g. “get order by id”, “get current status”) can produce “not found” or “Unknown” status even when the row exists and belongs to the intended company. The fix is not to remove tenant protection, but to use **explicit company-scoped queries** where the company id is already known.

---

## 3. Recommended Query Patterns

### Pattern A — Standard Tenant-Scoped Query (Normal Business Logic)

Use when the request pipeline has set tenant scope and the query should only see current-tenant data. Let global filters apply; do **not** use `IgnoreQueryFilters()`.

**Example:**

```csharp
var orders = await _context.Orders
    .Where(o => o.Status == OrderStatus.Assigned)
    .ToListAsync(cancellationToken);
```

**Use when:**

- The request pipeline has set TenantScope (e.g. normal API request).
- The query should only return data for the current tenant.
- Global filters should apply (no need to bypass them).

---

### Pattern B — Explicit Company-Scoped Query (Internal Services / Workflow / Jobs)

Use when the company id is already known and the code path may run in contexts where AsyncLocal is not set (background jobs, workflow engine, profitability/billing calculations, tests). This is **defense-in-depth**: keep tenant safety while making lookups deterministic.

**Example:**

```csharp
var order = await _context.Orders
    .IgnoreQueryFilters()
    .Where(o => o.Id == orderId && o.CompanyId == companyId)
    .FirstOrDefaultAsync(cancellationToken);
```

**Rules:**

- **Always** pair `IgnoreQueryFilters()` with an explicit `CompanyId` (or equivalent tenant) filter. Never call `IgnoreQueryFilters()` without constraining by tenant.
- **Fail closed** if `companyId` is null or empty where the operation requires a company (e.g. throw or return “not found” before querying).
- Validate `companyId` (e.g. via `FinancialIsolationGuard.RequireCompany(companyId, "OperationName")`) before executing explicit company-scoped queries where appropriate.

**Use when:**

- The caller already has `companyId` (e.g. from request, job payload, or parent entity).
- The service runs in background jobs, workflow engines, or other paths where AsyncLocal may not propagate.
- Profitability, billing, or rate resolution need deterministic lookups.
- Tests should not depend on fragile AsyncLocal propagation for the initial read.

**EF Core relationship fixup warning:** Even when using `IgnoreQueryFilters()` with an explicit `CompanyId` constraint, EF Core relationship fixup may still attach an already-tracked entity from another tenant to a navigation property. Explicit company-scoped queries prevent cross-tenant **reads**, but navigation properties must still be validated or cleared: fixup can attach entities that were already tracked in the same `DbContext` (e.g. from seed data or earlier queries). If your guarded query returns null, set the navigation property to null so a fixup-attached wrong-tenant entity is not used for updates. See **[EFCORE_RELATIONSHIP_FIXUP_RISK.md](EFCORE_RELATIONSHIP_FIXUP_RISK.md)** for the full scenario and defensive pattern.

### Query by Id only (FindAsync / Find / Single)

The automated audit script (`tools/tenant_safety_audit.ps1`) reports MEDIUM when it sees `FindAsync(id)` or `Single(id)` on a tenant-scoped set with no CompanyId in the nearby line window. **Primary tenant entity access** (e.g. loading an Asset or Order by Id for business logic, disposal creation, or workflow) should use an explicit company-scoped predicate when `companyId` is known (see Pattern B and CEPHAS004). **Reviewed-safe** cases that the script may not flag (or that are documented as acceptable after review) are: reference-data lookups (e.g. OrderCategory, Partner, Material names) used only for DTO enrichment **after** a create/update where the parent entity is already company-scoped; global filters remain active and there is no IgnoreQueryFilters. This does **not** apply to primary reads/writes of Orders, Assets, AssetDisposals, billing, or workflow entities—those must be explicitly scoped when company is known.

---

## 4. Real Examples in the Codebase

The following services use **explicit company-scoped queries** (Pattern B) for the indicated lookups. In each case, the company id is already known and the path can run in contexts where TenantScope is not set.

### WorkflowEngineService

- **GetEntityCompanyIdAsync (Order):** Resolves the order’s company id with `IgnoreQueryFilters()` and `Where(o => o.Id == entityId && o.CompanyId == companyId)` so transition logic does not depend on ambient tenant.
- **GetCurrentEntityStatusAsync (Order):** Reads current order status with `IgnoreQueryFilters()` and `Id` + `CompanyId` so status is resolved even when AsyncLocal is not set.
- **UpdateEntityStatusAsync (Order):** Updates order status with `IgnoreQueryFilters()` and `Id` + `CompanyId` so the update runs correctly in workflow continuations.

**Why:** The workflow engine executes transitions that may run after multiple awaits; AsyncLocal may not propagate. The API already passes `companyId` into `ExecuteTransitionAsync`; using it explicitly for these lookups makes behavior deterministic in jobs and tests.

### OrderProfitabilityService

- **Initial order read in CalculateOrderProfitabilityAsync:** Loads the order with `IgnoreQueryFilters()` and `Where(o => o.Id == orderId && o.CompanyId == companyId)` (caller provides `companyId`).
- **ServiceInstaller lookup (SI level):** Uses `IgnoreQueryFilters()` and `Where(s => s.Id == order.AssignedSiId.Value && s.CompanyId == order.CompanyId)` so payout resolution does not depend on ambient tenant.

**Why:** Profitability is often invoked with a known company id (e.g. from a job or API). Making the initial order and SI lookups explicit avoids “order not found” or missing payout when tenant scope is not set.

### BillingService

- **ResolveInvoiceLineFromOrderAsync:** Loads the order with `IgnoreQueryFilters()` and `Where(o => o.Id == orderId && o.CompanyId == companyId)`. The BillingRatecards base query uses `IgnoreQueryFilters()` with an explicit `companyId` filter.

**Why:** This method is called with an explicit `companyId` (e.g. from OrderProfitabilityService). Explicit scoping ensures invoice line resolution works in background and test scenarios without relying on TenantScope.

### RateEngineService

- **ResolveGponPayoutRateInternalAsync (legacy GponSiJobRate):** When `companyId` is provided and non-empty, the GponSiJobRate queries use `IgnoreQueryFilters()` and `Where(r => r.CompanyId == companyId)`. When `companyId` is null (e.g. from `GetGponPayoutRateAsync`), the existing global-filter behavior is retained.

**Why:** Rate resolution for workflow/profitability is called with `request.CompanyId`. Explicit company scoping for the legacy rate lookup makes payout resolution deterministic in jobs and tests.

---

## 5. Guardrail Rules

### Never do

- Use **IgnoreQueryFilters()** without an explicit company (or tenant) filter on the query. Every use of `IgnoreQueryFilters()` on tenant-scoped data must constrain by `CompanyId` (or equivalent) so that wrong-company data is never returned.

### Never rely on

- **TenantScope.CurrentTenantId** for critical cross-service or internal lookups when **companyId is already known**. Prefer passing `companyId` and using an explicit company-scoped query so behavior is deterministic in background jobs, workflow engines, and tests.

### Always validate

- **companyId** before executing explicit company-scoped queries when the operation requires a company (e.g. throw or return “not found” if null/empty, or use `FinancialIsolationGuard.RequireCompany(companyId, "OperationName")` at the entry of the method).

---

## 6. Testing Guidance

- **Do not remove or disable global query filters** in tests. Tests should run against the same filter behavior as production.
- **Set TenantScope.CurrentTenantId** where the test intends to simulate a request with tenant context (e.g. in constructor or via a helper like `RunWithTenantAsync`). Use a sync context that preserves tenant across async continuations (e.g. TenantPreservingSyncContext) if tests were previously flaky due to AsyncLocal not flowing.
- **Services that accept companyId** should prefer explicit company-scoped queries for the lookups that are critical to the test (order, status, rates). That way tests do not depend on fragile AsyncLocal propagation for those reads.
- **Wrong-company tests** should call the service with a different company id and assert “not found” or appropriate error (e.g. Unresolved with OrderNotFound, or InvalidOperationException). These tests can run **without** setting TenantScope to prove that explicit company scoping is enforced.

---

## 7. Summary

| Scenario | Pattern | Key point |
|----------|--------|-----------|
| Normal API request, tenant set by middleware | Pattern A — standard query | Let global filters apply; no IgnoreQueryFilters. |
| Background job, workflow, profitability, billing, tests | Pattern B — explicit company query | Use IgnoreQueryFilters() **only** with explicit CompanyId filter; validate companyId; fail closed. |

Tenant safety is preserved by: (1) keeping global query filters and TenantSafetyGuard unchanged, (2) using explicit company-scoped queries only where companyId is already known and constrained in the query, and (3) never introducing a broad bypass for normal business flows.

---

## Related Safety Notes

- **[TENANT_SAFETY_DEVELOPER_GUIDE.md](TENANT_SAFETY_DEVELOPER_GUIDE.md)** — Scope, bypass rules, PR checklist, and test execution for tenant safety.
- **[EFCORE_RELATIONSHIP_FIXUP_RISK.md](EFCORE_RELATIONSHIP_FIXUP_RISK.md)** — How EF fixup can attach wrong-tenant entities to navigation properties; defensive pattern (clear navigation when guarded lookup returns null).
- **[IGNORE_QUERY_FILTERS_AUDIT.md](../operations/IGNORE_QUERY_FILTERS_AUDIT.md)** — Audit of `IgnoreQueryFilters()` usage and tenant-safety remediation.
