# Phase 11: Event / Replay / Rebuild Convergence

## A. Audit summary

- **eventhandlingasync**: Producer = `AsyncEventEnqueuer.EnqueueAsync(eventId, domainEvent)` (from DomainEventDispatcher when async handlers exist). Created BackgroundJob with payload eventId, correlationId, companyId. Executor = `ProcessEventHandlingAsyncJobAsync`: load event from IEventStore, deserialize via IEventTypeRegistry, resolve IAsyncEventSubscriber<> handlers, run each with IEventProcessingLogStore claim and IJobRunRecorderForEvents, then MarkProcessedAsync.
- **operationalreplay**: Producer = `ReplayJobEnqueuer.EnqueueReplayAsync` / `EnqueueResumeAsync`. Created ReplayOperation (Pending) and BackgroundJob with replayOperationId, scopeCompanyId, requestedByUserId. Executor: worker claim via IWorkerCoordinator.TryClaimReplayOperationAsync, then IOperationalReplayExecutionService.ExecuteByOperationIdAsync; finally ReleaseReplayOperationAsync.
- **operationalrebuild**: Producer = `RebuildJobEnqueuer.EnqueueRebuildAsync` / `EnqueueResumeAsync`. Created RebuildOperation (Pending) and BackgroundJob with rebuildOperationId, scopeCompanyId, requestedByUserId. Executor: worker claim via IWorkerCoordinator.TryClaimRebuildOperationAsync, then IOperationalRebuildService.ExecuteByOperationIdAsync; finally ReleaseRebuildOperationAsync.

## B. Convergence model chosen

- **Target: JobExecution** for all three. Same orchestration pipeline as other migrated job types.
- **Implementation**: New executors EventHandlingAsyncJobExecutor, OperationalReplayJobExecutor, OperationalRebuildJobExecutor. Producers enqueue via IJobExecutionEnqueuer instead of creating BackgroundJob. Replay/Rebuild operations keep BackgroundJobId = null for new operations; worker claim/release runs inside the executors (IWorkerIdentity + IWorkerCoordinator).

## C. Producer/execution paths migrated

- **eventhandlingasync**: AsyncEventEnqueuer now injects IJobExecutionEnqueuer and calls EnqueueAsync("eventhandlingasync", payload, companyId, correlationId, maxAttempts: 5). No BackgroundJob. EventHandlingAsyncJobExecutor runs the same logic (event store, type registry, async handlers, processing log, MarkProcessedAsync).
- **operationalreplay**: ReplayJobEnqueuer creates ReplayOperation as before; enqueues via IJobExecutionEnqueuer.EnqueueAsync("operationalreplay", payload, companyId, maxAttempts: 2). operation.BackgroundJobId left null. OperationalReplayJobExecutor: try claim (if WorkerId set), ExecuteByOperationIdAsync, release in finally.
- **operationalrebuild**: RebuildJobEnqueuer creates RebuildOperation as before; enqueues via IJobExecutionEnqueuer.EnqueueAsync("operationalrebuild", payload, companyId, maxAttempts: 2). operation.BackgroundJobId left null. OperationalRebuildJobExecutor: try claim, ExecuteByOperationIdAsync, release in finally.

## D. Legacy responsibility reduced

- **eventhandlingasync**, **operationalreplay**, **operationalrebuild** no longer executed by BackgroundJobProcessorService; replaced by ProcessEventHandlingAsyncJobDeprecatedAsync, ProcessOperationalReplayJobDeprecatedAsync, ProcessOperationalRebuildJobDeprecatedAsync (drain only). Pre-dispatch worker claim block for replay/rebuild removed from legacy processor.

## E. Idempotency/retry/operational behavior

- **EventHandlingAsync**: MaxAttempts = 5; same handler idempotency via IEventProcessingLogStore. Event marked processed after all async handlers run.
- **Replay/Rebuild**: MaxAttempts = 2. Worker claim ensures only one worker runs a given operation; release in finally. Job type strings normalized to lowercase (eventhandlingasync, operationalreplay, operationalrebuild) for registry lookup.

## F. Enqueuer API

- IJobExecutionEnqueuer / JobExecutionEnqueuer: added optional **maxAttempts** parameter (default 5) to EnqueueAsync and EnqueueWithIdAsync so replay/rebuild can use 2.

## G. Tests added

- None this phase. Executors and producer paths can be covered in follow-up.

## H. Migrations added

- None.

## I. Remaining debt after this phase

- Legacy BackgroundJob table may still contain old rows for these types; they are drained (no-op) by BackgroundJobProcessorService. No remaining legacy-only job types in the switch.

## J. Files/docs created or updated

### Created
- `Executors/EventHandlingAsyncJobExecutor.cs`
- `Executors/OperationalReplayJobExecutor.cs`
- `Executors/OperationalRebuildJobExecutor.cs`
- `docs/PHASE11_EVENT_REPLAY_REBUILD_CONVERGENCE.md`

### Updated
- `IJobExecutionEnqueuer` / `JobExecutionEnqueuer`: added maxAttempts parameter (default 5).
- `AsyncEventEnqueuer`: uses IJobExecutionEnqueuer only; no ApplicationDbContext or BackgroundJob.
- `ReplayJobEnqueuer`: injects IJobExecutionEnqueuer; creates operation then enqueues JobExecution; BackgroundJobId = null.
- `RebuildJobEnqueuer`: same pattern.
- `BackgroundJobProcessorService`: eventhandlingasync, operationalreplay, operationalrebuild → deprecation no-ops; removed full handler implementations and pre-dispatch claim block; comment updated.
- `Program.cs`: registered EventHandlingAsyncJobExecutor, OperationalReplayJobExecutor, OperationalRebuildJobExecutor.

## K. Recommended next phase

- **Phase 12+**: Payroll/Payout boundary, Inventory boundary, Reporting/read models, Workflow final convergence and completion summary per original autonomous plan.
