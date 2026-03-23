# Platform Safety — Operator Response Guidance

**Purpose:** What to do when tenant-safety, finance, or EventStore safeguards fail or when CI reports drift or stale artifacts. Keep responses consistent and auditable.

---

## 1. Missing tenant context (TenantSafetyGuard / RequireTenantOrBypass)

**What you see:**  
`InvalidOperationException` or log with category **PlatformGuardViolation**: operation requires a valid tenant context (TenantScope.CurrentTenantId) or an approved platform bypass.

**Meaning:**  
Code tried to perform a tenant-scoped write or a guard-protected operation without `TenantScope.CurrentTenantId` set and without `TenantSafetyGuard.IsPlatformBypassActive`.

**What to do:**

1. **Identify the call path** — Use the operation name and stack trace. Common causes: new background job not using TenantScopeExecutor; API path where middleware did not set tenant; event handler running without scope.
2. **Fix the caller** — Ensure the operation runs inside `TenantScopeExecutor.RunWithTenantScopeAsync(companyId, ...)` or, for platform-wide work, `RunWithPlatformBypassAsync(...)`. Do not set `TenantScope.CurrentTenantId` or call `EnterPlatformBypass` manually in runtime code except in allowlisted paths.
3. **If this is bootstrap/design-time** — Add the file to `tools/tenant_safety_ci_allowlist.json` under `manual_scope_allowed` with a short reason (e.g. "Design-time EF factory"). Prefer refactoring to allowlist growth.

**CI equivalent:** MANUAL_SCOPE or MANUAL_BYPASS_* in `tenant_scope_ci.ps1`. Fix as above; see [TENANT_SAFETY_CI.md](TENANT_SAFETY_CI.md).

---

## 2. Financial isolation guard exceptions

**What you see:**  
`InvalidOperationException` from **FinancialIsolationGuard**: e.g. "CompanyId is required", "Company mismatch", "RequireSameCompanySet".

**Meaning:**  
A finance-sensitive path (invoice, payroll, payout snapshot, P&L rebuild, rate resolution, payout anomaly) was called with missing or mismatched company context.

**What to do:**

1. **Do not ignore** — These guard failures prevent cross-company financial data from being written or returned.
2. **Check the operation name** in the message — Identifies which service/method failed (e.g. CreateInvoiceAsync, CreateSnapshotForOrderIfEligibleAsync).
3. **Fix the caller** — Ensure the caller passes a valid CompanyId and that it matches the entity’s company (order, invoice, snapshot). Finance paths must run under tenant scope or an approved platform bypass with explicit company.
4. **Logs** — Filter by category **PlatformGuardViolation** and guard name **FinancialIsolationGuard** for recurrence. No persisted count; use operations overview in-memory buffer or logs.

**Reference:** [FINANCIAL_ISOLATION_GUARD_REPORT.md](FINANCIAL_ISOLATION_GUARD_REPORT.md).

---

## 3. EventStore consistency guard exceptions

**What you see:**  
`InvalidOperationException` from **EventStoreConsistencyGuard**: e.g. "EventStore append requires a valid tenant context or an approved platform bypass", "Parent event must belong to the same company", "Company mismatch in event stream", "EventId is required".

**Meaning:**  
An EventStore append was attempted without tenant or bypass; or metadata was invalid; or parent/root/stream company was inconsistent.

**What to do:**

1. **Append without tenant or bypass** — Ensure appends run after tenant resolution (e.g. API middleware) or inside `TenantScopeExecutor.RunWithTenantScopeAsync` / `RunWithPlatformBypassAsync`. Fix the caller (e.g. job worker, event dispatcher) so scope is set before append.
2. **Parent/root company mismatch** — Do not set ParentEventId/RootEventId to events from another company. If data is wrong, fix the source; do not relax the guard.
3. **Stream consistency (same entity, different company)** — The same entity stream (EntityType + EntityId) must keep the same CompanyId. Fix the caller so it does not append a second event with a different company for the same entity.
4. **Metadata (EventId, EventType, CompanyId when entity context)** — Ensure the domain event has non-empty EventId and EventType and, when EntityType/EntityId are set, CompanyId.

**Reference:** [EVENTSTORE_CONSISTENCY_GUARD_REPORT.md](EVENTSTORE_CONSISTENCY_GUARD_REPORT.md).

---

## 4. Artifact drift (score drop or stale dashboard/JSON)

**What you see (CI):**  
- "Architecture drift: tenant safety score dropped from X to Y"  
- "Tenant safety health dashboard, JSON, or history is out of date"

**Meaning:**  
- **Score drop:** The tenant safety score (from `tenant_safety_health.json`) is lower than the previous run (e.g. new unallowlisted manual scope, executor gap, allowlist increase, or doc/guard drift).  
- **Stale artifacts:** `TENANT_SAFETY_HEALTH_DASHBOARD.md`, `tenant_safety_health.json`, or `tenant_safety_history.json` were not regenerated and committed after code or config changes.

**What to do:**

1. **Score drop — fix causes**  
   - Run `./tools/tenant_scope_ci.ps1` locally. Fix unallowlisted manual scope (use TenantScopeExecutor or add justified allowlist entry).  
   - Fix executor adoption (ensure BackgroundService/IHostedService uses TenantScopeExecutor or is in `executor_not_required` with reason).  
   - If a guard or TenantScopeExecutor was changed, update at least one of the architecture docs in the same PR.  
   - Prefer fixing over using the drift override.

2. **Score drop — intentional override (rare)**  
   - In the same PR, update [TENANT_SAFETY_DRIFT_LOG.md](TENANT_SAFETY_DRIFT_LOG.md) with an entry: date, score before → after, reason/PR. CI allows the drop only if that file is modified in the PR.

3. **Stale artifacts**  
   - From repo root run **one command** to refresh all platform-safety artifacts: `./tools/architecture/regenerate_tenant_safety_artifacts.ps1`  
   - This updates: diagram, health dashboard, `tenant_safety_health.json`, `tenant_safety_history.json`, Platform Guardian report (`platform_guardian_report.json`, [PLATFORM_GUARDIAN_REPORT.md](PLATFORM_GUARDIAN_REPORT.md)).  
   - If in CI, ensure your branch includes the updated files (or run the command above and commit the changes).

**Reference:** [TENANT_SAFETY_CI.md](TENANT_SAFETY_CI.md), [TENANT_SAFETY_DRIFT_LOG.md](TENANT_SAFETY_DRIFT_LOG.md), [PLATFORM_SAFETY_HARDENING_INDEX.md](PLATFORM_SAFETY_HARDENING_INDEX.md).

---

## 5. Platform bypass misuse or unexpected bypass usage

**What you see:**  
- CI: unallowlisted `EnterPlatformBypass` or `ExitPlatformBypass` (MANUAL_BYPASS_*).  
- Runtime: code path using bypass in a place that is not allowlisted or not via TenantScopeExecutor.

**Meaning:**  
Platform bypass is only allowed in specific, documented cases (e.g. design-time, claim query, executor implementation). New or unallowlisted bypass usage risks tenant isolation.

**What to do:**

1. **Prefer TenantScopeExecutor** — Use `TenantScopeExecutor.RunWithPlatformBypassAsync(work, ct)` so bypass is entered and exited in one place (with proper `finally`). Do not call `EnterPlatformBypass`/`ExitPlatformBypass` manually in runtime code unless the path is allowlisted.
2. **If bypass is justified** — Add the file to `tools/tenant_safety_ci_allowlist.json` under `manual_scope_allowed` with a clear reason. Document in architecture docs if it is a new pattern.
3. **Review allowlist** — Full list: `tools/tenant_safety_ci_allowlist.json`. Do not add entries to avoid fixing a real violation.

**Reference:** [TENANT_SAFETY_CI.md](TENANT_SAFETY_CI.md), [TENANT_SAFETY_HEALTH_DASHBOARD.md](TENANT_SAFETY_HEALTH_DASHBOARD.md) (Approved platform bypass section).

---

## 6. Where to see guard failures and health

| Source | What it shows |
|--------|----------------|
| **Logs** | Filter by category **PlatformGuardViolation**. Guard name, operation, and safe identifiers (CompanyId, EntityType, EntityId, EventId) are logged before throw. |
| **Operations overview API** | `GET /api/admin/operations/overview` — **GuardViolations** section: in-memory buffer of recent violations (total, by guard, recent list). Process restart clears the buffer. |
| **tenant_safety_health.json** | Manual scope violations (locations), executor adoption, sensitive files, guards/coverage, enforcement limitations. Refreshed by `./tools/architecture/regenerate_tenant_safety_artifacts.ps1`. |
| **TENANT_SAFETY_HEALTH_DASHBOARD.md** | Human-readable score, trend, deductions, guarded areas, operator response links, limitations. Refreshed by same command. |
| **platform_guardian_report.json** / **PLATFORM_GUARDIAN_REPORT.md** | Guardian scan: categories (tenant, finance, EventStore, workflow, bypass, artifact drift), findings (enforced vs advisory), limitations. Refreshed by same command. See [PLATFORM_GUARDIAN_REPORT.md](PLATFORM_GUARDIAN_REPORT.md). |
| **platform_safety_drift_report.json** / **PLATFORM_SAFETY_DRIFT_REPORT.md** | Drift monitor: compares current run to previous baseline; deltas for enforced/advisory/documented-only, new sensitive files, bypass growth. Does not fail CI. See [PLATFORM_SAFETY_DRIFT_REPORT.md](PLATFORM_SAFETY_DRIFT_REPORT.md). |

Finance and EventStore guard failures are **not** detected by CI; they are enforced at runtime. Observability is via logs, operations overview, and this guidance.
