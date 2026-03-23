# CephasOps Production Deployment Architecture

**Purpose:** Recommended production topology for CephasOps—runtime roles, traffic flow, failure boundaries, and scaling model.

---

## 1. Runtime roles (minimum set)

| Role | Responsibility | Recommended form |
|------|----------------|------------------|
| **API nodes** | HTTP request handling, auth, tenant guard, rate limit, controller logic | Separate containers/processes; scale horizontally |
| **Background job worker nodes** | JobExecutionWorkerHostedService + BackgroundJobProcessorService; claim and execute tenant-scoped jobs | Separate process or container; scale horizontally with shared queue (DB) |
| **Scheduler / orchestration** | JobPollingCoordinatorService, EmailIngestionSchedulerService, StockSnapshotSchedulerService, LedgerReconciliationSchedulerService, PnlRebuildSchedulerService, SlaEvaluationSchedulerService, MissingPayoutSnapshotSchedulerService, PayoutAnomalyAlertSchedulerService | Singleton or leader-elected; one active instance per scheduler type |
| **Guardian / diagnostics worker** | PlatformGuardianHostedService (anomaly, drift, performance) | Singleton or one instance per environment; low frequency |
| **Storage lifecycle worker** | StorageLifecycleService (tier transitions) | Singleton or one instance; daily run |
| **Metrics aggregation** | TenantMetricsAggregationHostedService (daily/monthly rollups) | Singleton; typically one instance, runs daily |
| **Watchdog / reset services** | JobExecutionWatchdogService (stuck job reset) | Singleton or leader-elected; one active |
| **Event dispatcher** | EventStoreDispatcherHostedService, EventBusMetricsCollectorHostedService | Can scale with care (claim per stream/partition) or singleton; see WORKER_SCALING |
| **Other workers** | NotificationDispatchWorkerHostedService, NotificationRetentionHostedService, OutboundIntegrationRetryWorkerHostedService, EventPlatformRetentionWorkerHostedService, WorkerHeartbeatHostedService, EmailCleanupService | Classified in WORKER_SCALING.md |
| **PostgreSQL** | Primary data store; tenant data, jobs, metrics, Guardian events | Single primary; read replicas optional for reporting |
| **Redis (optional)** | Distributed rate-limit state, short-lived dashboard cache, coordination | Optional; see CACHE_STRATEGY.md |
| **Log aggregation / metrics / tracing** | Centralized logs, Prometheus/OpenTelemetry, tracing | External stack; see OBSERVABILITY_STACK.md |
| **Reverse proxy / ingress** | TLS termination, routing, optional rate limiting at edge | Nginx, Traefik, or cloud LB |
| **Object / file storage** | File uploads (current: local disk under uploads/); optional S3-compatible for scale | Local path or S3-compatible bucket; tenant path isolation |

---

## 2. Recommended topology (logical)

```
                    [Reverse proxy / Ingress]
                                    |
                    +---------------+---------------+
                    |               |               |
              [API-1]         [API-2]         [API-N]   (horizontal scale)
                    |               |               |
                    +------+--------+--------+-------+
                           |        |        |
                    [PostgreSQL]  [Redis*]  [Logs/Metrics]
                           |
                    +------+--------+--------+-------+
                    |      |        |        |       |
              [Job Workers] [Scheduler*] [Guardian*] [Storage Lifecycle*] [Metrics Agg*]
                    |      [Watchdog*]  [Event Dispatcher*]  [Other workers*]
                    |
              * = singleton or leader-elected where noted
```

- **API:** Stateless; multiple replicas behind load balancer. Each has in-memory rate-limit state unless Redis is used (then shared).
- **Job workers:** Multiple instances can run; they claim jobs from PostgreSQL (FOR UPDATE SKIP LOCKED) with tenant fairness. No message bus required; DB is the queue.
- **Schedulers / Guardian / Storage lifecycle / Metrics / Watchdog:** Run as **singleton** (one replica) or with **leader election** so only one instance runs each loop (avoids duplicate schedule fires or double lifecycle runs).
- **PostgreSQL:** Single primary. Read replicas optional for read-heavy analytics if needed.
- **Redis:** Optional; when used, for rate-limit buckets, optional dashboard cache, and (if needed) leader election.

---

## 3. Traffic flow

1. **User/tenant traffic:** Ingress → API nodes → TenantGuardMiddleware → TenantRateLimitMiddleware → controllers → DbContext (tenant-scoped) → PostgreSQL.
2. **Platform admin:** Same API nodes; SuperAdmin-only routes (e.g. /api/platform/*) with permission checks.
3. **Background jobs:** Schedulers (or external triggers) enqueue rows in JobExecutions/BackgroundJobs → job workers poll DB, claim under tenant scope, execute, update status.
4. **Guardian:** Single instance runs on timer; reads TenantMetricsDaily, JobExecutions, TenantAnomalyEvents; writes anomalies; no user traffic.
5. **File storage:** API serves upload/download; files under tenant/company path; optional future: stream to/from object storage.

---

## 4. Failure boundaries

| Boundary | Failure impact | Mitigation |
|----------|----------------|------------|
| Single API node | Loss of that node’s in-flight requests; LB routes to others | Multiple API replicas; health checks. |
| All API nodes | Full API outage | Multiple nodes; avoid single point; alert on replica count. |
| Job workers | Queue backlog grows; jobs delayed | Scale workers; monitor pending count; watchdog resets stuck. |
| Scheduler/Guardian singleton | Missed schedule or Guardian run | Leader election + restart; or accept one-miss. |
| PostgreSQL | Full app outage | HA: primary + standby; backups; PITR. |
| Redis | Rate limit resets per node if in-memory fallback; or use in-memory only | Optional Redis; document fallback. |
| File storage | Upload/download failures | Replicate or use durable object storage. |

---

## 5. Horizontal scaling

- **API nodes:** Scale out freely; stateless.
- **Job worker nodes:** Scale out; DB-based claiming with tenant fairness prevents one tenant from starving others.
- **Read replicas (PostgreSQL):** For reporting/analytics read-only queries if needed; not for tenant write path.

---

## 6. Singleton / leader-elected

- **One active instance per type:** JobExecutionWatchdogService, StorageLifecycleService, PlatformGuardianHostedService, TenantMetricsAggregationHostedService.
- **Schedulers:** One active instance per scheduler (or single process that runs all schedulers) so each schedule fires once per interval.
- **Implementation:** Run these in a single “worker” process/container, or use leader election (e.g. DB lock or Redis) so only the leader runs the loop. See WORKER_SCALING.md for role-based enablement.

---

## 7. Grouped vs separate processes

- **Option A (simple):** Two process types: (1) **Web** = API only; (2) **Worker** = all hosted services (job worker, watchdog, Guardian, storage lifecycle, schedulers, metrics aggregation). Worker can be single replica.
- **Option B (separate):** **API** | **Job workers** (scale out) | **Singleton workers** (one container: schedulers, Guardian, storage lifecycle, metrics, watchdog). Clearer separation; more deployment units.
- **Option C (minimal):** Single process (API + all workers) for small deployments; scale API by multiple replicas and run workers on one replica only (e.g. via role-based enablement so only one pod runs worker code).

Recommendation: **Option B** for production at scale; **Option A** for moderate scale with one worker pool.

---

## 8. References

- [WORKER_SCALING.md](WORKER_SCALING.md) – Per-role scaling and role-based enablement.
- [CACHE_STRATEGY.md](CACHE_STRATEGY.md) – Redis and cache usage.
- [OBSERVABILITY_STACK.md](OBSERVABILITY_STACK.md) – Logs, metrics, tracing.
