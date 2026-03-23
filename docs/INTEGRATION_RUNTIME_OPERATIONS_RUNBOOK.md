# Integration Runtime Operations Runbook (Phase 10)

## Inspecting failed deliveries

1. **List failed outbound deliveries**  
   `GET /api/integration/outbound/deliveries?status=Failed` (or `status=DeadLetter`). Use query params: `connectorEndpointId`, `companyId`, `eventType`, `fromUtc`, `toUtc`, `skip`, `take`.

2. **Delivery detail**  
   `GET /api/integration/outbound/deliveries/{id}`. Check `lastErrorMessage`, `lastHttpStatusCode`, `attemptCount`, `maxAttempts`. Correlate with internal event via `sourceEventId`, `correlationId`, `eventType`.

3. **Attempt history**  
   Stored in `OutboundIntegrationAttempts` (FK to delivery). Query by delivery id for full attempt history (status codes, response snippets, duration). Expose via future API or direct DB query.

## Inspecting verification failures (inbound)

1. **List receipts with VerificationFailed**  
   `GET /api/integration/inbound/receipts?status=VerificationFailed`. Optional: `connectorKey`, `companyId`, `fromUtc`, `toUtc`.

2. **Receipt detail**  
   `GET /api/integration/inbound/receipts/{id}`. Check `verificationFailureReason`, `connectorKey`, `receivedAtUtc`. No application action was taken; fix signature/secret or sender and retry from sender side.

## Replaying safely

1. **Replay outbound**  
   `POST /api/integration/outbound/replay` with body, e.g.:
   - `{ "connectorEndpointId": "<guid>", "status": "Failed", "maxCount": 50 }`
   - Or filter by `companyId`, `eventType`, `fromUtc`, `toUtc`.

2. **Behavior**  
   Deliveries in Failed or DeadLetter are set to Replaying, then re-dispatched. Success → Delivered; failure → Failed again (or DeadLetter after max attempts). Idempotency key is unchanged so duplicate deliveries are not created for the same event+endpoint.

3. **Inbound**  
   No replay of handler logic; idempotency prevents duplicate side effects if the sender retries. To “re-run” handler for a receipt you would need a custom admin action (not in scope for Phase 10).

## Pause / resume connectors

- **Pause**: Set `ConnectorEndpoint.IsPaused = true` (and optionally `IsActive = false`) in the database. Outbound bus skips paused endpoints when resolving targets; inbound endpoint resolution can exclude paused.
- **Resume**: Set `IsPaused = false` (and `IsActive = true`). No restart required; next publish or webhook uses the updated row.

(Phase 10 does not add a dedicated PATCH API for pause/resume; use DB update or a future admin endpoint.)

## Metrics to monitor

- **Outbound**: Count of deliveries by status (Pending, Delivered, Failed, DeadLetter) per connector/company; attempt count distribution; last HTTP status distribution. Derive from `OutboundIntegrationDeliveries` (and attempts) or add application metrics later.
- **Inbound**: Count of receipts by status (Received, Verified, Processed, VerificationFailed, HandlerFailed) per connector; idempotency reuse rate. Derive from `InboundWebhookReceipts`.
- **Operational**: Alert on DeadLetter count increase; alert on VerificationFailed spike; monitor replay API usage.

## Dead-letter handling

- **Outbound**: DeadLetter = delivery exceeded max attempts. Options: (1) Fix endpoint (URL, auth, payload) and run replay; (2) Mark endpoint paused and investigate; (3) Export payload for manual or alternative delivery. No automatic purge; add retention policy if needed.
- **Inbound**: HandlerFailed receipts are not auto-retried. Fix handler or data and have the sender retry (idempotency will suppress duplicate side effects).

## Common failure scenarios

| Scenario | What to check | Action |
|----------|----------------|--------|
| Outbound 4xx/5xx | `lastHttpStatusCode`, response snippet, endpoint URL/auth | Fix endpoint config or payload; replay. |
| Outbound timeout | Duration vs endpoint `TimeoutSeconds` | Increase timeout or optimize receiver; replay. |
| Inbound verification failed | Signature/timestamp, verifier config (AuthConfigJson) | Align secret and algorithm with sender; sender retries. |
| Inbound handler failed | `handlerErrorMessage`, receipt payload | Fix handler or input; sender can retry (idempotent). |
| Duplicate webhook | Receipt with idempotencyReused | Expected; no action. |
| No outbound delivery created | Connector endpoint missing or paused; event type not in AllowedEventTypes | Add/update endpoint or event filter; ensure bus is called (PublishAsync). |

## Logging and tracing

- Structured logs: delivery id, event type, connector key, receipt id, status, errors. Correlation id and source event id are on delivery/receipt for trace linkage.
- No secrets (signatures, tokens) in logs; redact if ever logged.
- For distributed tracing, add trace id to headers in `IOutboundHttpDispatcher` and to receipt/delivery metadata if needed.
