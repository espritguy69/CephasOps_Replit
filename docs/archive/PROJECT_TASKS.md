# Project Engineering Tasks

This document tracks engineering tasks that are registered in the project task tracker.
Each task here contains the full context and scope behind the short task titles stored in the tracker.

**Status values:** `pending` | `in-progress` | `completed`

---

## 1. Workflow Lifecycle Documentation Alignment

| Field | Value |
|-------|--------|
| **ID** | `workflow-doc-inprogress` |
| **Status** | completed |

### Description

Review the lifecycle status list in:

- `docs/architecture/21_workflow_order_lifecycle.md` (authoritative lifecycle doc; `docs/01_system/21_workflow_order_lifecycle.md` does not exist)

Compare it against the authoritative sources:

- `docs/05_data_model/WORKFLOW_STATUS_REFERENCE.md`
- Seeded workflow transitions in the database

### Actions

- Verify whether **InProgress** exists in the reference status list.
- Check whether it appears in seeded workflow transitions.
- If it does not exist in the authority sources, remove it from the lifecycle documentation.
- If it existed historically but is no longer used, add a clarification that the status is deprecated.

### Goal

Ensure the lifecycle documentation reflects the true authoritative workflow model.

**Completion note:** Already satisfied by previous implementation. `docs/architecture/21_workflow_order_lifecycle.md` and `docs/business/order_lifecycle_and_statuses.md` state that **InProgress** is not an order status; reference and seed use Assigned → OnTheWay → MetCustomer → OrderCompleted. WORKFLOW_STATUS_REFERENCE.md lists 17 order statuses with no InProgress; 07_gpon_order_workflow.sql seeds no InProgress transitions. No code or doc changes required. See WORKFLOW_AUDIT_SUMMARY.md §8 gap #5 and §9.

---

## 2. Building Type Fix Follow-Up

| Field | Value |
|-------|--------|
| **ID** | `building-type-fix-followup` |
| **Status** | completed |

### Description

Follow up on the implementation described in:

- `docs/06_ai/BUILDING_TYPE_FIX_IMPLEMENTATION.md`

### Tasks include

- Verify frontend handling of building type values.
- Implement any UI fixes required for building type selection.
- Perform data migration to normalize existing building type values.
- Validate compatibility with pricing logic and reporting queries.

### Goal

Ensure building type values remain consistent across database, backend, and frontend.

**Completion note:** Frontend already supports BuildingTypeId: BuildingsPage and QuickBuildingModal have BuildingTypeId dropdown; list/detail use BuildingTypeName with PropertyType fallback. Added idempotent data migration script `backend/scripts/normalize-building-type-id-from-property-type.sql` to set BuildingTypeId from PropertyType where names match. Updated BUILDING_TYPE_FIX_IMPLEMENTATION.md (frontend done, optional migration script). Compatibility: BuildingService and DTOs support both PropertyType (legacy) and BuildingTypeId; BillingRatecard still uses legacy string BuildingType for rate cards. Backend build validated.

---

## 3. Event Bus Phase 4 Follow-Up

| Field | Value |
|-------|--------|
| **ID** | `event-bus-phase4-followup` |
| **Status** | completed |

### Description

Follow up on Phase 4 improvements described in:

- `docs/EVENT_BUS_PHASE3_CORRELATION.md`

### Focus areas

- Add support for child events.
- Introduce **ParentEventId**.
- Improve event correlation tracking.
- Maintain replay compatibility.
- Preserve deterministic ordering and idempotency.

### Goal

Improve observability and lineage tracking across event chains.

**Completion note:** Added `IHasParentEvent` (Domain) and `ParentEventId` on `DomainEvent`; `EventStoreRepository.AppendAsync` now persists `ParentEventId` when present. Handlers publishing child events should set `CorrelationId` from parent and `ParentEventId = parent.EventId`. Replay and idempotency unchanged. Files: `IHasParentEvent.cs`, `DomainEvent.cs`, `EventStoreRepository.cs`; updated `EVENT_BUS_PHASE3_CORRELATION.md`. Backend build validated.

---

## 4. Service Profile Audit Follow-Up

| Field | Value |
|-------|--------|
| **ID** | `service-profile-audit-followup` |
| **Status** | completed |

### Description

Follow up on items identified in:

- `docs/SERVICE_PROFILE_AUDIT_AND_DESIGN.md`

### Tasks include

- Validate usage of **BaseWorkRate.ServiceProfileId**.
- Review cache key logic for service profiles.
- Add missing test coverage.
- Verify integration with pricing and payout logic.

### Goal

Ensure service profile logic is consistent, deterministic, and well-tested.

**Completion note:** Validated BaseWorkRate.ServiceProfileId usage in RateEngineService (resolution order: exact OrderCategoryId → ServiceProfileId → broad), BaseWorkRateService, DTOs, OrderPayoutSnapshotService, PayoutAnomalyService. Cache key in ResolveBaseWorkRateAsync correctly omits ServiceProfileId (resolution is deterministic from orderCategoryId; profile is resolved inside the cached delegate). Test coverage present in RateEngineServiceServiceProfileResolutionTests (exact category beats profile, profile fallback, no mapping → legacy, custom rate wins). Updated SERVICE_PROFILE_AUDIT_AND_DESIGN.md follow-up (c). No code changes required.

---

## 5. Operational Automation Roadmap (Phase 1: Installer Task Generation)

| Field | Value |
|-------|--------|
| **ID** | `automation-phase1-installer-task` |
| **Status** | completed |

### Description

Execute Phase 1 of the operational automation roadmap per **docs/operations/AUTOMATION_IMPLEMENTATION_PLAN.md**: Installer Task Generation.

### Delivered

- **TaskItem.OrderId** (nullable) + migration `AddOrderIdToTaskItem`; index `(CompanyId, OrderId)` with filter `OrderId IS NOT NULL`.
- **CreateTaskDto.OrderId**, **TaskDto.OrderId**; **TaskService.GetTaskByOrderIdAsync**; **CreateTaskAsync** idempotent when `OrderId` is set (returns existing task for that order).
- **CreateInstallerTaskSideEffectExecutor**: Key `createInstallerTask`, EntityType Order; on Pending→Assigned creates one task for the assigned SI (title "Complete job: Order {ServiceId}", idempotent per order).
- Registration in **Program.cs**; **SideEffectDefinition** in DatabaseSeeder; **07_gpon_order_workflow.sql** and **activate-installer-task-side-effect.sql** add `createInstallerTask: true` to Pending→Assigned.
- **CreateInstallerTaskSideEffectExecutorTests** (6 tests): Assigned creates task, non-Assigned skips, no SI skips, SI without UserId skips, Key/EntityType.

### Goal

When an order transitions to Assigned, a single installer task is created for the assigned SI; duplicate tasks for the same order are avoided.

---

## 6. Operational Automation Roadmap (Phase 2: Material Pack Generation)

| Field | Value |
|-------|--------|
| **ID** | `automation-phase2-material-pack` |
| **Status** | completed |

### Description

Phase 2 of **docs/operations/AUTOMATION_IMPLEMENTATION_PLAN.md**: Material Pack Generation.

### Delivered

- **MaterialPackDto**: OrderId, ServiceInstallerId, RequiresCollection, Message, RequiredMaterials, MissingMaterials.
- **MaterialCollectionService.GetMaterialPackAsync**: Uses existing CheckMaterialCollectionAsync and GetRequiredMaterialsForOrderAsync; no parser or material flow changes.
- **GET api/orders/{id}/material-pack**: Returns material pack for an order (required + missing). Permission: OrdersView.
- **MaterialPackTests** (3 tests): no-SI message, order not found throws, pack shape with required/missing lists.

### Goal

Single API to obtain an order’s material pack (required and missing materials) for packing or reporting.

---

## 7. Operational Automation Roadmap (Phase 3: SLA Monitoring)

| Field | Value |
|-------|--------|
| **ID** | `automation-phase3-sla-monitoring` |
| **Status** | completed |

### Description

Phase 3 of **docs/operations/AUTOMATION_IMPLEMENTATION_PLAN.md**: SLA Monitoring — ensure SLA evaluation runs automatically.

### Delivered

- **SlaEvaluationSchedulerService**: HostedService that every 15 minutes enqueues one `slaevaluation` job when none queued or running. Registered in Program.cs.
- **JobDefinitionProvider**: `slaevaluation` added to Defaults (DisplayName "SLA Evaluation", RetryAllowed true, MaxRetries 2).
- **Tests**: JobDefinitionProviderTests (GetByJobTypeAsync_ReturnsDefault_ForSlaEvaluation), SlaEvaluationSchedulerServiceTests (enqueue one when none pending; no second when one already queued).
- **Docs**: docs/operations/background_jobs.md updated with SlaEvaluationSchedulerService and slaevaluation job type.

### Goal

SLA evaluation runs periodically via the background job processor; breaches are recorded and exposed via SlaMonitorController.

---

## 8. Operational Automation Roadmap (Phase 4: Exception Detection)

| Field | Value |
|-------|--------|
| **ID** | `automation-phase4-exception-detection` |
| **Status** | completed |

### Description

Phase 4 of **docs/operations/AUTOMATION_IMPLEMENTATION_PLAN.md**: Exception Detection (payout anomaly alerting).

### Delivered

- **Verified**: PayoutAnomalyAlertSchedulerService runs when `PayoutAnomalyAlert:SchedulerEnabled` is true; calls IPayoutAnomalyAlertService.RunAlertsAsync on a configurable interval (SchedulerIntervalHours, 1–168h). No redesign.
- **Documented**: docs/operations/background_jobs.md updated to describe the scheduler (in-process, no job enqueue).

### Goal

Exception (payout anomaly) detection is scheduled and documented; alerts run automatically when enabled.

---

## 9. Operational Automation Roadmap (Phase 5: Documentation Consolidation)

| Field | Value |
|-------|--------|
| **ID** | `automation-phase5-docs` |
| **Status** | completed |

### Description

Phase 5 of **docs/operations/AUTOMATION_IMPLEMENTATION_PLAN.md**: Documentation consolidation.

### Delivered

- **AUTOMATION_IMPLEMENTATION_PLAN.md** as single source of truth for the five-phase roadmap with current state, design, and status per phase.
- **Cross-references**: background_jobs.md, PROJECT_TASKS.md (§5–§8), and operational references (installer tasks, material pack, SLA, exception detection) documented in the plan.
- **Key doc references** table added to the plan (background_jobs, workflow seeds, PROJECT_TASKS).

### Goal

Automation and operational behaviour are documented and discoverable from one plan and linked docs.

---

## 10. Event-Driven Operations Layer

| Field | Value |
|-------|--------|
| **ID** | `event-driven-operations-layer` |
| **Status** | completed |

### Description

Upgrade operational automations to an event-driven model: **OrderAssignedEvent** when order transitions to Assigned; single canonical path for installer task, material pack refresh, and SLA kickoff.

### Delivered

- **OrderAssignedEvent** (Application/Events); emitted in WorkflowEngineService when EntityType=Order and TargetStatus=Assigned (same transaction as status update).
- **OrderAssignedOperationsHandler**: creates installer task (idempotent by OrderId), calls IMaterialPackProvider.GetMaterialPackAsync, enqueues one slaevaluation job when none Queued/Running. Registered as IDomainEventHandler&lt;OrderAssignedEvent&gt; in Program.cs.
- **IMaterialPackProvider**; MaterialCollectionService implements it; handler uses interface for testability.
- **createInstallerTask** removed from Pending→Assigned in 07_gpon_order_workflow.sql; **remove-installer-task-side-effect-for-event-driven.sql** for existing DBs.
- **EventTypeRegistry**: OrderAssigned registered for replay/deserialization.
- **Tests**: WorkflowEngineServiceTests (3 events when to Assigned, 2 when not; OrderAssignedEvent content). OrderAssignedOperationsHandlerTests (task+pack+SLA; repeated call SLA idempotent; no SI skips task; order not found does nothing).
- **Docs**: EVENT_DRIVEN_OPERATIONS_PLAN.md (audit of event infra, where to emit, handlers), EVENT_DRIVEN_OPERATIONS_IMPLEMENTATION.md (event names, triggers, handlers, idempotency, retry). AUTOMATION_VERIFICATION_REPORT.md and AUTOMATION_IMPLEMENTATION_SUMMARY.md updated.

### Goal

One meaningful operational event (OrderAssignedEvent) drives installer task, material pack, and SLA kickoff with no duplicate side effects; idempotency preserved; parser/material/workflow guardrails unchanged.

---

## System Hardening Tasks

Tasks below are derived from **docs/SYSTEM_HARDENING_AUDIT.md**. They are registered for backlog tracking; implementation is deferred.

---

### Replay Engine Safety

| Field | Value |
|-------|--------|
| **ID** | `replay-company-lock` |
| **Status** | completed |

**Completion note:** Already satisfied. ReplayExecutionLock + ReplayExecutionLockStore; one active lock per company; acquire in ExecuteAsync/ExecuteByOperationIdAsync, release in finally. Stale locks expire after 2h.

**Description:** Prevent multiple Operational Replay jobs from running concurrently for the same company. Before starting or resuming a replay, acquire a lock (e.g. distributed or DB advisory lock) scoped by company (and optionally replay target). Release when the run completes or is cancelled. Reject or queue new replays for that company while the lock is held.

**Source:** SYSTEM_HARDENING_AUDIT.md §2.6 Gaps and risks; §7 Recommended Improvements #2.

**Goal:** Avoid replay race conditions, overlapping event processing, and non-deterministic rebuilds.

---

| Field | Value |
|-------|--------|
| **ID** | `replay-safety-window` |
| **Status** | completed |

**Completion note:** Already satisfied. ReplaySafetyWindow.GetCutoffUtc() (5 min); cutoff applied in preview and execution. See docs/operations/REPLAY_CONCURRENCY_AND_SAFETY.md.

**Description:** Guard against replay of very recent events while live traffic may still be writing. Reject or warn when `ToOccurredAtUtc` is within a configured window of “now” (e.g. last 5–15 minutes), or add an optional parameter to exclude recent events. Document as operational guidance.

**Source:** SYSTEM_HARDENING_AUDIT.md §2.6 Gaps and risks; §7 Recommended Improvements #5.

**Goal:** Reduce risk of replay overlapping with live updates and races on the same entities.

---

| Field | Value |
|-------|--------|
| **ID** | `replay-concurrency-docs` |
| **Status** | completed |

**Completion note:** Added docs/operations/REPLAY_CONCURRENCY_AND_SAFETY.md.

**Description:** Document replay concurrency behavior and operational guidance: one active replay per company recommendation, interaction with resume/rerun-failed, and how background job processing picks replay jobs.

**Source:** SYSTEM_HARDENING_AUDIT.md §2.5 Background job processing; §2.6.

**Goal:** Clear operational model for replay concurrency until application-level locks are implemented.

---

### Event Bus Stability

| Field | Value |
|-------|--------|
| **ID** | `event-bus-idempotency-guard` |
| **Status** | completed |

**Completion note:** Already satisfied. IEventProcessingLogStore + EventProcessingLog; TryClaimAsync per handler; single-event retry uses SuppressSideEffects.

**Description:** Introduce a global event idempotency guard so the same event is not processed more than once (including re-enqueue of async handlers). Options: “claimed until completed” or idempotency key per event/handler. Ensure single-event retry either sets a replay-like context so async handlers are not enqueued, or uses a dedicated retry path that never enqueues.

**Source:** SYSTEM_HARDENING_AUDIT.md §1.6 Gaps and risks; §7 Recommended Improvements #1.

**Goal:** Prevent duplicate event handling and duplicate async jobs when the same event is dispatched or retried multiple times.

---

| Field | Value |
|-------|--------|
| **ID** | `single-event-retry-suppress-async` |
| **Status** | completed |

**Completion note:** Already satisfied. EventReplayService uses ForSingleEventRetry() (SuppressSideEffects = true); dispatcher skips enqueue when true.

**Description:** When invoking dispatch from `EventReplayService` (single-event retry/replay), set a replay context with `SuppressSideEffects = true` so that async handlers are not enqueued, or introduce a dedicated retry path that never enqueues.

**Source:** SYSTEM_HARDENING_AUDIT.md §1.6 Gaps and risks; §7 Recommended Improvements #6.

**Goal:** Prevent a single-event retry from creating a second async job for the same event.

---

| Field | Value |
|-------|--------|
| **ID** | `stuck-processing-events` |
| **Status** | completed |

**Completion note:** Documented in docs/operations/EVENT_STORE_STUCK_PROCESSING.md (list Processing, retry endpoint, manual/Future mark Failed).

**Description:** Address events left in Processing after a crash. Document or add a small admin tool to list events in Processing older than N minutes and optionally mark them Failed for retry or move to DeadLetter. Optionally add a scheduled job that marks as Failed after a timeout so they can be retried.

**Source:** SYSTEM_HARDENING_AUDIT.md §1.6 Gaps and risks; §7 Recommended Improvements #8.

**Goal:** Recover or retry stuck Processing events and improve reliability of the event store.

---

### Event Bus Observability

| Field | Value |
|-------|--------|
| **ID** | `event-bus-observability` |
| **Status** | completed |

**Description:** Add an event processing log and observability layer: optional processing log table or structured logs per event (handler name, outcome, timestamp). Consider a small admin event bus monitoring panel with Processing/Failed/DeadLetter counts and last error. Optionally, handler execution visibility (e.g. last handler, run history) and a tool to list and optionally retry or dead-letter stuck Processing events.

**Source:** SYSTEM_HARDENING_AUDIT.md §1.6 Gaps and risks; §7 Recommended Improvements #3.

**Goal:** Make retries, stuck events, and handler failure history visible and actionable for operations.

**Completion note:** Already satisfied. EventProcessingLog table and EventBusObservabilityService; GetRecentProcessingLogAsync, GetProcessingLogByEventIdAsync; EventStoreController has dashboard (Processing/Failed/DeadLetter counts), ListProcessingLog, GetEventProcessing, GetEventDetailWithProcessing. Stuck events doc: EVENT_STORE_STUCK_PROCESSING.md.

---

### Event Ledger Integrity

| Field | Value |
|-------|--------|
| **ID** | `ledger-append-conflict-handling` |
| **Status** | completed |

**Completion note:** Already satisfied. LedgerWriter.SaveChangesAndHandleConflictAsync catches unique constraint; treats as success.

**Description:** In `LedgerWriter`, handle duplicate key on insert as success (no-op): catch unique constraint violation and treat as idempotent success, or use DB “insert on conflict do nothing” (e.g. PostgreSQL `ON CONFLICT`) so the second insert does not throw.

**Source:** SYSTEM_HARDENING_AUDIT.md §3.4 Gaps and risks; §7 Recommended Improvements #4.

**Goal:** Avoid handler failures and retries when two writers append for the same (SourceEventId, LedgerFamily) under concurrency.

---

| Field | Value |
|-------|--------|
| **ID** | `ledger-payload-snapshot-validation` |
| **Status** | completed |

**Completion note:** Already satisfied. LedgerWriter uses ILedgerPayloadValidator; max length and JSON validation. LedgerPayloadValidatorTests.

**Description:** Add optional payload snapshot validation: enforce a max length for `PayloadSnapshot` and/or validate JSON before insert. Document handler contract for snapshot format. Optional size limits and JSON validation guard to protect timeline/projection readers and avoid ledger bloat.

**Source:** SYSTEM_HARDENING_AUDIT.md §3.4 Gaps and risks; §7 Recommended Improvements #9.

**Goal:** Prevent incorrect or oversized payload snapshots from breaking timeline/projection readers or bloating the ledger.

---

### Database Performance

| Field | Value |
|-------|--------|
| **ID** | `db-replay-operations-state-index` |
| **Status** | completed |

**Description:** Add an index on `ReplayOperations.State` (or composite e.g. `(CompanyId, State)`) if profiling shows that listing or filtering by State (e.g. Running, PartiallyCompleted) is a bottleneck at scale.

**Source:** SYSTEM_HARDENING_AUDIT.md §6.4 Gaps and risks; §7 Recommended Improvements #7.

**Goal:** Improve performance of replay operation list, progress, and resume-eligibility queries and reduce table scan risk.

**Completion note:** Already satisfied. ApplicationDbContextModelSnapshot has index on ReplayOperations: (CompanyId, State, RequestedAtUtc). No migration change required.

---

### Admin API Safety

| Field | Value |
|-------|--------|
| **ID** | `admin-api-safety-verification` |
| **Status** | completed |

**Description:** Verify and document Admin API safety for replay, ledger, and timeline endpoints: confirm authorization checks (Jobs policy, JobsAdmin permission, company scope), pagination enforcement (page size and limit caps), and query limits. Document any remaining potential performance issues or unbounded query paths.

**Source:** SYSTEM_HARDENING_AUDIT.md §5 Admin API Safety (audit found no critical issues; this task formalizes verification and documentation).

**Goal:** Ensure replay, ledger, and timeline admin APIs remain safe and bounded as usage grows.

**Completion note:** Verified and documented in docs/operations/ADMIN_API_SAFETY_VERIFICATION.md (authorization, company scope, pagination caps, replay/timeline limits, indexes). No code changes required.

---

## Event Bus Phase 4 Follow-Up Pass (2026-03)

| Field | Value |
|-------|--------|
| **ID** | `eventbus-stuck-processing` |
| **Status** | completed |

**Description:** Add recovery logic for events stuck in Processing after app crash/termination. Configurable ProcessingTimeout; reset stale Processing to Failed with NextRetryAtUtc = now so they are re-claimed; structured logging. Safe against duplicate processing; preserve retry/dead-letter semantics. **Completion note:** ProcessingStartedAtUtc on EventStoreEntry; StuckProcessingTimeoutMinutes on EventBusDispatcherOptions; ResetStuckProcessingAsync in IEventStore/EventStoreRepository; dispatcher calls it each loop. EVENT_BUS_PHASE4_PRODUCTION.md updated.

---

| Field | Value |
|-------|--------|
| **ID** | `eventbus-correlation` |
| **Status** | completed |

**Description:** Ensure CorrelationId and ParentEventId are supported through event persistence and dispatch flow. **Completion note:** EventStoreEntry and AppendInCurrentTransaction already persist them; dispatch path uses deserialized payload (values preserved). Documented in EVENT_BUS_PHASE4_PRODUCTION.md §1 item 8.

---

| Field | Value |
|-------|--------|
| **ID** | `eventbus-test-provider` |
| **Status** | completed |

**Description:** Move Event Bus idempotency tests that require relational behavior (ExecuteUpdateAsync) off EF InMemory onto SQLite in-memory or equivalent. **Completion note:** EventBusIdempotencyGuardTests and ReplayExecutionLockStoreTests now use SQLite in-memory (shared connection per test class).

---

| Field | Value |
|-------|--------|
| **ID** | `docs-eventbus-phase4` |
| **Status** | completed |

**Description:** Ensure architecture/operations docs consistently describe Event Bus Phase 4 as authoritative. **Completion note:** EVENT_BUS_PHASE4_PRODUCTION.md updated (stuck recovery, StuckProcessingTimeoutMinutes, CorrelationId/ParentEventId, test provider note). Automation-rules USAGE.md fixed for InProgress (order status).

---

| Field | Value |
|-------|--------|
| **ID** | `workflow-pricing-context-test` |
| **Status** | pending |

**Description:** Record as pending only if still failing and truly outside this pass. (Workflow/order pricing context test.)

---

## Task Management Notes

- The short task titles are stored in the task tracker.
- This document exists to preserve:
  - architectural context
  - scope of work
  - links to relevant documentation
- Any updates to tasks should keep both the tracker and this file aligned.
