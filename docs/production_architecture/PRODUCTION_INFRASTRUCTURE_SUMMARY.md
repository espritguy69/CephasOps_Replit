# Production Infrastructure Summary

**Purpose:** Final summary of the Production Infrastructure Architecture phase—topology, workers, cache, observability, config, database ops, CI/CD, runbooks, staged rollout, and next actions.

---

## 1. Recommended deployment topology

- **API nodes:** Stateless; scale horizontally behind reverse proxy/ingress. Run only HTTP request handling when ProductionRoles:RunJobWorkers (and other worker flags) are false.
- **Job worker nodes:** Scale horizontally; claim jobs from PostgreSQL with tenant fairness (MaxJobsPerTenantPerCycle). Run when ProductionRoles:RunJobWorkers=true.
- **Singleton workers:** One instance per environment for Guardian, storage lifecycle, metrics aggregation, watchdog, schedulers, event dispatcher, notification workers, integration workers, email cleanup. Achieved by running one “worker” replica with all ProductionRoles worker flags true, or by leader election.
- **PostgreSQL:** Primary data store; optional read replicas for reporting. Backup and PITR recommended.
- **Redis:** Optional; for distributed rate-limit state and optional platform/dashboard cache when multiple API replicas are used. See CACHE_STRATEGY.md.
- **Logs/metrics/tracing:** Centralized aggregation and export (OpenTelemetry, Prometheus, Grafana). See OBSERVABILITY_STACK.md.
- **File storage:** Current: local path (uploads/); for scale consider S3-compatible object storage with tenant path isolation.

Details: [DEPLOYMENT_ARCHITECTURE.md](DEPLOYMENT_ARCHITECTURE.md).

---

## 2. Worker separation model

- **ProductionRoles** config (ProductionRoles:RunJobWorkers, RunGuardian, RunStorageLifecycle, RunMetricsAggregation, RunWatchdog, RunSchedulers, RunEventDispatcher, RunNotificationWorkers, RunIntegrationWorkers, RunEmailCleanup) enables role-based deployment: API-only nodes set worker flags false; worker-only nodes set RunJobWorkers (and others) true and can run as single replica for singletons.
- **Scaling:** Job workers scale out; Guardian, storage lifecycle, metrics, watchdog, schedulers, event dispatcher run as singleton (one replica or leader-elected).
- **Implemented:** ProductionRolesOptions and conditional registration of hosted services in Program.cs; defaults all true (all-in-one). Override in production per node type.

Details: [WORKER_SCALING.md](WORKER_SCALING.md).

---

## 3. Caching recommendation

- **Current:** In-memory rate limit (per API node); IMemoryCache for settings. No Redis.
- **Recommendation:** Redis optional for production when (1) multiple API replicas and consistent per-tenant rate limiting are required, (2) short-lived platform health/dashboard cache is desired. Use tenant-scoped cache keys for any tenant data; never cache tenant truth outside PostgreSQL.
- **Abstraction:** Documented in CACHE_STRATEGY.md; no forced Redis implementation. Rate limit middleware can be extended to use IDistributedCache when configured.

Details: [CACHE_STRATEGY.md](CACHE_STRATEGY.md).

---

## 4. Observability recommendation

- **Logging:** Structured (JSON) to stdout; CompanyId/TenantId in LogContext; central aggregation (e.g. Loki, Elasticsearch).
- **Metrics:** OpenTelemetry or Prometheus; per-tenant labels where cardinality is acceptable; platform health, job queue, Guardian, rate limit, anomalies. Export to Prometheus or OTLP.
- **Tracing:** Optional; OpenTelemetry with sampling; tenant id as span attribute.
- **Dashboards:** Platform health, tenant health, job queue, infrastructure (API, DB, workers). Grafana with tenant_id filter.
- **Alerts:** API availability, latency, job queue backlog, job failures, Guardian critical drift/anomaly, DB health, rate limit abuse.

Details: [OBSERVABILITY_STACK.md](OBSERVABILITY_STACK.md).

---

## 5. Configuration architecture

- **Environments:** Local, Dev, Staging, Production. Production requires ConnectionStrings:DefaultConnection and Jwt:SecretKey (length ≥ 16); validated at startup via ProductionStartupValidator when ASPNETCORE_ENVIRONMENT=Production.
- **Secrets:** Connection strings, JWT secret, Redis (if used), billing keys—from env or vault; never committed.
- **ProductionRoles:** Set per node (API vs worker). Safe defaults: all true; override for API-only or worker-only.
- **Config drift:** Use Platform Guardian GET /api/platform/analytics/drift; act on Critical.

Details: [ENVIRONMENT_CONFIGURATION.md](ENVIRONMENT_CONFIGURATION.md).

---

## 6. Database operations guidance

- **Migrations:** Apply before deploying app that depends on new schema; use idempotent script or dotnet ef database update in pipeline. No migration at API startup in production.
- **Backup:** Daily full; PITR if available (WAL or managed service). Pre-migration backup.
- **Restore:** Test periodically; document restore and PITR procedure.
- **Retention:** TenantMetricsDaily 13+ months; TenantAnomalyEvents 90 days; JobExecutions 90 days (tune); EventStore per policy; see DATABASE_OPERATIONS.md.
- **Health:** Connection and replication lag; long-running query and lock alerts.

Details: [DATABASE_OPERATIONS.md](DATABASE_OPERATIONS.md).

---

## 7. CI/CD and release model

- **Stages:** Build → tests → migration check → deploy staging → deploy production → post-deploy verify.
- **Gates:** Unit/integration tests pass; tenant-safety CI pass; migrations applied before app deploy; health check pass.
- **Worker draining:** API drain (readiness off); job workers stop claiming, allow in-flight to complete or lease expiry.
- **Rollback:** Deploy previous app version; avoid DB migration rollback (prefer fix-forward). Document rollback criteria.

Details: [CI_CD_PIPELINE.md](CI_CD_PIPELINE.md).

---

## 8. Incident runbook coverage

Runbooks for: API latency spike, job queue backlog, stuck jobs, tenant anomaly spike, rate-limit abuse, storage quota incident, failed signup/provisioning, failed migration, degraded dashboard/analytics, file upload failures, impersonation misuse review, database pressure incident. Each includes symptoms, checks, mitigations, follow-up, escalation.

Details: [PRODUCTION_RUNBOOKS.md](PRODUCTION_RUNBOOKS.md).

---

## 9. Staged rollout plan

- **Phases:** Internal only → Pilot tenants → Limited production (cap or %) → Full rollout.
- **Per phase:** Success criteria, Guardian and analytics checks, rollback triggers, communication.
- **Monitoring:** Platform health, tenant health, anomalies, job queue, SLO. Alerts before limited production.

Details: [STAGED_ROLLOUT_PLAN.md](STAGED_ROLLOUT_PLAN.md).

---

## 10. Critical infrastructure blockers

- **None** identified that block going to production with the current design. Optional improvements: Redis for multi-replica rate limit; object storage for file scale; read replica for analytics load.

---

## 11. Safe implementation changes made

- **ProductionRolesOptions** (ProductionRoles config section) and **conditional registration** of hosted services so API-only and worker-only nodes can be deployed. Defaults preserve all-in-one behavior.
- **ProductionStartupValidator:** When ASPNETCORE_ENVIRONMENT=Production, validates ConnectionStrings:DefaultConnection and Jwt:SecretKey (min length 16); throws at startup if missing. Does not run in non-Production (local/dev/staging remain unchanged).

---

## 12. Next recommended operational actions

1. **Configure ProductionRoles** per node type in staging/production (e.g. API-only for API replicas, full workers for worker replica).
2. **Enable production startup validation** by setting ASPNETCORE_ENVIRONMENT=Production and supplying required secrets.
3. **Apply migrations** via pipeline or dedicated step before app deploy; verify with check-migration-state script.
4. **Set up observability** (log aggregation, Prometheus/OpenTelemetry, dashboards, alerts) per OBSERVABILITY_STACK.md.
5. **Run staged rollout** per STAGED_ROLLOUT_PLAN.md; use Guardian and platform-health at each phase.
6. **Document** runbook ownership and escalation; test restore and rollback periodically.

---

## 13. Cross-references

- [docs/saas_scaling/SAAS_SCALE_READINESS_REPORT.md](../saas_scaling/SAAS_SCALE_READINESS_REPORT.md) – Scale & reliability.
- [docs/platform_guardian/PLATFORM_GUARDIAN_SUMMARY.md](../platform_guardian/PLATFORM_GUARDIAN_SUMMARY.md) – Guardian layer.
- [docs/saas_operations/OPERATIONAL_RUNBOOKS.md](../saas_operations/OPERATIONAL_RUNBOOKS.md) – Support runbooks.
- All production architecture docs in [docs/production_architecture/](.).
