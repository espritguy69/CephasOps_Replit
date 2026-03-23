# Migration Notes — Phase 9 (Command Bus and Workflow Orchestration)

## Summary

Phase 9 adds a **command bus**, **command pipeline** (validation, idempotency, logging, retry), **workflow orchestrator** (WorkflowInstance + WorkflowStepRecord), and **process manager foundation**. Existing behaviour (WorkflowEngineService, BackgroundJob, event bus) is unchanged; migration is **additive**.

---

## Database migrations

Apply in order:

1. **20260310100000_AddCommandProcessingLogs**  
   Creates table `CommandProcessingLogs` (Id, IdempotencyKey, CommandType, CorrelationId, WorkflowInstanceId, Status, ResultJson, ErrorMessage, CompletedAtUtc, CreatedAtUtc) and unique index on IdempotencyKey.

2. **20260310110000_AddWorkflowInstancesAndSteps**  
   Creates tables `WorkflowInstances` and `WorkflowSteps` with FKs and indexes.

**Apply:**

```bash
dotnet ef database update --project backend/src/CephasOps.Infrastructure --startup-project backend/src/CephasOps.Api
```

Or run the generated SQL scripts from the migrations if you prefer script-based deployments.

---

## Dependency injection

New registrations in `Program.cs`:

- `ICommandBus` → `CommandBus`
- `ICommandProcessingLogStore` → `CommandProcessingLogStore`
- `ICommandPipelineBehavior<,>` – Validation, Idempotency, Logging, Retry (four registrations)
- `ICommandHandler<ExecuteWorkflowTransitionCommand, WorkflowJobDto>` → `ExecuteWorkflowTransitionHandler`
- `IWorkflowOrchestrator` → `WorkflowOrchestratorService`
- `ICommandDiagnosticsQueryService` → `CommandDiagnosticsQueryService`

No existing registrations are removed or replaced.

---

## Optional: Migrate workflow transition calls to command bus

Current callers of `IWorkflowEngineService.ExecuteTransitionAsync` (e.g. WorkflowController, OrderService, SchedulerService) can remain as-is. To use the command bus instead:

1. Build `ExecuteWorkflowTransitionCommand` from the same DTOs (EntityType, EntityId, TargetStatus, etc.) and set IdempotencyKey if you need idempotency (e.g. from a job or event).
2. Call `await _commandBus.SendAsync(command, options, ct)` and use `result.Result` as the WorkflowJobDto.

Example:

```csharp
var command = new ExecuteWorkflowTransitionCommand
{
    CompanyId = companyId,
    EntityType = dto.EntityType,
    EntityId = dto.EntityId,
    TargetStatus = dto.TargetStatus,
    CorrelationId = dto.CorrelationId,
    IdempotencyKey = idempotencyKey  // optional
};
var result = await _commandBus.SendAsync(command, null, cancellationToken);
if (!result.Success)
    throw new InvalidOperationException(result.ErrorMessage);
var workflowJob = result.Result;
```

No schema changes are required for this; it is a call-site choice.

---

## New API surface

- **Command/orchestration diagnostics:** `GET /api/command-orchestration/command-executions`, `command-executions/{id}`, `command-executions/failed`, `workflow-instances`, `workflow-instances/{id}`.  
  Requires **Jobs** policy and **JobsView** permission (unchanged from event-store style access).

---

## Rollback

- To disable the command bus: do not register or inject ICommandBus; leave existing workflow and job code as-is.
- To remove Phase 9 tables: run down migrations for `AddWorkflowInstancesAndSteps` and `AddCommandProcessingLogs` (order reversed). Any data in those tables will be dropped.

---

## Testing

- **CommandBusTests:** Command dispatch, idempotency reuse.
- **WorkflowOrchestratorTests:** StartWorkflowAsync, AdvanceStepAsync, ListInstancesAsync.

Run:

```bash
cd backend/tests/CephasOps.Application.Tests
dotnet test --filter "FullyQualifiedName~CommandBusTests|FullyQualifiedName~WorkflowOrchestratorTests"
```

---

## Documentation

- **Architecture and model:** `docs/PHASE_9_COMMAND_BUS_AND_ORCHESTRATION.md`
- **Operations:** `docs/COMMAND_BUS_OPERATIONS_RUNBOOK.md`
- **Orchestration contract:** `docs/WORKFLOW_ORCHESTRATION_CONTRACT.md`
