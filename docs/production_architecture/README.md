# Production Architecture

**Purpose:** Deployment, runtime, observability, and rollout architecture for CephasOps in production. Application and tenant-safety design are unchanged; this folder covers how to run and operate the system.

---

## Documents

| Document | Description |
|----------|-------------|
| [DEPLOYMENT_ARCHITECTURE.md](DEPLOYMENT_ARCHITECTURE.md) | Recommended topology: API, workers, PostgreSQL, Redis (optional), logs/metrics; traffic flow; failure boundaries; scaling. |
| [WORKER_SCALING.md](WORKER_SCALING.md) | Worker role classification; scaling (horizontal vs singleton); role-based enablement via **ProductionRoles** config. |
| [CACHE_STRATEGY.md](CACHE_STRATEGY.md) | Distributed cache and coordination; rate limit state; tenant-safe keys; Redis optional; no tenant truth in cache. |
| [OBSERVABILITY_STACK.md](OBSERVABILITY_STACK.md) | Logging, metrics, tracing; tenant labeling; alerts; dashboards; retention. |
| [ENVIRONMENT_CONFIGURATION.md](ENVIRONMENT_CONFIGURATION.md) | Environment groups (local, dev, staging, production); config areas; secrets; validation; **ProductionStartupValidator**. |
| [DATABASE_OPERATIONS.md](DATABASE_OPERATIONS.md) | Migrations; backup; restore; PITR; retention; recovery checklist. |
| [CI_CD_PIPELINE.md](CI_CD_PIPELINE.md) | Build, test, migration, deploy order; worker draining; rollback; health gates. |
| [PRODUCTION_RUNBOOKS.md](PRODUCTION_RUNBOOKS.md) | Incident runbooks: API latency, job backlog, stuck jobs, anomalies, rate limit, storage, signup, migration, dashboard, file upload, impersonation, DB pressure. |
| [STAGED_ROLLOUT_PLAN.md](STAGED_ROLLOUT_PLAN.md) | Internal → pilot → limited production → full rollout; success and rollback criteria; Guardian monitoring. |
| [PRODUCTION_INFRASTRUCTURE_SUMMARY.md](PRODUCTION_INFRASTRUCTURE_SUMMARY.md) | Summary of topology, workers, cache, observability, config, DB ops, CI/CD, runbooks, rollout; safe changes made; next actions. |

---

## Implemented in code

- **ProductionRolesOptions** (section `ProductionRoles`): RunJobWorkers, RunGuardian, RunStorageLifecycle, RunMetricsAggregation, RunWatchdog, RunSchedulers, RunEventDispatcher, RunNotificationWorkers, RunIntegrationWorkers, RunEmailCleanup. Used in Program.cs to conditionally register hosted services. Defaults: all true (all-in-one).
- **ProductionStartupValidator**: When `ASPNETCORE_ENVIRONMENT=Production`, validates ConnectionStrings:DefaultConnection and Jwt:SecretKey (min 16 chars). Throws at startup if missing. Does not run in non-Production.

---

## Cross-references

- [SaaS Scale Readiness](../saas_scaling/SAAS_SCALE_READINESS_REPORT.md)
- [Platform Guardian](../platform_guardian/README.md)
- [SaaS Operations Runbooks](../saas_operations/OPERATIONAL_RUNBOOKS.md)
