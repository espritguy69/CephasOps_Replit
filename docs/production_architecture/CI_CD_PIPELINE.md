# Release Pipeline and Deployment Safety

**Purpose:** CI/CD stages, test gates, migration and deployment order, worker behavior during deploy, rollback.

---

## 1. CI/CD stages (recommended)

| Stage | Purpose | Gates |
|-------|---------|--------|
| **Build** | Compile; restore packages | Build success; no critical warnings (optional). |
| **Unit / integration tests** | Application tests | All pass. |
| **Migration check** | Pending migrations list | Fail if pending migrations and release includes migration; or generate script for approval. |
| **Deploy (staging)** | Deploy to staging | Smoke tests pass. |
| **Deploy (production)** | Deploy to production | Manual or automated gate; health check pass. |
| **Post-deploy** | Verify | Platform health, tenant health, job queue. |

---

## 2. Required test suites before deployment

- **Backend:** `dotnet test` for CephasOps.Application.Tests and CephasOps.Api.Tests (or equivalent). Must pass.
- **Tenant boundary tests:** Run the automatic tenant boundary suite so cross-tenant read/write/list regressions are caught: `dotnet test --filter "FullyQualifiedName~TenantBoundaryTests"` in CephasOps.Api.Tests. See [docs/tenant_boundary_tests/AUTOMATIC_TENANT_BOUNDARY_TEST_SUMMARY.md](../tenant_boundary_tests/AUTOMATIC_TENANT_BOUNDARY_TEST_SUMMARY.md). Failures are **release-blocking** for tenant-sensitive changes. When adding a new tenant-facing endpoint, add a boundary test per the coverage map.
- **Tenant safety:** Run tenant-safety CI (e.g. architecture-guardrails, tenant-safety workflows) so that guardrails and allowlists are enforced. Fail on new unallowlisted bypass or manual scope.
- **E2E (optional):** Playwright or API E2E against staging; required for major releases if available.
- **Migration:** Generate idempotent script; review; apply in release pipeline before or during deploy.

---

## 3. Migration ordering

1. **Before** new app version that depends on new schema: apply migrations (e.g. `dotnet ef database update` or run idempotent SQL script).
2. **Then** deploy new application code. Application must not assume a migration has run if the same version can run against an older DB (prefer backward-compatible migrations).
3. **Rollback:** If rolling back app, ensure the previous app version is compatible with the current DB schema (no drop column in the same release as app rollback).

---

## 4. Deployment ordering

1. **Database:** Apply migrations (separate step or first step of deploy).
2. **Workers (singleton):** Deploy or restart worker pool (one replica for Guardian, storage lifecycle, metrics, watchdog, schedulers). Allow in-flight jobs to complete or wait for lease expiry.
3. **API:** Deploy API replicas; use rolling update or blue/green. Drain old replicas (stop accepting new requests) before terminating.
4. **Job workers (scaled):** Deploy job worker replicas; same as API for graceful shutdown.

---

## 5. Worker draining / shutdown safety

- **API:** Use readiness probe that fails when draining; load balancer stops sending traffic. Wait for in-flight requests to complete (or timeout) then exit.
- **Job workers:** On SIGTERM, stop claiming new jobs; allow current batch to complete within lease time. If lease expires, another worker can claim; no duplicate execution if job is single-run.
- **Schedulers / Guardian / Storage lifecycle:** Finish current run if short; otherwise cancel and exit. Next replica (or restart) will pick up on next interval.
- **Event dispatcher:** Finish current batch; stop claiming new events; exit. Remaining events will be claimed after restart.

---

## 6. Job processing during deploy

- **Pending jobs:** Remain in queue; no data loss. New workers (after deploy) will claim them.
- **Running jobs:** Either let them complete (preferred) or let lease expire so watchdog or another worker resets to Pending. Avoid killing mid-transaction without cleanup.
- **Idempotency:** Job executors should be idempotent where possible (e.g. by job id or idempotency key) so retry after reset is safe.

---

## 7. Storage lifecycle and Guardian during deploy

- **Storage lifecycle:** Single instance; if restarted, next run is on next interval. No duplicate runs if only one replica.
- **Guardian:** Same; one instance; restart is safe; next cycle runs on interval.
- **Metrics aggregation:** Typically daily; missing one run can be caught next day or run manually if critical.

---

## 8. Environment promotion flow

- **Dev → Staging:** Deploy from main or release branch; run full tests; apply migrations to staging DB; deploy app; smoke test.
- **Staging → Production:** Approval gate; apply migrations to production DB; deploy app (rolling); verify health and Guardian; monitor for anomalies.

---

## 9. Emergency rollback guidance

- **Application rollback:** Deploy previous app version (same DB schema). Use blue/green or rolling back to previous image. Verify health.
- **Database rollback:** Avoid rolling back migrations that drop data (prefer additive migrations). If must roll back a migration, restore from backup or run down migration and then deploy previous app; high risk—prefer fix-forward.
- **Rollback criteria:** Trigger rollback if error rate spikes, health check fails consistently, or critical Guardian/tenant health degradation. Document who can approve rollback.

---

## 10. Health check gates

- **Readiness:** GET /health/ready returns 200 (database and event bus healthy). Use for load balancer and orchestration.
- **Liveness:** Process running; optional GET /health/live. Use for restart on hang.
- **Post-deploy:** Call GET /api/platform/analytics/platform-health (SuperAdmin); confirm no critical anomaly spike; confirm pending job count is not abnormally high.

---

## 11. Post-deploy verification checklist

- [ ] Health endpoint returns 200.
- [ ] Platform health shows no critical drift or critical anomaly spike.
- [ ] Tenant health list loads; no unexpected Critical count.
- [ ] Job queue: pending count stable or decreasing (no backlog explosion).
- [ ] Logs: no repeated exceptions; tenant context present in logs.
- [ ] One smoke request per critical path (e.g. login, one tenant list).

---

## 12. References

- [DEPLOYMENT_ARCHITECTURE.md](DEPLOYMENT_ARCHITECTURE.md) – Topology and roles.
- [DATABASE_OPERATIONS.md](DATABASE_OPERATIONS.md) – Migration and backup.
- [PRODUCTION_RUNBOOKS.md](PRODUCTION_RUNBOOKS.md) – Incident response.
