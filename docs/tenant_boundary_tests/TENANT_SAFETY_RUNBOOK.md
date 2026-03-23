# Tenant Safety Runbook

Internal ops note: what to check and how to triage when tenant guard violations or tenant-safety concerns appear in production.

## If tenant guard violations appear in production

### 1. Confirm the signal

- **Logs:** Search for category `PlatformGuardViolation` or message containing `TenantSafetyGuard`. Each entry includes GuardName, Operation, Message, and optionally CompanyId, EntityType.
- **Metrics:** Check counters under meter `CephasOps.TenantSafety`: `guard_violations`, `missing_tenant_context`, `platform_bypass_entered`. A sudden or sustained rise indicates real events.
- **API responses:** If clients report 403 with "Invalid request context.", the global exception handler has turned a tenant guard exception into a safe response; the underlying violation is in logs.

### 2. Triage

| Observation | Likely cause | Next step |
|-------------|--------------|-----------|
| Violations in **SaveChanges** / **SaveChangesTenantIntegrity** | Code path is saving tenant-scoped entities without tenant context, or with wrong CompanyId. | Correlate with request path/correlation ID; check recent deploys and which endpoints or jobs run in that path. |
| Violations in **AssertTenantContext** | Code path uses high-risk pattern (e.g. IgnoreQueryFilters) without tenant context or platform bypass. | Same as above; check for new or changed code that uses IgnoreQueryFilters or similar. |
| Spike in **missing_tenant_context** | Middleware or executor not setting tenant scope for some requests or jobs. | Check middleware order, job configuration (CompanyId propagation), and recent changes to API or background jobs. |
| Spike in **platform_bypass_entered** | More code paths entering platform bypass (retention, seed, jobs). | Confirm whether new jobs or scheduled tasks were added; ensure they are intended to run platform-wide. |
| 403 "Invalid request context" on specific endpoint | That endpoint (or something it calls) triggered a tenant guard. | Use correlation ID from response to find the corresponding PlatformGuardViolation log and stack trace. |

### 3. Logs and metrics to inspect

- **Structured log fields:** `GuardName`, `Operation`, `Message`, `CompanyId`, `EntityType`, `RequestPath` (in the handler warning log), `CorrelationId`, `TraceId`.
- **Metrics:** Use your observability UI to graph or alert on:
  - `cephasops.tenant_safety.guard_violations` (by guard and operation if available).
  - `cephasops.tenant_safety.missing_tenant_context`.
  - `cephasops.tenant_safety.platform_bypass_entered`.
- **In-memory buffer (if available):** If your deployment exposes an internal/admin endpoint that reads `IGuardViolationBuffer.GetRecent(N)`, use it for a quick view of recent violations. Do not expose this publicly.

### 4. Safe first-response actions

- **Do not disable guards or revert to a version without tenant validation** unless directed by a formal incident decision.
- **Preserve evidence:** Retain logs and metrics for the time window of the violation; note correlation IDs and request paths.
- **Correlate with deploy:** Check whether the violation rate started or changed after a deployment or config change.
- **Narrow the path:** Use stack traces (in logs) and request path to identify the controller, action, or background job.
- **Escalate to dev/platform:** Share correlation ID, timestamp, GuardName, Operation, EntityType, and (if safe) CompanyId so the team can locate the code path and fix or document the intended behaviour.

### 5. After the incident

- Add or adjust tests (e.g. Tenant Boundary Tests or Tenant Safety Invariants) if the root cause was a missing or wrong tenant scope.
- If a new legitimate platform-bypass path is added, document it and ensure it uses `TenantScopeExecutor.RunWithPlatformBypassAsync` (or Enter/Exit with proper finally).
- Update this runbook if you discover new recurring patterns or useful queries.

## References

- **Production hardening:** `TENANT_SAFETY_PRODUCTION_HARDENING.md`
- **Guards and architecture:** `TENANT_SAFETY_GUARDS.md`, `backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md`
- **CI and tests:** `TENANT_SAFETY_CI_ENFORCEMENT.md`, `AUTOMATIC_TENANT_BOUNDARY_TEST_SUMMARY.md`
