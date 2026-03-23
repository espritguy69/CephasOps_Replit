# Platform Resilience

**Purpose:** Improve failure handling for job execution: retry limits, tenant isolation, and recovery from stuck jobs.

---

## Job retry limits

- **JobExecution.MaxAttempts** (default 5) caps retries per job. After MaxAttempts failures, the job is marked **DeadLetter** and not retried.
- Backoff: delay before next run increases with attempt count (e.g. 60s, 300s, 900s, 3600s). See **JobExecutionStore.MarkFailedAsync**.

---

## Tenant isolation

- Each job is executed under **TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(job.CompanyId, ...)**. Failures in one tenant’s job do not affect other tenants.
- Subscription/tenant access is checked before execution; denied tenants are marked failed (non-retryable) and not executed.
- No shared mutable state between tenant jobs during execution.

---

## Stuck job detection and recovery

- **Running** jobs that are never completed (e.g. process crash, timeout) hold a lease. When the lease expires, they should be re-claimed.
- **JobExecutionWorkerHostedService** calls **IJobExecutionStore.ResetStuckRunningAsync** at the start of each poll cycle (lease expiry = 2× LeaseSeconds). Running jobs whose lease has expired (or that have no lease and started long ago) are reset to **Pending** so any worker can claim them again. AttemptCount is not incremented.
- **JobExecutionWatchdogService** runs on a fixed interval (default every 10 minutes) and also calls **ResetStuckRunningAsync**. This provides a safety net if the worker is busy or delayed. Same lease-expiry logic; no double-reset issue.

---

## Safe retry

- Reset to Pending does not increment **AttemptCount**. Only **MarkFailedAsync** does. So re-claiming a stuck job does not consume retry budget.
- Retries are scheduled via **NextRunAtUtc** with exponential backoff. Workers only claim jobs where Status = Pending and (NextRunAtUtc is null or ≤ now).

---

## Configuration

- **JobOrchestration:Worker:LeaseSeconds** (default 300): claim lease duration. Stuck detection uses 2× this value in the worker and a 10-minute window in the watchdog.
- **JobOrchestration:Worker:MaxAttempts** is per job (set at enqueue); default 5.

---

## Summary

| Mechanism | Purpose |
|-----------|---------|
| MaxAttempts | Limit retries per job; move to DeadLetter when exceeded. |
| Tenant scope per job | Isolate failures to one tenant. |
| ResetStuckRunningAsync (worker + watchdog) | Release stuck Running jobs so they can be re-claimed. |
| No AttemptCount on reset | Safe retry without consuming retry budget. |
