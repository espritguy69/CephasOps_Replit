# Phase 10: External Integration Bus and Webhook Runtime

## Architecture overview

Phase 10 adds a production-grade external integration layer:

- **Outbound integration bus**: Publishes internal platform events to external systems via connector endpoints. Each delivery is recorded, retried with policy, and can be dead-lettered or replayed.
- **Inbound webhook runtime**: Receives external webhooks by connector key, verifies (optional), persists receipt, enforces idempotency, normalizes to an integration message, and dispatches to a handler.
- **Connector abstraction**: Connector definitions (key, type, direction) and per-company endpoints (URL, auth, retry, event filters). Endpoints can be paused.
- **Observability**: Delivery and receipt records, attempt history, operator APIs for list/detail/replay.

Internal domain stays protected: integration contracts and mapping layers sit at the application/infrastructure boundary. No third-party schemas in domain entities.

## Outbound runtime

1. **Publish**: Application (or an event handler) calls `IOutboundIntegrationBus.PublishAsync(PlatformEventEnvelope)`. The bus resolves connector endpoints for the event type and company, creates one `OutboundIntegrationDelivery` per endpoint (idempotent by eventId+endpointId), maps the envelope to JSON via `IIntegrationPayloadMapper`, optionally signs via `IOutboundSigner`, and dispatches via `IOutboundHttpDispatcher`.
2. **Dispatch**: For each delivery, HTTP send is performed. Result is recorded in `OutboundIntegrationAttempt`. On success, delivery status → Delivered. On failure, status → Failed and `NextRetryAtUtc` is set; after max attempts, status → DeadLetter.
3. **Replay**: Operator calls replay API with filters (endpoint, company, event type, status, date range). Failed and DeadLetter deliveries are re-dispatched (status set to Replaying first).

Wiring to the internal event bus is optional: you can call `PublishAsync` from an event handler that builds a `PlatformEventEnvelope` from domain events, or from any service when a platform event occurs.

## Inbound webhook runtime

1. **Receive**: `POST /api/integration/webhooks/{connectorKey}` with optional headers (X-Company-Id, X-Event-Id, X-Signature, X-Timestamp). Body is raw JSON.
2. **Verify**: If a verifier is registered for the connector, it runs. Failure → receipt status VerificationFailed, no handler run, 401 response.
3. **Persist**: An `InboundWebhookReceipt` is created (Received then Verified).
4. **Idempotency**: External idempotency key = connectorKey + (externalEventId or SHA256 of body). If already completed, return 200 with idempotencyReused. Else claim via `IExternalIdempotencyStore`.
5. **Normalize**: Payload is parsed to `IntegrationMessage` (eventType, messageId, payloadJson, companyId).
6. **Handler**: First handler that `CanHandle(connectorKey, messageType)` runs. If none, receipt is still stored and marked Processed (no-op). Handler can dispatch to command bus or application services.
7. **Complete**: On success, receipt status → Processed and idempotency marked completed. On handler exception, status → HandlerFailed.

## Connector abstraction

- **ConnectorDefinition**: ConnectorKey, DisplayName, ConnectorType (e.g. Webhook, HttpPush), Direction (Outbound/Inbound/Bidirectional). Stored in `ConnectorDefinitions`.
- **ConnectorEndpoint**: Per definition and optional CompanyId. EndpointUrl, HttpMethod, AllowedEventTypes (comma-separated for outbound), SigningConfigJson, AuthConfigJson, RetryCount, TimeoutSeconds, IsPaused, IsActive. Stored in `ConnectorEndpoints`.
- **Registry**: `IConnectorRegistry` resolves definitions by key and endpoints by event type (outbound) or connector key (inbound). Company-scoped and global endpoints supported.

New providers: add connector definition and endpoint(s), optionally implement `IIntegrationPayloadMapper`, `IOutboundSigner`, `IInboundWebhookVerifier`, `IInboundWebhookHandler` and register in DI.

## Signing and verification

- **Outbound**: `IOutboundSigner` can add a signature (e.g. HMAC) to headers using endpoint `SigningConfigJson`. NoOp implementation does nothing; add provider-specific signers as needed.
- **Inbound**: `IInboundWebhookVerifier` validates signature/timestamp and returns (IsValid, FailureReason). NoOp does not run. Implement per connector (e.g. HMAC, provider webhook secret). Verification failure is logged and never runs application logic.

## Retry, dead-letter, replay

- **Outbound**: Per-endpoint RetryCount and MaxAttempts on delivery. Exponential backoff for NextRetryAtUtc. After max attempts, status → DeadLetter. Replay API re-dispatches Failed/DeadLetter deliveries.
- **Inbound**: No automatic retry of handler failures; receipt stays HandlerFailed. Idempotency prevents duplicate side effects on retries from the sender.

## Observability

- **Outbound**: `OutboundIntegrationDeliveries` and `OutboundIntegrationAttempts` store delivery id, event type, correlation id, status, attempt count, last error, HTTP status. Operator API: list/detail deliveries, replay.
- **Inbound**: `InboundWebhookReceipts` store connector key, external id, status, verification result, handler error. Operator API: list/detail receipts.
- **Logging**: Logging in bus and runtime (delivery created/delivered/failed, webhook received/verified/rejected/processed). No secrets in logs.

## Operator usage

- **GET /api/integration/connectors** – list connector definitions.
- **GET /api/integration/connectors/endpoints** – list endpoints (filter by definition, company).
- **GET /api/integration/outbound/deliveries** – list deliveries (filter by endpoint, company, eventType, status, date).
- **GET /api/integration/outbound/deliveries/{id}** – delivery detail.
- **POST /api/integration/outbound/replay** – body: ConnectorEndpointId, CompanyId, EventType, Status, FromUtc, ToUtc, MaxCount. Replays Failed/DeadLetter deliveries.
- **GET /api/integration/inbound/receipts** – list receipts (filter by connectorKey, company, status, date).
- **GET /api/integration/inbound/receipts/{id}** – receipt detail.

Jobs policy and JobsView/JobsAdmin permissions apply. Company scoping for non–super-admin.

## Limitations

- Outbound bus is not automatically wired to the event bus; call `IOutboundIntegrationBus.PublishAsync` from application code or add an event handler that forwards selected events.
- No built-in HMAC signer/verifier; add implementations per provider.
- Inbound webhook endpoint is AllowAnonymous; rely on signature verification and optional API gateway auth for production.
- Migration `AddExternalIntegrationBus` may include other schema changes if generated from a full snapshot; for existing DBs see MIGRATION_NOTES_PHASE_10.md.
- No background worker yet for retrying pending outbound deliveries; replay is on-demand via API or future worker.
