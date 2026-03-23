# Migration Notes – Phase 10 (External Integration Bus)

## Schema changes

Phase 10 adds the following tables:

- **ConnectorDefinitions** – connector key, type, direction, display name.
- **ConnectorEndpoints** – per-definition and optional company; URL, method, retry, timeout, event filters, signing/auth config.
- **OutboundIntegrationDeliveries** – one per event+endpoint; status, payload, attempt count, next retry, dead-letter.
- **OutboundIntegrationAttempts** – one per HTTP attempt; status code, response snippet, duration.
- **InboundWebhookReceipts** – one per received webhook; idempotency key, verification result, handler status.
- **ExternalIdempotencyRecords** – one per completed inbound key; dedup for webhooks.

Indexes support list/filter by company, connector, status, date, and idempotency lookups.

## Applying migrations

- **New database**: Run `dotnet ef database update` (or apply all migrations including `AddExternalIntegrationBus`) as usual.
- **Existing database**: The migration `20260310031127_AddExternalIntegrationBus` was generated from a full model snapshot and may include operations for tables that already exist. If update fails with “table already exists” (or similar), use one of:
  - **Option A**: Apply only the Phase 10 table creation statements manually (create the six tables and indexes that don’t exist).
  - **Option B**: Use an idempotent SQL script that creates each Phase 10 table and index only if it does not exist, then mark the migration as applied:  
    `INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20260310031127_AddExternalIntegrationBus', '10.0.x') ON CONFLICT DO NOTHING;`  
    (Adjust ProductVersion to match your EF version.)

## Operational impact

- New API routes: `/api/integration/*` (operator) and `POST /api/integration/webhooks/{connectorKey}` (inbound, anonymous). Ensure load balancer/gateway and auth policies allow or restrict as intended.
- No change to existing SLA alert, SMS, or WhatsApp paths; they remain as before. Phase 10 can later be used to replace or wrap them behind the connector abstraction.
- Outbound bus is not invoked automatically; no extra load until you call `IOutboundIntegrationBus.PublishAsync` or add an event handler that forwards events.

## Rollout

1. Deploy application and run migrations (or apply Phase 10 schema as above).
2. Seed or create at least one ConnectorDefinition and ConnectorEndpoint if you want to use outbound or inbound.
3. Optionally register HMAC (or other) signer/verifier and custom handlers; then open webhook URL to senders.
4. Use operator APIs to confirm deliveries and receipts and to replay failed outbound deliveries.

## Backward compatibility

- All new behavior is additive. Existing integrations (SlaAlertSender, SMS, WhatsApp, etc.) are unchanged unless you explicitly switch them to use the integration bus or new endpoints.
