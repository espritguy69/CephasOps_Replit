# Phase 9: Command Bus and Workflow Orchestration

## Architecture audit summary

- **No MediatR.** Commands are sent via `ICommandBus`; handlers implement `ICommandHandler<TCommand, TResult>` and are resolved from DI.
- **WorkflowJob** remains a single-transition job (EntityType, EntityId, CurrentStatus, TargetStatus, State). It is not multi-step.
- **BackgroundJob** is the generic queue (JobType, PayloadJson) processed by `BackgroundJobProcessorService`; unchanged.
- **Event processing idempotency** continues to use `IEventProcessingLogStore` (TryClaimAsync per event+handler).
- **Command idempotency** uses `ICommandProcessingLogStore` and the `CommandProcessingLogs` table (one successful completion per idempotency key).
- **Event bus** (IDomainEventDispatcher, EventStore, lineage) is Phase 8; commands can be sent from process managers in response to events.

---

## Command bus model

- **ICommand / ICommand&lt;TResult&gt;**  
  Marker with optional `IdempotencyKey`, `CorrelationId`, `WorkflowInstanceId`.

- **ICommandHandler&lt;TCommand, TResult&gt;**  
  `Task<TResult> HandleAsync(TCommand command, CancellationToken ct)`.

- **ICommandBus**  
  - `Task<CommandResult<TResult>> SendAsync<TCommand, TResult>(TCommand command, CommandOptions options = null, CancellationToken ct)`  
  - `Task<CommandResult<object?>> SendAsync(object command, CommandOptions options = null, CancellationToken ct)` for process managers (runtime type).

- **CommandResult&lt;T&gt;**  
  `Success`, `Result`, `ErrorMessage`, `IdempotencyReused`, `ExecutionId`.

- **CommandOptions**  
  `IdempotencyKey` override, `RequireIdempotency`, `EnqueueAsync`, `Timeout`.

---

## Pipeline

Order: **Validation → Idempotency → Logging → Retry → Handler.**

- **ValidationBehavior**  
  Optional validation (FluentValidation not in use; passes through).

- **IdempotencyBehavior**  
  Uses `ICommandProcessingLogStore`: TryClaimAsync; on reuse returns cached result with `IdempotencyReused = true`; on claim runs next then MarkCompletedAsync / MarkFailedAsync.

- **LoggingBehavior**  
  Logs command start and completion (success or failure).

- **RetryBehavior**  
  Polly retry (exponential backoff) for transient failures (e.g. timeout, deadlock, connection).

- **CommandBus**  
  Resolves `ICommandHandler<TCommand, TResult>` and ordered `ICommandPipelineBehavior<TCommand, TResult>`, builds the chain, sets `CommandPipelineContext` (options, execution id), runs pipeline, returns `CommandResult`.

---

## Idempotency

- **ICommandProcessingLogStore**  
  - `TryClaimAsync(commandId, idempotencyKey, commandType, correlationId, workflowInstanceId, ct)` → bool  
  - `MarkCompletedAsync(commandId, resultJson, ct)`  
  - `MarkFailedAsync(commandId, errorMessage, ct)`  
  - `TryGetCompletedResultAsync(idempotencyKey, ct)` → cached result for reuse  

- **CommandProcessingLog** (table `CommandProcessingLogs`): Id, IdempotencyKey, CommandType, CorrelationId, WorkflowInstanceId, Status (Pending/Completed/Failed), ResultJson, ErrorMessage, CompletedAtUtc, CreatedAtUtc. Unique index on IdempotencyKey.

- **CommandProcessingLogStore** (Application) uses `ApplicationDbContext`; claim is insert (first time) or delete+insert after Failed (retry). Concurrency-safe via unique key.

---

## Workflow orchestrator

- **WorkflowInstance** (table `WorkflowInstances`): Id, WorkflowDefinitionId, WorkflowType, EntityType, EntityId, CurrentStep, Status (Running/Completed/Failed/Compensating), CorrelationId, PayloadJson, CompanyId, CreatedAt, UpdatedAt, CompletedAt.

- **WorkflowStepRecord** (table `WorkflowSteps`): Id, WorkflowInstanceId, StepName, Status, StartedAt, CompletedAt, PayloadJson, CompensationDataJson.

- **IWorkflowOrchestrator**  
  - `StartWorkflowAsync(workflowType, entityType, entityId, initialPayloadJson, companyId, correlationId, ct)` → WorkflowInstanceDto (creates instance and first step "Started").  
  - `AdvanceStepAsync(instanceId, stepName, payloadJson, ct)` (updates CurrentStep, appends WorkflowStepRecord).  
  - `GetInstanceAsync(instanceId, ct)` → WorkflowInstanceDto.

- **WorkflowOrchestratorService**  
  Persists instances and steps; no full saga/compensation logic yet—instance and step tracking only.

---

## Saga / process manager foundation

- **IProcessManager**  
  `Task HandleEventAsync(IDomainEvent domainEvent, CancellationToken ct)`.

- **ProcessManagerBase**  
  Injects `ICommandBus` and `IWorkflowOrchestrator`. Template: load state via abstract `LoadStateAsync`, call abstract `OnEventAsync(state, event)` which returns `IReadOnlyList<object>?` (commands), then send each command via `ICommandBus.SendAsync(object command, ...)`. No saga state persistence in the base; subclasses can use WorkflowInstance or their own store.

---

## Observability

- **Commands:** ExecutionId is set per send and stored in `CommandPipelineContext`; LoggingBehavior logs command type, ExecutionId, CorrelationId, success/failure, IdempotencyReused.
- **Idempotency:** CommandProcessingLogs stores CommandType, CorrelationId, WorkflowInstanceId, Status, ResultJson (truncated), ErrorMessage, CompletedAtUtc—suitable for audits and replay debugging.
- **Workflow orchestrator:** WorkflowInstances and WorkflowSteps tables provide instance and step history; no dedicated metrics in this phase.

---

## Limitations

- No FluentValidation in pipeline (ValidationBehavior is a pass-through).
- EnqueueAsync in CommandOptions is not implemented (sync send only).
- CommandOptions.Timeout is not enforced by the pipeline.
- WorkflowOrchestrator does not implement compensation or full saga state machine; AdvanceStepAsync only records steps.
- Process managers are not auto-registered or invoked by the event bus; they must be wired to specific events by the application.
- Non-generic `SendAsync(object command)` uses reflection and is intended for process manager use only.

---

## Deliverable summary (Phase 9)

### A. Architecture audit findings

- **No MediatR.** Commands were implicit (direct calls to WorkflowEngineService.ExecuteTransitionAsync, etc.).
- **WorkflowJob** = single transition (EntityType, EntityId, CurrentStatus, TargetStatus, State); not multi-step.
- **BackgroundJob** = generic queue (JobType, PayloadJson) processed by BackgroundJobProcessorService; unchanged.
- **Event idempotency:** IEventProcessingLogStore (TryClaimAsync per event+handler) in DomainEventDispatcher.
- **No command-level idempotency** before Phase 9; no formal command bus or pipeline.

### B. What was implemented

- **Command bus:** ICommand/ICommand&lt;TResult&gt;, ICommandHandler&lt;TCommand,TResult&gt;, ICommandBus, CommandResult&lt;T&gt;, CommandOptions.
- **Pipeline:** ValidationBehavior, IdempotencyBehavior, LoggingBehavior, RetryBehavior (Polly); ordered chain before handler.
- **Idempotency:** ICommandProcessingLogStore, CommandProcessingLog entity, CommandProcessingLogs table, TryClaimAsync/MarkCompletedAsync/MarkFailedAsync/TryGetCompletedResultAsync.
- **Sample command:** ExecuteWorkflowTransitionCommand + ExecuteWorkflowTransitionHandler (wraps IWorkflowEngineService.ExecuteTransitionAsync).
- **Workflow orchestrator:** WorkflowInstance, WorkflowStepRecord, IWorkflowOrchestrator, WorkflowOrchestratorService (StartWorkflowAsync, AdvanceStepAsync, GetInstanceAsync, ListInstancesAsync).
- **Saga foundation:** IProcessManager, ProcessManagerBase (LoadStateAsync, OnEventAsync returns commands, sends via ICommandBus).
- **Operator APIs:** ICommandDiagnosticsQueryService, CommandOrchestrationController (command-executions, command-executions/failed, workflow-instances).

### C. Command bus model

- **Send:** `ICommandBus.SendAsync&lt;TCommand, TResult&gt;(command, options, ct)` or `SendAsync(object command, ...)` for process managers.
- **Pipeline order:** Validation → Idempotency → Logging → Retry → Handler. ExecutionId set at start; options in CommandPipelineContext.
- **Result:** CommandResult&lt;T&gt; with Success, Result, ErrorMessage, IdempotencyReused, ExecutionId.

### D. Orchestration model

- **WorkflowInstance:** Id, WorkflowType, EntityType, EntityId, CurrentStep, Status (Running/Completed/Failed/Compensating), CorrelationId, PayloadJson, CompanyId, timestamps.
- **WorkflowStepRecord:** Id, WorkflowInstanceId, StepName, Status, StartedAt, CompletedAt, PayloadJson, CompensationDataJson.
- **Orchestrator:** StartWorkflowAsync (create instance + first step "Started"), AdvanceStepAsync (update CurrentStep, append step), GetInstanceAsync, ListInstancesAsync. No automatic event-driven progression; callers must advance steps.

### E. Saga / process manager model

- **IProcessManager:** HandleEventAsync(IDomainEvent, ct). No return; subclasses use base to load state, OnEventAsync returns commands, base sends them.
- **ProcessManagerBase:** Injects ICommandBus, IWorkflowOrchestrator; template method; no built-in state store—subclasses use WorkflowInstance or custom store. Process managers are not auto-invoked; wire to event handlers explicitly.

### F. Idempotency and retry model

- **Idempotency:** One successful completion per IdempotencyKey (command property or CommandOptions). TryClaimAsync inserts Pending; on completion MarkCompletedAsync (result stored) or MarkFailedAsync. TryGetCompletedResultAsync used for reuse. Failed rows can be overwritten (delete+insert) on retry so same key runs again.
- **Retry:** RetryBehavior uses Polly (exponential backoff) for transient exceptions; after exhaustion, MarkFailedAsync is called.

### G. Observability and diagnostics

- **Logging:** LoggingBehavior logs command type, ExecutionId, CorrelationId, success/failure, IdempotencyReused.
- **CommandProcessingLogs:** CommandType, CorrelationId, WorkflowInstanceId, Status, ResultJson, ErrorMessage, CompletedAtUtc, CreatedAtUtc.
- **APIs:** GET /api/command-orchestration/command-executions (list), command-executions/{id} (detail), command-executions/failed, workflow-instances (list), workflow-instances/{id}. Jobs policy + JobsView permission.

### H. Risks / limitations

- **EnqueueAsync** not implemented; all sends are synchronous.
- **Timeout** not enforced in pipeline.
- **Validation** is pass-through; add FluentValidation in ValidationBehavior if needed.
- **Orchestrator** does not run compensation or auto-advance from events.
- **Process managers** must be registered and invoked by application code.
- **Stale Pending:** If a command claims (Pending) and the process crashes before MarkCompleted/MarkFailed, the key stays Pending; no automatic reaper (consider adding one or manual cleanup).

### I. Files changed

**Application:** Commands (ICommand, ICommandHandler, ICommandBus, CommandResult, CommandOptions, CommandBus, ICommandPipelineBehavior, CommandPipelineContext, Pipeline/*, ICommandProcessingLogStore, CommandProcessingLogStore, ExecuteWorkflowTransitionCommand, ExecuteWorkflowTransitionHandler, DTOs/CommandExecutionDto, ICommandDiagnosticsQueryService, CommandDiagnosticsQueryService). Workflow (IWorkflowOrchestrator + ListInstancesAsync, WorkflowOrchestratorService + ListInstancesAsync, WorkflowInstanceDto, ProcessManager/IProcessManager, ProcessManager/ProcessManagerBase).

**Domain:** Commands/CommandProcessingLog. Workflow/WorkflowInstance, WorkflowStepRecord.

**Infrastructure:** Persistence (ApplicationDbContext DbSets for CommandProcessingLogs, WorkflowInstances, WorkflowStepRecords). Configurations (CommandProcessingLogConfiguration, WorkflowInstanceConfiguration, WorkflowStepRecordConfiguration). Migrations (20260310100000_AddCommandProcessingLogs, 20260310110000_AddWorkflowInstancesAndSteps).

**Api:** Program.cs (DI for CommandBus, CommandProcessingLogStore, pipeline behaviors, ExecuteWorkflowTransitionHandler, WorkflowOrchestrator, CommandDiagnosticsQueryService). Controllers/CommandOrchestrationController.

**Tests:** Commands/CommandBusTests, Workflow/WorkflowOrchestratorTests.

**Docs:** PHASE_9_COMMAND_BUS_AND_ORCHESTRATION.md, COMMAND_BUS_OPERATIONS_RUNBOOK.md, WORKFLOW_ORCHESTRATION_CONTRACT.md, MIGRATION_NOTES_PHASE_9.md.

### J. Exact commands to run

**Apply migrations:**
```bash
cd backend
dotnet ef database update --project src/CephasOps.Infrastructure --startup-project src/CephasOps.Api
```

**Run Phase 9 tests:**
```bash
cd backend/tests/CephasOps.Application.Tests
dotnet test --filter "FullyQualifiedName~CommandBusTests|FullyQualifiedName~WorkflowOrchestratorTests"
```

**Build solution:**
```bash
cd backend
dotnet build
```
