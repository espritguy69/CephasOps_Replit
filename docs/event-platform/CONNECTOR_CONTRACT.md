# Connector Contract (Phase 10)

## Connector types

- **Webhook** – outbound: HTTP POST to a URL; inbound: receive POST at `/api/integration/webhooks/{connectorKey}`.
- **HttpPush** – outbound HTTP push (same as Webhook in practice; distinction for display or future policy).
- **Notification**, **Crm**, **Polling** – reserved for future use; registry and bus key off `ConnectorType` and optional `ConnectorKey` for signer/verifier/mapper.

## Required metadata

- **ConnectorDefinition**: ConnectorKey (unique), DisplayName, ConnectorType, Direction (Outbound | Inbound | Bidirectional), IsActive.
- **ConnectorEndpoint**: ConnectorDefinitionId, optional CompanyId, EndpointUrl (outbound target or inbound path hint), HttpMethod (outbound), AllowedEventTypes (optional comma-separated for outbound), RetryCount, TimeoutSeconds, IsPaused, IsActive. Optional: SigningConfigJson, AuthConfigJson (or verification config for inbound).

## Auth / signing rules

- **Outbound**: Optional. If `IOutboundSigner.CanSign(connectorType, connectorKey)` is true, the signer uses `SigningConfigJson` (e.g. secret ref, algorithm) and adds headers (e.g. X-Signature, X-Timestamp). Config must not be logged.
- **Inbound**: Optional. If `IInboundWebhookVerifier.CanVerify(connectorKey)` is true, the verifier uses request headers and body and optional `AuthConfigJson` (e.g. webhook secret). Invalid verification → 401, receipt status VerificationFailed, no handler run.

## Verification rules

- Verification runs before idempotency and handler. Failure must not execute application logic.
- Verifier returns (IsValid, FailureReason). Reason may be logged; no sensitive data.

## Idempotency rules

- **Outbound**: One delivery per (SourceEventId, ConnectorEndpointId) via IdempotencyKey `out-{eventId}-{endpointId}`. Replay reuses same delivery row and re-dispatches; no duplicate delivery row for same event+endpoint.
- **Inbound**: One successful processing per external idempotency key. Key = `in-{connectorKey}-{externalEventId}` or `in-{connectorKey}-{SHA256(body)[:32]}`. Duplicate request → 200 with idempotencyReused, no handler re-run.

## Replay rules

- **Outbound**: Only Failed and DeadLetter deliveries are eligible. Replay sets status to Replaying and calls dispatch again. Same idempotency key; receiver may see a duplicate HTTP request (receiver should be idempotent).
- **Inbound**: No built-in replay of handler; idempotency ensures sender retries are safe.

## Mapping boundary rules

- **Outbound**: Internal event → `PlatformEventEnvelope` → `IIntegrationPayloadMapper` → JSON payload. Mappers must not expose internal-only or sensitive fields. Default mapper exposes event id, name, correlation, payload; add connector-specific mappers as needed.
- **Inbound**: Raw body → `IntegrationMessage` (eventType, messageId, payloadJson, companyId). Handlers receive `IntegrationMessage` and optionally dispatch to command bus or services; domain must not depend on external schema.
