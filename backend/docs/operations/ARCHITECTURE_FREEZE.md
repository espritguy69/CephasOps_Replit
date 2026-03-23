# Architecture Freeze

**Purpose:** Prevent accidental changes to safety-critical platform files. Any change to a *frozen* file causes CI to **fail** unless the same PR updates the [override log](ARCHITECTURE_FREEZE_OVERRIDE.md) with a documented justification.

---

## Why freeze?

- **TenantSafetyGuard**, **TenantScope**, **TenantScopeExecutor**, and related guards are the core of tenant isolation. A single mistaken edit can weaken or bypass safety.
- **Tenant-safety CI** (workflow, scripts, allowlist) and **core safety docs** define how the platform is governed. Unintended edits can hide violations or change rules without review.

The freeze ensures that every touch to these paths is **explicit and auditable**: the PR must add a row to the override log (date, scope, reason).

---

## Frozen paths

The following paths are frozen. Changing any of them in a PR will fail CI unless [ARCHITECTURE_FREEZE_OVERRIDE.md](ARCHITECTURE_FREEZE_OVERRIDE.md) is updated in the same PR with a valid new table row.

### Runtime guards and executor

| Path |
|------|
| `backend/src/CephasOps.Infrastructure/Persistence/TenantSafetyGuard.cs` |
| `backend/src/CephasOps.Infrastructure/Persistence/TenantScope.cs` |
| `backend/src/CephasOps.Infrastructure/Persistence/TenantScopeExecutor.cs` |
| `backend/src/CephasOps.Application/Workflow/SiWorkflowGuard.cs` |
| `backend/src/CephasOps.Application/Common/FinancialIsolationGuard.cs` |
| `backend/src/CephasOps.Infrastructure/Persistence/EventStoreConsistencyGuard.cs` |

### Tenant-safety CI and tooling

| Path |
|------|
| `.github/workflows/tenant-safety.yml` |
| `tools/tenant_safety_audit.ps1` |
| `tools/tenant_scope_ci.ps1` |
| `tools/tenant_safety_ci_allowlist.json` |
| `tools/architecture/generate_tenant_safety_health.ps1` |
| `tools/architecture/regenerate_tenant_safety_artifacts.ps1` |
| `tools/architecture/run_platform_guardian.ps1` |
| `tools/architecture/generate_tenant_safety_diagram.ps1` |

### Core tenant-safety and platform standards docs

| Path |
|------|
| `backend/docs/operations/TENANT_SAFETY_CI.md` |
| `backend/docs/operations/TENANT_SAFETY_DRIFT_LOG.md` |
| `backend/docs/operations/TENANT_SAFETY_HEALTH_DASHBOARD.md` |
| `backend/docs/operations/TENANT_SAFETY_ANALYZER.md` |
| `backend/docs/operations/PLATFORM_SAFETY_OPERATOR_RESPONSE.md` |
| `backend/docs/operations/PLATFORM_SAFETY_HARDENING_INDEX.md` |
| `backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md` |
| `backend/docs/architecture/SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md` |
| `backend/docs/architecture/CEPHASOPS_PLATFORM_STANDARDS.md` |

The list is also defined in **`tools/architecture/check_architecture_freeze.ps1`** (single source of truth for CI). To add or remove frozen paths, update that script and this doc.

---

## How to document an intentional override

1. In the **same PR** where you modified a frozen file, open [ARCHITECTURE_FREEZE_OVERRIDE.md](ARCHITECTURE_FREEZE_OVERRIDE.md).
2. **Add one row** to the **Entries** table: **Date** (YYYY-MM-DD), **Files / scope** (short description of what changed), **Reason / PR** (non-empty justification or PR link).
3. Commit. CI will pass only if it finds this new row (date, scope, and reason all non-placeholder).
4. Prefer **platform owner review** for frozen-file changes (see [.github/CODEOWNERS](../../../.github/CODEOWNERS) and [CEPHASOPS_PLATFORM_STANDARDS.md](../architecture/CEPHASOPS_PLATFORM_STANDARDS.md)).

---

## CI behavior

- **Step:** "Architecture freeze check" in the tenant-safety workflow.
- **Logic:** `tools/architecture/check_architecture_freeze.ps1` receives the list of changed files (from `git diff`). If any changed file is in the frozen set, the script fails unless the override doc is also changed and contains at least one **added** table row with a date, files/scope, and reason.
- **Failure message:** Points to this doc and the override doc and lists which frozen files changed.

---

## Related

- [ARCHITECTURE_FREEZE_OVERRIDE.md](ARCHITECTURE_FREEZE_OVERRIDE.md) — override log (add a row when changing a frozen file).
- [TENANT_SAFETY_CI.md](TENANT_SAFETY_CI.md) — tenant-safety workflow and checks.
- [CEPHASOPS_PLATFORM_STANDARDS.md](../architecture/CEPHASOPS_PLATFORM_STANDARDS.md) — platform standards and code ownership.
