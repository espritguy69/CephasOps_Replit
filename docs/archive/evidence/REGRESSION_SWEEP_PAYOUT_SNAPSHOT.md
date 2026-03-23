# Regression Sweep: Payout Snapshot Integration

## 1. All OrderService instantiations

| Location | Type | Passes IOrderPayoutSnapshotService? |
|----------|------|-------------------------------------|
| **DI (production)** | Resolved via `IOrderService` | Yes — container injects `IOrderPayoutSnapshotService` (registered in Program.cs). No direct `new OrderService(...)` in API or Application. |
| **OrdersController** | Constructor `IOrderService orderService` | N/A — receives interface; snapshot service not used by controller. |
| **AgentModeService** | Constructor `IOrderService orderService` | N/A — receives interface. |
| **ParserService** | Constructor `IOrderService orderService` | N/A — receives interface. |
| **EmailIngestionService** | Constructor `IOrderService orderService` | N/A — receives interface. |
| **ReportsController** | Constructor `IOrderService orderService` | N/A — receives interface. |
| **OrderStatusChangedNotificationHandler** | `_serviceProvider.GetRequiredService<IOrderService>()` | Yes — resolved from DI; container supplies full constructor including `IOrderPayoutSnapshotService`. |
| **OrderServiceIntegrationTests** | `new OrderService(...)` in `CreateOrderService()` | **Yes** — `_orderPayoutSnapshotServiceMock.Object` is passed as the last constructor argument (line 241). Mock is set up to return completed tasks / null / DTO. |
| **ExtractBuildingInfoFromAddressTests** | `Mock.Of<IOrderService>()` for ParserService | N/A — does not construct OrderService; only needs an IOrderService for ParserService. |

**Conclusion:** The only place that constructs `OrderService` with `new` is **OrderServiceIntegrationTests.CreateOrderService()**, and it passes the snapshot service mock. All production and other test usages go through DI or a mock of the interface, so no missing dependency at runtime.

---

## 2. DI registration verification

| Registration | File | Status |
|--------------|------|--------|
| `IOrderService` → `OrderService` | Program.cs (line 243) | `AddScoped<IOrderService, OrderService>()` — no explicit constructor; container resolves all 21 parameters, including `IOrderPayoutSnapshotService`. |
| `IOrderPayoutSnapshotService` → `OrderPayoutSnapshotService` | Program.cs (line 306) | `AddScoped<IOrderPayoutSnapshotService, OrderPayoutSnapshotService>()` — registered in same scope block as `IOrderProfitabilityService`. |

**Order of registration:** `IOrderService` is registered before `IOrderPayoutSnapshotService`. That is fine: when a request resolves `IOrderService`, the container resolves `OrderService` and then resolves `IOrderPayoutSnapshotService` (and all other constructor dependencies). No circular dependency: `OrderPayoutSnapshotService` depends on `IOrderProfitabilityService` and `ApplicationDbContext`, not on `IOrderService`.

**Conclusion:** DI is correctly configured; no runtime null for `IOrderPayoutSnapshotService` when `OrderService` is resolved.

---

## 3. Test results summary

**Assembly:** `CephasOps.Application.Tests`

| Result | Count |
|--------|--------|
| Passed | 492 |
| Skipped | 5 |
| Failed | 0 |
| Total | 497 |

**Command run:** `dotnet test` from `backend/tests/CephasOps.Application.Tests` (with build).

**Relevant tests:**  
- **OrderServiceIntegrationTests** (4 tests) — each calls `CreateOrderService()`, which passes `_orderPayoutSnapshotServiceMock.Object`. All passed.  
- No other test project was run (no solution file found; only Application.Tests executed).

**Conclusion:** Full application test suite passes with the snapshot service integrated.

---

## 4. Potential risk areas and silent logic paths

### 4.1 Snapshot only when status is OrderCompleted or Completed

- **Behavior:** `CreateSnapshotForOrderIfEligibleAsync` is called only when `dto.Status == OrderStatus.OrderCompleted || dto.Status == OrderStatus.Completed` (OrderService, inside the “integrations” block after a successful workflow transition).
- **Risk:** If an order is ever moved to a “completed” state without going through `OrderService.ChangeOrderStatusAsync`, no snapshot would be created.
- **Finding:** All observed order status changes go through `OrderService.ChangeOrderStatusAsync`:
  - **OrdersController:** POST status change → `_orderService.ChangeOrderStatusAsync`.
  - **AgentModeService:** status changes → `_orderService.ChangeOrderStatusAsync`.
  - **EmailIngestionService:** reschedule approval → `_orderService.ChangeOrderStatusAsync` (to Assigned, not Completed).
  - **OrderService (internal):** automation rule auto-status and escalation both call `ChangeOrderStatusAsync` recursively.
- **WorkflowEngineService** updates `order.Status` in the database during `ExecuteTransitionAsync`, but that is only invoked from `OrderService.ChangeOrderStatusAsync`. So any transition to OrderCompleted or Completed in the workflow still returns to OrderService and runs the integrations block (including the snapshot step).
- **Mitigation:** Document that all order status changes must go through `OrderService.ChangeOrderStatusAsync`. If future code (e.g. background job or admin script) updates `Orders.Status` directly, snapshot creation will not run unless that path is changed to call the snapshot service or go through OrderService.

### 4.2 Snapshot failure does not fail the status change

- **Behavior:** Snapshot creation is inside a try/catch; on exception, a warning is logged and the status change still succeeds.
- **Risk:** Transient or persistent snapshot failures could leave some completed orders without a snapshot; support/audit would see “Live” instead of “Snapshot” when viewing payout.
- **Mitigation:** Acceptable for resilience (status change is not blocked). Optionally add monitoring on the warning log or a periodic job to backfill snapshots for completed orders that lack one.

### 4.3 Recursive calls to ChangeOrderStatusAsync

- **Paths:** (1) Automation rule auto-status (e.g. to OrderCompleted) and (2) escalation (e.g. to a target status) both call `ChangeOrderStatusAsync` again.
- **Behavior:** Each call runs the full workflow and then the integrations block; if the target status is OrderCompleted or Completed, the snapshot step runs.
- **Risk:** None for snapshot correctness; idempotent “create if eligible” ensures at most one snapshot per order.

### 4.4 OrderStatusChangedNotificationHandler

- **Behavior:** Resolves `IOrderService` via `GetRequiredService<IOrderService>()` to load order and send notifications. It does not call `ChangeOrderStatusAsync`; it only reads order data.
- **Risk:** None for snapshot integration; no missing dependency and no alternate status-change path.

---

## 5. Summary

| Check | Result |
|-------|--------|
| OrderService instantiations | Only test uses `new OrderService(...)`; it passes the snapshot service mock. Production uses DI. |
| DI registration | `IOrderPayoutSnapshotService` and `OrderService` registered; no null snapshot dependency at runtime. |
| Test suite | 492 passed, 5 skipped, 0 failed (CephasOps.Application.Tests). |
| Silent / alternate paths | All order status changes observed go through `OrderService.ChangeOrderStatusAsync`; snapshot runs for OrderCompleted and Completed. Direct DB updates to order status would bypass snapshot (document as a constraint). |

**Conclusion:** Adding `IOrderPayoutSnapshotService` to `OrderService` did not introduce unintended side effects. All consumers that need `OrderService` receive it via DI with the new dependency satisfied, and the only manual construction (in tests) supplies the snapshot service mock. Full regression sweep for the payout snapshot integration is satisfied.
