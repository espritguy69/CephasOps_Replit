# CephasOps Platform Standards

**Canonical reference** for engineering standards, architectural guardrails, and governance. For developers, reviewers, CI governance, contributors, and AI coding assistants.

---

## 1. Platform Overview

CephasOps is a **multi-tenant ISP operations platform** for work orders, service installers, inventory, billing, payroll, and P&L analytics. The platform is built with a **safety-first architecture**: tenant isolation is enforced at runtime and in CI, and cross-tenant access is never permitted. **Strong CI governance** and **automated architecture verification** (analyzers, invariant tests, health dashboard, drift detection) ensure that safety and standards are maintained as the codebase evolves.

---

## 2. Runtime Platform Standard

CephasOps is **strictly standardized** on:

| Item | Standard |
|------|----------|
| **.NET SDK** | 10.0.x |
| **Target framework** | net10.0 |

**Rules:**

- All application projects must target **net10.0**.
- All test projects must target **net10.0** (except where an exception is explicitly documented, e.g. analyzer library on netstandard2.0).
- CI workflows that build or test CephasOps code must use `dotnet-version: '10.0.x'`.
- Lower SDK or target framework versions must **not** be introduced unless explicitly documented as an exception.

**Enforcement:** The repository root `global.json` pins the SDK to 10.0.x. All backend and analyzer test projects declare `<TargetFramework>net10.0</TargetFramework>`.

---

## 3. Tenant Safety Architecture

The core safety model rests on four mechanisms:

- **TenantScope** — AsyncLocal holding the current tenant (company) context. API middleware and job workers set it; guards and persistence layer read it.
- **TenantSafetyGuard** — Final defensive guard: asserts tenant context before high-risk paths (e.g. `IgnoreQueryFilters`), and validates tenant context in `SaveChanges` for tenant-scoped entities. Platform-wide operations must use `EnterPlatformBypass` / `ExitPlatformBypass` so writes are intentional.
- **EF Core tenant query filters** — Global filters on tenant-scoped entities so queries are automatically scoped by `CompanyId` / tenant.
- **TenantScopeExecutor** — Central helper to run work under a tenant scope or platform bypass with guaranteed restore of previous state (no manual set/restore in finally).

**Principles:**

- **Cross-tenant access must never occur.** Reads and writes are always in a valid tenant context or under an explicit, controlled platform bypass.
- **Writes require either a valid tenant scope or a platform bypass.** `SaveChanges` fails closed when tenant-scoped entities are written without tenant context and no bypass is active.
- **Bypass must be explicit and controlled.** Use `TenantScopeExecutor.RunWithPlatformBypassAsync` (or paired `EnterPlatformBypass` / `ExitPlatformBypass`) only for intentional platform-wide operations (e.g. retention, seeding); never for tenant-owned work.

**Invariant tests** (see §7) protect these rules and run in CI.

See [TENANT_SAFETY_DEVELOPER_GUIDE.md](TENANT_SAFETY_DEVELOPER_GUIDE.md) for detailed patterns.

---

## 4. Executor and Background Safety

Background and orchestrated work must use the executor so tenant scope is never missing or left stale:

- **Tenant-owned orchestrators** (e.g. job worker processing a tenant’s job, event handler for a tenant’s event) must run work through **`TenantScopeExecutor.RunWithTenantScopeAsync(companyId, work)`**. Do not set `TenantScope.CurrentTenantId` manually and forget to restore it.
- **Platform tasks** (retention, reapers, scheduler enumeration across tenants) must use **`TenantScopeExecutor.RunWithPlatformBypassAsync(work)`** so bypass is entered and exited in a single place.

This prevents missing or incorrect tenant scope in background execution and ensures state is always restored (including on exception).

---

## 5. CI Governance System

Safety and architecture are enforced through multiple CI layers:

| Layer | Purpose |
|-------|---------|
| **Analyzer enforcement** | CEPHAS001 (IgnoreQueryFilters / tenant scope), CEPHAS004 (query by Id only), etc. API build uses these as errors. |
| **Tenant-safety workflow** | Runs on PR/push: analyzer tests, API build with analyzers as errors, tenant safety audit script, tenant scope CI, health dashboard generation, drift check, dashboard/JSON commit check. |
| **Architecture invariant tests** | `TenantSafetyInvariantTests` verify guard behavior, SaveChanges protection, executor safety, bypass exit. Run in the tenant-safety workflow. |
| **Tenant safety health dashboard** | Generated from codebase and allowlist; produces safety score, trend, manual scope violations, executor adoption, allowlist, sensitive files changed. |
| **Drift detection** | CI fails if the safety score drops compared to the previous run. |
| **Drift log override** | A drop is allowed only if a valid row is added to the drift log (see §6). |
| **Architecture freeze** | Changes to [frozen safety-critical files](../operations/ARCHITECTURE_FREEZE.md) fail CI unless [ARCHITECTURE_FREEZE_OVERRIDE.md](../operations/ARCHITECTURE_FREEZE_OVERRIDE.md) is updated in the same PR with a valid new row (date, scope, reason). |
| **PR comment reporting** | A dedicated reporting job posts or updates a single PR comment with tenant safety health (or a short failure message if the main job failed early). |

Architecture and safety regressions are **blocked by CI**; the workflow and reporting job are the single source of truth for “did this PR pass tenant safety?”

See [TENANT_SAFETY_CI.md](../operations/TENANT_SAFETY_CI.md) and [TENANT_SAFETY_ANALYZER.md](../operations/TENANT_SAFETY_ANALYZER.md).

---

## 6. Architecture Drift Governance

When the tenant safety **score drops** (e.g. new violations or reduced executor adoption), CI fails unless an override is documented.

- **Drift log:** [TENANT_SAFETY_DRIFT_LOG.md](../operations/TENANT_SAFETY_DRIFT_LOG.md)
- **Rules:**
  - CI fails if the safety score drops and no override is present.
  - Override is allowed only by adding a **documented row** to the drift log in the same PR.
  - The row must include: **date**, **exact score transition** (e.g. `90→85`), and **reason** (non-empty, meaningful).
- **CI validation:** The tenant-safety workflow checks that the drift log was changed and that an added row matches the actual score drop (date pattern, score transition, reason). If the file is changed but no valid row is found, the workflow fails.

This keeps every intentional score drop visible and auditable.

---

## 7. Safety Invariant Tests

**`TenantSafetyInvariantTests`** (in `backend/tests/CephasOps.Application.Tests/Architecture/`) are a small, high-signal suite that verifies:

- **Tenant scope enforcement** — `AssertTenantContext` throws when there is no tenant and no bypass; does not throw when tenant is set or bypass is active.
- **SaveChanges protection** — Saving a tenant-scoped entity without tenant context throws; with tenant context it succeeds; after bypass exits, SaveChanges without scope still fails.
- **Executor safety** — `RunWithTenantScopeAsync` rejects empty company ID; scope and bypass are restored after success and after exception.
- **Bypass exit behavior** — After `RunWithPlatformBypassAsync` completes, bypass is exited so subsequent SaveChanges without scope is rejected.

These tests run **automatically in CI** as part of the tenant-safety workflow (step: “Run tenant safety invariant tests”). Do not disable or remove them without platform owner approval.

---

## 8. Code Ownership

**.github/CODEOWNERS** protects safety-critical paths. Changes to the following require review from **platform owners** (e.g. `@cephasops/platform-safety` or the configured team):

- **Runtime guards and executor:** TenantSafetyGuard, TenantScopeExecutor, EventStoreConsistencyGuard, FinancialIsolationGuard, SiWorkflowGuard
- **Tenant-safety CI and health tooling:** tenant_safety_audit.ps1, tenant_scope_ci.ps1, tenant_safety_ci_allowlist.json, generate_tenant_safety_health.ps1, regenerate_tenant_safety_artifacts.ps1, run_platform_guardian.ps1, generate_tenant_safety_diagram.ps1
- **Workflow:** .github/workflows/tenant-safety.yml
- **Core tenant-safety documentation:** TENANT_SAFETY_CI.md, TENANT_SAFETY_DRIFT_LOG.md, TENANT_SAFETY_HEALTH_DASHBOARD.md, TENANT_SAFETY_ANALYZER.md, PLATFORM_SAFETY_OPERATOR_RESPONSE.md, TENANT_SAFETY_DEVELOPER_GUIDE.md, and related architecture/operations safety docs

Do not modify these guardrails or scripts without the required review.

---

## 9. Developer Rules

Developers **must** follow these rules:

1. **Never bypass tenant safety intentionally** without using the approved executor or bypass APIs and without documentation where required.
2. **Never introduce a project targeting a lower .NET version** (e.g. net8.0, net9.0); all application and test projects target net10.0 unless an exception is documented.
3. **Use TenantScopeExecutor** for tenant-owned background tasks (jobs, event handlers, webhooks) so scope is set and restored correctly.
4. **Do not disable analyzer warnings** (CEPHAS001, CEPHAS004, etc.) for tenant-safety rules; fix the code or add a justified allowlist entry where the pattern is documented.
5. **Do not modify tenant safety guardrails** (guards, executor, CI scripts, health generation, core safety docs) without platform owner review as required by CODEOWNERS.
6. **Do not add a drift log row** without the exact score transition and a real reason; CI validates the row.

---

## 10. Operational Visibility

Platform safety status is visible in:

- **Tenant Safety Health Dashboard** — [TENANT_SAFETY_HEALTH_DASHBOARD.md](../operations/TENANT_SAFETY_HEALTH_DASHBOARD.md) (generated); also in `tools/architecture/tenant_safety_health.json`.
- **PR comments** — The tenant-safety reporting job posts or updates a single comment per PR with status, score, trend, violations, executor adoption, allowlist, sensitive files changed, and whether a drift override was used (or a short message if the workflow failed before the dashboard was generated).
- **CI logs** — The tenant-safety workflow and reporting job logs; the “Tenant Safety Health (PR summary)” step echoes a summary to the workflow log.

**Investigating failures:** Use the failed step name and log in the Actions run. For analyzer failures, fix the reported location or follow the analyzer docs. For drift failures, add a valid drift log row or fix the violations. For invariant test failures, fix the guard/executor usage; do not relax the invariant.

See [PLATFORM_SAFETY_OPERATOR_RESPONSE.md](../operations/PLATFORM_SAFETY_OPERATOR_RESPONSE.md) for operator response playbooks.

---

## 11. Change Governance

Changes that affect **tenant safety**, **platform runtime standards** (e.g. .NET version, target framework), or **architecture enforcement** (analyzers, invariant tests, drift rules, CODEOWNERS) must be:

- **Reviewed carefully** — Prefer platform owner or designated reviewer for safety-critical paths.
- **Documented** — Update this document, TENANT_SAFETY_CI.md, or the relevant operations/architecture doc when the standard or process changes.
- **Reflected in CI** — Ensure workflows, analyzers, and tests still enforce the intended rules; do not weaken guardrails without explicit approval and documentation.

This document is the **canonical reference** for platform standards; keep it aligned with actual CI behavior and codebase rules.
