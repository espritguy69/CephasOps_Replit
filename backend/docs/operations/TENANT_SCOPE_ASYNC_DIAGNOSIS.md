# Tenant Scope Lost During Async Execution — Diagnosis

**Date:** 2025-03-12  
**Scope:** WorkflowEngineService.ExecuteTransitionAsync, OrderProfitabilityService.CalculateOrderProfitabilityAsync  
**Objective:** Find exactly where `TenantScope.CurrentTenantId` becomes unavailable so the order query returns no row (workflow: "Unknown", profitability: Unresolved).

---

## 1. Execution flow to the order-loading query

### 1.1 WorkflowEngineService.ExecuteTransitionAsync

| Step | Location | Code | Yields? |
|------|----------|------|--------|
| 1 | ExecuteTransitionAsync | `await ResolveWorkflowScopeAsync(...)` | Mock returns `ReturnsAsync(...)` → completed task → **no yield** in typical run |
| 2 | ExecuteTransitionAsync | `await _workflowDefinitionsService.GetEffectiveWorkflowDefinitionAsync(...)` | Mock returns completed task → **no yield** |
| 3 | ExecuteTransitionAsync | `await GetCurrentEntityStatusAsync(dto.EntityType, dto.EntityId, cancellationToken)` | **Yes** — enters private method |
| 4 | **GetCurrentEntityStatusAsync** | `await _context.Orders.FirstOrDefaultAsync(o => o.Id == entityId, cancellationToken)` | **Yes** — this is where the order is loaded and where the global tenant filter is applied |

**Order is loaded in:** `WorkflowEngineService.GetCurrentEntityStatusAsync` (line 449–451):

```csharp
var order = await _context.Orders
    .FirstOrDefaultAsync(o => o.Id == entityId, cancellationToken);
return order?.Status ?? "Unknown";
```

There is **no** explicit `companyId` in the query; the only tenant restriction is the **global query filter** on `Order` (CompanyScopedEntity), which evaluates `TenantScope.CurrentTenantId` at **query execution time** (see ApplicationDbContext OnModelCreating: filter uses `Expression.MakeMemberAccess(null, currentTenantIdProp)`).

### 1.2 OrderProfitabilityService.CalculateOrderProfitabilityAsync

| Step | Location | Code | Yields? |
|------|----------|------|--------|
| 1 | CalculateOrderProfitabilityAsync | `var order = await _context.Orders.Include(...).Where(o => o.Id == orderId && o.CompanyId == companyId).FirstOrDefaultAsync(cancellationToken)` | **Yes** — first line that performs I/O |

**Order is loaded in:** `OrderProfitabilityService.CalculateOrderProfitabilityAsync` (lines 38–42). The query also has an explicit `o.CompanyId == companyId`, but the **global query filter** is still applied by EF (filter is combined with the predicate). So when the query runs, the filter body is evaluated and reads `TenantScope.CurrentTenantId` at that moment. If it is null, the filter allows all rows (tenant part is “tenantId is null OR CompanyId == tenantId” → true when null). So for profitability, a null tenant would not by itself hide the order; the explicit `CompanyId == companyId` would still match. So either:

- The failure in tests is due to a different cause (e.g. wrong or cleared `companyId` in the continuation), or  
- The global filter is applied in a way that when `CurrentTenantId` is null, the provider still filters (e.g. “no tenant → no rows”) — that would be provider/setup specific.

So for a single, consistent explanation that matches “order not found”: the **first place** where tenant can affect visibility is the **execution of the query** (workflow: in `GetCurrentEntityStatusAsync`; profitability: first `await` in `CalculateOrderProfitabilityAsync`). At that moment, whatever thread/context is running will read `TenantScope.CurrentTenantId`; if it is null or wrong, the workflow filter can hide the order (or profitability can behave differently depending on filter semantics).

---

## 2. Where ExecutionContext / AsyncLocal can be lost

### 2.1 Checked in application code

- **WorkflowEngineService** (ExecuteTransitionAsync, ResolveWorkflowScopeAsync, GetCurrentEntityStatusAsync): **no** `ConfigureAwait(false)`, **no** `Task.Run`, **no** `ContinueWith`, **no** custom scheduler in these paths.
- **OrderProfitabilityService** (CalculateOrderProfitabilityAsync): **no** `ConfigureAwait(false)` in the order-loading path; **no** `Task.Run`/`ContinueWith` before the order query.
- **BillingService / RateEngineService**: not on the path **before** the order is loaded; they are used after. No `ConfigureAwait` in Billing for the call used by profitability.

So the **production code paths** that lead to the order query do **not** explicitly drop context (no `ConfigureAwait(false)` or `Task.Run` in these two services).

### 2.2 Async call chain and where context can be lost

- **Workflow**
  - Test sets `TenantScope.CurrentTenantId` and `SynchronizationContext` (TenantPreservingSyncContext), then `await _service.ExecuteTransitionAsync(...)`.
  - Call chain: Test → RunWithTenantAsync → lambda → `ExecuteTransitionAsync` → `ResolveWorkflowScopeAsync` (mock, no yield) → `GetEffectiveWorkflowDefinitionAsync` (mock, no yield) → `GetCurrentEntityStatusAsync` → `await _context.Orders.FirstOrDefaultAsync(...)`.
  - The first **real** await that can yield is either inside `GetCurrentEntityStatusAsync` (`FirstOrDefaultAsync`) or, if the mocks ever return an incomplete task, at step 1 or 2. When that await yields, the **continuation** runs later. For that continuation to see the same `TenantScope`, either:
    - ExecutionContext (and thus AsyncLocal) is preserved across the await, or  
    - The continuation is run through a SynchronizationContext that sets tenant again (e.g. TenantPreservingSyncContext).

- **Profitability**
  - Test sets tenant and SyncContext, then `await WithTenantAsync(() => _service.CalculateOrderProfitabilityAsync(...))`.
  - The first await is `await _context.Orders...FirstOrDefaultAsync(...)`. So the first yield is at the order query. The continuation that runs after the query completes will run on whatever sync context was current at the time of the await (or thread pool if null). If that continuation runs without the test’s ExecutionContext or without the custom SyncContext, it will see a different (e.g. null) `TenantScope.CurrentTenantId` when the **next** query runs; the **order** query itself runs in the same call stack as the method entry, so the loss would be observed on the **continuation** that runs after the first await (or on a later query in the same method that runs in that continuation).

So the **exact method where tenant context is observed as “lost”** (query returns no row / “Unknown” / Unresolved) is:

- **Workflow:** `WorkflowEngineService.GetCurrentEntityStatusAsync` — at the moment `_context.Orders.FirstOrDefaultAsync(...)` **executes** (and evaluates the global filter). If the thread/context executing the query has `TenantScope.CurrentTenantId` null or wrong, the filter hides the order.
- **Profitability:** `OrderProfitabilityService.CalculateOrderProfitabilityAsync` — at the first `await _context.Orders...FirstOrDefaultAsync(...)`. Either (a) the query runs before any yield (then the same context as the caller), or (b) the query runs inside an async continuation; in (b), that continuation is where `CurrentTenantId` can be null or wrong.

So the **loss** is not “in” a specific method by name, but at the **point in time** when the order query **executes** (and the global filter reads `TenantScope.CurrentTenantId`). That can be:

- In the initial call (no yield yet) → tenant should still be set.  
- In a continuation after an await → tenant can be lost if ExecutionContext or SyncContext is not preserved.

### 2.3 Why AsyncLocal might not flow in tests

- **ExecutionContext** is captured when an async method yields and is restored when the continuation runs — **unless** the continuation is scheduled in a way that doesn’t capture it (e.g. some `Task.Run` or custom scheduler) or the test runner does not preserve it.
- **xUnit** and some test runners are known to run test code and continuations in a context where **ExecutionContext** (and thus **AsyncLocal**) is not always propagated. So the **first** await that actually yields (here, very likely `FirstOrDefaultAsync`) can run a continuation on a thread/context where `TenantScope.CurrentTenantId` was never set or has been cleared.
- **SynchronizationContext**: If the test sets a custom SyncContext (TenantPreservingSyncContext), continuations should be posted to it **only if** `SynchronizationContext.Current` was that context at the time of the await. If the test or runner clears it, or if the continuation is run inline (e.g. when the awaited task is already completed on another thread), the continuation may run without going through the custom SyncContext and without the test’s tenant.

So the most likely place where **tenant context is lost** is:

- **The async continuation that runs after the first real await** in the service (workflow: after `GetCurrentEntityStatusAsync`’s `FirstOrDefaultAsync`; profitability: after the first `FirstOrDefaultAsync`). That continuation executes in an environment where:
  - Either **ExecutionContext** was not propagated (e.g. test runner behavior), or  
  - **SynchronizationContext** was null or different, so the continuation did not run through TenantPreservingSyncContext and never had tenant re-applied.

The **exact method** where the “missing” tenant is **observed** (query returns no row) is:

- **WorkflowEngineService.GetCurrentEntityStatusAsync** (the `_context.Orders.FirstOrDefaultAsync` call).
- **OrderProfitabilityService.CalculateOrderProfitabilityAsync** (the first `_context.Orders...FirstOrDefaultAsync` call).

The **logical** place where context is lost is the **continuation** that runs after the first await in that path (or the thread/context on which that continuation runs), not a specific “evil” line in production code.

---

## 3. Confirming where TenantScope.CurrentTenantId is null

To confirm, add **temporary** debug logging (tests or local dev only) at:

1. **WorkflowEngineService.GetCurrentEntityStatusAsync**, before the order query:
   - Log `TenantScope.CurrentTenantId`, `ExecutionContext.IsFlowSuppressed`, and `SynchronizationContext.Current?.GetType().Name`.
2. **OrderProfitabilityService.CalculateOrderProfitabilityAsync**, before the order query:
   - Same three values.
3. Optional: in the **test**’s `RunWithTenantAsync` / `WithTenantAsync`, log `CurrentTenantId` and `SynchronizationContext.Current` immediately before `await act()` and inside the first line of the service (if possible) to see if the service entry still sees the test’s context.

Expectation: in the failing test runs, you will see `CurrentTenantId` null (or wrong) and/or `SynchronizationContext.Current` null or not `TenantPreservingSyncContext` at the moment the order query runs (or in the continuation that runs it).

---

## 4. Summary answers

### 4.1 Exact method where tenant context is lost

- **Where the missing tenant is observed (query returns no row):**  
  - **WorkflowEngineService.GetCurrentEntityStatusAsync** (around the `_context.Orders.FirstOrDefaultAsync` call).  
  - **OrderProfitabilityService.CalculateOrderProfitabilityAsync** (the first `await _context.Orders...FirstOrDefaultAsync`).
- **Why it’s “lost”:** The code that **executes** the query (or the continuation that runs after the first await) runs in a thread/context where `TenantScope.CurrentTenantId` is null or wrong. That is most likely because the **async continuation** after the first real await is run without the test’s ExecutionContext (and thus without the AsyncLocal value) and/or without the test’s SynchronizationContext (so TenantPreservingSyncContext never re-applies tenant).

### 4.2 Async call chain to that point

**Workflow:**

- Test → `RunWithTenantAsync` → lambda → `ExecuteTransitionAsync` → `ResolveWorkflowScopeAsync` (mock, no yield) → `GetEffectiveWorkflowDefinitionAsync` (mock, no yield) → `GetCurrentEntityStatusAsync` → **`await _context.Orders.FirstOrDefaultAsync(...)`** → (continuation) where filter is evaluated and order is resolved.

**Profitability:**

- Test → `WithTenantAsync` → **`await _service.CalculateOrderProfitabilityAsync(...)`** → **`await _context.Orders...FirstOrDefaultAsync(...)`** → (continuation) where result is used and later queries run.

The **first** await that can yield in these paths is the **order** query (or an earlier await if mocks return incomplete tasks). The continuation after that await is the first place where ExecutionContext/AsyncLocal can differ from the test’s.

### 4.3 Can production be affected?

- **Normal API request:** Middleware sets `TenantScope.CurrentTenantId` (and usually there is no custom SyncContext). The whole request runs in that context. When the service awaits, continuations run with the same ExecutionContext in typical ASP.NET Core hosting, so AsyncLocal (and thus tenant) **should** flow. So **production API** is unlikely to see this** if** the pipeline always sets tenant and doesn’t use a custom SyncContext that strips context.
- **Background jobs / workers:** If a job is executed on a worker thread that **does not** set `TenantScope.CurrentTenantId` before calling the same services, or if the job is dispatched with `Task.Run` / a queue that doesn’t propagate ExecutionContext, then the **same** loss can occur in production (order not found, “Unknown”, or Unresolved). So **production jobs** that invoke these services without setting tenant (or without ensuring ExecutionContext flow) **could** be affected.

### 4.4 Safest fix (conceptual; no code change in this doc)

- **Tests:** Ensure the **continuation** that runs the order query sees tenant: either run the test in an environment that preserves ExecutionContext (e.g. single-threaded or runner that propagates it), or keep using a custom SynchronizationContext that **re-sets** `TenantScope.CurrentTenantId` in every continuation (and ensure that context is actually current when the service awaits). Optionally, make the **order lookup** in the engine explicit by company (see below) so tests don’t depend on AsyncLocal.
- **Production (defense in depth):** Make the **order-loading** path **explicit** in company/tenant so it doesn’t rely only on AsyncLocal:
  - **WorkflowEngineService:** Pass `companyId` (or tenant) into `GetCurrentEntityStatusAsync` and use it in the query (e.g. `Where(o => o.Id == entityId && o.CompanyId == companyId)`) in addition to (or instead of relying solely on) the global filter. That way, even if AsyncLocal is lost in a background job or edge case, the query is still scoped.
  - **OrderProfitabilityService:** Already passes `companyId` and uses `o.CompanyId == companyId` in the predicate; the only remaining dependency is the **global filter** evaluated at execution time. If the global filter when `CurrentTenantId` is null is “allow all,” the explicit predicate is enough; if not, the same “explicit company in query” approach removes reliance on AsyncLocal for this query.

No production logic, TenantSafetyGuard, or query filters were modified in this diagnosis.
