# CephasOps Distributed Ops Platform — Phase 2 Audit (Notifications, Phase 8, Phase 7)

**Date:** 2026-03-09

---

## STEP 1 — Current notification paths

### Inline notification (workflow)

- **Location:** `WorkflowEngineService.ExecuteTransitionAsync` (after UpdateEntityStatusAsync, before staging domain events).
- **Behavior:** If `EntityType == "Order"` and `_notificationHandler != null`, calls `_notificationHandler.HandleAsync(dto.EntityId, dto.TargetStatus, entityCompanyId, cancellationToken)` inside `Task.Run` (fire-and-forget). Exceptions are logged; workflow is not blocked.
- **OrderStatusChangedNotificationHandler:** Resolves IGlobalSettingsService, IOrderService; checks SMS_AutoSendOnStatusChange / WhatsApp_AutoSendOnStatusChange; loads order; calls `CustomerNotificationService.SendOrderStatusNotificationAsync(order, newStatus, companyId)`.
- **CustomerNotificationService:** Sends SMS and/or WhatsApp via templates (SmsProviderFactory, WhatsAppProviderFactory, ISmsTemplateService, IWhatsAppTemplateService). No persistent dispatch record; no retry/audit beyond logging.

### Event-driven path

- **OrderStatusChangedEvent** is appended to EventStore in the same transaction as the workflow (outbox). Only **OrderLifecycleLedgerHandler** is registered as `IDomainEventHandler<OrderStatusChangedEvent>` (Program.cs). **OrderStatusChangedNotificationHandler is NOT registered as an event handler** — so customer SMS/WhatsApp are sent only via the inline Task.Run path, not via the event store.

### Other notification usage

- **INotificationService / NotificationService:** CRUD for in-app `Notification` entity (GetNotificationsAsync, CreateNotificationAsync, MarkNotificationStatusAsync, ResolveUsersByRoleAsync, etc.). Used by NotificationsController, OrderService, EmailIngestionService, SchedulerService.
- **SchedulerService:** SendConflictNotificationsAsync, SendRescheduleRequestNotificationsAsync — call `_notificationService.CreateNotificationAsync` (in-app) and optionally resolve admin/manager users.
- **BackgroundJobProcessorService:** Job types `notificationsend` and `notificationretention`. ProcessNotificationSendJobAsync loads Notification by id from payload, sends email via IEmailSendingService for "Email" channel; SMS/WhatsApp logged as "not yet implemented". ProcessNotificationRetentionJobAsync archives and deletes old notifications.
- **UnifiedMessagingService:** Customer-facing WhatsApp (TrySendWhatsAppJobUpdateAsync, TrySendWhatsAppSiOnTheWayAsync, TrySendWhatsAppTtktAsync). Used elsewhere for job updates, not from workflow transition path.

### Auditability and retry

- Inline path: no persistent “notification requested” record; no retry; only log on failure.
- Event store path: events are durable and replayed; no notification handler on that path today.
- In-app Notification + BackgroundJob “notificationsend”: notification row exists; job payload has notificationId; email send is best-effort (metadata updated, no per-channel attempt history).

---

## STEP 2 — Phase 8 envelope completion

### Domain and persistence

- **EventStoreEntry:** All Phase 8 fields present (RootEventId, PartitionKey, ReplayId, SourceService, SourceModule, CapturedAtUtc, IdempotencyKey, TraceId, SpanId, Priority). Migration `20260309210000_AddEventStorePhase8PlatformEnvelope` adds columns.
- **EventStoreRepository.AppendInCurrentTransaction:** Sets Phase 8 from `envelope` parameter: RootEventId, PartitionKey, ReplayId, SourceService, SourceModule, CapturedAtUtc (defaults to `now` when envelope null), IdempotencyKey, TraceId, SpanId, Priority. When `envelope == null`, only CapturedAtUtc gets a value (now); all others stay null.
- **WorkflowEngineService:** Calls `_eventStore.AppendInCurrentTransaction(evt)` and `AppendInCurrentTransaction(orderEvt)` / `AppendInCurrentTransaction(assignedEvt)` **with no envelope**. So for all workflow-originated events, Phase 8 fields (except CapturedAtUtc which repo sets to now) are **not populated**.
- **DomainEventDispatcher.PublishAsync:** When appending (not alreadyStored), uses `_envelopeBuilder?.Build(domainEvent)` and passes envelope to `_eventStore.AppendAsync`. So events appended via the dispatcher (e.g. non-outbox code paths) get Phase 8 populated; outbox path does not use the dispatcher for append.
- **EventStoreRepository.ClaimNextPendingBatchAsync:** RETURNING and MapReaderToEntry include Phase 8 columns (26–35); MapReaderToEntry uses ReaderGuid/ReaderString/ReaderDateTime with ordinal checks for missing columns.
- **IPlatformEventEnvelopeBuilder / PlatformEventEnvelopeBuilder:** Builds PartitionKey (IPartitionKeyResolver), RootEventId from IHasRootEvent, CapturedAtUtc = UtcNow, SourceService/SourceModule/Priority from options, TraceId/SpanId from Activity.Current.

### Gaps

- **RootEventId:** Never set for workflow events (envelope not passed). Child events (OrderStatusChangedEvent, OrderAssignedEvent) have CausationId/ParentEventId but no RootEventId on the persisted entry.
- **PartitionKey, SourceService, SourceModule, Priority, TraceId, SpanId:** Null for workflow events.
- **CapturedAtUtc:** Set in repo to `now` when envelope is null; otherwise from envelope.
- **IdempotencyKey, ReplayId:** Not used on append in workflow path.

### Required fix

- In WorkflowEngineService, obtain envelope (e.g. via IPlatformEventEnvelopeBuilder) for each append and pass it to AppendInCurrentTransaction. Set RootEventId on child events (e.g. first event’s EventId as root for the chain) and ensure envelope is built for each event so PartitionKey, SourceService, SourceModule, CapturedAtUtc, Priority (and optionally TraceId/SpanId) are persisted.

---

## STEP 3 — Phase 7 lease / attempt-history status

### Migrations

- **20260309065950_VerifyNoPending:** Adds to EventStore: LastClaimedAtUtc, LastClaimedBy, LastErrorType, ProcessingLeaseExpiresAtUtc, ProcessingNodeId, ProcessingStartedAtUtc. Creates table EventStoreAttemptHistory (Id, EventId, EventType, CompanyId, HandlerName, AttemptNumber, Status, StartedAtUtc, FinishedAtUtc, DurationMs, ProcessingNodeId, ErrorType, ErrorMessage, StackTraceSummary, WasRetried, WasDeadLettered) and indexes.
- **20260312100000_AddEventStorePhase7LeaseAndAttemptHistory:** Adds same EventStore columns (LastClaimedBy, LastClaimedAtUtc, LastErrorType, ProcessingLeaseExpiresAtUtc, ProcessingNodeId) and creates same EventStoreAttemptHistory table. **Duplicate** of 20260309065950 when run in timestamp order (09 before 12). Running `dotnet ef database update` would apply 20260309065950 first, then 20260312100000 would attempt to add the same columns/table and **fail** (e.g. “column already exists”).
- **20260309065620_PendingModelCheck:** Raw SQL migration that uses ADD COLUMN IF NOT EXISTS and CREATE TABLE IF NOT EXISTS for Phase 7 and EventStoreAttemptHistory — idempotent for script-only or repair scenarios.

### Script

- **apply-EventStorePhase7LeaseAndAttemptHistory.sql:** Idempotent (DO block with IF NOT EXISTS per column/table). Inserts into __EFMigrationsHistory with MigrationId `20260312100000_AddEventStorePhase7LeaseAndAttemptHistory` so EF considers it applied. Intended for environments where Phase 7 is applied via script instead of migration.

### Code

- **EventStoreRepository:** ClaimNextPendingBatchAsync stamps ProcessingNodeId, ProcessingLeaseExpiresAtUtc, LastClaimedAtUtc, LastClaimedBy. MarkProcessedAsync clears them. ResetStuckProcessingAsync / RecoverStuckProcessingAsync use ProcessingLeaseExpiresAtUtc and ProcessingStartedAtUtc. Full Phase 7 behavior is implemented.
- **EventStoreAttemptHistoryStore:** Implements IEventStoreAttemptHistoryStore; persists EventStoreAttemptRecord to EventStoreAttemptHistory. Domain contract in IEventStoreAttemptHistoryStore (Domain); implementation in Infrastructure.

### Conclusion and action

- **Schema truth:** Phase 7 schema is **authoritatively** added by **20260309065950_VerifyNoPending**. The script is for script-only or repair deployments and is idempotent.
- **20260312100000:** Duplicate; will fail if applied after 20260309065950. **Action:** Make 20260312100000 a no-op (empty Up/Down with comment) so migration history remains consistent and new deploys do not double-apply. Document in Phase 7 closure doc.

---

## Summary

| Area | Status | Action |
|------|--------|--------|
| Notifications inline | Single path: WorkflowEngineService Task.Run → OrderStatusChangedNotificationHandler → CustomerNotificationService (SMS/WhatsApp). No event-handler registration for notifications. | Remove inline call; add notification dispatch entity and worker; handle OrderStatusChangedEvent by creating dispatch work. |
| Phase 8 envelope | Fields exist in Domain/Infrastructure; workflow appends pass no envelope, so all Phase 8 fields null (except CapturedAtUtc). | Inject IPlatformEventEnvelopeBuilder in WorkflowEngineService; build and pass envelope for each AppendInCurrentTransaction; set RootEventId on child events. |
| Phase 7 | 20260309065950 is authoritative; 20260312100000 is duplicate and would fail. Script is idempotent alternative. | Make 20260312100000 no-op; document Phase 7 truth source. |
