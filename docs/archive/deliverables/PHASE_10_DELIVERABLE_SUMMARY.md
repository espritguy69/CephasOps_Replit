# Phase 10 Deliverable Summary

## A. Architecture audit findings

**Current state discovered**

- **Outbound**: SLA alerts use `SlaAlertSender` with a single global `WebhookUrl` (options), raw `HttpClient.PostAsync`, no delivery record, no retry or dead-letter, no signing. SMS and WhatsApp use `SmsGatewaySender` and `WhatsAppCloudApiProvider` with direct HTTP calls and config-based credentials. No shared integration bus.
- **Inbound**: No generic webhook receiver; WhatsApp “status updates” mentioned as webhook-driven but no CephasOps endpoint for them.
- **Background jobs**: `BackgroundJobProcessorService` and job types (e.g. slaevaluation) unchanged. No integration-specific job type yet.
- **Retry**: Polly used in command pipeline (Phase 9); no retry for external HTTP in SLA/SMS/WhatsApp.
- **Secrets/config**: Options (SlaAlerts, WhatsAppCloudApi), IConfiguration; no central secret store referenced for connectors.
- **Event bus**: IDomainEventDispatcher, EventStore, PlatformEventEnvelope exist. No automatic forwarding to an external bus.
- **Multi-company**: CompanyId on many entities; SLA and notifications are company-aware. No per-company connector config before Phase 10.

**Weaknesses found**

- Single global webhook URL; no per-connector or per-company endpoints.
- No durable delivery record or attempt history for external calls.
- No idempotency for inbound webhooks; no signature verification contract.
- No replay or dead-letter for failed outbound calls.
- No formal connector abstraction or registry.

**Assumptions validated or corrected**

- Confirmed: Application references Infrastructure (DbContext); store implementations live in Application. Confirmed: No MediatR; command bus is Phase 9. Confirmed: Event store and envelope exist and can be used as the source for outbound integration.

---

## B. What was implemented

**Major runtime components**

- **Outbound integration bus**: `IOutboundIntegrationBus`, `OutboundIntegrationBus`. PublishAsync(envelope) resolves endpoints, creates delivery records, maps payload, optionally signs, dispatches via `IOutboundHttpDispatcher`; retry and dead-letter via status and NextRetryAtUtc; ReplayAsync for Failed/DeadLetter.
- **Inbound webhook runtime**: `IInboundWebhookRuntime`, `InboundWebhookRuntime`. ProcessAsync: resolve endpoint, create receipt, verify (optional), idempotency claim, normalize to IntegrationMessage, run handler, mark completed/failed.
- **Connector registry**: `IConnectorRegistry`, `ConnectorRegistry` (DB). GetOutboundEndpointsForEventAsync, GetInboundEndpointAsync, ListDefinitionsAsync, ListEndpointsAsync. Company and event-type filtering.
- **Stores**: `IOutboundDeliveryStore`/`OutboundDeliveryStore`, `IInboundWebhookReceiptStore`/`InboundWebhookReceiptStore`, `IExternalIdempotencyStore`/`ExternalIdempotencyStore` (Application, using ApplicationDbContext).
- **Payload mapper**: `IIntegrationPayloadMapper`, `DefaultIntegrationPayloadMapper` (envelope → JSON). Optional `IOutboundSigner` (NoOp), `IInboundWebhookVerifier` (NoOp), `IInboundWebhookHandler` (NoOp).
- **HTTP dispatcher**: `IOutboundHttpDispatcher`, `OutboundHttpDispatcher` (Api, IHttpClientFactory). HttpClient named "IntegrationOutbound".

**Schema/migration**

- Domain entities: `ConnectorDefinition`, `ConnectorEndpoint`, `OutboundIntegrationDelivery`, `OutboundIntegrationAttempt`, `InboundWebhookReceipt`, `ExternalIdempotencyRecord`. EF configs and DbSets. Migration `20260310031127_AddExternalIntegrationBus` (full snapshot diff; for existing DBs see MIGRATION_NOTES_PHASE_10).

**Inbound/outbound pipeline**

- Outbound: PublishAsync → registry → create delivery (idempotent key) → mapper → signer → HTTP send → attempt record → status update (Delivered/Failed/DeadLetter).
- Inbound: POST webhook → receipt created → verifier → idempotency check/claim → normalize → handler → receipt/idempotency updated.

**Observability**

- Delivery and receipt tables; attempt history; operator APIs (list/detail deliveries, receipts, replay). Logging in bus and runtime (no secrets).

---

## C. Outbound integration model

- **Routing**: By event type and company. Registry returns endpoints where definition is Outbound, endpoint is active and not paused, company matches (or global), and AllowedEventTypes contains the event type (or is empty).
- **Delivery model**: One `OutboundIntegrationDelivery` per (eventId, endpointId); idempotency key `out-{eventId}-{endpointId}`. Status: Pending → Delivered or Failed; after max attempts → DeadLetter. Replaying status for replay.
- **Retry/dead-letter**: Per-endpoint RetryCount/MaxAttempts; exponential backoff for NextRetryAtUtc. Replay API re-dispatches Failed/DeadLetter; no automatic background retry worker in Phase 10.

---

## D. Inbound webhook model

- **Verification flow**: Optional verifier per connector key; runs before idempotency and handler. Failure → receipt VerificationFailed, 401, no handler.
- **Normalization flow**: Request body → IntegrationMessage (eventType, messageId, payloadJson, companyId) via JSON parse with fallback.
- **Idempotency**: Key = connectorKey + externalEventId or body hash. One successful processing per key; duplicate → 200 and idempotencyReused, no handler run.

---

## E. Connector abstraction model

- **Provider/endpoint**: ConnectorDefinition (key, type, direction); ConnectorEndpoint (URL, method, filters, auth/signing config, retry, timeout, paused). Stored in DB; resolved by `IConnectorRegistry`.
- **Configuration model**: Endpoint holds SigningConfigJson, AuthConfigJson (and verifier uses AuthConfigJson for inbound). No secrets in logs.
- **Extensibility**: Add definitions/endpoints in DB; implement IIntegrationPayloadMapper, IOutboundSigner, IInboundWebhookVerifier, IInboundWebhookHandler and register in DI.

---

## F. Security and idempotency model

- **Signing/verification**: Optional outbound signer (per connector type/key); optional inbound verifier. NoOp implementations; add HMAC or provider-specific in future.
- **Duplicate suppression**: Outbound by delivery idempotency key; inbound by external idempotency key (ExternalIdempotencyRecord). Idempotency store uses unique constraint and catch on insert.
- **Replay safety**: Outbound replay reuses same delivery row (same idempotency key); receiver may get duplicate HTTP call (receiver should be idempotent). Inbound: sender retries are safe due to idempotency.

---

## G. Cross-system observability

- **Logs**: Delivery created/delivered/failed, webhook received/verified/rejected/processed. Correlation and event ids on entities.
- **Metrics**: Not implemented in Phase 10; can be added from delivery/receipt/attempt data.
- **Tracing**: Correlation id and source event id on delivery; can be extended with trace id in headers.
- **Diagnostics**: GET /api/integration/connectors, connectors/endpoints, outbound/deliveries, outbound/deliveries/{id}, inbound/receipts, inbound/receipts/{id}; POST outbound/replay. Jobs policy and JobsView/JobsAdmin.

---

## H. Risks / limitations

- Outbound bus is not auto-wired to the event bus; application must call PublishAsync or add an event handler that forwards events.
- Migration may include non–Phase 10 schema; existing DBs may need idempotent script or manual Phase 10 tables (see MIGRATION_NOTES_PHASE_10).
- Inbound webhook endpoint is AllowAnonymous; production should use verification and/or gateway auth.
- No background worker for retrying pending outbound deliveries; replay is on-demand.
- No built-in HMAC signer/verifier; add per provider.

---

## I. Files changed

**Domain**

- `Domain/Integration/Entities/ConnectorDefinition.cs`, `ConnectorEndpoint.cs`, `OutboundIntegrationDelivery.cs`, `OutboundIntegrationAttempt.cs`, `InboundWebhookReceipt.cs`, `ExternalIdempotencyRecord.cs`.

**Application**

- `Application/Integration/IntegrationMessage.cs`, `IOutboundIntegrationBus.cs`, `IOutboundDeliveryStore.cs`, `IInboundWebhookReceiptStore.cs`, `IExternalIdempotencyStore.cs`, `IConnectorRegistry.cs`, `IIntegrationPayloadMapper.cs`, `IOutboundSigner.cs`, `IInboundWebhookVerifier.cs`, `IInboundWebhookRuntime.cs`, `IInboundWebhookHandler.cs`, `IOutboundHttpDispatcher.cs`, `OutboundDeliveryStore.cs`, `InboundWebhookReceiptStore.cs`, `ExternalIdempotencyStore.cs`, `ConnectorRegistry.cs`, `DefaultIntegrationPayloadMapper.cs`, `OutboundIntegrationBus.cs`, `InboundWebhookRuntime.cs`, `NoOpOutboundSigner.cs`, `NoOpInboundWebhookVerifier.cs`, `NoOpInboundWebhookHandler.cs`.

**Infrastructure**

- `Persistence/ApplicationDbContext.cs` (DbSets for Phase 10). `Persistence/Configurations/Integration/ConnectorDefinitionConfiguration.cs`, `ConnectorEndpointConfiguration.cs`, `OutboundIntegrationDeliveryConfiguration.cs`, `OutboundIntegrationAttemptConfiguration.cs`, `InboundWebhookReceiptConfiguration.cs`, `ExternalIdempotencyRecordConfiguration.cs`. `Persistence/Migrations/20260310031127_AddExternalIntegrationBus.cs`, `*.Designer.cs`.

**Api**

- `Program.cs` (Phase 10 DI and HttpClient). `Integration/OutboundHttpDispatcher.cs`. `Controllers/IntegrationController.cs`, `WebhooksController.cs`.

**Tests**

- `Application.Tests/Integration/OutboundIntegrationBusTests.cs`.

**Docs**

- `docs/PHASE_10_EXTERNAL_INTEGRATION_BUS.md`, `docs/INTEGRATION_RUNTIME_OPERATIONS_RUNBOOK.md`, `docs/CONNECTOR_CONTRACT.md`, `docs/MIGRATION_NOTES_PHASE_10.md`, `docs/PHASE_10_DELIVERABLE_SUMMARY.md`.

---

## J. Exact commands to run

**Apply migrations (new DB)**

```bash
cd backend
dotnet ef database update --project src/CephasOps.Infrastructure --startup-project src/CephasOps.Api
```

**Build solution**

```bash
cd backend
dotnet build
```

**Run Phase 10 tests**

```bash
cd backend/tests/CephasOps.Application.Tests
dotnet test --filter "OutboundIntegrationBusTests"
```

(If the test project does not build due to unrelated errors, run from solution root: `dotnet test backend/tests/CephasOps.Application.Tests --filter "OutboundIntegrationBusTests"`.)

**Start API**

```bash
cd backend/src/CephasOps.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://localhost:5000
```

**Verification**

- GET /api/integration/connectors (with Jobs policy auth).
- POST /api/integration/webhooks/{connectorKey} with JSON body (create a ConnectorDefinition and ConnectorEndpoint for that key first via DB or future seed).
