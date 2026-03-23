# Tenant-Aware Background Job Isolation

**Purpose:** Prevent one tenant from overloading job workers and starving others. Provide configuration and observability for job processing at scale.

---

## Model

- **JobExecution** already includes **CompanyId** (tenant scope) and **Priority** (higher = run first).
- No separate TenantId column; CompanyId is the tenant boundary for jobs.

---

## Configuration: JobOrchestration:Worker

| Option | Default | Description |
|--------|---------|-------------|
| **MaxJobsPerTenantPerCycle** | 5 | When tenant fairness is enabled, max jobs claimed per tenant (CompanyId) per poll cycle. |
| **MaxConcurrentJobs** | 0 | Max concurrent jobs per worker (0 = no limit). Reserved for future use. |
| **TenantJobFairnessEnabled** | true | When true, claim logic caps jobs per tenant per cycle (round-robin style). |

Example `appsettings.json`:

```json
{
  "JobOrchestration": {
    "Worker": {
      "BatchSize": 10,
      "PollIntervalMs": 5000,
      "LeaseSeconds": 300,
      "MaxJobsPerTenantPerCycle": 5,
      "TenantJobFairnessEnabled": true
    }
  }
}
```

---

## Tenant fairness behavior

- **ClaimNextPendingBatchAsync(maxCount, nodeId, leaseExpiresAtUtc, maxPerTenant)**  
  When `maxPerTenant` is set, the store uses a single SQL statement that:
  - Ranks pending jobs by `(CompanyId, Priority DESC, CreatedAtUtc)`.
  - Keeps at most `maxPerTenant` jobs per CompanyId (partition).
  - Claims up to `maxCount` jobs total from that set (FOR UPDATE SKIP LOCKED).

So a single poll cycle will not claim more than `MaxJobsPerTenantPerCycle` jobs per tenant, spreading capacity across tenants.

---

## Logging

Job execution logs include:

- **TenantId / CompanyId** – tenant scope.
- **JobType** – job type name.
- **ExecutionTimeMs** – duration from start to completion or failure.

Example:

```
Job execution {JobId} ({JobType}) completed. CompanyId: {CompanyId}, ExecutionTimeMs: {ExecutionTimeMs}
```

---

## Stuck job recovery

- **JobExecutionWorkerHostedService** calls **ResetStuckRunningAsync** at the start of each cycle (lease expiry = 2× LeaseSeconds).
- Running jobs whose lease has expired are reset to Pending so they can be re-claimed; AttemptCount is not incremented.
- See **PLATFORM_RESILIENCE.md** for the dedicated **JobExecutionWatchdogService** and retry limits.

---

## Safety

- All job execution runs under **TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(job.CompanyId, ...)**.
- Subscription/tenant access is checked before execution; denied tenants are marked failed (non-retryable) and not executed.
- No manual tenant scope or platform bypass in the worker; executor and guards are respected.
