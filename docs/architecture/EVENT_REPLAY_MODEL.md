# Event Replay Model

**Date:** Event Platform Layer phase.  
**Purpose:** Define what “replay” means for domain events and integration deliveries, and how to do it safely.  
**Depends on:** EVENT_PLATFORM_ARCHITECTURE_DECISION.md.

---

## 1. Replay types

| Type | What is replayed | How | Safety |
|------|------------------|-----|--------|
| **Domain event (EventStore)** | Stored domain events by ID or bulk filter (e.g. status=Failed/DeadLetter, event type, date range). | Dispatcher (or replay job) re-claims and re-dispatches to IDomainEventHandler&lt;T&gt;. | Handlers must be idempotent. Replay can target IProjectionEventHandler only (SuppressSideEffects) to avoid side effects. |
| **Outbound integration** | Failed or DeadLetter OutboundIntegrationDelivery rows. | Replay API (ReplayAsync) re-dispatches HTTP to the same endpoint; status set to Replaying then updated to Delivered/Failed. | Endpoints must be idempotent; same delivery row re-attempted. |
| **Inbound receipt (optional)** | HandlerFailed InboundWebhookReceipt. | “Replay receipt” re-runs the handler for that receipt. | Idempotency key already used; handler must be idempotent by business key. |

---

## 2. Domain event replay

### 2.1 What it does

- **By event ID:** Load event(s) from EventStore, then call IDomainEventDispatcher.PublishAsync(evt, alreadyStored: true) so handlers run again. EventProcessingLog still enforces at-most-once per (EventId, Handler) unless replay explicitly bypasses or resets that (current design: log is respected, so handlers that already succeeded may be skipped; failed handlers get another attempt).
- **Bulk:** Reset Failed/DeadLetter events to Pending (e.g. BulkResetDeadLetterToPendingAsync, BulkResetFailedToPendingAsync) so the normal dispatcher picks them up. Or run a replay job that claims by filter and re-dispatches.
- **Projection-only:** When IReplayExecutionContext.ReplayTarget = Projection, only IProjectionEventHandler&lt;T&gt; run; side-effect handlers (e.g. task creation, notifications) can be skipped to avoid duplicate side effects.

### 2.2 What it does not do

- Does **not** re-insert events into the store (event already exists).
- Does **not** re-run business transactions (e.g. workflow transition); it only re-runs handlers for an existing event.
- Does **not** guarantee order across multiple events unless replay is sequential by partition/order.

### 2.3 Safety

- **Idempotency:** Handlers must be idempotent (e.g. create task by OrderId only if not exists). EventProcessingLog reduces duplicate handler runs for the same (EventId, Handler).
- **Side effects:** For “full” replay, side effects (e.g. create installer task) may run again; idempotency is required. For “projection only” replay, only projection handlers run.

---

## 3. Outbound integration replay

### 3.1 What it does

- **API:** ReplayAsync(ReplayOutboundRequest) with filters: ConnectorEndpointId, CompanyId, EventType, Status (Failed/DeadLetter), FromUtc, ToUtc, MaxCount.
- **Behavior:** Load matching OutboundIntegrationDelivery rows (Failed or DeadLetter), set Status = Replaying, then re-attempt HTTP dispatch for each. On success → Delivered; on failure → Failed with updated NextRetryAtUtc and AttemptCount. Record ReplayOperationId and IsReplay for audit.

### 3.2 What it does not do

- Does **not** create new domain events. It only re-sends existing delivery records to the same endpoint.
- Does **not** change the payload (same PayloadJson); endpoint must handle duplicate delivery idempotently.

### 3.3 Safety

- Endpoints should be idempotent (e.g. accept duplicate event id). Same delivery row is re-attempted; idempotency key is unchanged.

---

## 4. Inbound receipt replay (optional)

- **Concept:** For a receipt in HandlerFailed, an operator can trigger “replay this receipt” so the handler runs again for the same receipt (same payload, same idempotency key). The idempotency store already marks this key as “claimed” or “completed”; replay is an explicit override to re-run the handler once.
- **Safety:** Handler must be idempotent; duplicate external calls must not cause double side effects. Document in runbook if implemented.

---

## 5. Audit trail

- **EventStore:** ReplayId (and optionally ReplayOperation) can be set on replayed events for audit.
- **Outbound:** ReplayOperationId and IsReplay on OutboundIntegrationDelivery; ReplayOperation table if we store replay runs.
- **Inbound:** If receipt replay is added, log “replay requested at X for receipt Y” in audit or receipt metadata.

---

## 6. Operational rules

- **No “re-run everything” without scope:** Replay is always by ID, status, or bounded filter (date range, event type, endpoint). MaxCount limits bulk operations.
- **Document in runbook:** How to replay failed domain events, how to replay failed/DeadLetter outbound deliveries, and (if applicable) how to replay HandlerFailed receipts. What is safe vs unsafe (e.g. full replay without idempotent handlers is unsafe).

This document defines the replay model; implementation for EventStore and Outbound is in place; inbound receipt replay is optional and can be added and documented in the runbook.
