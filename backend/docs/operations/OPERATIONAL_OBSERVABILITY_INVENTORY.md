# Operational Observability — Inventory

**Purpose:** What is already surfaced, what is missing, and what is ambiguous for tenant-safety and platform-integrity observability. Use this to interpret dashboards, JSON, and CI output.

---

## 1. What is already surfaced

| Output | Content |
|--------|---------|
| **tenant_safety_health.json** | Safety score, trend, deductions (manual scope, executor gap, allowlist increase, docs, sensitive files). Manual scope violation locations (file, line, kind). Executor adoption (with/allowlisted/total, gap files). Sensitive safety files (tracked, changed in branch). Documentation and test health. **Guards:** FinancialIsolation and EventStoreConsistency coverage (documented report path, coverage summary, enforcement=runtime). **approvedPlatformBypass:** summary, allowlist path, note. **enforcementLimitations:** list of what is runtime-only or advisory. |
| **TENANT_SAFETY_HEALTH_DASHBOARD.md** | Human-readable score, trend, status, score breakdown, health indicators, sensitive files table, primary docs, allowlist summary, autonomous remediation hints, "What to do if this changes", **Guards and coverage** (table + links), **Approved platform bypass**, **When a safeguard fails** (link to operator response), **Limitations (enforced vs advisory)**. |
| **tenant_safety_history.json** | Last 50 runs: date, score, manualScope count, executorCoverage, allowlistTotal. Used for trend and drift. |
| **CI (tenant-safety workflow)** | Analyzer tests, API build (CEPHAS001+CEPHAS004), tenant_safety_audit.ps1, tenant_scope_ci.ps1, health generation, drift check (score vs previous), stale-artifact check (dashboard/JSON/history committed). PR summary: status, score, trend, manual scope violations, executor adoption, allowlist, sensitive files changed. |
| **Platform Guardian** | **platform_guardian_report.json** and **PLATFORM_GUARDIAN_REPORT.md**: scan of tenant, finance, EventStore, workflow, bypass, and artifact-drift patterns; findings classified as Enforced vs Advisory; limitations and sensitive files for review. Refreshed by `./tools/architecture/regenerate_tenant_safety_artifacts.ps1`. See [PLATFORM_GUARDIAN_REPORT.md](PLATFORM_GUARDIAN_REPORT.md), [TENANT_SAFETY_CI.md](TENANT_SAFETY_CI.md) §7. |
| **Platform Safety Drift** | **platform_safety_drift_report.json** and **PLATFORM_SAFETY_DRIFT_REPORT.md**: comparison of current guardian/health to previous baseline; enforced/advisory/documented-only deltas, new sensitive files, bypass growth. Does not fail CI; advisory drift is informational. Baseline: `platform_guardian_baseline.json`. See [PLATFORM_SAFETY_DRIFT_REPORT.md](PLATFORM_SAFETY_DRIFT_REPORT.md). |
| **Operations overview API** | Job executions, event store (24h), payout health, system health, **GuardViolations** (in-memory: total, by guard, recent list). |
| **Logs** | Category **PlatformGuardViolation**: guard name, operation, safe identifiers (CompanyId, EntityType, EntityId, EventId) when a guard is about to throw. |

---

## 2. What is missing or not auto-detected

| Gap | Why |
|-----|-----|
| **Finance guard failures in CI** | FinancialIsolationGuard runs at runtime only. CI does not execute finance paths; no static check for "caller passed wrong company". |
| **EventStore guard failures in CI** | EventStoreConsistencyGuard runs at runtime only. Analyzer does not check EventStore append call sites for tenant/bypass. |
| **Persisted guard violation counts** | No table of guard-trigger counts. Overview buffer is in-memory (cleared on restart). For history, use logs. |
| **Which code paths use platform bypass** | Allowlist lists paths; CI does not report "bypass used here" at runtime. Bypass usage is by design in allowlisted paths. |
| **EventStore replay/repair scope** | Replay and rebuild runners use TenantScopeExecutor; scope is not verified by analyzer. EventStore report documents behavior. |

---

## 3. What can be ambiguous for operators

| Topic | Clarification |
|-------|----------------|
| **Score drop vs stale artifacts** | **Score drop:** CI compares current score to previous run; fix violations or document in TENANT_SAFETY_DRIFT_LOG. **Stale artifacts:** Dashboard, JSON, history, or Platform Guardian report not regenerated after changes; run `./tools/architecture/regenerate_tenant_safety_artifacts.ps1` and commit the updated files. |
| **Enforced vs advisory** | **Enforced in CI:** Manual scope (unallowlisted), executor adoption, guard/doc drift. **Runtime-only:** Finance and EventStore guard failures (guard throws; no CI check). **Advisory:** Allowlist growth (prefer refactoring); some EventStore/repair scope (docs and code review). See dashboard "Limitations" section. |
| **Guard violations in overview** | In-memory buffer; process restart clears. For durable history use logs (PlatformGuardViolation). |
| **"Sensitive files changed"** | Means one of the five tracked guard/executor files was in the PR diff; update architecture docs in same PR (GUARD_DOC_DRIFT). |

---

## 4. Where finance / EventStore guard failures are visible

| Guard | Visible at runtime | Visible in CI | Visible in dashboard/JSON |
|-------|--------------------|---------------|----------------------------|
| **FinancialIsolationGuard** | Yes (exception + LogWarning, operations overview buffer) | No | Documented coverage and report link; no failure count |
| **EventStoreConsistencyGuard** | Yes (exception + LogWarning, operations overview buffer) | No | Documented coverage and report link; no failure count |

Both guards are **runtime-enforced**; observability is via logs, operations overview, and [PLATFORM_SAFETY_OPERATOR_RESPONSE.md](PLATFORM_SAFETY_OPERATOR_RESPONSE.md).
