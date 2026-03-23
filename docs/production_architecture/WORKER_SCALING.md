# Worker Role Separation and Scaling Strategy

**Purpose:** Classify hosted services and background processing into scaling groups; define scaling, concurrency, and role-based enablement.

---

## 1. Scaling groups

| Group | Hosted services | Scale model | Notes |
|-------|------------------|-------------|--------|
| **Web/API** | None (API is request-handling only) | N/A | Scale API replicas horizontally. |
| **Job execution** | JobExecutionWorkerHostedService, BackgroundJobProcessorService | **Horizontal** | Multiple workers claim from DB (FOR UPDATE SKIP LOCKED); tenant fairness caps jobs per tenant per cycle. |
| **Guardian** | PlatformGuardianHostedService | **Singleton** | One instance; runs anomaly, drift, performance on interval. |
| **Storage lifecycle** | StorageLifecycleService | **Singleton** | One instance; daily tier transitions. |
| **Metrics aggregation** | TenantMetricsAggregationHostedService | **Singleton** | One instance; daily rollup. |
| **Watchdog** | JobExecutionWatchdogService | **Singleton** | One instance; resets stuck Running jobs. |
| **Schedulers** | JobPollingCoordinatorService, EmailIngestionSchedulerService, StockSnapshotSchedulerService, LedgerReconciliationSchedulerService, PnlRebuildSchedulerService, SlaEvaluationSchedulerService, MissingPayoutSnapshotSchedulerService, PayoutAnomalyAlertSchedulerService | **Singleton** (or one leader) | Only one active should fire schedules; avoid duplicate job enqueue. |
| **Event dispatcher** | EventStoreDispatcherHostedService, EventBusMetricsCollectorHostedService | **Singleton or scaled with partition** | Event store claim is per-batch; multiple dispatchers can increase throughput if claiming is partition-safe; default single. |
| **Notifications** | NotificationDispatchWorkerHostedService, NotificationRetentionHostedService | **Singleton** (or one per type) | Dispatch/retention typically one instance. |
| **Integration** | OutboundIntegrationRetryWorkerHostedService, EventPlatformRetentionWorkerHostedService | **Singleton** | One instance each. |
| **Worker heartbeat** | WorkerHeartbeatHostedService | **Per process** | Each process that runs workers can heartbeat; no conflict. |
| **Email cleanup** | EmailCleanupService | **Singleton** | One instance. |

---

## 2. Per-role guidance

### 2.1 Job execution (JobExecutionWorkerHostedService + BackgroundJobProcessorService)

- **Scale:** Horizontal. Add more replicas of the process that runs these.
- **Concurrency:** JobExecutionWorkerOptions.BatchSize, MaxConcurrentJobs, MaxJobsPerTenantPerCycle control claim size and per-tenant fairness.
- **Resource isolation:** Run in dedicated worker pool; avoid long-running job bursts blocking API.
- **Anti-starvation:** TenantJobFairnessEnabled and MaxJobsPerTenantPerCycle ensure no single tenant monopolizes the queue.
- **Operational:** Monitor pending job count and job failure rate; scale workers if backlog grows.

### 2.2 Guardian (PlatformGuardianHostedService)

- **Scale:** Singleton. One instance per environment.
- **Concurrency:** Single loop; RunIntervalMinutes (min 5) avoids DB overload.
- **Resource isolation:** Can run in same process as other singletons or in a dedicated “guardian” pod.
- **Operational:** Disable via PlatformGuardian:Enabled=false if needed; check drift/anomaly endpoints after deploy.

### 2.3 Storage lifecycle (StorageLifecycleService)

- **Scale:** Singleton. One instance.
- **Concurrency:** Processes tenants in sequence; MaxFilesPerTenantPerRun caps work per tenant per run.
- **Operational:** Runs on interval (e.g. 24h); ensure only one replica runs it (role-based enablement).

### 2.4 Metrics aggregation (TenantMetricsAggregationHostedService)

- **Scale:** Singleton. One instance.
- **Operational:** Daily aggregation; one run per day sufficient.

### 2.5 Watchdog (JobExecutionWatchdogService)

- **Scale:** Singleton. One instance.
- **Operational:** Resets stuck jobs; duplicate runners could double-reset (harmless but redundant); prefer one.

### 2.6 Schedulers (all *SchedulerService)

- **Scale:** Singleton or leader-elected. Only one instance should fire each schedule (e.g. daily PnL, ledger reconciliation).
- **Operational:** If multiple replicas run, use leader election (DB lock or Redis) so only leader runs scheduler loops; or run all schedulers in a single “scheduler” pod.

### 2.7 Event dispatcher (EventStoreDispatcherHostedService)

- **Scale:** Singleton by default; or scale with partition-aware claiming if implemented.
- **Operational:** Single instance avoids duplicate event handling; if scaling, partition by stream/aggregate.

### 2.8 Signup / provisioning

- **Flow:** Synchronous API (POST /api/platform/signup or provision); may enqueue follow-up work (e.g. welcome email). No separate worker role; job workers pick up enqueued jobs.
- **Operational:** Ensure job workers are running so post-signup jobs are processed.

---

## 3. Role-based service enablement

To support **API-only** vs **worker** vs **all-in-one** deployments, use configuration to enable/disable hosted service groups.

Recommended config structure (see ENVIRONMENT_CONFIGURATION.md):

```json
{
  "ProductionRoles": {
    "RunApi": true,
    "RunJobWorkers": true,
    "RunSchedulers": true,
    "RunGuardian": true,
    "RunStorageLifecycle": true,
    "RunMetricsAggregation": true,
    "RunWatchdog": true,
    "RunEventDispatcher": true,
    "RunNotificationWorkers": true,
    "RunIntegrationWorkers": true,
    "RunEmailCleanup": true
  }
}
```

- **API-only node:** RunApi=true, all others false. Use for pure HTTP replicas.
- **Worker node (singletons):** RunApi=false, RunJobWorkers=true, RunSchedulers=true, RunGuardian=true, RunStorageLifecycle=true, RunMetricsAggregation=true, RunWatchdog=true, RunEventDispatcher=true, RunNotificationWorkers=true, RunIntegrationWorkers=true, RunEmailCleanup=true. Single replica.
- **Worker node (job only):** RunApi=false, RunJobWorkers=true, others false. Scale this role horizontally.
- **All-in-one (small env):** All true; single replica; API + workers in one process.

Implementation: add a wrapper or conditional registration that checks these flags before adding each HostedService (or a single “HostedServiceOrchestrator” that starts only the enabled ones). See optional implementation in Stage 2 below.

---

## 4. Concurrency and resource isolation summary

| Role | Concurrency | Isolation |
|------|-------------|-----------|
| API | One request per thread/async context | Stateless; no long-running work. |
| Job workers | BatchSize × replicas; MaxConcurrentJobs per replica | Run in worker process/pod; separate from API. |
| Guardian | One loop per instance | Low CPU; DB reads/writes. |
| Storage lifecycle | Sequential per tenant | One process; can share with Guardian. |
| Schedulers | One fire per schedule per interval | Single process; avoid multiple scheduler replicas. |
| Watchdog | One loop | Single process. |
| Event dispatcher | One or more consumers (if partitioned) | Single process by default. |

---

## 5. Operational notes

- **Deployment:** When deploying, drain API nodes (stop accepting new requests) before stopping; for workers, allow in-flight jobs to complete or timeout (lease expiry) then stop.
- **Migration:** Run migrations before starting new worker replicas so schema is ready.
- **Health checks:** API exposes /health/ready; workers can expose a simple liveness endpoint that checks process is running; readiness can check DB connectivity.
- **Leader election:** If running multiple replicas that include schedulers/Guardian/watchdog, implement leader election (e.g. DB table “leader_lock” with expiry) so only leader runs singleton services.
