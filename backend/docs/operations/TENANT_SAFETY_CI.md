# Tenant Safety CI — Scope, Executor, and Guard Checks

**Purpose:** Automatically block pull requests that violate CephasOps tenant-safety architecture so unsafe changes are caught even if a developer ignores Cursor rules.

**Workflow:** `.github/workflows/tenant-safety.yml`  
**Script:** `tools/tenant_scope_ci.ps1`  
**Allowlist:** `tools/tenant_safety_ci_allowlist.json`  
**Health dashboard:** [TENANT_SAFETY_HEALTH_DASHBOARD.md](TENANT_SAFETY_HEALTH_DASHBOARD.md) (generated; run `./tools/architecture/regenerate_tenant_safety_artifacts.ps1` to refresh dashboard, diagram, health JSON/history, and [PLATFORM_GUARDIAN_REPORT.md](PLATFORM_GUARDIAN_REPORT.md))  
**Autonomous sync:** `.github/workflows/tenant-safety-artifacts-sync.yml` optionally commits updated dashboard/diagram/history on push to main when those are the only changes.  
**Drift override:** [TENANT_SAFETY_DRIFT_LOG.md](TENANT_SAFETY_DRIFT_LOG.md) (only way to allow an intentional score drop in CI)  
**Architecture freeze:** [ARCHITECTURE_FREEZE.md](ARCHITECTURE_FREEZE.md) — changes to frozen safety-critical files (guards, executor, tenant-safety CI, core docs) fail unless [ARCHITECTURE_FREEZE_OVERRIDE.md](ARCHITECTURE_FREEZE_OVERRIDE.md) is updated in the same PR with a documented row.

---

## 1. What the CI checks

| Check | Rule | What it blocks |
|-------|------|----------------|
| **MANUAL_SCOPE** | No manual `TenantScope.CurrentTenantId` in runtime code | Direct read/write of current tenant outside the executor pattern. |
| **MANUAL_BYPASS_ENTER / EXIT** | No manual `EnterPlatformBypass()` / `ExitPlatformBypass()` in runtime code | Bypass must go through `TenantScopeExecutor.RunWithPlatformBypassAsync` so exit is always in `finally`. |
| **EXECUTOR_REQUIRED** | BackgroundService / IHostedService must use TenantScopeExecutor | New workers, schedulers, dispatchers, replay flows, and webhook runtimes must run work inside `RunWithTenantScopeAsync`, `RunWithPlatformBypassAsync`, or `RunWithTenantScopeOrBypassAsync`. |
| **GUARD_DOC_DRIFT** | Guard file changes require architecture doc update | When any of TenantSafetyGuard, SiWorkflowGuard, FinancialIsolationGuard, or EventStoreConsistencyGuard is modified, at least one of the architecture docs must be updated in the same PR/commit. |

Comment-only lines (e.g. `//`, `///`, `*`) are ignored for manual scope/bypass detection.

---

## 2. Rules enforced

- **Runtime code** under `backend/src` is scanned; `backend/tests` is not (tests may set scope for assertions).
- **Intentional exceptions** are listed in `tools/tenant_safety_ci_allowlist.json`:
  - **manual_scope_allowed:** Bootstrap/design-time (e.g. DatabaseSeeder, ApplicationDbContextFactory), the executor/guard implementation itself, API middleware (Program.cs), and a small set of approved runtime paths (e.g. AuthService, JobExecutionStore) with documented reasons.
  - **executor_not_required:** Hosted services that do not touch tenant data (e.g. heartbeat, metrics) or that delegate to a service that already uses TenantScopeExecutor.

Adding a new exception requires a clear **reason** in the allowlist. Do not add entries to avoid fixing a real violation.

---

## 3. Failure messages and what to do

- **MANUAL_SCOPE / MANUAL_BYPASS_***  
  **Use instead:** Prefer `TenantScopeExecutor.RunWithTenantScopeAsync(companyId, work, ct)`, `RunWithPlatformBypassAsync(work, ct)`, or `RunWithTenantScopeOrBypassAsync(...)` so scope and bypass are always set/restored in one place. If this is bootstrap or design-time (e.g. migrations factory), add the file to `manual_scope_allowed` with a short reason.

- **EXECUTOR_REQUIRED**  
  **Use instead:** Wrap the work executed by the hosted service/scheduler in one of the `TenantScopeExecutor` methods. If this component truly does not touch tenant data (e.g. platform-only metrics), add it to `executor_not_required` with a reason.

- **GUARD_DOC_DRIFT**  
  **Use instead:** In the same PR, update at least one of:
  - `backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md`
  - `backend/docs/architecture/TENANT_SCOPE_EXECUTOR_COMPLETION.md`
  - `backend/docs/architecture/SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md`  
  or add a PR comment explaining why no doc change is needed.

- **Architecture drift (score dropped)**  
  CI fails when the tenant safety score is lower than the previous run. **Override (intentional drift only):** In the same PR, add a **new table row** to [TENANT_SAFETY_DRIFT_LOG.md](TENANT_SAFETY_DRIFT_LOG.md) with: Date (YYYY-MM-DD), **Score exactly matching the drop CI reported** (e.g. if CI says "dropped from 100 to 95", use `100→95` or `100->95`—no other score pair is accepted), and a non-empty Reason/PR. CI validates that an added row contains that transition plus date and reason; modifying the file without a matching entry still fails. Prefer fixing the causes (manual scope, executor gap, docs) over using the override.

- **Architecture freeze (frozen file changed)**  
  CI fails when any [frozen safety-critical file](ARCHITECTURE_FREEZE.md#frozen-paths) (guards, executor, tenant-safety CI scripts, core safety docs) is modified. **Override:** In the same PR, add a **new table row** to [ARCHITECTURE_FREEZE_OVERRIDE.md](ARCHITECTURE_FREEZE_OVERRIDE.md) with Date (YYYY-MM-DD), Files/scope (short description), and Reason/PR (non-empty). CI validates that the override doc was changed and contains a valid new row. Prefer platform owner review for frozen-file changes.

For **what to do when any safeguard fails** (missing tenant context, finance guard, EventStore guard, artifact drift, bypass misuse), see [PLATFORM_SAFETY_OPERATOR_RESPONSE.md](PLATFORM_SAFETY_OPERATOR_RESPONSE.md).

---

## 4. Running locally

From the repository root:

```powershell
# All checks; guard-doc uses git diff against origin/main (or GITHUB_BASE_REF in CI)
./tools/tenant_scope_ci.ps1

# Skip guard-doc drift (e.g. when not in CI)
./tools/tenant_scope_ci.ps1 -SkipGuardDocCheck

# Pass explicit changed file list (e.g. for guard-doc only)
./tools/tenant_scope_ci.ps1 -ChangedFiles @("backend/src/.../TenantSafetyGuard.cs")
```

Exit code **0** = all checks passed; **1** = at least one violation.

---

## 5. Relation to other checks

- **Roslyn analyzers (CEPHAS001–CEPHAS004):** Same workflow builds the API with CEPHAS001 and CEPHAS004 as errors; they cover query filters and query-by-id-only.
- **tenant_safety_audit.ps1:** Same workflow runs this script for IgnoreQueryFilters and fixup-risk heuristics.
- **Tenant scope CI** adds enforcement for **manual scope**, **executor usage**, and **guard/doc drift** so the most dangerous runtime patterns are blocked before merge.

---

## 6. Extending the checks

- To add a new **allowlist entry:** Edit `tools/tenant_safety_ci_allowlist.json` and provide a short `reason`.
- To add a new **rule:** Extend `tools/tenant_scope_ci.ps1` with a new function (e.g. `Test-Something`) and call it from the “Run checks” section; keep failure messages clear and point to this doc or the architecture guides.

See also: `.cursor/rules/00_no_manual_scope.mdc`, `02_tenant_safety.mdc`, `03_backend_workers.mdc`, and `backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md`.

---

## 7. Platform Guardian (detection and reporting)

The **Platform Guardian** is a lightweight detection and reporting layer (not a runtime blocker). It scans the codebase and health artifacts, then produces:

- **Machine-readable:** `tools/architecture/platform_guardian_report.json` (categories, findings, enforced vs advisory counts, limitations).
- **Human-readable:** [PLATFORM_GUARDIAN_REPORT.md](PLATFORM_GUARDIAN_REPORT.md) (what is protected, advisory items, sensitive files to review).

**How to run:** From repo root, run `./tools/architecture/run_platform_guardian.ps1`. Use `-RegenerateHealth` to refresh health first. The regeneration flow `./tools/architecture/regenerate_tenant_safety_artifacts.ps1` runs the guardian after health generation.

**How to interpret findings:**

| Classification | Meaning | Action |
|----------------|---------|--------|
| **Enforced** | CI or runtime code already blocks or flags this. | Fix violation or add justified allowlist entry (same as CI failures above). |
| **Advisory** | Heuristic or scan result; not a hard guarantee. | Review the listed files/paths; confirm guard coverage or document exception. No CI block. |
| **Documented-only** | Safeguard is documented; guardian does not re-scan. | No action unless you change that area. |

**Blockers vs advisory:** Only **Enforced** findings correspond to things CI can fail on (manual scope, executor gap, bypass, doc/diagram drift). **Advisory** findings are for visibility and review; address them when touching the relevant code or during safety reviews.
