# CephasOps Projection / Read Model Strategy (Phase 1)

**Date:** 2026-03-09

---

## 1. Current read-model / projection-style components

| Component | Type | Source | Use |
|-----------|------|--------|-----|
| **WorkflowTransitionHistoryEntry** | Projection table | WorkflowTransitionHistoryProjectionHandler (on WorkflowTransitionCompletedEvent) | Timeline of workflow transitions per entity. |
| **LedgerEntry** | Event-sourced ledger | OrderLifecycleLedgerHandler, WorkflowTransitionLedgerHandler | Order lifecycle and workflow events; unified order history and timeline. |
| **LedgerQueryService / OrderTimelineFromLedger** | Read from LedgerEntry | — | Operational trace and order timeline. |

Transactional state (Orders, Inventory, etc.) remains the source of truth. Events are durable and replayable; projections are eventually consistent with the event stream.

---

## 2. Strategy

- **Do not** rebuild every report as a projection in Phase 1. Keep existing transactional queries where they are correct and performant.
- **Use projections** for: (1) timeline/history views that are naturally event-sourced (already in place), (2) new dashboard aggregates that would otherwise require heavy joins or recalculation.
- **Replay:** Event replay can target "Projection" to rebuild WorkflowTransitionHistory and Ledger from EventStore without re-running side effects.
- **First high-value candidates for future projection:** P&L aggregates (currently rebuilt by PnlRebuild job from transactional data), dashboard counts by status/period. These stay as-is in Phase 1; the foundation (event types, envelope, outbox, handlers) is in place to add projection handlers later.

---

## 3. Queries that remain transactional (Phase 1)

- Order CRUD and list/filter (Orders, OrderService).
- Inventory balances, movements, serials (StockLedgerService, InventoryService).
- Billing and invoice generation (BillingService).
- Payroll and rate resolution (PayrollService, RateEngine).
- P&L rebuild and report data (PnlService, report services) — still driven by rebuild job and transactional reads.

---

## 4. Queries that are already projection-driven

- Order timeline / unified history from ledger (LedgerQueryService, OrderTimelineFromLedger).
- Workflow transition history (WorkflowTransitionHistoryEntry).
- Event store query and replay (EventStoreQueryService, replay APIs).
