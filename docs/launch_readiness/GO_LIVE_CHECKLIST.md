# Final Go-Live Checklist

Use this checklist before cutting over to production or onboarding the first real tenants. Complete each section; sign off only when all items are verified or explicitly deferred with a ticket/date.

---

## Infrastructure ready

- [ ] **Hosting** (e.g. Kubernetes, Azure App Service, AWS) is provisioned and network/DNS configured.
- [ ] **PostgreSQL** is deployed, backups and retention configured, connection string set and tested.
- [ ] **Redis** (if used) is deployed and `ConnectionStrings:Redis` set; connectivity verified.
- [ ] **Secrets** (JWT, DB password, Redis password, etc.) are in a secure store (e.g. Key Vault, K8s secrets) and not in source control.
- [ ] **Database backups** are verified (restore test, RPO/RTO documented).

## Schema and migrations (operator-owned)

- [ ] **Schema applied** before first run: idempotent migration script (or bundle) per [AGENTS.md](../../AGENTS.md) and [backend/docs/operations/EF_MIGRATION_SCHEMA_GUARD.md](../../backend/docs/operations/EF_MIGRATION_SCHEMA_GUARD.md); any supplemental SQL (e.g. OperationalInsights, TenantFeatureFlags) applied as documented.
- [ ] **Schema verified** after apply: `backend/scripts/check-migration-state.sql` run; all expected tables/columns present. (StartupSchemaGuard at runtime checks only four critical tables; full schema readiness is operator-verified.)

---

## Tests and tenant safety

- [ ] **Tenant boundary test suite** has been run and passed before release: `cd backend/tests/CephasOps.Api.Tests && dotnet test --filter "FullyQualifiedName~TenantBoundaryTests"`. See [docs/tenant_boundary_tests/AUTOMATIC_TENANT_BOUNDARY_TEST_SUMMARY.md](../tenant_boundary_tests/AUTOMATIC_TENANT_BOUNDARY_TEST_SUMMARY.md). Failures are **release-blocking** for tenant-sensitive changes.
- [ ] **403 vs 404:** Tenant boundary tests expect cross-tenant by-id to return **404** (or 403); list endpoints must return only same-tenant data. When adding a new tenant-facing endpoint, add a boundary test per [BOUNDARY_TEST_COVERAGE_MAP.md](../tenant_boundary_tests/BOUNDARY_TEST_COVERAGE_MAP.md).

## Application and config

- [ ] **ASPNETCORE_ENVIRONMENT=Production** on production nodes.
- [ ] **ConnectionStrings:DefaultConnection** and **Jwt:SecretKey** (or **Jwt:Key**) set and meet Production validation (see [ENVIRONMENT_VALIDATION.md](ENVIRONMENT_VALIDATION.md)).
- [ ] **SYNCFUSION_LICENSE_KEY** set from secure config in Production (do not rely on in-code fallback); see [ENVIRONMENT_VALIDATION.md](ENVIRONMENT_VALIDATION.md#8-syncfusion-license-production).
- [ ] **ProductionRoles** configured per node type (API-only vs worker; Guardian on at least one node if desired).
- [ ] **Rate limit** and **Guardian** config present and validated at startup (no validation errors in logs).

---

## Guardian running

- [ ] **PlatformGuardian:Enabled** and **ProductionRoles:RunGuardian** are true on the intended node(s).
- [ ] Guardian health check reports **Healthy** on `/health/platform` for that node (or Degraded is intentional and documented).
- [ ] First Guardian run has completed without errors (check logs or platform health).

---

## Monitoring active

- [ ] **Monitoring** (e.g. Prometheus, Grafana, or cloud APM) is deployed and scraping/app receiving data.
- [ ] **GET /health** and **GET /health/ready** are reachable and return expected status.
- [ ] **GET /health/platform** is used for operations (and optionally dashboarded).
- [ ] **GET /metrics** (Prometheus) is exposed and scraped when OpenTelemetry metrics are enabled.

---

## Alerting active

- [ ] **Critical alerts** are configured (database, Guardian, job backlog, critical anomalies, Redis if used) per [ALERTING_RULES.md](ALERTING_RULES.md).
- [ ] **Warning alerts** are configured (latency, storage, rate-limit, worker restarts) where desired.
- [ ] **Notification channel** (e.g. PagerDuty, Slack, email) is tested.

---

## Workers and scaling

- [ ] **Workers** (job workers, schedulers, Guardian, etc.) are running on the intended nodes (role flags and process count).
- [ ] **Scaling** plan is documented (when to add API replicas vs worker replicas).
- [ ] **Job backlog** and **event bus** health checks are Healthy or Degraded with a known reason.

---

## Rollout plan approved

- [ ] **Rollout plan** (phased tenants, feature flags, rollback steps) is documented and approved.
- [ ] **Rollback** procedure is documented: [infra/scripts/rollback.ps1](../../infra/scripts/rollback.ps1) reverts deployment only; **database restore** is operator-owned (see [ROLLBACK_AND_DB_RESTORE.md](ROLLBACK_AND_DB_RESTORE.md)). Restore procedure, RPO/RTO, and a restore test are required before go-live.
- [ ] **Tenant onboarding** playbook ([TENANT_ONBOARDING_PLAYBOOK.md](TENANT_ONBOARDING_PLAYBOOK.md)) is agreed and first-tenant steps are assigned.

---

## Sign-off

| Role | Name | Date |
|------|------|------|
| Tech lead / DevOps | | |
| Product / Launch owner | | |

**Notes / deferred items:**

---

## See also

- [ENVIRONMENT_VALIDATION.md](ENVIRONMENT_VALIDATION.md)
- [HEALTH_CHECKS.md](HEALTH_CHECKS.md)
- [ROLLBACK_AND_DB_RESTORE.md](ROLLBACK_AND_DB_RESTORE.md)
- [CEPHASOPS_LAUNCH_READINESS_REPORT.md](CEPHASOPS_LAUNCH_READINESS_REPORT.md)
- [FINAL_GO_LIVE_AUDIT.md](FINAL_GO_LIVE_AUDIT.md) — Launch conditions resolution and verdict.
