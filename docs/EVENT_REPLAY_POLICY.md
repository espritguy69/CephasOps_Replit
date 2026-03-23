# Event Replay Policy

**Purpose:** Define which event types are safe for manual replay and how replay is controlled.

---

## 1. Principles

- **No full event sourcing.** Replay is for operational recovery and re-running handlers, not system-wide reconstruction.
- **Safe by default.** Only event types explicitly allowed may be replayed via the Replay API. Retry API does not check policy (operator responsibility).
- **Idempotency.** Handlers for replayable events must be idempotent and safe for retries.

---

## 2. Safe Event Types (Allowed for Replay)

| Event Type | Reason |
|------------|--------|
| WorkflowTransitionCompleted | Handlers are logging/audit; no destructive side effects; idempotent. |

To add a new safe type: add its name to `EventReplayPolicy.AllowedForReplay` and register the .NET type in `EventTypeRegistry`.

---

## 3. Blocked / Unsafe Event Types

- Any type **not** in the allowed list is **not** replayable via Replay API.
- Optionally add types to `EventReplayPolicy.BlockedForReplay` to make blocking explicit (e.g. future destructive or non-idempotent events).

---

## 4. Retry vs Replay

- **Retry** (`POST /api/event-store/events/{id}/retry`): Re-dispatch the stored event to current handlers. No policy check. Use for failed/dead-letter events when the operator accepts responsibility.
- **Replay** (`POST /api/event-store/events/{id}/replay`): Same as retry but only if the event type is in the replay-allowed list. Returns 400 with `BlockedReason` if type is not allowed.

---

## 5. Authorization

- View (list, detail, dashboard, replay-policy): `JobsView`.
- Retry and Replay: `JobsAdmin`.

---

## 6. Correlation and Audit

- Replay/retry preserves the event’s **CorrelationId** (same payload re-dispatched).
- **InitiatedByUserId** is passed to the replay service and logged for audit.
