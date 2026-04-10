# Financial Ledger Readiness Audit

This document is a **bounded readiness audit** for potential future financial operational ledger families. It does not implement any financial ledger. It assesses whether source events and data are sufficient to support such families later.

---

## Scope

Candidates considered:

| Candidate | Description |
|-----------|-------------|
| Payout-calculation completion | Job/order earning calculated and persisted |
| Invoice generation/submission | Invoice created or submitted to portal |
| Payment matching | Payment matched to invoice/submission |
| Direct cost recognition | Cost recorded against order/job |
| Payroll earning generation | Earning included in payroll run |

---

## Assessment Criteria

For each candidate we assess:

- **Real domain event**: Is there a domain event emitted at the fact occurrence?
- **Ordering**: Can events be ordered deterministically (e.g. OccurredAtUtc + id)?
- **Payload/data**: Is there sufficient payload or linked data for a canonical ledger fact?
- **Replay-safe**: Would replaying the event be idempotent and side-effect free?
- **Verdict**: Ready now / Partially ready / Not ready.

---

## Results

### Payout-calculation completion

- **Domain event**: **No.** Payout/earning calculation is performed in services (e.g. payroll, order completion snapshot). No event is published when a calculation is completed or when an earning record is created.
- **Ordering**: N/A.
- **Payload**: Earning records and snapshots exist in DB but are not event-sourced.
- **Replay-safe**: N/A.
- **Verdict**: **Not ready.** Introduce a domain event (e.g. `EarningRecordCreated` or `PayoutSnapshotCreated`) at the point of persistence if a future ledger family is desired.

### Invoice generation/submission

- **Domain event**: **No.** Invoice creation and submission are done in BillingService / InvoiceSubmissionService. Order status transition to `SubmittedToPortal` is driven by workflow (OrderStatusChangedEvent), but the invoice submission itself does not emit a dedicated domain event.
- **Ordering**: Order lifecycle events could provide ordering for order-related invoice flow; no event for the submission fact itself.
- **Payload**: Invoice and submission entities exist; no event payload.
- **Replay-safe**: N/A.
- **Verdict**: **Not ready.** A future `InvoiceSubmittedEvent` (or similar) at submission persistence would be required for a ledger family.

### Payment matching

- **Domain event**: **No.** No domain event found for payment matching or payment application to invoices.
- **Ordering**: N/A.
- **Payload**: Payment/submission status updates exist in services; not event-sourced.
- **Replay-safe**: N/A.
- **Verdict**: **Not ready.**

### Direct cost recognition

- **Domain event**: **No.** No domain event for cost recognition or direct cost posting to orders/jobs.
- **Verdict**: **Not ready.**

### Payroll earning generation

- **Domain event**: **No.** Payroll runs and earning inclusion are handled in services. No event is published when earnings are attached to a run or when a run is finalized.
- **Verdict**: **Not ready.**

---

## Summary

| Candidate | Verdict |
|-----------|---------|
| Payout-calculation completion | Not ready |
| Invoice generation/submission | Not ready |
| Payment matching | Not ready |
| Direct cost recognition | Not ready |
| Payroll earning generation | Not ready |

**No financial ledger family is implemented or claimed.** The Event Ledger today supports only WorkflowTransition, ReplayOperationCompleted, and OrderLifecycle. Financial ledger families should be added only after corresponding domain events exist, with clear ordering and replay-safe handling.

---

## Future Steps (when product requires)

1. Introduce domain events at the point where financial facts are committed (e.g. earning created, invoice submitted, payment matched).
2. Register events in the event store and replay policy.
3. Define a bounded ledger family and idempotency key (e.g. by source event id).
4. Implement a ledger handler and document ordering and guarantees.
