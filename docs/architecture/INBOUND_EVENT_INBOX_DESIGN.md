# Inbound Event Inbox Design

**Date:** Event Platform Layer phase.  
**Purpose:** Document the inbound event and webhook receipt model, idempotency, and processing flow.  
**Depends on:** EVENT_PLATFORM_ARCHITECTURE_DECISION.md, EVENT_PLATFORM_CURRENT_STATE_AUDIT.md.

---

## 1. Role of the inbox

The **inbound** path receives events or webhooks from **external** systems (e.g. MyInvois, payment providers, partners). To avoid duplicate side effects when senders retry, we:

- **Persist a receipt** for every incoming request.
- **Enforce idempotency** by external key so the same logical message is processed at most once.
- **Record processing outcome** (Processed vs HandlerFailed) for observability and optional replay.

The **InboundWebhookReceipt** entity and **InboundWebhookRuntime** implement this inbox.

---

## 2. Receipt lifecycle

| Step | Description |
|------|--------------|
| **Receive** | HTTP POST to `/api/integration/webhooks/{connectorKey}`. Body and headers (e.g. X-Event-Id, X-Signature, X-Timestamp) captured. |
| **Verify** | If an `IInboundWebhookVerifier` is registered for the connector, it validates signature/timestamp. Failure → receipt Status = VerificationFailed, 401, no handler run. |
| **Persist receipt** | Create `InboundWebhookReceipt` with Status = Received (then Verified if verification passed). Stored in InboundWebhookReceipts. |
| **Idempotency claim** | Compute external idempotency key (e.g. connectorKey + externalEventId or hash of body). `IExternalIdempotencyStore.TryClaimAsync`. If already completed → return 200 with idempotencyReused, do not run handler again. |
| **Normalize** | Parse body to `IntegrationMessage` (eventType, messageId, payloadJson, companyId). |
| **Handler** | First registered `IInboundWebhookHandler` that `CanHandle(connectorKey, messageType)` runs. If none, receipt is still marked Processed (no-op). Handler may call command bus or application services. |
| **Complete** | On success: receipt Status = Processed, idempotency marked completed. On handler exception: Status = HandlerFailed, HandlerErrorMessage and HandlerAttemptCount updated. |

---

## 3. Idempotency model

- **Key:** External idempotency key = f(connectorKey, externalEventId or body hash). Must be deterministic so sender retries produce the same key.
- **Store:** `ExternalIdempotencyRecord` (table) with (ConnectorKey, ExternalIdempotencyKey) and completion state. Unique constraint prevents duplicate completion.
- **Semantic:** Same key → at most one successful handler execution. Duplicate request → 200 with idempotencyReused and no second handler run.

---

## 4. Receipt schema (conceptual)

| Field | Purpose |
|-------|---------|
| Id | Primary key. |
| ConnectorEndpointId, ConnectorKey | Which connector received this. |
| CompanyId | Tenant (from header or payload). |
| ExternalIdempotencyKey, ExternalEventId | Idempotency and external reference. |
| MessageType | Normalized event/message type. |
| Status | Received | Verified | Processing | Processed | VerificationFailed | HandlerFailed | DeadLetter. |
| PayloadJson | Raw or normalized payload. |
| CorrelationId | For tracing. |
| VerificationPassed | Whether signature/timestamp verification succeeded. |
| ReceivedAtUtc, ProcessedAtUtc | Timestamps. |
| HandlerErrorMessage, HandlerAttemptCount | Failure observability. |

---

## 5. Normalized internal message

After parsing, the runtime builds an **IntegrationMessage** (or equivalent) with eventType, messageId, payloadJson, companyId so handlers work against a stable internal shape regardless of connector-specific JSON.

---

## 6. Handler result recording

- Success: receipt Status = Processed, ProcessedAtUtc set, idempotency marked completed.
- Handler throws: receipt Status = HandlerFailed, HandlerErrorMessage and HandlerAttemptCount updated. Idempotency remains claimed so a duplicate request does not re-run the handler; a **replay** operation can explicitly re-run the handler for that receipt (if we add receipt replay).

---

## 7. Replay and retry

- **Automatic retry:** We do **not** automatically re-run handlers for HandlerFailed receipts. The sender may retry the HTTP call; idempotency will return 200 and no second handler execution.
- **Replay receipt (optional):** An operational “replay this receipt” can re-invoke the handler for a specific receipt (e.g. after fixing a bug). The receipt is already stored; idempotency key is already used; the handler must be idempotent. Document in runbook if implemented.

---

## 8. Safe boundaries

- **Verification before persistence:** Receipt is created so we have a log of every attempt; verification failure does not run application logic.
- **No secrets in logs:** Payload and headers are logged with redaction where needed.
- **Connector-scoped:** Handlers and verifiers are registered per connector key; no cross-connector leakage.

This document describes the current inbound inbox design; implementation is in InboundWebhookRuntime, InboundWebhookReceipt, IExternalIdempotencyStore, and WebhooksController.
