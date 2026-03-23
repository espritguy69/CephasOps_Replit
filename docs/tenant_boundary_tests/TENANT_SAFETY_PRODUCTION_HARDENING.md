# Tenant Safety Production Hardening (Phase 6)

This document describes what was hardened for production so tenant-safety issues are **visible, diagnosable, and operationally controlled** in live environments. It does not change business behaviour or weaken tenant isolation.

## What was hardened

| Area | Implementation |
|------|----------------|
| **Guard violation logging** | `PlatformGuardLogger` (category `PlatformGuardViolation`) logs GuardName, Operation, Message, CompanyId, EntityType, EntityId where applicable. Violations are also written to `IGuardViolationBuffer` (bounded in-memory buffer) for operational overview. |
| **API exception handling** | Tenant guard failures (`InvalidOperationException` with message containing "TenantSafetyGuard") are detected in `GlobalExceptionHandler`. Response: **403 Forbidden** with safe message "Invalid request context."; request path and correlation ID are logged; internal details are not returned to the client. |
| **Metrics** | Meter `CephasOps.TenantSafety`: counters `cephasops.tenant_safety.guard_violations` (tags: guard, operation), `cephasops.tenant_safety.missing_tenant_context`, `cephasops.tenant_safety.platform_bypass_entered`. Exported when OpenTelemetry metrics are enabled (Prometheus scrape at `/metrics`). |
| **Startup validation** | `TenantSafetyStartupValidator` runs at startup (when not Testing): ensures `ITenantProvider` and `ApplicationDbContext` are registered; throws if missing so the app fails fast instead of at first request. |
| **CompanyId in SaveChanges logs** | When a SaveChanges tenant-integrity violation occurs, the current tenant ID (CompanyId) is included in the violation log for diagnosis. |

## Observability points

- **Logs**
  - **Category:** `PlatformGuardViolation` — filter by this for guard violations.
  - **Fields:** GuardName, Operation, Message, CompanyId, EntityType, EntityId, OrderId, EventId (no sensitive payloads).
  - **On API request:** When a tenant guard exception is caught, a warning is logged with RequestPath, CorrelationId, TraceId; full detail remains in the PlatformGuardViolation log.
- **Metrics** (when `OpenTelemetry:Metrics:Enabled` is true)
  - `cephasops.tenant_safety.guard_violations` — increment per guard violation (tags: guard, operation).
  - `cephasops.tenant_safety.missing_tenant_context` — increment when tenant context was required but missing.
  - `cephasops.tenant_safety.platform_bypass_entered` — increment each time `EnterPlatformBypass` is called.
- **In-memory buffer**  
  `IGuardViolationBuffer.GetRecent(maxCount)` returns the most recent violations (newest first). Used by operations overview and internal tooling; no public endpoint exposes this by default. Do not expose raw violation lists to unauthenticated or tenant-facing APIs.

## Startup and runtime safeguards

- **Startup:** `TenantSafetyStartupValidator.Validate` runs after the guard logger is initialized (non-Testing only). Missing `ITenantProvider` or `ApplicationDbContext` registration causes startup to fail.
- **Runtime:** Tenant guard violations throw as before; they are now logged with structure, counted in metrics, and (in API) returned as 403 with a safe message.

## Production deployment checklist (tenant safety)

Use this as a supplement to your general deployment and launch checklists.

- [ ] **Migrations:** All EF Core migrations applied; schema matches code (e.g. run idempotent migration script or `dotnet ef database update` where appropriate).
- [ ] **Branch protection:** Default branch (e.g. `main`) requires the **Tenant Safety** CI workflow to pass before merge.
- [ ] **Tenant safety workflow:** `.github/workflows/tenant-safety.yml` is required on PRs; no bypass of Tenant Boundary Tests or Tenant Safety Invariants.
- [ ] **Logging:** Structured logging (e.g. Serilog) and log aggregation are enabled; category `PlatformGuardViolation` is included in production log level (e.g. Warning or higher).
- [ ] **Monitoring:** Metrics endpoint (`/metrics`) is enabled and scraped (or equivalent); alerts can be built from `CephasOps.TenantSafety` counters.
- [ ] **Alert routing:** Operations team knows how to receive and triage tenant-safety alerts (see Alerting recommendations below).
- [ ] **Platform bypass:** Code paths that use `EnterPlatformBypass`/`ExitPlatformBypass` are documented and reviewed; usage is expected only for retention, seeding, design-time, or other platform-wide operations.
- [ ] **SuperAdmin usage:** SuperAdmin and cross-tenant capabilities are reviewed and limited to intended roles and flows.
- [ ] **Verification commands (emergency):** Team knows how to run tenant-safety test suites locally:
  - Application: `dotnet test backend/tests/CephasOps.Application.Tests/CephasOps.Application.Tests.csproj --filter "FullyQualifiedName~TenantSafetyInvariantTests|FullyQualifiedName~SaveChangesTenantIntegrity" -c Release`
  - Api: `dotnet test backend/tests/CephasOps.Api.Tests/CephasOps.Api.Tests.csproj --filter "FullyQualifiedName~TenantBoundaryTests" -c Release`
- [ ] **Rollback:** Rollback plan does not disable tenant guards or revert to versions without SaveChanges validation or executor usage.

## Alerting recommendations

These are **recommendations** only; implement according to your existing alerting and on-call practices.

- **Repeated tenant guard violations:** Alert when `cephasops.tenant_safety.guard_violations` (or equivalent) exceeds a threshold in a short window (e.g. > N in 5 minutes). Indicates possible misconfiguration or bug in tenant context.
- **Spike in missing tenant context:** Alert on a sharp increase in `cephasops.tenant_safety.missing_tenant_context` (e.g. rate or delta). May indicate middleware/executor regression or new path without scope.
- **Unexpected platform bypass usage:** Alert if `cephasops.tenant_safety.platform_bypass_entered` rate is above baseline (e.g. only retention jobs and seed run at known times). Helps detect unintended bypass usage.
- **Repeated cross-tenant denials:** If you expose access-denial metrics at the API layer (e.g. 403s for tenant-boundary), consider alerting on spikes that could indicate probing or misconfiguration.
- **Failed tenant-safety CI on default branch:** Ensure branch protection requires the Tenant Safety workflow; notify when the default branch’s tenant-safety check fails (e.g. via GitHub Actions or your CI notification channel).

## What is not covered

- **No public health/diagnostics endpoint for tenant safety:** The app does not expose a dedicated public endpoint with violation counts or last-failure timestamp. Use `IGuardViolationBuffer` and logs/metrics for internal or admin-only tooling if needed.
- **No automatic alerting in-repo:** Alerting rules (e.g. Prometheus, Application Insights) are not defined in this repository; they are recommendations only.
- **Buffer is in-memory:** Restart clears the violation buffer; for long-term history rely on logs and metrics.
- **Controller-level 403 (RequireCompanyId):** Controllers that return 403 when company context is missing are unchanged; they are not routed through the global exception handler for tenant guard exceptions that occur later (e.g. in SaveChanges).

## References

- **Guards and architecture:** `TENANT_SAFETY_GUARDS.md`, `backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md`
- **CI enforcement:** `TENANT_SAFETY_CI_ENFORCEMENT.md`
- **Runbook:** `TENANT_SAFETY_RUNBOOK.md`
- **Summary:** `AUTOMATIC_TENANT_BOUNDARY_TEST_SUMMARY.md`
