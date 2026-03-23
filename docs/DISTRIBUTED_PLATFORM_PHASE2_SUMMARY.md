# CephasOps Distributed Ops Platform — Phase 2 Execution Summary

**Date:** 2026-03-09

---

## A. Notification flows audited

- **Inline path (removed):** WorkflowEngineService previously called OrderStatusChangedNotificationHandler.HandleAsync in a fire-and-forget Task.Run after workflow transition. That path is removed.
- **Event-driven path:** OrderStatusChangedEvent is appended to the outbox; OrderLifecycleLedgerHandler (ledger) and **OrderStatusNotificationDispatchHandler** (Phase 2) now handle it. The dispatch handler creates NotificationDispatch rows; a dedicated worker sends via Sms/WhatsApp.
- **Other paths:** INotificationService (in-app Notification CRUD), SchedulerService (conflict/reschedule notifications), BackgroundJobProcessorService (notificationsend job for email), UnifiedMessagingService (customer WhatsApp) unchanged. No inline notification call remains in the workflow execution path.

---

## B. Inline notification paths moved to async

- Workflow transition no longer invokes any notification handler inline. Order status change notifications are driven by **OrderStatusChangedEvent**: handler **OrderStatusNotificationDispatchHandler** calls INotificationDispatchRequestService.RequestOrderStatusNotificationAsync, which creates one or two **NotificationDispatch** rows (Sms, WhatsApp) when settings allow. **NotificationDispatchWorkerHostedService** claims pending dispatches and sends via **INotificationDeliverySender** (existing template/provider stack). CorrelationId, CausationId, SourceEventId, and CompanyId are preserved.

---

## C. New notification entities/models introduced

- **NotificationDispatch** (Domain.Notifications.Entities): Id, CompanyId, Channel, Target, TemplateKey, PayloadJson, Status (Pending | Processing | Sent | Failed | DeadLetter), AttemptCount, MaxAttempts, NextRetryAtUtc, CreatedAtUtc, UpdatedAtUtc, LastError, LastErrorAtUtc, CorrelationId, CausationId, SourceEventId, IdempotencyKey, ProcessingNodeId, ProcessingLeaseExpiresAtUtc. Table **NotificationDispatches** (migration 20260309220000_AddNotificationDispatches).

---

## D. Workers/handlers added or updated

- **OrderStatusNotificationDispatchHandler** (IDomainEventHandler&lt;OrderStatusChangedEvent&gt;): Enqueues notification work via INotificationDispatchRequestService; idempotent via IdempotencyKey (sourceEventId:Channel:Target).
- **NotificationDispatchWorkerHostedService** (BackgroundService): Polls INotificationDispatchStore.ClaimNextPendingBatchAsync, sends via INotificationDeliverySender, marks processed/retry/dead-letter. Options: Notifications:DispatchWorker (BatchSize, PollIntervalMs, LeaseSeconds, NodeId).
- **WorkflowEngineService:** No longer takes OrderStatusChangedNotificationHandler; takes IPlatformEventEnvelopeBuilder (optional) for Phase 8 envelope. All event appends pass envelope when builder is present.

---

## E. Retry/dead-letter/audit behavior implemented

- **NotificationDispatches:** Pending → (claim) → Processing → Sent or Failed. On failure: AttemptCount incremented, NextRetryAtUtc set (60s, 5m, 15m, 1h), Status = Failed; after MaxAttempts (5) or isNonRetryable, Status = DeadLetter. LastError, LastErrorAtUtc stored. Claim uses FOR UPDATE SKIP LOCKED and lease (ProcessingNodeId, ProcessingLeaseExpiresAtUtc). IdempotencyKey prevents duplicate sends on replay.

---

## F. Phase 8 envelope completion status

- **WorkflowEngineService** now builds and passes **EventStoreEnvelopeMetadata** for every AppendInCurrentTransaction call (WorkflowTransitionCompletedEvent, OrderStatusChangedEvent, OrderAssignedEvent). RootEventId set on the first event and on child events; PartitionKey, SourceService, SourceModule, CapturedAtUtc, Priority (and TraceId/SpanId when Activity is present) flow from IPlatformEventEnvelopeBuilder. EventStoreRepository already persisted Phase 8 columns; they are now populated for workflow-originated events.

---

## G. Phase 7 lease/attempt-history closure status

- **Schema truth:** 20260309065950_VerifyNoPending is the migration that adds Phase 7 EventStore columns and EventStoreAttemptHistory table.
- **20260312100000_AddEventStorePhase7LeaseAndAttemptHistory:** Converted to **no-op** (empty Up/Down) to avoid duplicate schema application when 20260309065950 is already applied. Migration remains in history for compatibility.
- **Script:** backend/scripts/apply-EventStorePhase7LeaseAndAttemptHistory.sql is the idempotent script path; documented in docs/DISTRIBUTED_PLATFORM_PHASE7_CLOSURE.md.

---

## H. Tenant/company propagation completed

- NotificationDispatch has CompanyId; RequestOrderStatusNotificationAsync uses order.CompanyId ?? companyId. OrderStatusNotificationDispatchHandler passes event.CompanyId. Worker and sender use dispatch.CompanyId for template lookup. All new notification paths are CompanyId-aware.

---

## I. Tests added

- **WorkflowEngineServiceTests:** CreateServiceWithEventStore updated to drop notificationHandler and pass envelopeBuilder: null. No new test project for notification dispatch yet; recommended: unit tests for NotificationDispatchRequestService (idempotency, skip when disabled), and integration-style test for claim/send/mark (optional).

---

## J. Migrations/scripts added or formalized

- **Added:** 20260309220000_AddNotificationDispatches.cs (NotificationDispatches table and indexes).
- **Formalized:** Phase 7 documented; 20260312100000 made no-op. Script apply-EventStorePhase7LeaseAndAttemptHistory.sql remains the script-only path.

---

## K. Remaining notification/platform debt

- OrderStatusChangedNotificationHandler and CustomerNotificationService remain registered and used only by the worker’s INotificationDeliverySender path (same template/provider stack). They could be refactored to a single “send from dispatch” path if desired.
- SchedulerService and other callers still use INotificationService.CreateNotificationAsync (in-app + optional email job); no change in Phase 2.
- Phase 8: TraceId/SpanId depend on Activity; ensure workflow API uses tracing when needed. IdempotencyKey and ReplayId on events not yet set from workflow path.
- Event store and notification dispatch both use lease-based claim; consider unified node identity (e.g. WorkerOptions.NodeId) if scaling multiple nodes.

---

## L. Recommended Phase 3 extraction candidate

- **Job orchestration** (BackgroundJob + processor, worker coordination) per DISTRIBUTED_PLATFORM_BOUNDARIES.md. Then Payroll/Payout, Inventory, Reporting, Workflow.

---

## M. Files/docs created or updated

**Created**
- docs/DISTRIBUTED_PLATFORM_PHASE2_AUDIT.md
- docs/DISTRIBUTED_PLATFORM_PHASE7_CLOSURE.md
- docs/DISTRIBUTED_PLATFORM_PHASE2_SUMMARY.md
- backend/src/CephasOps.Domain/Notifications/Entities/NotificationDispatch.cs
- backend/src/CephasOps.Domain/Notifications/INotificationDispatchStore.cs
- backend/src/CephasOps.Application/Notifications/INotificationDispatchRequestService.cs
- backend/src/CephasOps.Application/Notifications/INotificationDeliverySender.cs
- backend/src/CephasOps.Application/Notifications/Services/NotificationDispatchRequestService.cs
- backend/src/CephasOps.Application/Notifications/Services/NotificationDeliverySender.cs
- backend/src/CephasOps.Application/Notifications/Handlers/OrderStatusNotificationDispatchHandler.cs
- backend/src/CephasOps.Application/Notifications/NotificationDispatchWorkerHostedService.cs
- backend/src/CephasOps.Infrastructure/Persistence/Configurations/Notifications/NotificationDispatchConfiguration.cs
- backend/src/CephasOps.Infrastructure/Persistence/Migrations/20260309220000_AddNotificationDispatches.cs
- backend/src/CephasOps.Infrastructure/Persistence/NotificationDispatchStore.cs

**Updated**
- backend/src/CephasOps.Application/Workflow/Services/WorkflowEngineService.cs — removed inline notification; added IPlatformEventEnvelopeBuilder; pass envelope for all event appends; set RootEventId on events.
- backend/src/CephasOps.Infrastructure/Persistence/ApplicationDbContext.cs — DbSet NotificationDispatches.
- backend/src/CephasOps.Api/Program.cs — register INotificationDispatchStore, INotificationDispatchRequestService, INotificationDeliverySender, OrderStatusNotificationDispatchHandler, NotificationDispatchWorkerOptions, NotificationDispatchWorkerHostedService.
- backend/src/CephasOps.Infrastructure/Persistence/Migrations/20260312100000_AddEventStorePhase7LeaseAndAttemptHistory.cs — no-op Up/Down.
- backend/tests/CephasOps.Application.Tests/Workflow/WorkflowEngineServiceTests.cs — CreateServiceWithEventStore uses envelopeBuilder: null, no notificationHandler.

**Deleted**
- (Application’s INotificationDispatchStore and Persistence/NotificationDispatchStore removed in favor of Domain interface and Infrastructure implementation.)
