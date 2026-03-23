# SaaS remediation changelog

This document records remediation changes made to harden the CephasOps backend for multi-tenant SaaS. Each entry describes the change, scope, and backward-compatibility behavior.

---

## 2026-03-13 — Automated Operational Intelligence (Phase 1)

### Goal

Add a production-safe, rule-based operational intelligence layer that detects and surfaces patterns (installer risk, building/site risk, order risk, tenant risk, SLA/delay risk) so operations teams can act before problems escalate. Read-heavy, tenant-safe, explainable; no ML, no weakening of tenant isolation.

### Scope

- **Application:** `IOperationalIntelligenceService`, `OperationalIntelligenceService`, DTOs (InstallerRiskSignalDto, BuildingRiskSignalDto, OrderRiskSignalDto, TenantRiskSignalDto, IntelligenceExplanationDto, OperationalIntelligenceSummaryDto), `OperationalIntelligenceOptions` (configurable thresholds). Rules: stuck order, likely stuck soon, reschedule-heavy, blocker accumulation, replacement-heavy, silent order, SLA nearing breach; installer repeated blockers/high replacements/stuck orders/issue ratio; building repeated blockers/replacements; tenant stuck spike, abnormal replacement ratio.
- **API:** `OperationalIntelligenceController` (tenant: summary, orders-at-risk, installers-at-risk, buildings-at-risk, tenant-risk-signals; all RequireCompanyId). `PlatformOperationalIntelligenceController` (admin-only: platform-operational-intelligence aggregate summary via TenantScopeExecutor.RunWithPlatformBypassAsync).
- **Frontend:** `operationalIntelligence.ts` (API client + query key factory with department for cache invalidation), `OperationalIntelligenceDashboard.tsx` (summary cards, at-risk tables with expandable reasons, severity filter), route `/insights/intelligence`, Command Center menu item.
- **Docs:** `AUTOMATED_OPERATIONAL_INTELLIGENCE.md` (data sources, rules, thresholds, permissions, five SaaS scaling mistakes), OPERATIONAL_DASHBOARDS.md and SAAS_ARCHITECTURE_MAP.md updated.
- **Tests:** `OperationalIntelligenceApiTests` (tenant 200/403, platform admin 200/member 403), `OperationalIntelligenceServiceTests` (empty company throws, summary/empty lists, stuck-order rule flags order).

### Tenant safety

- Tenant endpoints require company context; all queries filter by CompanyId. Platform summary is read-only aggregation; no tenant business data in response. No new writes to tenant-scoped entities; no change to financial logic or TenantSafetyGuard.

### What remains for future phases

- Optional TenantAnomalyEvent persistence for critical intelligence signals; live War Room map; proactive alert subscriptions.

---

## 2026-03-13 — SLA Breach Engine (Phase 2)

### Goal

Implement a production-safe SLA Breach Engine: explicit nearing-breach and breached detection based on Order.KpiDueAt, with explainable states (NoSla, OnTrack, NearingBreach, Breached). Read-only, tenant-safe.

### Scope

- **Application:** OperationalSlaOptions (NearingBreachMinutes, BreachedCriticalOverdueMinutes, MaxOrdersAtRisk), SlaBreachDto (states, SlaBreachOrderItemDto, SlaBreachDistributionDto, SlaBreachSummaryDto), ISlaBreachService, SlaBreachService. Classification uses Order.KpiDueAt only; no KpiDueAt → NoSla.
- **API:** SlaBreachController (GET summary, GET orders-at-risk with optional breachState/severity); PlatformOperationalIntelligenceController (GET platform-sla-summary, admin only).
- **Frontend:** api/slaBreach.ts (client + query keys), SlaBreachDashboard.tsx (summary cards, orders-at-risk table, filters), route /insights/sla, Command Center menu, Operations Control link to SLA page.
- **Tests:** SlaBreachApiTests (tenant 200/403, platform 200/403), SlaBreachServiceTests (empty company throws, breached order flagged, no KpiDueAt not in at-risk list, platform summary).
- **Docs:** SLA_BREACH_ENGINE.md, OPERATIONAL_DASHBOARDS.md, SAAS_ARCHITECTURE_MAP.md, SAAS_REMEDIATION_CHANGELOG.md.

### Tenant safety

- Tenant endpoints RequireCompanyId; all queries company-scoped. Platform summary read-only aggregation. No new writes; no change to financial or TenantSafetyGuard.

### What remains for next phase

- Alert subscriptions; optional TenantAnomalyEvent for critical SLA breaches; War Room integration; SlaProfile-based KpiDueAt calculation.

---

## 2026-03-13 — Enterprise SaaS Upgrades Complete

Implemented enterprise SaaS operational controls:

- Tenant rate limiting
- Tenant feature flags
- Tenant health scoring
- Tenant activity audit timeline

Applied migration:

`20260313140000_AddEnterpriseSaaSColumnsAndTenantActivity`

All financial isolation tests passing.  
CephasOps SaaS hardening and enterprise upgrades are complete.

---

## 2026-03-13 — Async Event Bus / Domain Events Upgrade

### Goal

Introduce a production-safe async event bus and domain events architecture to support scalable, decoupled workflows (orders, notifications, integrations, audit timeline) without weakening tenant isolation.

### Scope

- **Documentation:** Added ASYNC_EVENT_BUS_ARCHITECTURE.md (current flows, abstractions, dispatch strategy, reliable outbox, first-use cases, observability). Added DOMAIN_EVENTS_GUIDE.md (event contract, publishing, handlers, tenant safety). Added EVENT_HANDLING_GUARDRAILS.md (five guardrails: tenant context, no cross-tenant execution, idempotency/duplicate safety, no platform bypass creep, no wrong use of eventual consistency).
- **Tenant activity timeline from events:** New handler TenantActivityTimelineFromEventsHandler records OrderCreated, OrderCompleted, OrderAssigned, OrderStatusChanged to ITenantActivityService when event.CompanyId is set; skips when missing (fail closed for timeline). Registered in DI for the four event types.
- **Architecture map:** SAAS_ARCHITECTURE_MAP.md updated with async event bus section (IEventBus, EventStore, dispatcher, tenant-scoped dispatch, guardrails refs). SAAS_ENTERPRISE_UPGRADES.md updated with event bus subsection.
- **No code removal or weakening:** Existing IEventBus, IEventStore, DomainEventDispatcher, EventStoreDispatcherHostedService, TenantScopeExecutor usage, and all tenant-safety guards unchanged. Additive only.

### Files changed

- `Application/Audit/TenantActivityTimelineFromEventsHandler.cs` (new)
- `Api/Program.cs` (register timeline handler for four event types)
- `docs/operations/ASYNC_EVENT_BUS_ARCHITECTURE.md` (new)
- `docs/operations/DOMAIN_EVENTS_GUIDE.md` (new)
- `docs/operations/EVENT_HANDLING_GUARDRAILS.md` (new)
- `docs/operations/SAAS_ARCHITECTURE_MAP.md` (event bus overview and section 8)
- `docs/operations/SAAS_ENTERPRISE_UPGRADES.md` (section 5 Async Event Bus)
- `docs/remediation/SAAS_REMEDIATION_CHANGELOG.md` (this entry)

### Behavior

- Events already published by Orders, WorkflowEngine, Billing, Payroll, Inventory, Parser continue to flow through the store and handlers. Timeline handler adds timeline rows for the four order-related event types when CompanyId is set.
- All tenant-safety guarantees preserved: dispatcher runs under entry.CompanyId; async job executor enforces job vs event company; no new platform bypass; no financial writes moved to fire-and-forget.

### What remains for future phases

- Optional EventEnvelope DTO for API/observability; more domain events as needed; event health widget in platform dashboard; request-error aggregation correlated with event failures.

---

## EventStore consistency guard (2026-03-13)

### Goal

Harden the event-driven consistency layer so duplicate processing, replay drift, and wrong-tenant event access are detected or prevented. No weakening of tenant isolation; no schema changes; minimal, production-safe guards.

### Scope

- **Duplicate event append:** In `EventStoreRepository.AppendAsync`, before adding an entry, check if `EventId` already exists; if so, call `EventStoreConsistencyGuard.RequireDuplicateAppendRejected` (log + throw). Prevents duplicate append with a clear exception instead of relying only on DB PK.
- **Replay/requeue tenant mismatch:** In `EventReplayService`, when `scopeCompanyId` is provided and `entry.CompanyId != scopeCompanyId`, log a structured warning (EventId, EventCompanyId, ScopeCompanyId, Operation, GuardReason=TenantMismatch) and return "Event not in scope." (existing behavior; observability added).
- **Async event-handling job tenant safety:** In `EventHandlingAsyncJobExecutor`, after loading the event from the store, if `job.CompanyId` and `entry.CompanyId` are both set and differ, throw `InvalidOperationException` and log. Ensures a job for one tenant cannot process another tenant’s event.
- **Replay side-effect suppression observability:** In `DomainEventDispatcher`, when replay context has `SuppressSideEffects` and async handlers are skipped, log with Operation=ReplaySuppressSideEffects, EventId, EventType, CompanyId, GuardReason=SuppressSideEffects.

### Files changed

- `Infrastructure/Persistence/EventStoreConsistencyGuard.cs` — New method `RequireDuplicateAppendRejected`.
- `Infrastructure/Persistence/EventStoreRepository.cs` — Duplicate EventId check before append.
- `Application/Events/Replay/EventReplayService.cs` — Structured warning log on replay/requeue tenant mismatch.
- `Application/Workflow/JobOrchestration/Executors/EventHandlingAsyncJobExecutor.cs` — Company match check and throw on mismatch.
- `Application/Events/DomainEventDispatcher.cs` — Structured log when SuppressSideEffects skips async enqueue.
- `docs/operations/EVENTSTORE_CONSISTENCY_GUARD.md` (new) — Audit, risks, protections, replay safeguards, limitations, verdict.
- `docs/operations/EVENTSTORE_CONSISTENCY_GUARD_REPORT.md` — Pre-existing; unchanged.
- This changelog.

### Tests added

- **EventStoreRepositoryConsistencyTests.AppendAsync_DuplicateEventId_ThrowsBeforeSave** — Duplicate EventId append throws with "Duplicate event append".
- **EventStoreConsistencyGuardTests.RequireDuplicateAppendRejected_Throws** — Guard throws with expected message.
- **EventReplayServiceTenantScopeTests.ReplayAsync_WhenScopeCompanyIdDoesNotMatchEntry_ReturnsNotInScope** — Replay returns not in scope when scope company ≠ event company.
- **EventReplayServiceTenantScopeTests.RetryAsync_WhenScopeCompanyIdDoesNotMatchEntry_ReturnsNotInScope** — Same for RetryAsync.
- **EventHandlingAsyncJobExecutorEventConsistencyTests.ExecuteAsync_WhenEventCompanyIdDoesNotMatchJobCompanyId_Throws** — Executor throws when job company ≠ event company.

### Behavior

- Valid appends (new EventId) unchanged. Duplicate append (same EventId) now fails fast with a clear guard exception and log.
- Replay/requeue API behavior unchanged (still returns "Event not in scope."); added structured logs for tenant mismatch.
- Async event-handling jobs that would process another tenant’s event now fail with a clear exception; same-company jobs unchanged.
- No API response changes; no schema or migrations.

### Remaining limitations

- **IdempotencyKey** is not enforced at append (no unique index or dedup). Duplicate logical events with different EventIds can still be stored if callers do not enforce idempotency.
- **Sync handlers** still run during replay; only async enqueue is suppressed. Handlers that perform non-idempotent side effects without idempotency keys can produce duplicates on replay; critical flows (invoice/payment) use idempotency where set. See **Sync handler replay safety** entry below for Phase 2 audit and guarding.

---

## Sync handler replay safety (2026-03-13)

### Goal

Address the remaining documented risk: replay still runs sync handlers, and non-idempotent sync handlers could create duplicate effects. Inventory all sync handlers, classify replay safety, and add minimal guards only where needed.

### Scope

- **Inventory:** All synchronous event handlers (IDomainEventHandler that are not IAsyncEventSubscriber) were listed and classified as pure/read-only, idempotent, or side-effecting (replay-safe or replay-unsafe).
- **Classification:** WorkflowTransitionCompletedEventHandler (pure), WorkflowTransitionHistoryProjectionHandler (idempotent by EventId), WorkflowTransitionLedgerHandler / OrderLifecycleLedgerHandler (ledger idempotent), OrderStatusNotificationDispatchHandler (idempotent via sourceEventId in key), IntegrationEventForwardingHandler (idempotent per doc), OrderCompletedAutomationHandler (idempotency key + InvoiceId check), OrderCompletedInsightHandler (exists check before insert) — all replay-safe. **OrderAssignedOperationsHandler:** task creation idempotent by OrderId, material pack read-only; SLA job enqueue is **not** idempotent → replay-unsafe.
- **Guard:** OrderAssignedOperationsHandler now takes optional `IReplayExecutionContextAccessor`. When `Current?.IsReplay == true`, the handler skips enqueueing the SLA evaluation BackgroundJob and logs with GuardReason=ReplaySkipSlaEnqueue. Task creation and material pack refresh still run (idempotent/safe).

### Files changed

- `Application/Events/OrderAssignedOperationsHandler.cs` — Optional `IReplayExecutionContextAccessor`; when IsReplay, skip SLA job enqueue and log.
- `docs/operations/EVENTSTORE_CONSISTENCY_GUARD.md` — New §11 (sync handler inventory, classification, guard, residual risks); §4, §6, §8, §9 updated.
- This changelog.
- `docs/operations/MULTI_TENANT_TRANSITION_AUDIT.md` — Event consistency verdict updated to mention sync handler replay safety.

### Tests added

- **OrderAssignedOperationsHandlerTests.HandleAsync_WhenReplayContextActive_DoesNotEnqueueSlaJob** — With replay context set, handler does not add any SLA job to BackgroundJobs; proves replay does not create duplicate SLA side effect.

### Behavior

- Live (non-replay): OrderAssignedOperationsHandler unchanged; still creates task, refreshes material pack, enqueues SLA job when no pending SLA.
- Replay: Same handler runs (or is skipped by processing log if already completed); when it runs under replay context, SLA enqueue is skipped; task and material pack still run. No duplicate SLA jobs.

### Residual replay risks

- When IEventProcessingLogStore is not registered, all sync handlers run on every replay; all are either idempotent or guarded.
- New sync handlers must be designed for replay (idempotent or replay guard).

---

## Platform observability dashboard (2026-03-13)

### Goal

Provide platform operators with a tenant-aware operational dashboard to monitor request volume, errors, background jobs, notifications, and integration deliveries per tenant, and to spot suspicious or overloaded tenants, without weakening tenant isolation or exposing cross-tenant data to tenant users.

### Scope

- **Backend:** New endpoints under `GET /api/platform/analytics`: `operations-summary`, `tenant-operations-overview`, `tenant-operations-detail/{tenantId}`. All require SuperAdmin or `admin.tenants.view`; aggregation uses `TenantScopeExecutor.RunWithPlatformBypassAsync` and reads only from Tenants, TenantMetricsDaily, JobExecutions, NotificationDispatches, OutboundIntegrationDeliveries, TenantAnomalyEvents. No database schema change.
- **Frontend:** New page “Platform Observability” at `/admin/platform-observability` (summary cards, tenant operations table, tenant detail drawer with 7-day trend and recent anomalies). Nav item and page visible only to users with `admin.tenants.view` or SuperAdmin.
- **Signals:** Warning/Critical derived from job failure counts and activity; optional recent anomalies from TenantAnomalyEvent.

### Files changed

- **Backend:** `Application/Platform/PlatformAnalyticsDto.cs` (new DTOs), `Application/Platform/IPlatformAnalyticsService.cs`, `Application/Platform/PlatformAnalyticsService.cs`, `Api/Controllers/PlatformAnalyticsController.cs`.
- **Frontend:** `api/platformObservability.ts` (new), `pages/admin/PlatformObservabilityPage.tsx` (new), `App.tsx` (route), `components/layout/Sidebar.tsx` (nav item + `admin.tenants.view` fallback).
- **Docs:** `docs/operations/OBSERVABILITY_DASHBOARD_DISCOVERY.md` (new), `docs/operations/TENANT_OPERATIONAL_OBSERVABILITY.md` (section 7), this changelog.

### Tests added

- **Api.Tests/Integration/PlatformObservabilityApiTests.cs:** OperationsSummary and TenantOperationsOverview return 200 for SuperAdmin; OperationsSummary returns 403 for Member; TenantOperationsDetail returns 404 for non-existent tenant.

### Authorization

- Platform observability endpoints and UI are restricted to platform admins (SuperAdmin or `admin.tenants.view`). Tenant users do not see the menu or data; API returns 403 for unauthorized callers.

### Remaining / future

- Request error count per tenant is not stored in DB; only job failures and HealthStatus drive the dashboard. Optional: add request-error aggregation to TenantMetricsDaily later.
- TenantOperationsGuard in-memory “recent warnings” are not exposed via API; dashboard uses HealthStatus and TenantAnomalyEvent for anomaly visibility.

---

## Tenant Financial Safety (2026-03-13)

### Goal

Harden financial paths (billing, payment, payout, rate resolution) so tenant financial execution remains safe, auditable, and resistant to cross-tenant or duplicate execution. No schema or API contract changes.

### Scope

- **PaymentService.CreatePaymentAsync:** Added `FinancialIsolationGuard.RequireTenantOrBypass` and `RequireCompany(companyId, "CreatePayment")` so payment creation never proceeds without valid tenant (or bypass) and company.
- **BillingService.GetInvoiceCompanyIdAsync:** When tenant scope is set and not platform bypass, return null if the invoice’s CompanyId ≠ CurrentTenantId (prevents cross-tenant company id leak).
- **Audit logs:** BillingService (Create/Update/Delete invoice) and PaymentService (Create/Update/Delete/Void/Reconcile payment) and OrderPayoutSnapshotService (CreatePayoutSnapshot) now log with tenantId, invoiceId/paymentId/orderId/snapshotId, operation, success for financial writes.
- **Duplicate execution:** Payout snapshot already has one-per-order check; invoice/payment creation have no idempotency key (documented as optional follow-up).

### Files changed

- `src/CephasOps.Application/Billing/Services/PaymentService.cs`
- `src/CephasOps.Application/Billing/Services/BillingService.cs`
- `src/CephasOps.Application/Rates/Services/OrderPayoutSnapshotService.cs`
- `tests/CephasOps.Application.Tests/Billing/PaymentServiceFinancialSafetyTests.cs` (new)
- `tests/CephasOps.Application.Tests/Billing/BillingServiceFinancialIsolationTests.cs` (GetInvoiceById cross-tenant, GetInvoiceCompanyIdAsync cross-tenant)

### Tests added

- **PaymentServiceFinancialSafetyTests:** CreatePaymentAsync with companyId null throws; GetPaymentByIdAsync other-tenant returns null; same-tenant get succeeds.
- **BillingServiceFinancialIsolationTests:** GetInvoiceByIdAsync when invoice from other tenant returns null; GetInvoiceCompanyIdAsync when tenant scope set and invoice from other tenant returns null.

### Documentation

- **backend/docs/operations/TENANT_FINANCIAL_SAFETY.md** (new): audited paths, fixes, duplicate protections, remaining items, verdict.

### Behavior

- Controllers already use RequireCompanyId or tenant provider; no API change. Payment create now fails fast with clear exception when company is missing. GetInvoiceCompanyIdAsync returns null instead of another tenant’s company when scope is set.

---

## Financial idempotency for invoice and payment (2026-03-13)

### Goal

Prevent duplicate invoice and payment creation from retries, repeated client submits, replayed requests, or background retry behavior. No weakening of tenant isolation; minimal, production-safe changes; no schema change.

### Scope

- **CreatePaymentAsync:** Optional `CreatePaymentDto.IdempotencyKey`. When set (with companyId), key `{companyId:N}:CreatePayment:{key}` is stored in existing **CommandProcessingLog**; replay returns existing payment. Email ingestion sets `IdempotencyKey = "email-payment-{emailMessage.Id}"` so the same email does not create duplicate payments.
- **CreateInvoiceAsync:** Optional `CreateInvoiceDto.IdempotencyKey`. When set (with companyId), key `{companyId:N}:CreateInvoice:{key}` is stored in CommandProcessingLog; replay returns existing invoice. OrderCompletedAutomationHandler sets `IdempotencyKey = "order-invoice-{orderId}"` so the same order does not get two automation-created invoices.
- **Design:** Reuse of `ICommandProcessingLogStore` (no new tables). Idempotency key is tenant-scoped (companyId prefix). Without key, behavior unchanged (one record per request).

### Files changed

- `Application/Billing/Services/PaymentService.cs`, `BillingService.cs` (idempotency branch + CreatePaymentCoreAsync / CreateInvoiceCoreAsync)
- `Application/Billing/DTOs/PaymentDto.cs`, `InvoiceDto.cs` (optional IdempotencyKey)
- `Application/Parser/Services/EmailIngestionService.cs` (idempotency key for payment-advice)
- `Application/Automation/Handlers/OrderCompletedAutomationHandler.cs` (idempotency key for invoice)
- `Application.Tests/Billing/*` (idempotency tests; BillingService/PaymentService constructors updated for ICommandProcessingLogStore)
- `Application.Tests/Pnl/OrderProfitabilityServiceTests.cs`, `OrderProfitAlertServiceTests.cs`, `BillingServiceInvoiceLineTests.cs` (BillingService ctor: add CommandProcessingLogStore)
- `Api/Controllers/BillingController.cs`, `PaymentsController.cs` (idempotency docs, X-Idempotency-Key header, null-dto guard)
- `Api.Tests/Integration/FinancialIdempotencyApiTests.cs` (payment idempotency API tests; require DB with ExecuteUpdate support)

### Tests added

- **Payment:** Same idempotency key twice returns same payment (no duplicate); different keys create separate payments; same key different company creates separate payments (tenant-scoped).
- **Invoice:** Same idempotency key twice returns same invoice (no duplicate); different keys create separate invoices.

### Residual limitations

- Protection only when caller supplies `IdempotencyKey` (or uses automation/email flows that set it). Requests without a key still create a new record per call.
- CommandProcessingLog retention: old idempotency rows may be purged by retention; after purge, same key could create a new record (acceptable for replay window).

### Documentation

- **backend/docs/operations/TENANT_FINANCIAL_SAFETY.md** updated: duplicate protections (section 3), remaining items (section 4), files changed (section 5), verdict (section 6).

### API/controller rollout (2026-03-13)

- **BillingController.CreateInvoice** and **PaymentsController.CreatePayment:** XML summary and remarks document optional `idempotencyKey` and `X-Idempotency-Key` header; header is applied when body key is not set; null-dto guard returns 400.
- **Integration tests:** `Api.Tests/Integration/FinancialIdempotencyApiTests.cs` added: same idempotency key twice returns same payment; same key different tenant creates separate payments; X-Idempotency-Key header replay. Tests require a DB that supports ExecuteUpdate (e.g. PostgreSQL); in-memory provider used by the test collection does not.
- **TENANT_FINANCIAL_SAFETY.md:** New section 6 (API adoption and consumer guidance): paths verified, DTOs/controllers/validation, recommended IdempotencyKey usage for API consumers.

---

## Single company mode removal (2026-03-13)

### Goal

Eliminate service behavior where null or `Guid.Empty` `companyId` was treated as "single company mode" or "all tenants," which could expose cross-tenant data when callers omitted or mis-set tenant context.

### Scope

Eight priority services from the multi-tenant transition audit were hardened so that **null/empty company never means all tenants**:

- **InventoryService** – GetMaterialsAsync, GetMaterialByIdAsync, GetMaterialByBarcodeAsync, CreateMaterialAsync, UpdateMaterialAsync, DeleteMaterialAsync, GetStockMovementsAsync
- **MaterialCategoryService** – GetMaterialCategoriesAsync, GetMaterialCategoryByIdAsync, CreateMaterialCategoryAsync, UpdateMaterialCategoryAsync, DeleteMaterialCategoryAsync
- **PartnerService** – GetPartnersAsync, GetPartnerByIdAsync, CreatePartnerAsync, UpdatePartnerAsync, DeletePartnerAsync
- **PartnerGroupService** – GetPartnerGroupsAsync, GetPartnerGroupByIdAsync, CreatePartnerGroupAsync, UpdatePartnerGroupAsync, DeletePartnerGroupAsync
- **ServiceInstallerService** – GetServiceInstallersAsync, GetServiceInstallerByIdAsync, CreateServiceInstallerAsync, UpdateServiceInstallerAsync, DeleteServiceInstallerAsync, GetContactsAsync, CreateContactAsync, UpdateContactAsync, DeleteContactAsync, GetAvailableInstallersAsync, GetInstallerSkillsAsync, AssignSkillsAsync, RemoveSkillAsync
- **BuildingTypeService** – GetBuildingTypesAsync, GetBuildingTypeByIdAsync, CreateBuildingTypeAsync, UpdateBuildingTypeAsync, DeleteBuildingTypeAsync
- **OrderCategoryService** – GetOrderCategoriesAsync, GetOrderCategoryByIdAsync, CreateOrderCategoryAsync, UpdateOrderCategoryAsync, DeleteOrderCategoryAsync
- **SplitterTypeService** – GetSplitterTypesAsync, GetSplitterTypeByIdAsync, CreateSplitterTypeAsync, UpdateSplitterTypeAsync, DeleteSplitterTypeAsync

### Pattern applied

- **effectiveCompanyId** = `companyId ?? TenantScope.CurrentTenantId`. If `!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty`: **fail closed** – list methods return empty list, get-by-id returns null, create/update/delete throw `InvalidOperationException` ("Company context is required to ..."). All queries and entity creation then use `effectiveCompanyId.Value`; no path returns or operates on all tenants' data.

### Files changed

- `src/CephasOps.Application/Inventory/Services/InventoryService.cs`
- `src/CephasOps.Application/Inventory/Services/MaterialCategoryService.cs`
- `src/CephasOps.Application/Companies/Services/PartnerService.cs`
- `src/CephasOps.Application/Companies/Services/PartnerGroupService.cs`
- `src/CephasOps.Application/ServiceInstallers/Services/ServiceInstallerService.cs`
- `src/CephasOps.Application/Buildings/Services/BuildingTypeService.cs`
- `src/CephasOps.Application/Orders/Services/OrderCategoryService.cs`
- `src/CephasOps.Application/Buildings/Services/SplitterTypeService.cs`
- `tests/CephasOps.Application.Tests/TenantIsolation/SingleCompanyModeRemovalTests.cs` (new)

### Tests added

- **SingleCompanyModeRemovalTests.cs**: No company context → GetPartnersAsync returns empty, GetPartnerByIdAsync returns null, CreatePartnerAsync throws; valid companyId / TenantScope → same-tenant success, cross-tenant get returns null.

### Behavior

- Callers that pass a valid `companyId` or have `TenantScope.CurrentTenantId` set see no change.
- Callers that pass null or `Guid.Empty` and have no tenant scope: list/get return empty/null; create/update/delete throw. No silent all-tenant visibility.
- No intentionally global methods remain in these services.

### Remaining

- **BillingRatecardService** – addressed 2026-03-13; see **BillingRatecardService tenant verification** entry below.
- **EmailTemplateService** / **SmsMessagingService** – addressed in **Email and SMS tenant-aware** entry below.

---

## BillingRatecardService tenant verification (2026-03-13)

### Goal

Remove single-company mode from BillingRatecardService so that `Guid.Empty` / missing company never means "all tenants." Controller already used `RequireCompanyId(_tenantProvider)` and passed `companyId`; the service was hardened to require valid company and fail closed.

### Scope

- **BillingRatecardService:** effectiveCompanyId = companyId != Guid.Empty ? companyId : (Guid?)TenantScope.CurrentTenantId; no valid context → GetBillingRatecardsAsync returns empty, GetBillingRatecardByIdAsync returns null, Create/Update/Delete throw "Company context is required." All queries and lookups (partners, partner groups, order categories) use effectiveCompanyId.Value; Create sets CompanyId = effectiveCompanyId.Value.
- **BillingRatecardController:** ImportPartnerRates reference-data loading changed from `(CompanyId == companyId || companyId == Guid.Empty)` to `CompanyId == companyId` only.

### Files changed

- `src/CephasOps.Application/Billing/Services/BillingRatecardService.cs`
- `src/CephasOps.Api/Controllers/BillingRatecardController.cs` (import reference-data queries only)
- `tests/CephasOps.Application.Tests/TenantIsolation/BillingRatecardTenantIsolationTests.cs` (new)

### Tests added

- **BillingRatecardTenantIsolationTests.cs:** GetBillingRatecardsAsync with Guid.Empty and no TenantScope returns empty; GetBillingRatecardByIdAsync other-tenant returns null; CreateBillingRatecardAsync with Guid.Empty and no TenantScope throws; GetBillingRatecardsAsync with valid companyId returns only that tenant's ratecards.

### PnlService and SkillService tenant isolation (2026-03-13)

**Goal:** Remove single-company / null-company behavior from PnlService and SkillService so that Guid.Empty or null company never means "all tenants."

**Scope:**

- **PnlService:** GetPnlSummaryAsync, GetPnlOrderDetailsAsync, GetPnlDetailPerOrderAsync, GetPnlPeriodsAsync, GetPnlPeriodByIdAsync, GetOverheadEntriesAsync now use effectiveCompanyId = companyId != Guid.Empty ? companyId : (Guid?)TenantScope.CurrentTenantId; when missing/empty they return empty summary, empty list, or null. CreateOverheadEntryAsync, DeleteOverheadEntryAsync, and RebuildPnlAsync throw when company context is missing; RebuildPnlAsync and all read/write paths use effectiveCompanyId.Value for filtering.
- **SkillService:** GetSkillsAsync, GetSkillByIdAsync, GetSkillCategoriesAsync, CreateSkillAsync, UpdateSkillAsync, DeleteSkillAsync now use effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId; when missing/empty, list/categories return empty, get-by-id returns null, create/update/delete throw. All queries and entity CompanyId use effectiveCompanyId.Value.

**Files changed:**

- `src/CephasOps.Application/Pnl/Services/PnlService.cs`
- `src/CephasOps.Application/ServiceInstallers/Services/SkillService.cs`
- `tests/CephasOps.Application.Tests/TenantIsolation/PnlAndSkillTenantIsolationTests.cs` (new)

**Tests added:** PnlAndSkillTenantIsolationTests: PnlService Guid.Empty/no scope → empty summary/list, CreateOverheadEntry throws; SkillService null/no scope → empty list, null get-by-id, CreateSkill throws.

---

### _MigrationHelper removal (2026-03-13)

- **Api/Controllers/_MigrationHelper.cs** was removed. The file was an empty static class with comments that it was temporary and would be deleted after migration; no references existed in the repo (Program.cs, startup, seeding, migrations, background jobs). Safe to delete.

---

## Email and SMS tenant-aware (2026-03-13)

### Goal

Remove single-company assumptions from EmailTemplateService and SmsMessagingService: tenant-specific templates and SMS, with platform (CompanyId null) fallback where intended. No DefaultCompanyId for template SMS.

### Scope

- **EmailTemplateService:** All methods accept optional `Guid? companyId`; resolve `effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId`. List/get with no context return empty/null; create requires company. Lookup by code/entity type: tenant template first, then platform template (CompanyId null). Create sets `CompanyId = effectiveCompanyId.Value`.
- **SmsMessagingService.SendTemplateSmsAsync:** Optional `Guid? companyId`; resolve from parameter or TenantScope; **DefaultCompanyId** removed. Missing context returns "Company context is required for template SMS."
- **SmsTemplateService.GetTemplateByCodeAsync:** Tenant-first then platform fallback: (CompanyId, Code) then (CompanyId null, Code).

### Tenant template resolution rules

1. **Email GetByCodeAsync / GetActiveByEntityTypeAsync:** First (Code/entityType + tenant CompanyId), then (Code/entityType + CompanyId null).
2. **SMS GetTemplateByCodeAsync:** First (Code + CompanyId), then (Code + CompanyId null).
3. **Email GetAllAsync / GetByIdAsync:** Only templates where CompanyId = tenant or CompanyId null.
4. **Create:** Company context required; new template gets tenant CompanyId.

### Files changed

- `src/CephasOps.Application/Parser/Services/IEmailTemplateService.cs`, `EmailTemplateService.cs`
- `src/CephasOps.Application/Notifications/Services/ISmsMessagingService.cs`, `SmsMessagingService.cs`
- `src/CephasOps.Application/Settings/Services/SmsTemplateService.cs`
- `src/CephasOps.Api/Controllers/EmailTemplatesController.cs`, `SmsController.cs`
- `src/CephasOps.Application/Parser/Services/EmailSendingService.cs`, `Agent/Services/AgentModeService.cs`
- `tests/CephasOps.Application.Tests/TenantIsolation/EmailTemplateTenantAwarenessTests.cs` (new)

### Tests added

- **EmailTemplateTenantAwarenessTests.cs**: No company → GetAllAsync empty, CreateAsync throws; GetByCodeAsync platform fallback; GetByIdAsync other-tenant returns null.

---

## Idempotency keys – tenant-safe format (2026-03-13)

### Goal

Ensure idempotency keys are tenant-safe: tenant-owned operations use keys that include `CompanyId` so the same logical key in different tenants does not collide. Platform-wide operations remain unchanged.

### Scope

All idempotency key generation and storage was audited. The following were updated or confirmed:

| Location | Operation | Change |
|----------|-----------|--------|
| **Command pipeline** (`IdempotencyBehavior`) | Command execution idempotency | Already tenant-safe: key prefixed with `{CompanyId:N}:` when `TenantScope.CurrentTenantId` is set. **Added:** backward-compatible lookup: when reuse fails with tenant-prefixed key, try raw key so existing records (stored before prefix) still match. |
| **External idempotency** (`ExternalIdempotencyStore`, inbound webhooks) | Inbound webhook deduplication | No change. Already tenant-safe: storage and lookup use `(IdempotencyKey, ConnectorKey, CompanyId)`; `CompanyId` is a separate column and all methods take `companyId`. |
| **NotificationService** | Email dispatch for in-app notification | Key format: when `notification.CompanyId` is set, use `{CompanyId:N}:{notification.Id}:Email`; otherwise `{notification.Id}:Email` (backward compatible). |
| **NotificationDispatchRequestService** | SMS and WhatsApp dispatch for order status | Key format: when `effectiveCompanyId` is set, use `{CompanyId:N}:{sourceEventId}:{Channel}:{target}`; otherwise previous format (backward compatible). |
| **OutboundIntegrationBus** | Outbound event delivery to connector endpoints | Key format: when `endpoint.CompanyId ?? envelope.CompanyId` is set, use `out-{CompanyId:N}-{EventId:N}-{EndpointId:N}`; otherwise `out-{EventId:N}-{EndpointId:N}` (platform-wide unchanged). |

### Key format rules

- **Tenant-owned:** When the operation has a tenant context (`CompanyId`), the stored idempotency key includes the company id (e.g. `{CompanyId:N}:{logicalKey}` or `out-{CompanyId:N}-...`) so that the same logical key in different tenants does not reuse the same result.
- **Platform-wide:** When there is no tenant (e.g. platform event, or null `CompanyId`), the key format is unchanged (no company prefix).
- **Backward compatibility:** Where applicable, lookup tries the new key first; for command pipeline, a fallback to the raw key (without tenant prefix) allows existing stored results to still be reused.

### Files changed

- `src/CephasOps.Application/Commands/Pipeline/IdempotencyBehavior.cs` – backward-compat lookup for old keys.
- `src/CephasOps.Application/Notifications/Services/NotificationService.cs` – tenant-prefixed idempotency key for email dispatch.
- `src/CephasOps.Application/Notifications/Services/NotificationDispatchRequestService.cs` – tenant-prefixed keys for SMS and WhatsApp.
- `src/CephasOps.Application/Integration/OutboundIntegrationBus.cs` – tenant-prefixed idempotency key for outbound delivery.
- `tests/CephasOps.Application.Tests/Notifications/NotificationServiceTests.cs` – expectations updated for new key format (`{CompanyId:N}:{notification.Id}:Email`).

### Behavior

- Existing idempotent behavior is preserved: same key in the same tenant still returns cached result or skips duplicate work.
- New keys under tenant context are globally unambiguous; no cross-tenant reuse.
- Platform-wide flows (no company) continue to use keys without `CompanyId`.
- Command pipeline: old records stored with raw key (no prefix) are still found via fallback lookup so existing clients do not lose idempotency until they naturally migrate to new keys.

---

## SaveChanges tenant-integrity validation (2026-03-13)

### Goal

Turn SaveChanges into a final tenant-integrity guard: when not in platform bypass, ensure every Added/Modified/Deleted tenant-scoped entity's `CompanyId` is consistent with `TenantScope.CurrentTenantId`. Fail closed on mismatch; do not persist.

### Scope

- **ApplicationDbContext.SaveChangesAsync:** After the existing "tenant context required" check, when tenant context *is* present, a second pass over Added/Modified/Deleted tenant-scoped entities enforces:
  - **Added:** If the entity has a `CompanyId` value and it is not equal to `CurrentTenantId`, throw (tenant-integrity violation). Added with null `CompanyId` is allowed.
  - **Modified / Deleted:** The entity's `CompanyId` must equal `CurrentTenantId`; otherwise throw.
- **Platform bypass:** Unchanged. When `TenantSafetyGuard.IsPlatformBypassActive` is true, all tenant validation (including the new integrity check) is skipped.
- **Entity types:** Same as before – only types considered tenant-scoped by `TenantSafetyGuard.IsTenantScopedEntityType` (CompanyScopedEntity, User, BackgroundJob, JobExecution, OrderPayoutSnapshot, InboundWebhookReceipt). No schema changes, no migrations.

### Files changed

- `src/CephasOps.Infrastructure/Persistence/ApplicationDbContext.cs` – added `GetEntityCompanyId(object entity)` (reflection) and tenant-integrity loop in `SaveChangesAsync`.
- `tests/CephasOps.Application.Tests/Persistence/SaveChangesTenantIntegrityTests.cs` – new tests: same-tenant save, Added/Modified/Deleted mismatch throws, platform bypass works, no-tenant-context throws.

### Behavior

- Same-tenant saves (entity.CompanyId == CurrentTenantId) succeed.
- Cross-tenant Add (entity.CompanyId set to another tenant) throws before persist.
- Cross-tenant Modify/Delete (entity from another tenant) throws before persist.
- Platform bypass (seeding, design-time, retention) unchanged; no validation when bypass is active.
- Exception message: `"TenantSafetyGuard: Tenant integrity violation..."` for mismatch cases.
