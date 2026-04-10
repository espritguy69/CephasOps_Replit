# Command Bus — Operations Runbook (Phase 9)

## Overview

The command bus provides deterministic command execution with idempotency, retry, and observability. Use this runbook for day-to-day operations and troubleshooting.

---

## Command lifecycle

1. **Send** – Caller invokes `ICommandBus.SendAsync(command, options, ct)`.
2. **Pipeline** – Validation → Idempotency (claim or reuse) → Logging → Retry → Handler.
3. **Result** – `CommandResult<T>` with Success, Result, ErrorMessage, IdempotencyReused, ExecutionId.

If **IdempotencyKey** is set and a completed execution exists for that key, the pipeline returns the cached result without running the handler (`IdempotencyReused = true`).

---

## Operator workflows

### Inspect command execution history

- **API:** `GET /api/command-orchestration/command-executions`
- **Query:** `status`, `commandType`, `correlationId`, `workflowInstanceId`, `skip`, `take`.
- **Use:** See recent command runs, filter by status (Pending/Completed/Failed) or correlation.

### Get a single command execution

- **API:** `GET /api/command-orchestration/command-executions/{id}`
- **Use:** Full detail including ResultJson and ErrorMessage for debugging.

### List failed commands

- **API:** `GET /api/command-orchestration/command-executions/failed?take=100`
- **Use:** Identify failed commands for retry or root-cause analysis. Failed rows remain in `CommandProcessingLogs` with Status = Failed; retrying the same command (same IdempotencyKey) will delete the failed row and re-claim so the handler runs again.

### List workflow instances

- **API:** `GET /api/command-orchestration/workflow-instances`
- **Query:** `workflowType`, `entityType`, `status`, `companyId`, `skip`, `take`.
- **Use:** See running/completed/failed orchestration instances.

### Get workflow instance

- **API:** `GET /api/command-orchestration/workflow-instances/{id}`
- **Use:** Current step, status, payload; step history is in `WorkflowSteps` (query DB or future API).

---

## Idempotency rules

- **Unique key:** One logical command per `IdempotencyKey`. Key is from command property or `CommandOptions.IdempotencyKey`.
- **Reuse:** If a **Completed** execution exists for the key, the bus returns the stored result and does not run the handler.
- **Retry:** If the execution is **Failed**, the next send with the same key deletes the failed row and runs the handler again (new ExecutionId).
- **Pending:** If an execution is still **Pending** (e.g. crashed before MarkCompleted/MarkFailed), a second send with the same key will not claim (returns false in IdempotencyBehavior). Caller gets no cached result; consider adding a “stale Pending” reaper or manual reset if needed.

---

## Retry behavior

- **RetryBehavior** uses Polly (exponential backoff) for transient failures (e.g. timeout, DB deadlock).
- Configurable in pipeline; after max retries the command fails and IdempotencyBehavior calls `MarkFailedAsync`.
- Non-transient exceptions (e.g. validation) are not retried.

---

## Logging and observability

- **LoggingBehavior** logs command type, ExecutionId, CorrelationId, success/failure, IdempotencyReused at start and end.
- **CommandProcessingLogs** table stores: Id, IdempotencyKey, CommandType, CorrelationId, WorkflowInstanceId, Status, ResultJson, ErrorMessage, CompletedAtUtc, CreatedAtUtc.
- No dedicated metrics in Phase 9; extend with counters/timers (e.g. command_duration_seconds, command_total by type and status) if needed.

---

## Troubleshooting

| Symptom | Check | Action |
|--------|--------|--------|
| Command “never runs” | IdempotencyKey reuse | Inspect `command-executions` for same key with Status=Completed; if correct, reuse is intended. |
| Command failed | Get execution by id | `GET command-executions/{id}` for ErrorMessage and context; fix input or handler and retry with same IdempotencyKey. |
| Duplicate work | Handler not idempotent | Ensure handler is safe to run twice (e.g. upsert by business key); use IdempotencyKey for at-most-once delivery. |
| Stuck Pending | CommandProcessingLogs Status=Pending | No automatic reaper; optionally implement “stale Pending” cleanup or manual delete for that key. |

---

## Limitations (Phase 9)

- **EnqueueAsync** (async command queue) is not implemented; all sends are synchronous.
- **CommandOptions.Timeout** is not enforced by the pipeline.
- **Validation** is pass-through (no FluentValidation); add a validator in ValidationBehavior if required.
- **Workflow orchestrator** does not auto-advance steps from events; process managers must be wired to event handlers and call orchestrator/command bus explicitly.

---

## Security and scoping

- Command/orchestration APIs require **Jobs** policy and **JobsView** permission.
- List workflow instances: non–super-admin users are scoped by company; super-admin can pass `companyId` or see all.
