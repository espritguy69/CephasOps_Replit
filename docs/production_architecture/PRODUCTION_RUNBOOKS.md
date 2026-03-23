# Production Runbooks and Incident Operations

**Purpose:** Runbooks for common production incidents—symptoms, checks, mitigations, escalation.

---

## 1. API latency spike

**Symptoms:** High p95/p99 latency; slow dashboard or list endpoints; timeouts.

**Likely causes:** DB contention, N+1 queries, heavy report query, rate limit or lock contention.

**Checks:**
- Query slow query log or APM for top slow endpoints and queries.
- Check GET /api/platform/analytics/performance-health (pending jobs, degraded tenants).
- Check DB connections and CPU.

**Immediate mitigations:** Scale API replicas if CPU-bound; scale DB or add read replica for read-heavy load; optimize or short-circuit heaviest endpoints; consider caching (see CACHE_STRATEGY.md).

**Follow-up:** Add indexes or tune queries; add request timeouts or circuit breaker for external calls.

**Escalate:** If DB is saturated or unknown root cause; involve DBA or platform lead.

---

## 2. Job queue backlog

**Symptoms:** Pending job count high and growing; GET /api/platform/analytics/performance-health shows high PendingJobCount; delayed processing.

**Likely causes:** Job worker capacity insufficient; one or more job types failing and retrying; DB lock contention; scheduler enqueueing faster than workers consume.

**Checks:**
- List pending/running jobs by type; identify failing job types (GET job execution list or logs).
- Check JobExecutionWatchdogService and JobExecutionWorkerHostedService logs; lease and claim rate.
- Check ProductionRoles:RunJobWorkers on worker nodes.

**Immediate mitigations:** Scale job worker replicas (if RunJobWorkers); fix or disable failing job type; increase BatchSize or MaxConcurrentJobs with care (watch DB load); run watchdog to reset stuck Running jobs.

**Follow-up:** Tune batch size and tenant fairness; add alert on pending count threshold; retention policy for old completed jobs.

**Escalate:** If backlog is critical and scaling does not help; check for poison messages or schema issue.

---

## 3. Stuck jobs

**Symptoms:** Jobs in Running state for longer than lease; no progress; duplicate work if multiple workers.

**Likely causes:** Worker crash or restart without completing; long-running job exceeding lease; deadlock.

**Checks:**
- Query JobExecutions where Status = Running and ProcessingLeaseExpiresAtUtc < now.
- Check JobExecutionWatchdogService logs (reset count).
- Check worker process health and restart events.

**Immediate mitigations:** Ensure JobExecutionWatchdogService is running (RunWatchdog=true); it will reset stuck jobs to Pending. Manually retry specific job via POST /api/platform/support/tenants/{tenantId}/jobs/{jobExecutionId}/retry if needed.

**Follow-up:** Tune LeaseSeconds if jobs legitimately need longer; ensure only one watchdog instance.

**Escalate:** If reset loop (jobs stuck repeatedly); investigate executor or DB lock.

---

## 4. Tenant anomaly spike

**Symptoms:** GET /api/platform/analytics/anomalies shows many Critical or Warning; GET /api/platform/analytics/platform-health shows TenantsInCriticalAnomalyState > 0.

**Likely causes:** API spike, storage growth, or job failure spike for one or more tenants; misconfiguration; abuse.

**Checks:**
- List anomalies by tenant and kind (ApiSpike, StorageSpike, JobFailureSpike).
- Check tenant health (GET /api/platform/analytics/tenant-health) for affected tenants.
- Review Guardian detection thresholds (PlatformGuardian:AnomalyDetection).

**Immediate mitigations:** Contact tenant if abuse or misconfiguration; temporarily increase rate limit or investigate failing jobs for that tenant; no automatic tenant suspension unless policy.

**Follow-up:** Tune anomaly thresholds; add alert on critical anomaly count; document response per tenant.

**Escalate:** If critical anomaly indicates security incident or platform-wide issue.

---

## 5. Rate-limit abuse

**Symptoms:** High 429 responses; TenantRateLimitExceeded in logs for one or few tenants; tenant reports “rate limit exceeded”.

**Likely causes:** Legitimate high load; misconfigured client (e.g. retry storm); abuse.

**Checks:**
- Logs: filter by TenantRateLimitExceeded and CompanyId.
- Check tenant’s plan and limits (SaaS:TenantRateLimit:Plans).
- Check API request volume per tenant (TenantMetricsDaily or request logs).

**Immediate mitigations:** If abuse: consider temporary block or throttle at edge; if legitimate: increase plan limit or RequestsPerMinute for that tenant (config or plan override). Communicate with tenant.

**Follow-up:** Implement ITenantRateLimitResolver with plan-based limits; consider Redis for consistent limit across API replicas.

**Escalate:** If DDoS or security team needed.

---

## 6. Storage quota incident

**Symptoms:** Tenant cannot upload; 403 “Storage quota exceeded”; storage usage at or over limit.

**Likely causes:** Tenant over quota; quota not updated after plan change; usage recording lag.

**Checks:**
- GET /api/platform/usage/tenants/{tenantId} or diagnostics; compare StorageBytes to TenantSubscription.StorageLimitBytes.
- Verify TenantUsageRecord and TenantMetricsDaily for that tenant.

**Immediate mitigations:** Increase StorageLimitBytes for tenant (PATCH subscription) or ask tenant to delete files; fix usage recording if undercounted.

**Follow-up:** Align quota enforcement and usage recording; alert when tenant approaches quota.

**Escalate:** If platform-wide storage or quota bug.

---

## 7. Failed signup / provisioning

**Symptoms:** POST /api/platform/signup or provision returns 4xx/5xx; tenant cannot create account.

**Likely causes:** Validation failure (duplicate code/email); DB or dependency failure; config (trial plan missing).

**Checks:**
- Response body and logs for error (409, 500).
- Verify default trial BillingPlan exists (slug trial); verify CompanyProvisioningService and TenantSubscription creation.
- Check DB connectivity and constraints.

**Immediate mitigations:** Fix config (e.g. ensure trial plan); retry after fixing duplicate or transient error; check subscription and tenant creation in DB.

**Follow-up:** Add health check for “provisioning ready” (plan exists, DB writable); document required config.

**Escalate:** If data corruption or repeated failure.

---

## 8. Failed migration

**Symptoms:** Migration fails during deploy; app fails to start with schema exception; pending model changes.

**Likely causes:** Migration not applied; migration conflict; incompatible migration order; permissions.

**Checks:**
- Run `dotnet ef migrations list`; compare applied vs pending.
- Check migration error message (constraint, column, permission).
- Verify DB user has DDL rights if applying via app or pipeline.

**Immediate mitigations:** Do not deploy app that requires new schema until migration is fixed and applied. Fix migration (additive preferred); apply in separate step; then deploy app. If already applied and broken, consider restore from backup and fix migration.

**Follow-up:** Use idempotent migration script; run migration in CI and apply in release pipeline before app deploy.

**Escalate:** If production DB is in inconsistent state; DBA and restore may be needed.

---

## 9. Degraded dashboard / analytics

**Symptoms:** GET /api/platform/analytics/dashboard or tenant-health slow or failing; Guardian endpoints timeout.

**Likely causes:** Heavy aggregation; DB load; missing or stale TenantMetricsDaily; timeout.

**Checks:**
- Check TenantMetricsAggregationHostedService ran (RunMetricsAggregation); check TenantMetricsDaily has recent rows.
- Check DB CPU and slow queries for aggregation queries.
- Check platform-health and performance-health endpoints.

**Immediate mitigations:** Increase timeout for analytics endpoints; ensure metrics aggregation runs (singleton); add short TTL cache for dashboard (see CACHE_STRATEGY.md).

**Follow-up:** Optimize aggregation queries; consider read replica for analytics; retention policy for TenantMetricsDaily.

**Escalate:** If DB cannot support aggregation load; consider async rollup or materialized views.

---

## 10. File upload failures

**Symptoms:** 403 or 500 on file upload; “Storage quota exceeded” or disk full; tenant reports upload broken.

**Likely causes:** Quota exceeded; disk full on server; path permission; storage lifecycle or file service error.

**Checks:**
- Check storage quota (tenant usage vs limit).
- Check disk space on node serving uploads; check path (uploads/files/...) permissions.
- Logs for FileService or storage exceptions.

**Immediate mitigations:** Free disk space or scale storage; fix permissions; increase quota for tenant if appropriate.

**Follow-up:** Monitor disk and quota; consider object storage (S3) for scale.

**Escalate:** If data loss or corruption.

---

## 11. Impersonation misuse review

**Symptoms:** Suspicion of impersonation abuse; audit request; compliance review.

**Likely causes:** Unauthorized use of SuperAdmin impersonation; token leakage.

**Checks:**
- Audit log for impersonation events (who, when, which tenant).
- Review access to SuperAdmin accounts and token handling.
- GET /api/platform/analytics/anomalies filter by ImpersonationEvent if implemented.

**Immediate mitigations:** Revoke tokens if leakage; disable compromised SuperAdmin account; rotate secrets.

**Follow-up:** Enforce MFA for SuperAdmin; time-limited impersonation tokens; regular audit review.

**Escalate:** Security and compliance team.

---

## 12. Database pressure incident

**Symptoms:** High DB CPU or connections; slow queries; connection pool exhaustion; health check failing.

**Likely causes:** Heavy query load; long-running transactions; connection leak; missing index; lock contention.

**Checks:**
- DB metrics: connections, CPU, slow query log, lock waits.
- Application: connection pool settings; queries without tenant filter (see QUERY_SAFETY_REPORT).
- Job workers: batch size and concurrency.

**Immediate mitigations:** Scale DB or add read replica; kill long-running queries if safe; scale down job workers temporarily to reduce load; add index if clear hotspot.

**Follow-up:** Tune queries and indexes; set connection pool limits; add alerts on connection count and CPU.

**Escalate:** DBA; consider read replica or sharding if persistent.

---

## References

- [DEPLOYMENT_ARCHITECTURE.md](DEPLOYMENT_ARCHITECTURE.md)
- [OBSERVABILITY_STACK.md](OBSERVABILITY_STACK.md)
- [docs/saas_operations/OPERATIONAL_RUNBOOKS.md](../saas_operations/OPERATIONAL_RUNBOOKS.md)
- [docs/platform_guardian/README.md](../platform_guardian/README.md)
