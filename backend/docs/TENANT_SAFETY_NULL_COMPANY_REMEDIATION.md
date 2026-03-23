# Tenant Safety – Null-Company Provenance and Late Inference Remediation

This document reports the third pass: null-company provenance and late tenant inference. It follows the required 6-part format for each changed path.

---

## 1. OrderAssignedOperationsHandler (SLA job enqueue when no company)

### 1.1 Root cause
When both `order.CompanyId` and the event’s `CompanyId` (coalesced to `Guid.Empty`) were missing, the handler still enqueued an SLA evaluation `BackgroundJob` with `CompanyId = null`. That created tenant-scoped work without a tenant and forced downstream job processing to infer company from payload or run under bypass.

### 1.2 Design decision
Treat SLA enqueue as tenant-bound: do not enqueue when the effective company is missing. If `jobCompanyId == Guid.Empty` (i.e. `order.CompanyId ?? companyId` is null or empty), skip enqueue, log a warning, and return. Propagate company explicitly in payload and on the job when we do enqueue.

### 1.3 Files changed
- `backend/src/CephasOps.Application/Events/OrderAssignedOperationsHandler.cs`

### 1.4 Exact fix applied
- After resolving `jobCompanyId = order.CompanyId ?? companyId`, add a guard: `if (jobCompanyId == Guid.Empty) { _logger.LogWarning("OrderAssignedEvent: Order {OrderId} has no CompanyId; skipping SLA job enqueue (tenant-boundary).", orderId); return; }`.
- Build payload with `["companyId"] = jobCompanyId.ToString()` and set `BackgroundJob.CompanyId = jobCompanyId` (no longer set to null when empty).

### 1.5 Validation performed
- `OrderAssignedOperationsHandlerTests.HandleAsync_WhenOrderAndEventHaveNoCompanyId`: creates an order with `CompanyId = null` and an event with `CompanyId = null`, calls the handler under tenant scope, asserts no SLA job is enqueued.

### 1.6 Remaining assumptions / risks
- Orders with null `CompanyId` are data anomalies in normal operation; the handler still performs task creation and material-pack refresh when the order is found (under current tenant scope). Only the SLA job enqueue is skipped.

---

## 2. EmailIngestionSchedulerService (accounts with missing CompanyId)

### 2.1 Root cause
The scheduler enqueues email ingestion jobs for all active accounts. When an `EmailAccount` had `CompanyId` null or empty (e.g. legacy data or misconfiguration), jobs were still enqueued with `companyId: account.CompanyId` (null), creating tenant-scoped work without a tenant.

### 2.2 Design decision
Treat email ingestion as tenant-bound: only enqueue jobs for accounts that have a non-null, non-empty `CompanyId`. Skip accounts with missing company and log at debug level so the gap is visible without creating unscoped work.

### 2.3 Files changed
- `backend/src/CephasOps.Application/Workflow/Services/EmailIngestionSchedulerService.cs`

### 2.4 Exact fix applied
- At the start of the loop over `activeAccounts`, add: `if (!account.CompanyId.HasValue || account.CompanyId.Value == Guid.Empty) { _logger.LogDebug("Email ingestion: skipping account {AccountId} ({AccountName}) - CompanyId is missing (tenant-boundary).", account.Id, account.Name); continue; }`.

### 2.5 Validation performed
- No new unit test (scheduler runs under platform bypass and would require broader setup to assert skip). Manual/log review recommended for environments that may have accounts with null `CompanyId`.

### 2.6 Remaining assumptions / risks
- Email accounts are expected to be created with a company in normal flows; null is treated as misconfiguration. If a future product need requires “platform” email accounts, a separate design (e.g. explicit platform flag or bypass only for that job type) should be used instead of allowing null company.

---

## 3. NotificationDispatchRequestService (order status dispatch without company)

### 3.1 Root cause
When requesting order-status notification dispatch (SMS/WhatsApp), `effectiveCompanyId = order.CompanyId ?? companyId` could be null if both the order and the caller-supplied `companyId` were null. The code then created `NotificationDispatch` rows with `CompanyId = effectiveCompanyId` (null), i.e. tenant-bound delivery work without a tenant.

### 3.2 Design decision
Treat order-status notification dispatch as tenant-bound: require a resolved company before creating any dispatch. If `effectiveCompanyId` is null or `Guid.Empty` after `order.CompanyId ?? companyId`, return early with a warning and do not call the dispatch store.

### 3.3 Files changed
- `backend/src/CephasOps.Application/Notifications/Services/NotificationDispatchRequestService.cs`

### 3.4 Exact fix applied
- After computing `effectiveCompanyId = order.CompanyId ?? companyId`, add: `if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty) { _logger.LogWarning("Order {OrderId} has no CompanyId; skipping notification dispatch (tenant-boundary).", orderId); return; }` before building payload and creating any dispatch.

### 3.5 Validation performed
- `NotificationDispatchRequestServiceTests.RequestOrderStatusNotificationAsync_WhenOrderAndParamHaveNoCompanyId`: mocks `IOrderService` to return an order with `CompanyId = null` and `companyId` parameter null, and `IGlobalSettingsService` to allow auto-send; asserts `INotificationDispatchStore.AddAsync` is never called.

### 3.6 Remaining assumptions / risks
- Order DTOs are expected to carry `CompanyId` when loaded from tenant-scoped APIs; the early return only triggers when both order and parameter lack company.

---

## 4. NotificationService.CreateNotificationAsync (CompanyId required / resolved early)

### 4.1 Root cause
Notifications are tenant-scoped (`Notification` is guard-listed). Create allowed `dto.CompanyId` to be null and persisted notifications with null `CompanyId`, relying on middleware or callers to set tenant scope. That allowed tenant-scoped entities to be created without an explicit company and made provenance unclear.

### 4.2 Design decision
Resolve company early and fail clearly when missing: use `dto.CompanyId ?? TenantScope.CurrentTenantId` so API callers under tenant scope do not have to pass company. If the result is still null or `Guid.Empty`, throw `InvalidOperationException` so no tenant-scoped notification is saved without a company.

### 4.3 Files changed
- `backend/src/CephasOps.Application/Notifications/Services/NotificationService.cs`

### 4.4 Exact fix applied
- At the start of `CreateNotificationAsync`: `var companyId = dto.CompanyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;` then `if (!companyId.HasValue || companyId.Value == Guid.Empty) throw new InvalidOperationException("CompanyId is required to create a notification. Set it on the request or ensure tenant scope is set.");`
- Set `notification.CompanyId = companyId` (the resolved value) instead of `dto.CompanyId`.

### 4.5 Validation performed
- `NotificationServiceTests.CreateNotificationAsync_WhenCompanyIdNullAndNoTenantScope_Throws`: sets `TenantScope.CurrentTenantId = null`, calls `CreateNotificationAsync` with `dto.CompanyId = null`, asserts `InvalidOperationException` with message containing "CompanyId" and "required".
- `NotificationServiceTests.CreateNotificationAsync_WhenCompanyIdNullButTenantScopeSet_UsesTenantScope`: sets `TenantScope.CurrentTenantId = _companyId`, creates notification with `dto.CompanyId = null`, asserts `result.CompanyId == _companyId`.

### 4.6 Remaining assumptions / risks
- Callers that previously relied on middleware-only tenant scope and did not pass `CompanyId` continue to work as long as `TenantScope` is set. Callers that set neither and are not under tenant scope will now get a clear exception instead of persisting a notification with null company.

---

## 5. InboundWebhookRuntime (receipt CompanyId provenance logging)

### 5.1 Root cause
When both `request.CompanyId` and `endpoint.CompanyId` were null, the receipt was created with `CompanyId = null` under platform bypass. There was no visibility that the webhook was processed without a company, which can complicate tenant-boundary audits and handler behavior.

### 5.2 Design decision
Do not change acceptance or bypass behavior. Add a single warning log when the resolved receipt company is null or empty so provenance gaps are visible in logs. No rejection or new validation.

### 5.3 Files changed
- `backend/src/CephasOps.Application/Integration/InboundWebhookRuntime.cs`

### 5.4 Exact fix applied
- Before building the receipt: `var receiptCompanyId = request.CompanyId ?? endpoint.CompanyId;` then `if (!receiptCompanyId.HasValue || receiptCompanyId.Value == Guid.Empty) _logger.LogWarning("Inbound webhook: receipt will have null CompanyId (request and endpoint both missing company). ConnectorKey={ConnectorKey}, EndpointId={EndpointId}", request.ConnectorKey, endpoint.Id);`
- Use `receiptCompanyId` for `receipt.CompanyId` (behavior unchanged; only logging added).

### 5.5 Validation performed
- No new test (logging-only change). Manual or integration verification when both request and endpoint lack company.

### 5.6 Remaining assumptions / risks
- Webhooks that are intentionally platform-wide (no company) remain supported; the log documents the case. If certain connectors are later defined as tenant-only, rejection when company is missing could be added at that time.

---

## 6. Paths audited and not changed

- **WorkflowEngineService** – Events get `CompanyId` from `entityCompanyId`; `GetEntityCompanyIdAsync` throws when it cannot resolve company, so no null propagation.
- **AsyncEventEnqueuer** – Passes `domainEvent.CompanyId` through; event sources (e.g. workflow) already enforce company. No change.
- **StockSnapshotSchedulerService / LedgerReconciliationSchedulerService** – Enqueue with `companyId: null` by design (platform-wide jobs); documented and left as-is.
- **BackgroundJobProcessorService** – Continues to use `job.CompanyId ?? TryGetCompanyIdFromPayload` for scope; second pass ensured jobs get `CompanyId` at creation where possible; payload fallback remains for legacy or platform jobs.
- **NotificationDeliverySender / OrderStatusChangedNotificationHandler** – Use `effectiveCompanyId` from order/event; dispatch request service now refuses to create dispatch when company is missing, so no change in these call paths.
- **ParserService / EmailIngestionService** – Use `draft.CompanyId ?? companyId` and `account.CompanyId ?? Guid.Empty` in many places; email ingestion scheduler now skips accounts with null company; deeper parser/import null-company handling left for a future pass if needed.
- **CreateNotificationDto callers** (OrderService, SchedulerService, etc.) – They pass company from order or context; new validation in `CreateNotificationAsync` only affects callers that pass null and are not under tenant scope.

---

## 7. Test summary

| Test | Purpose |
|------|--------|
| `OrderAssignedOperationsHandlerTests.HandleAsync_WhenOrderAndEventHaveNoCompanyId` | Asserts no SLA job is enqueued when order and event have no company. |
| `NotificationServiceTests.CreateNotificationAsync_WhenCompanyIdNullAndNoTenantScope_Throws` | Asserts InvalidOperationException when both dto.CompanyId and TenantScope are missing. |
| `NotificationServiceTests.CreateNotificationAsync_WhenCompanyIdNullButTenantScopeSet_UsesTenantScope` | Asserts notification gets CompanyId from TenantScope when dto.CompanyId is null. |
| `NotificationDispatchRequestServiceTests.RequestOrderStatusNotificationAsync_WhenOrderAndParamHaveNoCompanyId_DoesNotCallAddAsync` | Asserts no dispatch is created when order and param have no company. |

---

## 8. Summary

- **OrderAssignedOperationsHandler**: No longer enqueues SLA job when company cannot be resolved; logs and returns.
- **EmailIngestionSchedulerService**: Skips accounts with null/empty `CompanyId` and logs.
- **NotificationDispatchRequestService**: Returns early without creating dispatch when `effectiveCompanyId` is null/empty.
- **NotificationService.CreateNotificationAsync**: Resolves company from dto or TenantScope and throws if still missing; no tenant-scoped notification without company.
- **InboundWebhookRuntime**: Logs a warning when receipt will have null CompanyId; no behavior change.
- No weakening of TenantSafetyGuard; no new blanket bypasses. CompanyId is required or resolved early on tenant-bound paths, with clear failure or skip when missing.
